using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.FileManagersMain;
using OneOf;

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

    public static MultiDatabaseProcessStepParameters? Create(ILogger logger, IHttpClientFactory httpClientFactory,
        bool useConsole, ApiClients apiClients, string? databaseServerConnectionName,
        DatabaseServerConnections databaseServerConnections, string procLogFilesFolder)
    {
        OneOf<IDatabaseManager, Error[]> createDatabaseManagerResult = DatabaseManagersFactory
            .CreateDatabaseManager(logger, useConsole, databaseServerConnectionName, databaseServerConnections,
                apiClients, httpClientFactory, null, null, CancellationToken.None).Result;

        if (createDatabaseManagerResult.IsT1)
        {
            Error.PrintErrorsOnConsole(createDatabaseManagerResult.AsT1);
        }

        FileManager? localWorkFileManager =
            FileManagersFactory.CreateFileManager(useConsole, logger, procLogFilesFolder);

        if (localWorkFileManager != null)
        {
            return new MultiDatabaseProcessStepParameters(createDatabaseManagerResult.AsT0, localWorkFileManager);
        }

        logger.LogError("workFileManager for procLogFilesFolder does not created");
        return null;
    }
}
