using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.StepParameters;

public sealed class DownloadBackupParameters
{
    private DownloadBackupParameters(FileManager localFileManager, FileManager downloadFileManager)
    {
        LocalFileManager = localFileManager;
        DownloadFileManager = downloadFileManager;
    }

    public FileManager LocalFileManager { get; }
    public FileManager DownloadFileManager { get; }

    public static DownloadBackupParameters? Create(ILogger logger, bool useConsole, string? localPath,
        FileStorageData downloadFileStorage)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            StShared.WriteErrorLine("localPath is not specified", useConsole, logger);
            return null;
        }

        FileManager? localFileManager = FileManagersFactory.CreateFileManager(useConsole, logger, localPath);

        if (localFileManager is null)
        {
            StShared.WriteErrorLine("FileManager for localPath does not created", useConsole, logger);
            return null;
        }

        FileManager? downloadFileManager =
            FileManagersFactoryExt.CreateFileManager(useConsole, logger, localPath, downloadFileStorage);

        if (downloadFileManager is null)
        {
            StShared.WriteErrorLine("downloadFileManager does not created", useConsole, logger);
            return null;
        }

        return new DownloadBackupParameters(localFileManager, downloadFileManager);
    }
}
