using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data;

public sealed class ProcLogFile
{
    private readonly string _dateMask;
    private readonly string _extension;
    private readonly TimeSpan _holeEndTime;
    private readonly TimeSpan _holeStartTime;
    private readonly ILogger _logger;
    private readonly MaskManager _maskManager;
    private readonly EPeriodType _periodType;
    private readonly string _processName;
    private readonly DateTime _startAt;
    private readonly Lock _syncObject = new();
    private readonly bool _useConsole;
    private readonly FileManager _workFileManager;
    private readonly string _workFolder;
    private string? _justCreatedFileName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProcLogFile(bool useConsole, ILogger logger, string processName, EPeriodType periodType, DateTime startAt,
        TimeSpan holeStartTime, TimeSpan holeEndTime, string workFolder, FileManager workFileManager,
        string dateMask = "yyyy_MM_dd_HHmmss_fffffff", string extension = ".log")
    {
        _extension = extension.AddNeedLeadPart(".");
        _useConsole = useConsole;
        _logger = logger;
        _processName = processName;
        _periodType = periodType;
        _startAt = startAt;
        _holeStartTime = holeStartTime;
        _holeEndTime = holeEndTime;
        _workFolder = workFolder;
        _workFileManager = workFileManager;
        _dateMask = dateMask;
        _maskManager = new MaskManager(processName, dateMask);
    }

    public void CreateNow(string withText)
    {
        string fileName = Path.Combine(_workFolder, _maskManager.GetFileNameForDate(DateTime.Now, _extension));
        var checkFile = new FileInfo(fileName);
        if (checkFile.Directory is { Exists: false })
        {
            checkFile.Directory.Create();
        }

        if (checkFile.Directory is not { Exists: true })
        {
            lock (_syncObject)
            {
                _logger.LogError("File {FileName} cannot be created", fileName);
            }

            return;
        }

        lock
            (_syncObject) //ამ ბრძანების გამოყენებით მივაღწიეთ იმას, რომ სხვადასხვა ნაკადი ერთდროულად არ მიმართავს ჩასაწერად ფაილს
        {
            try
            {
                // ReSharper disable once using
                // ReSharper disable once DisposableConstructor
                using var sw = new StreamWriter(fileName, true, Encoding.UTF8);
                try
                {
                    sw.WriteLine(withText);
                }
                catch (Exception ex)
                {
                    StShared.WriteException(ex, _useConsole, _logger);
                }
                finally
                {
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                StShared.WriteException(ex, _useConsole, _logger);
            }
        }

        _justCreatedFileName = fileName;
    }

    public void DeleteOldFiles()
    {
        var wfd = new DirectoryInfo(_workFolder);
        foreach (FileInfo file in wfd.GetFiles(_maskManager.GetFullMask(_extension)))
        {
            if (file.FullName == _justCreatedFileName)
            {
                continue;
            }

            file.Delete();
        }
    }

    internal bool HaveCurrentPeriodFile()
    {
        var currentPeriodFileChecker = new CurrentPeriodFileChecker(_periodType, _startAt, _holeStartTime, _holeEndTime,
            _processName, _dateMask, _extension, _workFileManager);
        return currentPeriodFileChecker.HaveCurrentPeriodFile();
    }
}
