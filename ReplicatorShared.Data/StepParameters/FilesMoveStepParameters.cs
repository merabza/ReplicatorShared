using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.StepParameters;

public sealed class FilesMoveStepParameters
{
    private FilesMoveStepParameters(FileStorageData sourceFileStorage, bool sourceIsLocal,
        FileStorageData destinationFileStorage, bool destinationIsLocal, ExcludeSet excludeSet,
        ExcludeSet? deleteDestinationFilesSet, FileManager sourceFileManager, FileManager destinationFileManager,
        EMoveMethod useMethod, ReplacePairsSet? replacePairsSet, string moveFolderMask, string uploadTempExtension,
        string downloadTempExtension, int maxFolderCount, bool createFolderWithDateTime, List<string> priorityPoints)
    {
        SourceFileStorage = sourceFileStorage;
        SourceIsLocal = sourceIsLocal;
        DestinationFileStorage = destinationFileStorage;
        DestinationIsLocal = destinationIsLocal;
        ExcludeSet = excludeSet;
        DeleteDestinationFilesSet = deleteDestinationFilesSet;
        SourceFileManager = sourceFileManager;
        DestinationFileManager = destinationFileManager;
        UseMethod = useMethod;
        ReplacePairsSet = replacePairsSet;
        MoveFolderMask = moveFolderMask;
        UploadTempExtension = uploadTempExtension;
        DownloadTempExtension = downloadTempExtension;
        MaxFolderCount = maxFolderCount;
        CreateFolderWithDateTime = createFolderWithDateTime;
        PriorityPoints = priorityPoints;
    }

    public FileStorageData SourceFileStorage { get; }
    public bool SourceIsLocal { get; }
    public FileStorageData DestinationFileStorage { get; }
    public bool DestinationIsLocal { get; }
    public ExcludeSet ExcludeSet { get; }
    public ExcludeSet? DeleteDestinationFilesSet { get; }
    public FileManager SourceFileManager { get; }
    public FileManager DestinationFileManager { get; }
    public EMoveMethod UseMethod { get; }
    public ReplacePairsSet? ReplacePairsSet { get; }
    public string MoveFolderMask { get; }
    public string UploadTempExtension { get; }
    public string DownloadTempExtension { get; }
    public int MaxFolderCount { get; } //მაქსიმუმ რამდენი ფოლდერის სახელი შეუნარჩუნდეს ფაილს

    public bool CreateFolderWithDateTime { get; } //შევქმნათ თუ არა თარიღიანი და დროიანი ცალკე ფოლდერი ერთი სესიისათვის

    //public Dictionary<string, string> PriorityFolderPaths { get; set; } //პრიორიტეტული ფოლდერების ჩამონათვალი. (სახელი გამოიყენება ნიღბის კოდად)
    public List<string> PriorityPoints { get; } //პრიორიტეტული ფოლდერების ჩამონათვალი.

