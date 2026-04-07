using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.BackgroundTasks;
using ToolsManagement.CompressionManagement;

namespace ReplicatorShared.Data.StepCommands;

public sealed class UnZipOnPlaceCommand : ProcessesToolAction
{
    private readonly ILogger _logger;
    private readonly string _pathWithZips;
    private readonly bool _useConsole;
    private readonly bool _withSubFolders;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UnZipOnPlaceCommand(ILogger logger, bool useConsole, ProcessManager processManager, string pathWithZips,
        bool withSubFolders, int procLineId) : base(logger, null, null, processManager, "UnZip On Place", procLineId)
    {
        _logger = logger;
        _useConsole = useConsole;
        _pathWithZips = pathWithZips;
        _withSubFolders = withSubFolders;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking parameters...");

        //უნდა შემოწმდეს არსებობს თუ არა ფოლდერი _unZipOnPlaceStep.WithSubFolders
        //თუ არ არსებობს ვჩერდებით

        var curDir = new DirectoryInfo(_pathWithZips);
        return ValueTask.FromResult(ProcessFolder(curDir, _withSubFolders));
    }

    private bool ProcessFolder(DirectoryInfo curDir, bool useSubFolders = true)
    {
        Console.WriteLine($"Process Folder {curDir.FullName}");

        if (useSubFolders && curDir.GetDirectories().Any(dir => !ProcessFolder(dir)))
        {
            return false;
        }

        foreach (FileInfo file in curDir.GetFiles())
        {
            if (!file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string zipFileName = Path.GetFileNameWithoutExtension(file.Name);
            int i = 0;
            while (Directory.Exists(Path.Combine(curDir.FullName, GetNewFolderName(zipFileName, i))))
            {
                i++;
            }

            DirectoryInfo newDir =
                Directory.CreateDirectory(Path.Combine(curDir.FullName, GetNewFolderName(zipFileName, i)));
            var archiver = new ZipClassArchiver(_logger, _useConsole, file.Extension);

            Console.WriteLine($"Unzip {file.FullName}");

            if (!archiver.ArchiveToPath(file.FullName, newDir.FullName))
            {
                return false;
            }

            file.Delete();
        }

        return true;
    }

    private static string GetNewFolderName(string zipFileName, int i)
    {
        return $"{zipFileName}{(i == 0 ? string.Empty : $"({i})")}";
    }
}
