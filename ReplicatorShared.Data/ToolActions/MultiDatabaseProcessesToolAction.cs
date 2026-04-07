using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools.Models;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepParameters;
using ReplicatorShared.Data.Steps;
using SystemTools.BackgroundTasks;
using ToolsManagement.DatabasesManagement;

namespace ReplicatorShared.Data.ToolActions;

public /*open*/ class MultiDatabaseProcessesToolAction : ProcessesToolAction
{
    private readonly ILogger _logger;
    private readonly MultiDatabaseProcessStep _multiDatabaseProcessStep;
    private readonly MultiDatabaseProcessStepParameters _par;
    private readonly string _procLogFilesFolder;
    private readonly string _stepName;
    private readonly bool _useConsole;

    protected MultiDatabaseProcessesToolAction(ILogger logger, bool useConsole, string procLogFilesFolder,
        string stepName, ProcessManager processManager, MultiDatabaseProcessStep multiDatabaseProcessStep,
        MultiDatabaseProcessStepParameters par, string actionName, int procLineId) : base(logger, null, null,
        processManager, actionName, procLineId)
    {
        _logger = logger;
        _useConsole = useConsole;
        _procLogFilesFolder = procLogFilesFolder;
        _stepName = stepName;
        _multiDatabaseProcessStep = multiDatabaseProcessStep;
        _par = par;
    }

    private async ValueTask<List<string>> GetDatabaseNames(CancellationToken cancellationToken = default)
    {
        List<string> databaseNames;
        if (_multiDatabaseProcessStep.DatabaseSet == EDatabaseSet.DatabasesBySelection)
        {
            databaseNames = _multiDatabaseProcessStep.DatabaseNames;
        }
        else
        {
            var databasesListCreator =
                new DatabasesListCreator(_multiDatabaseProcessStep.DatabaseSet, _par.AgentClient);
            List<DatabaseInfoModel> dbList = await databasesListCreator.LoadDatabaseNames(cancellationToken);
            databaseNames = dbList.Select(s => s.Name).ToList();
        }

        return databaseNames;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_procLogFilesFolder))
        {
            _logger.LogError("Process log files Folder not specified");
            return false;
        }

        List<string> databaseNames = await GetDatabaseNames(cancellationToken);
        bool all = true;
        foreach (string databaseName in databaseNames)
        {
            var procLogFile = new ProcLogFile(_useConsole, _logger, $"{_stepName}_{databaseName}_",
                _multiDatabaseProcessStep.PeriodType, _multiDatabaseProcessStep.StartAt,
                _multiDatabaseProcessStep.HoleStartTime, _multiDatabaseProcessStep.HoleEndTime, _procLogFilesFolder,
                _par.LocalWorkFileManager);

            if (procLogFile.HaveCurrentPeriodFile())
            {
                if (_logger.IsEnabled(LogLevel.Information))

                {
                    _logger.LogInformation("{DatabaseName} {StepName} had executed in this period", databaseName,
                        _stepName);
                }

                continue;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("{StepName} for database {DatabaseName}", _stepName, databaseName);
            }

            if (!await RunOneDatabaseAction(_par.AgentClient, databaseName, cancellationToken))
            {
                all = false;
                break;
            }

            //სამუშაო ფოლდერში შეიქმნას ფაილი, რომელიც იქნება იმის აღმნიშვნელი,
            //რომ ეს პროცესი შესრულდა და წარმატებით დასრულდა.
            //ფაილის სახელი უნდა შედგებოდეს პროცედურის სახელისაგან თარიღისა და დროისაგან
            //(როცა პროცესი დასრულდა)
            //ასევე სერვერის სახელი და ბაზის სახელი.
            //გაფართოება log
            procLogFile.CreateNow("Ok");
            //ასევე წაიშალოს ანალოგიური პროცესის მიერ წინათ შექმნილი ფაილები
            procLogFile.DeleteOldFiles();
            _logger.LogInformation("Ok");
        }

        return all;
    }

    protected virtual Task<bool> RunOneDatabaseAction(IDatabaseManager agentClient, string databaseName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
