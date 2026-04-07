using System;
using System.Collections.Generic;
using System.Linq;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

namespace ReplicatorShared.Data;

public sealed class CurrentPeriodFileChecker
{
    private readonly string _dateMask;
    private readonly TimeSpan _holeEndTime;
    private readonly TimeSpan _holeStartTime;
    private readonly EPeriodType _periodType;
    private readonly string _prefix;
    private readonly DateTime _startAt;
    private readonly string _suffix;
    private readonly FileManager _workFileManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CurrentPeriodFileChecker(EPeriodType periodType, DateTime startAt, TimeSpan holeStartTime,
        TimeSpan holeEndTime, string prefix, string dateMask, string suffix, FileManager workFileManager)
    {
        _periodType = periodType;
        _startAt = startAt;
        _holeStartTime = holeStartTime;
        _holeEndTime = holeEndTime;
        _prefix = prefix;
        _dateMask = dateMask;
        _suffix = suffix;
        _workFileManager = workFileManager;
    }

    public bool HaveCurrentPeriodFile()
    {
        DateTime nowDateTime = DateTime.Now;
        DateTime endTime = DateTime.Today + _holeEndTime;

        if (endTime < nowDateTime)
        {
            return true;
        }

        DateTime start = _startAt + _holeStartTime;
        DateTime currentPeriodStart = start.DateAdd(_periodType, nowDateTime.DateDiff(_periodType, start));
        DateTime currentPeriodEnd = currentPeriodStart.DateAdd(_periodType, 1);
        List<BuFileInfo> files = _workFileManager.GetFilesByMask(_prefix, _dateMask, _suffix);
        return files.Any(s => s.FileDateTime >= currentPeriodStart && s.FileDateTime < currentPeriodEnd);
    }
}
