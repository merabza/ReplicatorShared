using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.Models;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.SubCommands;

public sealed class DuplicateFilesRemover
{
    private readonly FileListModel _fileList;
    private readonly ILogger _logger;

    private readonly List<string> _priorityList;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DuplicateFilesRemover(ILogger logger, bool useConsole, FileListModel fileList, List<string> priorityList)
    {
        _logger = logger;
        _useConsole = useConsole;
        _fileList = fileList;
        _priorityList = priorityList;
    }

    internal bool Run()
    {
        StShared.ConsoleWriteInformationLine(_logger, _useConsole, "Delete duplicate Files");

        StShared.ConsoleWriteInformationLine(_logger, _useConsole, "Remove Duplicate Files");
        foreach (KeyValuePair<string, DuplicateFilesStorage> kvp in _fileList.DuplicateFilesStorage)
        {
            kvp.Value.RemoveDuplicates(_priorityList);
        }

        StShared.ConsoleWriteInformationLine(_logger, _useConsole, "Finish");

        return true;
    }
}
