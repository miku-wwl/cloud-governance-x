namespace FinOps.Worker;

internal interface IProcessExitCode
{
    int Value { get; set; }
}

internal sealed class ProcessExitCode : IProcessExitCode
{
    public int Value
    {
        get => Environment.ExitCode;
        set => Environment.ExitCode = value;
    }
}
