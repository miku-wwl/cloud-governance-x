using FinOps.Application.Cloud;
using FinOps.Application.Tenancy;
using FinOps.Worker;
using FinOps.Worker.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinOps.Tests.Worker;

public sealed class WorkerJobTests
{
    [Fact]
    public async Task Resource_handler_executes_resource_sync()
    {
        var syncService = new StubResourceSyncService();
        var handler = new ResourceSyncJobHandler(
            syncService,
            NullLogger<FinOps.Worker.Worker>.Instance);

        await handler.ExecuteAsync(CancellationToken.None);

        Assert.True(syncService.WasCalled);
    }

    [Fact]
    public async Task Cost_handler_uses_configured_cost_days()
    {
        var syncService = new StubCostSyncService();
        var handler = new CostSyncJobHandler(
            syncService,
            Options.Create(new EtlWorkerOptions
            {
                Job = "Costs",
                CostDays = 31
            }),
            NullLogger<FinOps.Worker.Worker>.Instance);

        await handler.ExecuteAsync(CancellationToken.None);

        Assert.Equal(31, syncService.RequestedDays);
    }

    [Fact]
    public async Task Dispatcher_selects_jobs_case_insensitively()
    {
        var resources = new StubJobHandler("Resources");
        var costs = new StubJobHandler("Costs");
        var dispatcher = new WorkerJobDispatcher([resources, costs]);

        await dispatcher.DispatchAsync("resources", CancellationToken.None);
        await dispatcher.DispatchAsync("COSTS", CancellationToken.None);

        Assert.Equal(1, resources.ExecutionCount);
        Assert.Equal(1, costs.ExecutionCount);
    }

    [Fact]
    public async Task Dispatcher_rejects_unknown_job()
    {
        var dispatcher = new WorkerJobDispatcher(
            [new StubJobHandler("Resources"), new StubJobHandler("Costs")]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync("Unknown", CancellationToken.None));

        Assert.Contains("Unsupported ETL job 'Unknown'", exception.Message);
        Assert.Contains("Costs", exception.Message);
        Assert.Contains("Resources", exception.Message);
    }

