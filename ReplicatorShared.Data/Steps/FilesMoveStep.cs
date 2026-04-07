using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Steps;

public sealed class FilesMoveStep : JobStep
{
    public string? MoveFolderMask { get; set; }

    //ფაილსაცავის სახელი, რომლიდანაც უნდა აიტვირთოს ფაილები
    public string? SourceFileStorageName { get; set; }

    //ფაილსაცავის სახელი, რომელშიც უნდა ჩაიტვირთოს ფაილები
    public string? DestinationFileStorageName { get; set; }

    //გამოსარიცხი ფაილებისა და გზების კომპლექტის სახელი
    public string? ExcludeSet { get; set; }

    //მიზნის მხარეს წინასწარ წასაშლელი ფაილები კომპლექტის სახელი
    public string? DeleteDestinationFilesSet { get; set; }

    //აკრძალული თანმიმდევრობის ჩანაცვლების კომპლექტის სახელი
    public string? ReplacePairsSet { get; set; }

    //მაქსიმუმ რამდენი ფოლდერის სახელი შეუნარჩუნდეს ფაილს
    public int MaxFolderCount { get; set; }

    //შევქმნათ თუ არა თარიღიანი და დროიანი ცალკე ფოლდერი ერთი სესიისათვის
    public bool CreateFolderWithDateTime { get; set; }

    public List<string> PriorityPoints { get; set; } = []; //პრიორიტეტული ფოლდერების ჩამონათვალი.

    public override ProcessesToolAction? GetToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        bool useConsole, ProcessManager processManager, ApAgentParameters parameters, string procLogFilesFolder)
    {
        var filesMoveStepParameters = FilesMoveStepParameters.Create(logger, useConsole, SourceFileStorageName,
            DestinationFileStorageName, ExcludeSet, DeleteDestinationFilesSet, ReplacePairsSet,
            string.IsNullOrWhiteSpace(MoveFolderMask) ? parameters.DateMask : MoveFolderMask,
            parameters.UploadFileTempExtension, parameters.DownloadFileTempExtension,
            new FileStorages(parameters.FileStorages), new ExcludeSets(parameters.ExcludeSets),
            new ReplacePairsSets(parameters.ReplacePairsSets), MaxFolderCount, CreateFolderWithDateTime,
            PriorityPoints);

        if (filesMoveStepParameters is not null)
        {
            return new FilesMoveStepCommand(logger, useConsole, processManager, this, filesMoveStepParameters);
        }

        StShared.WriteErrorLine("filesMoveStepParameters does not created for Files Move step", useConsole, logger);
        return null;
    }
}
