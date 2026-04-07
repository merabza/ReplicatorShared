using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ReplicatorShared.Data.ToolActions;

public sealed class UploadToolAction : ProcessesToolAction
{
    private readonly BackupFileParameters _backupFileParameters;
    private readonly UploadParameters _par;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadToolAction(ILogger logger, ProcessManager? processManager, UploadParameters par,
        BackupFileParameters backupFileParameters) : base(logger, null, null, processManager, "Upload",
        par.UploadProcLineId)
    {
        _par = par;
        _backupFileParameters = backupFileParameters;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        List<BuFileInfo> filesForUpload = _par.WorkFileManager.GetFilesByMask(_backupFileParameters.Prefix,
            _backupFileParameters.DateMask, _backupFileParameters.Suffix);

        List<BuFileInfo> filesAlreadyUploaded = _par.UploadFileManager.GetFilesByMask(_backupFileParameters.Prefix,
            _backupFileParameters.DateMask, _backupFileParameters.Suffix);

        var allFiles = new List<BuFileInfo>();
        allFiles.AddRange(filesForUpload);
        allFiles.AddRange(filesAlreadyUploaded);
        List<DateTime> preserveFileDates = _par.UploadSmartSchema.GetPreserveFileDates(allFiles);

        if (filesForUpload
            .Where(fileInfo => !_par.UploadFileManager.ContainsFile(fileInfo.FileName) &&
                               preserveFileDates.Contains(fileInfo.FileDateTime)).Any(fileInfo =>
                !_par.UploadFileManager.UploadFile(fileInfo.FileName, _par.UploadTempExtension)))
        {
            return ValueTask.FromResult(false);
        }

        _par.UploadFileManager.RemoveRedundantFiles(_backupFileParameters.Prefix, _backupFileParameters.DateMask,
            _backupFileParameters.Suffix, _par.UploadSmartSchema);
        return ValueTask.FromResult(true);
    }
}
