using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.StepParameters;

public sealed class MultiDatabaseProcessStepParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private MultiDatabaseProcessStepParameters(IDatabaseManager agentClient, FileManager localWorkFileManager)
    {
        AgentClient = agentClient;
        LocalWorkFileManager = localWorkFileManager;
    }

    public IDatabaseManager AgentClient { get; }
    public FileManager LocalWorkFileManager { get; } //ლოკალური ფოლდერის მენეჯერი

    public static MultiDatabaseProcessStepParameters? Create(string appName, ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, ApiClients apiClients,
        string? databaseServerConnectionName, DatabaseServerConnections databaseServerConnections,
        string procLogFilesFolder)
    {
        Result<IDatabaseManager> createDatabaseManagerResult = DatabaseManagersFactory.CreateDatabaseManager(appName,
            logger, useConsole, databaseServerConnectionName, databaseServerConnections, apiClients, httpClientFactory,
            null, null, CancellationToken.None).Result;

        if (createDatabaseManagerResult.IsFailure)
        {
            createDatabaseManagerResult.Error.PrintErrorsOnConsole();
            return null;
        }

        FileManager? localWorkFileManager =
            FileManagersFactory.CreateFileManager(useConsole, logger, procLogFilesFolder);

        if (localWorkFileManager != null)
        {
            return new MultiDatabaseProcessStepParameters(createDatabaseManagerResult.Value, localWorkFileManager);
        }

        logger.LogError("workFileManager for procLogFilesFolder does not created");
        return null;
    }
}