    public static FilesMoveStepParameters? Create(ILogger logger, bool useConsole, string? sourceFileStorageName,
        string? destinationFileStorageName, string? excludeSetName, string? deleteDestinationFilesSetName,
        string? replacePairsSetName, string moveFolderMask, string uploadTempExtension, string downloadTempExtension,
        FileStorages fileStorages, ExcludeSets excludeSets, ReplacePairsSets replacePairsSets, int maxFolderCount,
        bool createFolderWithDateTime, List<string> priorityPoints)
    {
        if (string.IsNullOrWhiteSpace(sourceFileStorageName))
        {
            StShared.WriteErrorLine("sourceFileStorageName does not specified for Files Sync step", useConsole, logger);
            return null;
        }

        FileStorageData? sourceFileStorage = fileStorages.GetFileStorageDataByKey(sourceFileStorageName);

        if (sourceFileStorage == null)
        {
            StShared.WriteErrorLine("Source File Storage not specified for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(sourceFileStorage.FileStoragePath))
        {
            StShared.WriteErrorLine("sourceFileStorage.FileStoragePath does not specified for Files Sync step",
                useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(destinationFileStorageName))
        {
            StShared.WriteErrorLine("destinationFileStorageName does not specified for Files Sync step", useConsole,
                logger);
            return null;
        }

        FileStorageData? destinationFileStorage = fileStorages.GetFileStorageDataByKey(destinationFileStorageName);

        if (destinationFileStorage == null)
        {
            StShared.WriteErrorLine("destination File Storage not specified for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(destinationFileStorage.FileStoragePath))
        {
            StShared.WriteErrorLine("destinationFileStorage.FileStoragePath does not specified for Files Sync step",
                useConsole, logger);
            return null;
        }

        bool? sourceIsLocal = sourceFileStorage.IsFileSchema();
        if (sourceIsLocal is null)
        {
            StShared.WriteErrorLine("could not be determined source is File Schema or not", useConsole, logger);
            return null;
        }

        bool? destinationIsLocal = destinationFileStorage.IsFileSchema();
        if (destinationIsLocal is null)
        {
            StShared.WriteErrorLine("could not be determined destination is File Schema or not", useConsole, logger);
            return null;
        }

        if (!sourceIsLocal.Value && !destinationIsLocal.Value)
        {
            StShared.WriteErrorLine("At Least one file storage must be a local storage", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(excludeSetName))
        {
            StShared.WriteErrorLine("excludeSetName does not specified for Files Sync step", useConsole, logger);
            return null;
        }

        ExcludeSet? excludeSet = excludeSets.GetExcludeSetByKey(excludeSetName);

        if (excludeSet == null)
        {
            StShared.WriteErrorLine("excludeSet does not created for Files Backup step", useConsole, logger);
            return null;
        }

        ExcludeSet? deleteDestinationFilesSet = null;
        if (!string.IsNullOrWhiteSpace(deleteDestinationFilesSetName))
        {
            deleteDestinationFilesSet = excludeSets.GetExcludeSetByKey(deleteDestinationFilesSetName);
        }

        string? fileStoragePath = sourceFileStorage.FileStoragePath;
        StShared.ConsoleWriteInformationLine(logger, useConsole, "Source is From {fileStoragePath}", fileStoragePath);

        FileManager? sourceFileManager = sourceIsLocal.Value
            ? FileManagersFactory.CreateFileManager(useConsole, logger, sourceFileStorage.FileStoragePath)
            : FileManagersFactoryExt.CreateFileManager(useConsole, logger, destinationFileStorage.FileStoragePath,
                sourceFileStorage);
        //თუ წყარო ლოკალურია

        if (sourceFileManager == null)
        {
            StShared.WriteErrorLine("sourceFileManager does not created for Files Backup step", useConsole, logger);
            return null;
        }

        Console.WriteLine($"Destination is {destinationFileStorage.FileStoragePath}");

        FileManager? destinationFileManager = destinationIsLocal.Value
            ? FileManagersFactory.CreateFileManager(useConsole, logger, destinationFileStorage.FileStoragePath)
            : FileManagersFactoryExt.CreateFileManager(useConsole, logger, sourceFileStorage.FileStoragePath,
                destinationFileStorage);
        //თუ მიზანი ლოკალურია

        if (destinationFileManager == null)
        {
            StShared.WriteErrorLine("destinationFileManager does not created for Files Backup step", useConsole,
                logger);
            return null;
        }

        EMoveMethod locOrUp = destinationIsLocal.Value ? EMoveMethod.Local : EMoveMethod.Upload;
        EMoveMethod useMethod = !sourceIsLocal.Value ? EMoveMethod.Download : locOrUp;
        //თუ წყარო მოშორებულია

        ReplacePairsSet? replacePairsSet = null;
        if (!string.IsNullOrWhiteSpace(replacePairsSetName))
        {
            replacePairsSet = replacePairsSets.GetReplacePairsSetByKey(replacePairsSetName);
        }

        return new FilesMoveStepParameters(sourceFileStorage, sourceIsLocal.Value, destinationFileStorage,
            destinationIsLocal.Value, excludeSet, deleteDestinationFilesSet, sourceFileManager, destinationFileManager,
            useMethod, replacePairsSet, moveFolderMask, uploadTempExtension, downloadTempExtension, maxFolderCount,
            createFolderWithDateTime, priorityPoints);
    }
}
