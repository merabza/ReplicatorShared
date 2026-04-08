using System.Net.Http;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Steps;

public sealed class ExecuteSqlCommandStep : JobStep
{
    //თუ ბაზასთან დასაკავშირებლად ვიყენებთ პირდაპირ კავშირს, მაშინ ვებაგენტი აღარ გამოიყენება და პირიქით
    public string? DatabaseServerConnectionName { get; set; } //ბაზასთან დაკავშირების პარამეტრების ჩანაწერის სახელი

    public string? DatabaseWebAgentName { get; set; } //შეიძლება ბაზასთან დასაკავშირებლად გამოვიყენოთ ვებაგენტი
    //public string? DatabaseServerName { get; set; } //გამოიყენება მხოლოდ იმ შემთხვევაში თუ ვიყენებთ WebAgent-ს

    public string? DatabaseName { get; set; } //მონაცემთა ბაზის სახელი

    public string? ExecuteQueryCommand { get; set; } //შესასრულებელი ბრძანების ტექსტი

    //ბრძანების შესრულების ტაიმაუტი. თუ ამ დროში ბრძანება არ დასრულდა. პროცესი უნდა გაჩერდეს.
    public int CommandTimeOut { get; set; }

    public override ProcessesToolAction? GetToolAction(string appName, ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, ProcessManager processManager,
        ReplicatorParameters parameters, string procLogFilesFolder)
    {
        var par = ExecuteSqlCommandStepParameters.Create(appName, logger, httpClientFactory, useConsole,
            ExecuteQueryCommand, DatabaseWebAgentName, new ApiClients(parameters.ApiClients),
            DatabaseServerConnectionName, new DatabaseServerConnections(parameters.DatabaseServerConnections));

        if (par is not null)
        {
            return new ExecuteSqlCommandStepCommand(logger, processManager, this, par);
        }

        StShared.WriteErrorLine("par does not created", useConsole, logger);
        return null;
    }
}
