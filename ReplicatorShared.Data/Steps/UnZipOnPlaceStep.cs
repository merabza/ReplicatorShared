using System.Net.Http;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Steps;

public sealed class UnZipOnPlaceStep : JobStep
{
    public string? PathWithZips { get; set; }
    public bool WithSubFolders { get; set; }

    public override ProcessesToolAction? GetToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        bool useConsole, ProcessManager processManager, ReplicatorParameters parameters, string procLogFilesFolder)
    {
        if (!string.IsNullOrWhiteSpace(PathWithZips))
        {
            return new UnZipOnPlaceCommand(logger, useConsole, processManager, PathWithZips, WithSubFolders,
                ProcLineId);
        }

        StShared.WriteErrorLine("PathWithZips is empty. PathWithZips sis not run", useConsole, logger);
        return null;
    }
}
