using System.Net;
using System.Net.Sockets;
using FinOps.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Tests.Infrastructure;

public sealed class PostgreSqlTcpHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenEndpointIsReachable()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            var check = new PostgreSqlTcpHealthCheck(new PostgreSqlHealthCheckOptions
            {
                Host = IPAddress.Loopback.ToString(),
                Port = endpoint.Port,
                TimeoutSeconds = 1
            });

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
        finally
        {
            listener.Stop();
        }
    }
}
