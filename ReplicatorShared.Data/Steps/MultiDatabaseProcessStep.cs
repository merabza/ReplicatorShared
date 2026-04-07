using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.Steps;

public sealed class MultiDatabaseProcessStep : JobStep
{
    public EMultiDatabaseActionType ActionType { get; set; } //გასაშვები პროცესის ტიპი

    //თუ ბაზასთან დასაკავშირებლად ვიყენებთ პირდაპირ კავშირს, მაშინ ვებაგენტი აღარ გამოიყენება და პირიქით
    public string? DatabaseServerConnectionName { get; set; } //ბაზასთან დაკავშირების პარამეტრების ჩანაწერის სახელი
    public string? DatabaseWebAgentName { get; set; } //შეიძლება ბაზასთან დასაკავშირებლად გამოვიყენოთ ვებაგენტი
    public string? DatabaseServerName { get; set; } //გამოიყენება მხოლოდ იმ შემთხვევაში თუ ვიყენებთ WebAgent-ს

    public EDatabaseSet DatabaseSet { get; set; } //ბაზების სიმრავლე, რომლისთვისაც უნდა გაეშვას ეს პროცესი.

    //თუ DatabaseSet-ის მნიშვნელობაა DatabasesBySelection, მაშინ მონაცემთა ბაზების სახელები უნდა ავიღოთ ქვემოთ მოცემული სიიდან
    public List<string> DatabaseNames { get; set; } = [];

    public override ProcessesToolAction? GetToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        bool useConsole, ProcessManager processManager, ApAgentParameters parameters, string procLogFilesFolder)
    {
        FileManager? localWorkFileManager =
            FileManagersFactory.CreateFileManager(useConsole, logger, procLogFilesFolder);

        if (localWorkFileManager == null)
        {
            logger.LogError("workFileManager for procLogFilesFolder does not created");
            return null;
        }

        var par = MultiDatabaseProcessStepParameters.Create(logger, httpClientFactory, useConsole,
            new ApiClients(parameters.ApiClients), DatabaseServerConnectionName,
            new DatabaseServerConnections(parameters.DatabaseServerConnections), procLogFilesFolder);

        if (par is not null)
        {
            return CreateActionClass(ActionType, logger, useConsole, processManager, procLogFilesFolder, par);
        }

        logger.LogError("Error when creating MultiDatabaseProcessStep parameters");
        return null;
    }

    private ProcessesToolAction CreateActionClass(EMultiDatabaseActionType actionType, ILogger logger, bool useConsole,
        ProcessManager processManager, string procLogFilesFolder, MultiDatabaseProcessStepParameters par)
    {
        return actionType switch
        {
            EMultiDatabaseActionType.UpdateStatistics => new UpdateStatisticsStepCommand(logger, useConsole,
                procLogFilesFolder, processManager, this, par, ProcLineId),
            EMultiDatabaseActionType.CheckRepairDataBase => new CheckRepairDatabaseStepCommand(logger, useConsole,
                procLogFilesFolder, processManager, this, par, ProcLineId),
            EMultiDatabaseActionType.RecompileProcedures => new RecompileProceduresStepCommand(logger, useConsole,
                procLogFilesFolder, processManager, this, par, ProcLineId),
            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType,
                $"Unsupported action type: {actionType}")
        };
    }
}
