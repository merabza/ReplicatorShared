using System;
using System.IO;
using ConnectionTools.ConnectTools;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.StepCommands;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.FolderProcessors;

public class CopyMoveFiles : FolderProcessor
{
    private readonly ILogger _logger;
    protected readonly FileManager DestinationFileManager;
    protected readonly int FileMaxLength;
    protected readonly string TempExtension;
    protected readonly EMoveMethod UseMethod;

    protected CopyMoveFiles(string name, string description, FileManager fileManager, string? fileSearchPattern,
        bool deleteEmptyFolders, ExcludeSet? excludeSet, bool useSubFolders, bool useProcessFiles,
        FileManager destinationFileManager, ILogger logger, EMoveMethod useMethod, string uploadTempExtension,
        string downloadTempExtension, int destinationFileMaxLength) : base(name, description, fileManager,
        fileSearchPattern, deleteEmptyFolders, excludeSet, useSubFolders, useProcessFiles)
    {
        DestinationFileManager = destinationFileManager;
        _logger = logger;
        UseMethod = useMethod;
        TempExtension = useMethod.CountTempExtension(uploadTempExtension, downloadTempExtension);
        FileMaxLength = destinationFileMaxLength - TempExtension.Length;
    }

    protected bool ProcessOneFile(EMoveMethod useMethod, string? afterRootPath, MyFileInfo file,
        string? destinationAfterRootPath, string preparedFileName, bool deleteSourceFile)
    {
        switch (useMethod)
        {
            case EMoveMethod.Upload:
                if (!DestinationFileManager.UploadFile(afterRootPath, file.FileName, destinationAfterRootPath,
                        preparedFileName, TempExtension))
                {
                    //თუ ვერ აიტვირთა, გადავდივართ შემდეგზე
                    _logger.LogWarning("Folder with name {FileName} cannot Upload", file.FileName);
                    return true;
                }

                if (deleteSourceFile)
                {
                    FileManager.DeleteFile(afterRootPath, file.FileName);
                }

                break;
            case EMoveMethod.Download:
                if (!FileManager.DownloadFile(afterRootPath, file.FileName, destinationAfterRootPath, preparedFileName,
                        TempExtension))
                {
                    //თუ ვერ აიტვირთა, გადავდივართ შემდეგზე
                    _logger.LogWarning("Folder with name {FileName} cannot Download", file.FileName);
                    return true;
                }

                if (deleteSourceFile)
                {
                    FileManager.DeleteFile(afterRootPath, file.FileName);
                }

                break;
            case EMoveMethod.Local:
                if (deleteSourceFile)

                {
                    File.Move(FileManager.GetPath(afterRootPath, file.FileName),
                        DestinationFileManager.GetPath(destinationAfterRootPath, preparedFileName));
                }
                else
                {
                    File.Copy(FileManager.GetPath(afterRootPath, file.FileName),
                        DestinationFileManager.GetPath(destinationAfterRootPath, preparedFileName));
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(useMethod), useMethod, "Invalid EMoveMethod value.");
        }

        return false;
    }
}
