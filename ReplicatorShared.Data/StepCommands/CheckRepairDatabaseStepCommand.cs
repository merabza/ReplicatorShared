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

public sealed class CheckRepairDatabaseStepCommand : MultiDatabaseProcessesToolAction
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckRepairDatabaseStepCommand(ILogger logger, bool useConsole, string procLogFilesFolder,
        ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, int procLineId, ResiliencePipeline<bool>? retryPipeline = null) : base(
        logger, useConsole, procLogFilesFolder, "CheckRepairDataBase", processManager, multiDatabaseProcessStep, par,
        "Check Repair DataBase", procLineId, retryPipeline)
    {
    }

    protected override async Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        Result checkRepairDatabaseResult = await agentClient.CheckRepairDatabase(databaseName, cancellationToken);
        if (checkRepairDatabaseResult.IsSuccess)
        {
            return true;
        }

        checkRepairDatabaseResult.Error.PrintErrorsOnConsole();
        return false;
    }
}
