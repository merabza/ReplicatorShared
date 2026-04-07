using System.Collections.Generic;
using System.Linq;
using ConnectionTools.ConnectTools;
using ParametersManagement.LibFileParameters.Models;
using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class DeleteRedundantFiles : FolderProcessor
{
    private readonly FileManager _sourceFileManager;

    public DeleteRedundantFiles(FileManager sourceFileManager, FileManager destinationFileManager,
        ExcludeSet excludeSet) : base("Delete redundant files", "Delete redundant files after compare two places",
        destinationFileManager, null, true, excludeSet, true, true)
    {
        _sourceFileManager = sourceFileManager;
    }

    //success, folderNameChanged, continueWithThisFolder
    protected override (bool, bool, bool) ProcessOneFolder(string? afterRootPath, string folderName)
    {
        //დავადგინოთ ასეთი ფოლდერი გვაქვს თუ არა წყაროში და თუ არ გვაქვს წავშალოთ მიზნის მხარესაც

        List<string> folders = _sourceFileManager.GetFolderNames(afterRootPath, null);

        if (folders.Contains(folderName))
        {
            return (true, false, false);
        }

        bool deleted = FileManager.DeleteDirectory(afterRootPath, folderName, true);
        return deleted ? (true, true, true) : (false, false, false);
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        IEnumerable<MyFileInfo> myFileInfos = _sourceFileManager.GetFilesWithInfo(afterRootPath, null);

        if (ExcludeSet != null && ExcludeSet.NeedExclude(FileManager.PathCombine(afterRootPath, file.FileName)) ||
            !myFileInfos.Select(x => x.FileName).Contains(file.FileName))
        {
            return FileManager.DeleteFile(afterRootPath, file.FileName);
        }

        return true;
    }
}
