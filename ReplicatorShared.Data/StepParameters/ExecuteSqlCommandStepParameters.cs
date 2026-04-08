using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement;

namespace ReplicatorShared.Data.StepParameters;

public sealed class ExecuteSqlCommandStepParameters
{
    private ExecuteSqlCommandStepParameters(IDatabaseManager agentClient, string executeQueryCommand)
    {
        AgentClient = agentClient;
        ExecuteQueryCommand = executeQueryCommand;
    }

    public IDatabaseManager AgentClient { get; }

    public string ExecuteQueryCommand { get; }

    public static ExecuteSqlCommandStepParameters? Create(string appName, ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, string? executeQueryCommand, string? webAgentName,
        ApiClients apiClients, string? databaseServerConnectionName,
        DatabaseServerConnections databaseServerConnections)
    {
        OneOf<IDatabaseManager, Error[]> createDatabaseManagerResult = DatabaseManagersFactory
            .CreateDatabaseManager(appName, logger, useConsole, databaseServerConnectionName, databaseServerConnections,
                apiClients, httpClientFactory, null, null, CancellationToken.None).Result;

        if (createDatabaseManagerResult.IsT1)
        {
            Error.PrintErrorsOnConsole(createDatabaseManagerResult.AsT1);
        }

        if (!string.IsNullOrWhiteSpace(executeQueryCommand))
        {
            return new ExecuteSqlCommandStepParameters(createDatabaseManagerResult.AsT0, executeQueryCommand);
        }

        StShared.WriteErrorLine("executeQueryCommand does not Specified", useConsole, logger);
        return null;
    }
}
