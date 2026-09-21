using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
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
        Result<IDatabaseManager> createDatabaseManagerResult = DatabaseManagersFactory.CreateDatabaseManager(appName,
            logger, useConsole, databaseServerConnectionName, databaseServerConnections, apiClients, httpClientFactory,
            null, null, CancellationToken.None).Result;

        if (createDatabaseManagerResult.IsFailure)
        {
            createDatabaseManagerResult.Error.PrintErrorsOnConsole();
            return null;
        }

        if (!string.IsNullOrWhiteSpace(executeQueryCommand))
        {
            return new ExecuteSqlCommandStepParameters(createDatabaseManagerResult.Value, executeQueryCommand);
        }

        StShared.WriteErrorLine("executeQueryCommand does not Specified", useConsole, logger);
        return null;
    }
}
