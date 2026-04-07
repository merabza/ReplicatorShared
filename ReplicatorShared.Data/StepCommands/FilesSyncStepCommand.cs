using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.FolderProcessors;
using ReplicatorShared.Data.StepParameters;
using ReplicatorShared.Data.Steps;
using SystemTools.BackgroundTasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace ReplicatorShared.Data.StepCommands;

public sealed class FilesSyncStepCommand : ProcessesToolAction
{
    private readonly ILogger _logger;
    private readonly FilesSyncStepParameters _par;

    public FilesSyncStepCommand(ILogger logger, ProcessManager processManager, JobStep jobStep,
        FilesSyncStepParameters filesSyncStepParameters) : base(logger, null, null, processManager, "Files Sync",
        jobStep.ProcLineId)
    {
        _logger = logger;
        _par = filesSyncStepParameters;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //სანამ რაიმეს გადაწერას დავიწყებთ, დავრწმუნდეთ, რომ მიზნის მხარეს არ არის შემორჩენილი ძველი დროებითი ფაილები
        if (_par.DeleteDestinationFilesSet != null)
        {
            var deleteTempFiles = new DeleteTempFiles(_par.DestinationFileManager,
                [.. _par.DeleteDestinationFilesSet.FolderFileMasks]);

            if (!deleteTempFiles.Run())
            {
                return ValueTask.FromResult(false);
            }
        }

        //თუ მიზანი მოშორებულია და FTP-ა, 
        //სანამ გადაწერას დავიწყებთ, დავრწმუნდეთ, რომ არ არის აკრძალული სახელის ფაილები.
        //ჯერჯერობით რაც ვერ მუშავდება FTP-ს მხარეს არის თანმიმდევრობით სამი წერტილი სახელში 
        //ზოგადი მიდგომა ასეთი უნდა იყოს. ვიპოვოთ ფაილები, რომელთაც აქვთ აკრძალული თანმიმდევრობა სახელში
        //და ჩავანაცვლოთ ახლოს მდგომი დასაშვები ვარიანტით
        //მაგალითად ... -> .
        //ჩანაცვლებისას აღმოჩნდება, რომ ახალი სახელით უკვე არის სხვა ფაილი იმავე ფოლდერში,
        //მაშინ ბოლოში (გაფართოების წინ) მივაწეროთ ფრჩილებში ჩასმული 2.
        //თუ ასეთიც არის, მაშინ ავიღოთ სამი და ასე მანამ, სანამ არ ვიპოვით თავისუფალ სახელს

        //FTP-ს მხარეს არ აიტვირთოს ისეთი ფაილები, რომლებიც შეიცავენ მიმდევრობით 2 ან მეტ წერტილს.
        //ასეთ შემთხვევაში დავიდეთ 1 წერტილამდე და ისე ავტვირთოთ
        //თუ ასეთი სახელი იარსებებს გამოვიყენოთ ციფრები ბოლოში

        if (_par.ReplacePairsSet != null)
        {
            var changeFilesWithManyDots =
                new ChangeFilesWithRestrictPatterns(_par.SourceFileManager, _par.ReplacePairsSet.PairsDict);
            if (!changeFilesWithManyDots.Run())
            {
                return ValueTask.FromResult(false);
            }
        }

        int destinationFileMaxLength = _par.DestinationFileStorage.FileNameMaxLength == 0
            ? 255
            : _par.DestinationFileStorage.FileNameMaxLength;

        //თუ წყაროს ფოლდერი ცარელაა, გასაკეთებლი არაფერია
        if (!_par.SourceFileManager.IsFolderEmpty(null))
        {
            var prepareFolderFileNames = new PrepareFolderFileNames(_par.SourceFileManager, _par.UseMethod,
                _par.UploadTempExtension, _par.DownloadTempExtension, _par.ExcludeSet, destinationFileMaxLength);

            if (!prepareFolderFileNames.Run())
            {
                return ValueTask.FromResult(false);
            }

            var copyAndReplaceFiles = new CopyAndReplaceFiles(_logger, _par.SourceFileManager,
                _par.DestinationFileManager, _par.UseMethod, _par.UploadTempExtension, _par.DownloadTempExtension,
                _par.ExcludeSet, destinationFileMaxLength);

            if (!copyAndReplaceFiles.Run())
            {
                return ValueTask.FromResult(false);
            }
        }

        //თუ მიზნის ფოლდერი ცარელაა, გასაკეთებლი არაფერია
        if (!_par.DestinationFileManager.IsFolderEmpty(null))
        {
            var deleteRedundantFiles =
                new DeleteRedundantFiles(_par.SourceFileManager, _par.DestinationFileManager, _par.ExcludeSet);

            if (!deleteRedundantFiles.Run())
            {
                return ValueTask.FromResult(false);
            }
        }

        //ცარელა ფოლდერების წაშლა
        var emptyFoldersRemover = new EmptyFoldersRemover(_par.DestinationFileManager);
        return ValueTask.FromResult(emptyFoldersRemover.Run());
    }
}
