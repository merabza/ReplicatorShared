using System.Collections.Generic;
using System.Linq;
using ConnectionTools.ConnectTools;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.StepCommands;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class CopyAndReplaceFiles : CopyMoveFiles
{
    private readonly Dictionary<string, List<MyFileInfo>> _checkedFolderFiles = [];

    private readonly List<string> _checkedFolders = [];
    private readonly int _fileMaxLength;

    public CopyAndReplaceFiles(ILogger logger, FileManager sourceFileManager, FileManager destinationFileManager,
        EMoveMethod useMethod, string uploadTempExtension, string downloadTempExtension, ExcludeSet excludeSet,
        int destinationFileMaxLength) : base("Copy And Replace files",
        "Copy And Replace files from one place to another", sourceFileManager, null, true, excludeSet, true, true,
        destinationFileManager, logger, useMethod, uploadTempExtension, downloadTempExtension, destinationFileMaxLength)
    {
        _fileMaxLength = destinationFileMaxLength - TempExtension.Length;
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        List<string> dirNames = afterRootPath is null
            ? []
            : afterRootPath.PrepareAfterRootPath(FileManager.DirectorySeparatorChar);
        string? destinationAfterRootPath = CheckDestinationDirs(dirNames);

        string preparedFileName = file.FileName.PreparedFileNameConsideringLength(_fileMaxLength);

        MyFileInfo? myFileInfo = GetOneFileWithInfo(destinationAfterRootPath, preparedFileName);

        //თუ ფაილის სახელი და სიგრძე ემთხვევა, ვთვლით, რომ იგივე ფაილია
        if (myFileInfo != null && myFileInfo.FileLength == file.FileLength)
        {
            return true;
        }

        if (myFileInfo != null)
            //იგივე სახელით ფაილი არსებობს და ამიტომ ჯერ უნდა წაიშალოს
        {
            DestinationFileManager.DeleteFile(destinationAfterRootPath, preparedFileName);
        }

        return ProcessOneFile(UseMethod, afterRootPath, file, destinationAfterRootPath, preparedFileName, false);
    }

    private MyFileInfo? GetOneFileWithInfo(string? afterRootPath, string fileName)
    {
        return GetFileInfos(afterRootPath).SingleOrDefault(x => x.FileName == fileName);
    }

    private IEnumerable<MyFileInfo> GetFileInfos(string? afterRootPath)
    {
        if (afterRootPath is null)
        {
            return DestinationFileManager.GetFilesWithInfo(null, null);
        }

        if (_checkedFolderFiles.TryGetValue(afterRootPath, out List<MyFileInfo>? value))
        {
            return value;
        }

        value = DestinationFileManager.GetFilesWithInfo(afterRootPath, null).ToList();
        _checkedFolderFiles.Add(afterRootPath, value);

        return value;
    }

    private string? CheckDestinationDirs(IEnumerable<string> dirNames)
    {
        string? afterRootPath = null;
        foreach (string dir in dirNames)
        {
            string forCreateDirPart = DestinationFileManager.PathCombine(afterRootPath, dir);
            if (!_checkedFolders.Contains(forCreateDirPart))
            {
                if (!DestinationFileManager.CareCreateDirectory(afterRootPath, dir, true))
                {
                    return null;
                }

                _checkedFolders.Add(forCreateDirPart);
            }

            afterRootPath = forCreateDirPart;
        }

        return afterRootPath;
    }
}
