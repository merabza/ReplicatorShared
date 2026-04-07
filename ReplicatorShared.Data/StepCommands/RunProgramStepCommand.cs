using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.StepCommands;

public sealed class RunProgramStepCommand : ProcessesToolAction
{
    private readonly ILogger _logger;
    private readonly RunProgramStepParameters _par;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RunProgramStepCommand(ILogger logger, bool useConsole, int procLineId, RunProgramStepParameters par) : base(
        logger, null, null, null, "Run Program", procLineId)
    {
        _logger = logger;
        _useConsole = useConsole;
        _par = par;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        StShared.RunProcessWithOutput(_useConsole, _logger, _par.Program, _par.Arguments);
        return ValueTask.FromResult(true);
    }
}