    [Fact]
    public void Dispatcher_rejects_duplicate_job_names()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new WorkerJobDispatcher(
            [
                new StubJobHandler("Resources"),
                new StubJobHandler("resources")
            ]));

        Assert.Contains("Multiple Worker job handlers", exception.Message);
    }

    [Fact]
    public async Task Worker_execution_initializes_explicit_background_tenant()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        var handler = new TenantCapturingJobHandler(tenantContext);
        var execution = new WorkerExecution(
            new WorkerJobDispatcher([handler]),
            (ITenantContextInitializer)tenantContext,
            new StubTenantResolver(isActiveTenant: true));

        await execution.ExecuteAsync(
            new WorkerJobRequest(handler.Name, tenantId),
            CancellationToken.None);

        Assert.Equal(tenantId, handler.CapturedTenantId);
        Assert.Equal(
            TenantContextSource.BackgroundJob,
            tenantContext.RequireCurrent().Source);
    }

    [Fact]
    public async Task Worker_execution_rejects_missing_tenant_before_dispatch()
    {
        var handler = new StubJobHandler("Resources");
        var execution = new WorkerExecution(
            new WorkerJobDispatcher([handler]),
            (ITenantContextInitializer)new TenantContext(),
            new StubTenantResolver(isActiveTenant: true));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            execution.ExecuteAsync(
                new WorkerJobRequest("Resources", Guid.Empty),
                CancellationToken.None));

        Assert.Equal(0, handler.ExecutionCount);
    }

    [Fact]
    public async Task Worker_execution_rejects_unknown_or_inactive_tenant()
    {
        var handler = new StubJobHandler("Resources");
        var tenantContext = new TenantContext();
        var execution = new WorkerExecution(
            new WorkerJobDispatcher([handler]),
            (ITenantContextInitializer)tenantContext,
            new StubTenantResolver(isActiveTenant: false));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            execution.ExecuteAsync(
                new WorkerJobRequest("Resources", Guid.NewGuid()),
                CancellationToken.None));

        Assert.Contains("does not exist or is not active", exception.Message);
        Assert.Null(tenantContext.Current);
        Assert.Equal(0, handler.ExecutionCount);
    }

    [Fact]
    public async Task Worker_cancellation_stops_host_without_failure_exit_code()
    {
        var exitCode = new StubProcessExitCode();
        var lifetime = new StubHostApplicationLifetime();
        var execution = new CancellableWorkerExecution();
        using var provider = CreateWorkerProvider(execution);
        var worker = new FinOps.Worker.Worker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            lifetime,
            Options.Create(new EtlWorkerOptions { Job = "Resources" }),
            exitCode,
            NullLogger<FinOps.Worker.Worker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await execution.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
        await lifetime.StopRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, exitCode.Value);
    }

    [Fact]
    public async Task Worker_failure_sets_nonzero_exit_code_and_stops_host()
    {
        var exitCode = new StubProcessExitCode();
        var lifetime = new StubHostApplicationLifetime();
        using var provider = CreateWorkerProvider(
            new StubWorkerExecution((_, _) =>
                Task.FromException(new InvalidOperationException("forced failure"))));
        var worker = new FinOps.Worker.Worker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            lifetime,
            Options.Create(new EtlWorkerOptions { Job = "Costs" }),
            exitCode,
            NullLogger<FinOps.Worker.Worker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await lifetime.StopRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, exitCode.Value);
    }

    private static ServiceProvider CreateWorkerProvider(IWorkerExecution execution)
    {
        return new ServiceCollection()
            .AddScoped<IWorkerExecution>(_ => execution)
            .BuildServiceProvider();
    }

    private sealed class StubResourceSyncService : ICloudResourceSyncService
    {
        public bool WasCalled { get; private set; }

        public Task<CloudResourceSyncResult> SyncAsync(
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new CloudResourceSyncResult(Guid.Empty, 1, 1, 0));
        }
    }

    private sealed class StubCostSyncService : ICloudCostSyncService
    {
        public int RequestedDays { get; private set; }

        public Task<CloudCostSyncResult> SyncRecentAsync(
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            RequestedDays = days;
            return Task.FromResult(new CloudCostSyncResult(
                Guid.Empty,
                DateOnly.MinValue,
                DateOnly.MinValue,
                1,
                1,
                0,
                UsedSampleData: false));
        }
    }

    private sealed class StubJobHandler(string name) : IWorkerJobHandler
    {
        public string Name => name;

        public int ExecutionCount { get; private set; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubWorkerExecution(
        Func<WorkerJobRequest, CancellationToken, Task> execute) : IWorkerExecution
    {
        public Task ExecuteAsync(
            WorkerJobRequest request,
            CancellationToken cancellationToken) =>
            execute(request, cancellationToken);
    }

    private sealed class CancellableWorkerExecution : IWorkerExecution
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ExecuteAsync(
            WorkerJobRequest request,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class TenantCapturingJobHandler(ITenantContext tenantContext) :
        IWorkerJobHandler
    {
        public string Name => "Resources";

        public Guid? CapturedTenantId { get; private set; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            CapturedTenantId = tenantContext.RequireCurrent().TenantId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTenantResolver(bool isActiveTenant) :
        ITenantMembershipResolver
    {
        public Task<bool> IsActiveTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(isActiveTenant);

        public Task<bool> HasActiveMembershipAsync(
            Guid tenantId,
            string issuer,
            string subject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StubProcessExitCode : IProcessExitCode
    {
        public int Value { get; set; }
    }

    private sealed class StubHostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource started = new();
        private readonly CancellationTokenSource stopping = new();
        private readonly CancellationTokenSource stopped = new();

        public TaskCompletionSource StopRequested { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken ApplicationStarted => started.Token;

        public CancellationToken ApplicationStopping => stopping.Token;

        public CancellationToken ApplicationStopped => stopped.Token;

        public void StopApplication()
        {
            stopping.Cancel();
            StopRequested.TrySetResult();
            stopped.Cancel();
        }
    }
}
