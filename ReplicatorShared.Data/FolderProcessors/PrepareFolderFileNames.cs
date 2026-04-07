using ConnectionTools.ConnectTools;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.StepCommands;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class PrepareFolderFileNames : FolderProcessor
{
    private readonly int _fileMaxLength;

    public PrepareFolderFileNames(FileManager sourceFileManager, EMoveMethod useMethod, string uploadTempExtension,
        string downloadTempExtension, ExcludeSet excludeSet, int destinationFileMaxLength) : base("Prepare Names",
        "Prepare Folder and File Names", sourceFileManager, null, false, excludeSet, true, true)
    {
        string tempExtension = useMethod.CountTempExtension(uploadTempExtension, downloadTempExtension);
        _fileMaxLength = destinationFileMaxLength - tempExtension.Length;
    }

    //success, folderNameChanged, continueWithThisFolder
    protected override (bool, bool, bool) ProcessOneFolder(string? afterRootPath, string folderName)
    {
        string preparedFolderName = folderName.Trim();
        return preparedFolderName != folderName
            ? (FileManager.RenameFolder(afterRootPath, folderName, preparedFolderName), true, true)
            : (true, false, false);
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        string preparedFileName = file.FileName.PreparedFileNameConsideringLength(_fileMaxLength);
        return preparedFileName == file.FileName ||
               FileManager.RenameFile(afterRootPath, file.FileName, preparedFileName);
    }
}
