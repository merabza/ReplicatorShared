using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.CompressionManagement;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.StepParameters;

public sealed class FilesBackupStepParameters
{
    private FilesBackupStepParameters(string localPath, string maskName, string dateMask, Archiver archiver,
        Dictionary<string, string> backupFolderPaths, FileManager localWorkFileManager, ExcludeSet excludeSet,
        FileStorageData uploadFileStorage, SmartSchema localSmartSchema, bool backupSeparately,
        UploadParameters uploadParameters, string archivingTempExtension)
    {
        LocalPath = localPath;
        Archiver = archiver;
        BackupFolderPaths = backupFolderPaths;
        LocalWorkFileManager = localWorkFileManager;
        ExcludeSet = excludeSet;
        UploadFileStorage = uploadFileStorage;
        MaskName = maskName;
        DateMask = dateMask;
        LocalSmartSchema = localSmartSchema;
        BackupSeparately = backupSeparately;
        UploadParameters = uploadParameters;
        ArchivingTempExtension = archivingTempExtension;
    }

    //ფოლდერი, სადაც შეინახება ფაილების მარქაფები
    public string LocalPath { get; }

    //ლოკალური ფოლდერის მენეჯერი
    public FileManager LocalWorkFileManager { get; }
    public SmartSchema LocalSmartSchema { get; }
    public Archiver Archiver { get; }
    public Dictionary<string, string> BackupFolderPaths { get; }
    public ExcludeSet ExcludeSet { get; }
    public FileStorageData UploadFileStorage { get; }
    public UploadParameters UploadParameters { get; }
    public string MaskName { get; }
    public string DateMask { get; }

    public string ArchivingTempExtension { get; }

    //True - არჩეული გზები ცალ-ცალკე არქივებში წავიდეს. False - ყველა გზა წავიდეს ერთ ერქივში.
    public bool BackupSeparately { get; }

    public static FilesBackupStepParameters? Create(ILogger logger, bool useConsole, string? localPath,
        string? archiverName, string? excludeSetName, string? uploadFileStorageName, string? maskName, string? dateMask,
        string? localSmartSchemaName, string? uploadSmartSchemaName, Dictionary<string, string> backupFolderPaths,
        Archivers archivers, ExcludeSets excludeSets, FileStorages fileStorages, SmartSchemas smartSchemas,
        int uploadProcLineId, bool backupSeparately, string? archivingTempExtension, string? uploadTempExtension)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            StShared.WriteErrorLine("localPath is not specified", useConsole, logger);
            return null;
        }

        FileManager? localWorkFileManager = FileManagersFactory.CreateFileManager(useConsole, logger, localPath);

        if (localWorkFileManager is null)
        {
            StShared.WriteErrorLine("FileManager for localPath does not created", useConsole, logger);
            return null;
        }

        Archiver? archiver = ArchiverFactory.CreateArchiver(logger, useConsole, archivers, archiverName);

        if (archiver is null)
        {
            StShared.WriteErrorLine("Can not create Archiver for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(excludeSetName))
        {
            StShared.WriteErrorLine("excludeSetName is not specified", useConsole, logger);
            return null;
        }

        ExcludeSet? excludeSet = excludeSets.GetExcludeSetByKey(excludeSetName);

        if (excludeSet is null)
        {
            StShared.WriteErrorLine("Can not create excludeSet for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(uploadFileStorageName))
        {
            StShared.WriteErrorLine("uploadFileStorageName is not specified", useConsole, logger);
            return null;
        }

        FileStorageData? uploadFileStorage = fileStorages.GetFileStorageDataByKey(uploadFileStorageName);

        if (uploadFileStorage is null)
        {
            StShared.WriteErrorLine("Can not create uploadFileStorage for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(maskName))
        {
            StShared.WriteErrorLine("maskName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(dateMask))
        {
            StShared.WriteErrorLine("dateMask does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(localSmartSchemaName))
        {
            StShared.WriteErrorLine("localSmartSchemaName does not specified", useConsole, logger);
            return null;
        }

        SmartSchema? localSmartSchema = smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);

        if (localSmartSchema is null)
        {
            StShared.WriteErrorLine("Can not create localSmartSchema for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(uploadSmartSchemaName))
        {
            StShared.WriteErrorLine("uploadSmartSchemaName does not specified", useConsole, logger);
            return null;
        }

        SmartSchema? uploadSmartSchema = smartSchemas.GetSmartSchemaByKey(uploadSmartSchemaName);

        if (uploadSmartSchema is null)
        {
            StShared.WriteErrorLine("Can not create uploadSmartSchema for Files Backup step", useConsole, logger);
            return null;
        }

        var uploadParameters = UploadParameters.Create(logger, useConsole, localPath, uploadFileStorage,
            uploadSmartSchema, uploadTempExtension, uploadProcLineId);

        if (uploadParameters is null)
        {
            StShared.WriteErrorLine("uploadParameters does not created", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(archivingTempExtension))
        {
            StShared.WriteErrorLine("archivingTempExtension does not specified", useConsole, logger);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(uploadTempExtension))
        {
            return new FilesBackupStepParameters(localPath, maskName, dateMask, archiver, backupFolderPaths,
                localWorkFileManager, excludeSet, uploadFileStorage, localSmartSchema, backupSeparately,
                uploadParameters, archivingTempExtension);
        }

        StShared.WriteErrorLine("uploadTempExtension does not specified", useConsole, logger);
        return null;
    }
}
