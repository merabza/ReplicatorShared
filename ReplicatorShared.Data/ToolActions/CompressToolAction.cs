using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ReplicatorShared.Data.ToolActions;

public sealed class CompressToolAction : ProcessesToolAction
{
    private readonly BackupFileParameters _backupFileParameters;
    private readonly SmartSchema _localSmartSchema;
    private readonly ILogger _logger;
    private readonly CompressParameters? _par;
    private readonly FileStorageData _uploadFileStorage;
    private readonly UploadParameters _uploadParameters;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CompressToolAction(ILogger logger, bool useConsole, ProcessManager? processManager, CompressParameters? par,
        UploadParameters uploadParameters, BackupFileParameters backupFileParameters, int compressProcLine,
        SmartSchema localSmartSchema, FileStorageData uploadFileStorage) : base(logger, null, null, processManager,
        "Compress Backup", compressProcLine)
    {
        _logger = logger;
        _useConsole = useConsole;
        _par = par;
        _uploadParameters = uploadParameters;
        _backupFileParameters = backupFileParameters;
        _localSmartSchema = localSmartSchema;
        _uploadFileStorage = uploadFileStorage;
    }

    public override ProcessesToolAction? GetNextAction()
    {
        var uploadToolAction = new UploadToolAction(_logger, ProcessManager, _uploadParameters, _backupFileParameters);

        return NeedUpload(_uploadFileStorage) ? uploadToolAction : uploadToolAction.GetNextAction();
    }

    private bool NeedUpload(FileStorageData uploadFileStorage)
    {
        if (uploadFileStorage.FileStoragePath is null)
        {
            StShared.WriteWarningLine("uploadFileStorage.FileStoragePath does not specified", _useConsole, _logger);
            return false;
        }

        //თუ ასატვირთი ფაილსაცავი ქსელურია, აქაჩვა გვჭირდება
        if (!FileStat.IsFileSchema(uploadFileStorage.FileStoragePath))
        {
            return true;
        }

        //თუ ატვირთვის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა ლოკალურ ფოლდერს
        //მაშინ აქაჩვა არ გვჭირდება
        //თუ ფოლდერები არ ემთხვევა აქაჩვა გვჭირდება
        return FileStat.NormalizePath(_uploadParameters.LocalPath) !=
               FileStat.NormalizePath(uploadFileStorage.FileStoragePath);
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (_par is null)
        {
            return ValueTask.FromResult(true);
        }

        List<BuFileInfo> filesForCompress = _par.WorkFileManager.GetFilesByMask(_backupFileParameters.Prefix,
            _backupFileParameters.DateMask, _backupFileParameters.Suffix);

        foreach (BuFileInfo fileInfo in filesForCompress)
        {
            string sourceFileName = Path.Combine(_par.WorkPath, fileInfo.FileName);
            string destinationFileFullName = sourceFileName + _par.Archiver.FileExtension;
            string tempFileName = destinationFileFullName + _par.ArchiveTempExtension;
            if (!_par.Archiver.PathToArchive(sourceFileName, tempFileName))
            {
                File.Delete(tempFileName);
                return ValueTask.FromResult(false);
            }

            File.Delete(destinationFileFullName);
            File.Move(tempFileName, destinationFileFullName);
            File.Delete(sourceFileName);
        }

        //დაგროვილი ზედმეტი ფაილების წაშლა
        _par.WorkFileManager.RemoveRedundantFiles(_backupFileParameters.Prefix, _backupFileParameters.DateMask,
            _backupFileParameters.Suffix + _par.Archiver.FileExtension, _localSmartSchema);

        //გავასწოროთ სუფიქსი, რადგან ფაილები დაარქივდა
        _backupFileParameters.Suffix += _par.Archiver.FileExtension;

        return ValueTask.FromResult(true);
    }
}
