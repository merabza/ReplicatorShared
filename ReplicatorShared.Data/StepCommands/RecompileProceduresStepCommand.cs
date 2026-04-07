using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.StepParameters;
using ReplicatorShared.Data.Steps;
using ReplicatorShared.Data.ToolActions;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement;

namespace ReplicatorShared.Data.StepCommands;

public sealed class RecompileProceduresStepCommand : MultiDatabaseProcessesToolAction
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public RecompileProceduresStepCommand(ILogger logger, bool useConsole, string procLogFilesFolder,
        ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, int procLineId) : base(logger, useConsole, procLogFilesFolder,
        "RecompileProcedures", processManager, multiDatabaseProcessStep, par, "Recompile Procedures", procLineId)
    {
    }

    protected override async Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        Option<Error[]> recompileProceduresResult =
            await agentClient.RecompileProcedures(databaseName, cancellationToken);
        if (!recompileProceduresResult.IsSome)
        {
            return true;
        }

        Error.PrintErrorsOnConsole((Error[])recompileProceduresResult);
        return false;
    }
}
