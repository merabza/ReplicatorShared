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

public sealed class UpdateStatisticsStepCommand : MultiDatabaseProcessesToolAction
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateStatisticsStepCommand(ILogger logger, bool useConsole, string procLogFilesFolder,
        ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, int procLineId) : base(logger, useConsole, procLogFilesFolder,
        "UpdateStatistics", processManager, multiDatabaseProcessStep, par, "Update Statistics", procLineId)
    {
    }

    protected override async Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        Option<Error[]> updateStatisticsResult = await agentClient.UpdateStatistics(databaseName, cancellationToken);
        if (!updateStatisticsResult.IsSome)
        {
            return true;
        }

        Error.PrintErrorsOnConsole((Error[])updateStatisticsResult);
        return false;
    }
}
