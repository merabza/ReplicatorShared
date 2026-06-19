using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.Models;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.SubCommands;

public sealed class MultiDuplicatesFinder
{
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MultiDuplicatesFinder(ILogger logger, bool useConsole, FileListModel fileList)
    {
        _logger = logger;
        _useConsole = useConsole;
        FileList = fileList;
    }

    public FileListModel FileList { get; }

    internal bool Run()
    {
        StShared.ConsoleWriteInformationLine(_logger, _useConsole, "Find Multi duplicate Files");

        foreach (KeyValuePair<string, DuplicateFilesStorage> kvp in FileList.DuplicateFilesStorage)
        {
            kvp.Value.CountMultiDuplicates();
        }

        StShared.ConsoleWriteInformationLine(_logger, _useConsole, "Finish");

        return true;
    }
}
