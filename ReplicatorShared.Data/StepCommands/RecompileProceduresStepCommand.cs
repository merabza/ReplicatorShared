using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using ReplicatorShared.Data.StepParameters;
using ReplicatorShared.Data.Steps;
using ReplicatorShared.Data.ToolActions;
using SystemTools.BackgroundTasks;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement;

namespace ReplicatorShared.Data.StepCommands;

public sealed class RecompileProceduresStepCommand : MultiDatabaseProcessesToolAction
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public RecompileProceduresStepCommand(ILogger logger, bool useConsole, string procLogFilesFolder,
        ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, int procLineId, ResiliencePipeline<bool>? retryPipeline = null) : base(
        logger, useConsole, procLogFilesFolder, "RecompileProcedures", processManager, multiDatabaseProcessStep, par,
        "Recompile Procedures", procLineId, retryPipeline)
    {
    }

    protected override async Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        Result recompileProceduresResult =
            await agentClient.RecompileProcedures(databaseName, cancellationToken);
        if (recompileProceduresResult.IsSuccess)
        {
            return true;
        }

        recompileProceduresResult.Error.PrintErrorsOnConsole();
        return false;
    }
}
