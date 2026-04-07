using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.StepParameters;

public sealed class UploadParameters
{
    private UploadParameters(FileManager uploadFileManager, FileManager workFileManager, SmartSchema uploadSmartSchema,
        string uploadTempExtension, int uploadProcLine, string localPath)
    {
        UploadFileManager = uploadFileManager;
        WorkFileManager = workFileManager;
        UploadSmartSchema = uploadSmartSchema;
        UploadTempExtension = uploadTempExtension;
        UploadProcLineId = uploadProcLine;
        LocalPath = localPath;
    }

    public FileManager UploadFileManager { get; }
    public FileManager WorkFileManager { get; }
    public SmartSchema UploadSmartSchema { get; }
    public string UploadTempExtension { get; }
    public int UploadProcLineId { get; }
    public string LocalPath { get; }

    public static UploadParameters? Create(ILogger logger, bool useConsole, string localPath,
        FileStorageData uploadFileStorage, SmartSchema uploadSmartSchema, string? uploadTempExtension,
        int uploadProcLine)
    {
        FileManager? uploadFileManager =
            FileManagersFactoryExt.CreateFileManager(useConsole, logger, localPath, uploadFileStorage);

        if (uploadFileManager is null)
        {
            StShared.WriteErrorLine("UploadParameters: uploadFileManager does not created", useConsole, logger);
            return null;
        }

        FileManager? workFileManager = FileManagersFactory.CreateFileManager(useConsole, logger, localPath);

        if (workFileManager is null)
        {
            StShared.WriteErrorLine("UploadParameters: workFileManager does not created", useConsole, logger);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(uploadTempExtension))

        {
            return new UploadParameters(uploadFileManager, workFileManager, uploadSmartSchema,
                uploadTempExtension.AddNeedLeadPart("."), uploadProcLine, localPath);
        }

        StShared.WriteErrorLine("uploadTempExtension does not specified", useConsole, logger);
        return null;
    }
}
