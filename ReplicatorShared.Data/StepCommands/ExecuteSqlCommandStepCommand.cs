using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.StepParameters;
using ReplicatorShared.Data.Steps;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared.Errors;

namespace ReplicatorShared.Data.StepCommands;

public sealed class ExecuteSqlCommandStepCommand : ProcessesToolAction
{
    private readonly ExecuteSqlCommandStep _executeSqlCommandStep;

    private readonly ExecuteSqlCommandStepParameters _par;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ExecuteSqlCommandStepCommand(ILogger logger, ProcessManager processManager,
        ExecuteSqlCommandStep executeSqlCommandStep, ExecuteSqlCommandStepParameters par) : base(logger, null, null,
        processManager, "Execute Sql Command", executeSqlCommandStep.ProcLineId)
    {
        _executeSqlCommandStep = executeSqlCommandStep;
        _par = par;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        Option<Error[]> executeCommandResult = await _par.AgentClient.ExecuteCommand(_par.ExecuteQueryCommand,
            _executeSqlCommandStep.DatabaseName, cancellationToken);
        if (executeCommandResult.IsSome)
        {
            Error.PrintErrorsOnConsole((Error[])executeCommandResult);
        }

        return true;
    }
}
