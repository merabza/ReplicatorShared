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

public sealed class UpdateStatisticsStepCommand : MultiDatabaseProcessesToolAction
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateStatisticsStepCommand(ILogger logger, bool useConsole, string procLogFilesFolder,
        ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, int procLineId, ResiliencePipeline<bool>? retryPipeline = null) : base(
        logger, useConsole, procLogFilesFolder, "UpdateStatistics", processManager, multiDatabaseProcessStep, par,
        "Update Statistics", procLineId, retryPipeline)
    {
    }

    protected override async Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        Result updateStatisticsResult = await agentClient.UpdateStatistics(databaseName, cancellationToken);
        if (updateStatisticsResult.IsSuccess)
        {
            return true;
        }

        updateStatisticsResult.Error.PrintErrorsOnConsole();
        return false;
    }
}
