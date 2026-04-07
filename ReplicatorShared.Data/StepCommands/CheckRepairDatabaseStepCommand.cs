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

public sealed class CheckRepairDatabaseStepCommand : MultiDatabaseProcessesToolAction
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckRepairDatabaseStepCommand(ILogger logger, bool useConsole, string procLogFilesFolder,
        ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, int procLineId) : base(logger, useConsole, procLogFilesFolder,
        "CheckRepairDataBase", processManager, multiDatabaseProcessStep, par, "Check Repair DataBase", procLineId)
    {
    }

    protected override async Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        Option<Error[]> checkRepairDatabaseResult =
            await agentClient.CheckRepairDatabase(databaseName, cancellationToken);
        if (!checkRepairDatabaseResult.IsSome)
        {
            return true;
        }

        Error.PrintErrorsOnConsole((Error[])checkRepairDatabaseResult);
        return false;
    }
}
