using System;
using ConnectionTools.ConnectTools;
using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class DeleteTempFiles : FolderProcessor
{
    private readonly string[] _patterns;

    public DeleteTempFiles(FileManager destinationFileManager, string[] patterns) : base("Temp files",
        "Delete Temp files", destinationFileManager, null, true, null, true, true)
    {
        _patterns = patterns;
    }

    protected override bool CheckParameters()
    {
        if (_patterns is { Length: > 0 })
        {
            return true;
        }

        Console.WriteLine("Delete Files patterns not specified");
        return false;
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        return !NeedExclude(file.FileName, _patterns) || FileManager.DeleteFile(afterRootPath, file.FileName);
    }
}
