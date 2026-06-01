using ConnectionTools.ConnectTools;
using Microsoft.Extensions.Logging;
using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class DeleteTempFiles : FolderProcessor
{
    private readonly ILogger _logger;
    private readonly string[] _patterns;

    public DeleteTempFiles(ILogger logger, FileManager destinationFileManager, string[] patterns) : base("Temp files",
        "Delete Temp files", destinationFileManager, null, true, null, true, true)
    {
        _logger = logger;
        _patterns = patterns;
    }

    protected override bool CheckParameters()
    {
        if (_patterns is { Length: > 0 })
        {
            return true;
        }

        _logger.LogError("Delete Files patterns not specified");
        return false;
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        return !NeedExclude(file.FileName, _patterns) || FileManager.DeleteFile(afterRootPath, file.FileName);
    }
}
