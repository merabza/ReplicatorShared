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

public sealed class FilesBackupStep : JobStep
{
    public string? MaskName { get; set; }

    //თარიღის ფორმატი, რომელიც გამოიყენება ფაილის სახელში "yyyy_MM_dd_HHmmss_fffffff"
    public string? DateMask { get; set; }

    //ფოლდერი, სადაც შეინახება ფაილების ბექაპები
    public string? LocalPath { get; set; }

    //ფაილების დასაკუმშად გამოსაყენებელი არქივატორი. (აქ არქივატორი აუცილებლად უნდა იყოს მითითებული, რადგან დაკუმშვის გარეშე ფაილების დაკოპირება არ შეიძლება)
    public string? ArchiverName { get; set; }

    //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს ბექაპების ფოლდერისათვის მხარეს)
    public string? LocalSmartSchemaName { get; set; }

    //ატვირთვა რეზერვაციისათვის
    //ფაილსაცავის სახელი, რომელიც გამოიყენება რეზერვაციისათვის ბექაპების ასატვირთად
    public string? UploadFileStorageName { get; set; }

    //ატვირთვის პროცესის ხაზის იდენტიფიკატორი
    public int UploadProcLineId { get; set; }

    //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს რეზერვაციის ფაილსაცავის მხარეს)
    public string? UploadSmartSchemaName { get; set; }

    //public bool AnaliseGitignore { get; set; } //Gitignore ფაილის გაანალიზება. (ჯერჯერობით არ გამოიყენება)

    //True - არჩეული გზები ცალ-ცალკე არქივებში წავიდეს. False - ყველა გზა წავიდეს ერთ ერქივში.
    public bool BackupSeparately { get; set; }

    public string? ExcludeSetName { get; set; } //გამოსარიცხი ფაილებისა და გზების კომპლექტის სახელი

    //დასაარქივებელი ფოლდერების ჩამონათვალი. (სახელი გამოიყენება ნიღბის კოდად)
    public Dictionary<string, string> BackupFolderPaths { get; set; } = [];

    public override ProcessesToolAction? GetToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        bool useConsole, ProcessManager processManager, ReplicatorParameters parameters, string procLogFilesFolder)
    {
        var par = FilesBackupStepParameters.Create(logger, useConsole, LocalPath, ArchiverName, ExcludeSetName,
            UploadFileStorageName, MaskName, DateMask, LocalSmartSchemaName, UploadSmartSchemaName, BackupFolderPaths,
            new Archivers(parameters.Archivers), new ExcludeSets(parameters.ExcludeSets),
            new FileStorages(parameters.FileStorages), new SmartSchemas(parameters.SmartSchemas), UploadProcLineId,
            BackupSeparately, parameters.ArchivingFileTempExtension, parameters.UploadFileTempExtension);

        if (par is not null)
        {
            return new FilesBackupStepCommand(logger, useConsole, par, processManager, this);
        }

        StShared.WriteErrorLine("FilesBackupStepParameters does not created", useConsole, logger);
        return null;
    }
}
