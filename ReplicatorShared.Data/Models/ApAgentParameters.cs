using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Interfaces;
using ParametersManagement.LibFileParameters.Models;
using ReplicatorShared.Data.Steps;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Models;

public sealed class ApAgentParameters : IParametersWithFileStorages, IParametersWithDatabaseServerConnections,
    IParametersWithApiClients, IParametersWithSmartSchemas, IParametersWithArchivers, IParametersWithExcludeSets
{
    public const string DefaultUploadFileTempExtension = ".up!";
    public const string DefaultDownloadFileTempExtension = ".down!";
    public const string DefaultArchivingFileTempExtension = ".go!";
    public const string DefaultDateMask = "yyyyMMddHHmmss";

    public string? LogFolder { get; set; }
    public string? WorkFolder { get; set; }
    public string? ProcLogFilesFolder { get; set; }
    public string? ApAgentParametersFileNameForLocalReServer { get; set; }

    public string UploadFileTempExtension
    {
        get => field ?? DefaultUploadFileTempExtension;
        set;
    } //.up!

    public string DownloadFileTempExtension
    {
        get => field ?? DefaultDownloadFileTempExtension;
        set;
    } //.down!

    public string ArchivingFileTempExtension
    {
        get => field ?? DefaultArchivingFileTempExtension;
        set;
    } //.go!

    public string DateMask
    {
        get => field ?? DefaultDateMask;
        set;
    } //.go!

    public Dictionary<string, ReplacePairsSet> ReplacePairsSets { get; set; } = [];
    public Dictionary<string, JobSchedule> JobSchedules { get; set; } = [];
    public Dictionary<string, DatabaseBackupStep> DatabaseBackupSteps { get; set; } = [];
    public Dictionary<string, MultiDatabaseProcessStep> MultiDatabaseProcessSteps { get; set; } = [];
    public Dictionary<string, RunProgramStep> RunProgramSteps { get; set; } = [];
    public Dictionary<string, ExecuteSqlCommandStep> ExecuteSqlCommandSteps { get; set; } = [];
    public Dictionary<string, FilesBackupStep> FilesBackupSteps { get; set; } = [];
    public Dictionary<string, FilesSyncStep> FilesSyncSteps { get; set; } = [];
    public Dictionary<string, FilesMoveStep> FilesMoveSteps { get; set; } = [];
    public Dictionary<string, UnZipOnPlaceStep> UnZipOnPlaceSteps { get; set; } = [];
    public List<JobStepBySchedule> JobsBySchedules { get; set; } = [];
    public Dictionary<string, ApiClientSettings> ApiClients { get; set; } = [];
    public Dictionary<string, ArchiverData> Archivers { get; set; } = [];
    public Dictionary<string, DatabaseServerConnectionData> DatabaseServerConnections { get; set; } = [];
    public Dictionary<string, ExcludeSet> ExcludeSets { get; set; } = [];
    public Dictionary<string, FileStorageData> FileStorages { get; set; } = [];

    public bool CheckBeforeSave()
    {
        Dictionary<string, JobStep> steps = GetSteps();
        List<string> jobStepNames = JobsBySchedules.Select(s => s.JobStepName).ToList();

        List<string> missingJobStepNames = jobStepNames.Except(steps.Keys).ToList();

        List<JobStepBySchedule> jb = missingJobStepNames
            .Select(missingJobStepName => JobsBySchedules.Where(x => x.JobStepName == missingJobStepName))
            .SelectMany(jbs => jbs).ToList();
        foreach (JobStepBySchedule j in jb)
        {
            JobsBySchedules.Remove(j);
        }

        return true;
    }

    public Dictionary<string, SmartSchema> SmartSchemas { get; set; } = [];

    public string? CountLocalPath(string? currentPath, string? parametersFileName, string defaultFolderName)
    {
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            return currentPath;
        }

        FileInfo? pf = string.IsNullOrWhiteSpace(parametersFileName) ? null : new FileInfo(parametersFileName);
        string? workFolder = WorkFolder ?? pf?.Directory?.FullName;
        string? workFolderCandidate = workFolder is null ? null : Path.Combine(workFolder, defaultFolderName);
        return workFolderCandidate;
    }

    //public string GetDownloadFileTempExtension()
    //{
    //    return DownloadFileTempExtension ?? DefaultDownloadFileTempExtension;
    //}

    //public string GetArchivingFileTempExtension()
    //{
    //    return ArchivingFileTempExtension ?? DefaultArchivingFileTempExtension;
    //}

    //public string GetDateMask()
    //{
    //    return DateMask ?? DefaultDateMask;
    //}

    public Dictionary<string, JobStep> GetSteps()
    {
        Dictionary<string, JobStep> steps =
            DatabaseBackupSteps.ToDictionary<KeyValuePair<string, DatabaseBackupStep>, string, JobStep>(kvp => kvp.Key,
                kvp => kvp.Value);

        foreach (KeyValuePair<string, MultiDatabaseProcessStep> kvp in MultiDatabaseProcessSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<string, RunProgramStep> kvp in RunProgramSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<string, ExecuteSqlCommandStep> kvp in ExecuteSqlCommandSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<string, FilesBackupStep> kvp in FilesBackupSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<string, FilesSyncStep> kvp in FilesSyncSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<string, FilesMoveStep> kvp in FilesMoveSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<string, UnZipOnPlaceStep> kvp in UnZipOnPlaceSteps)
        {
            steps.Add(kvp.Key, kvp.Value);
        }

        return steps;
    }

    public void ClearAll()
    {
        ClearSteps();
        Archivers.Clear();
        JobSchedules.Clear();
        SmartSchemas.Clear();
        ExcludeSets.Clear();
        FileStorages.Clear();
        DatabaseServerConnections.Clear();
    }

    public void ClearSteps()
    {
        JobsBySchedules.Clear();
        FilesBackupSteps.Clear();
        ExecuteSqlCommandSteps.Clear();
        RunProgramSteps.Clear();
        MultiDatabaseProcessSteps.Clear();
        DatabaseBackupSteps.Clear();
    }

    public Dictionary<string, JobSchedule> GetNotStartUpJobSchedules()
    {
        Dictionary<string, JobStep> steps = GetSteps();

        //&& s.jsFreqType != EFreqTypes.WhenCpuIdle 
        return JobSchedules
            .Where(w => w.Value.ScheduleType != EScheduleType.AtStart && w.Value.Enabled &&
                        JobsBySchedules.Any(j => steps.ContainsKey(j.JobStepName) && steps[j.JobStepName].Enabled))
            .ToDictionary(k => k.Key, v => v.Value);
        //&& s.JobsRow != null && s.JobsRow.jobEnabled && s.JobsRow.GetJobStepsRows().Any(j => j.jsEnabled));
    }

    public Dictionary<string, JobSchedule> GetStartUpJobSchedules(bool byTime,
        Dictionary<string, DateTime> nextRunDatesByScheduleNames)
    {
        DateTime nowDateTime = DateTime.Now;
        return JobSchedules.Where(delegate(KeyValuePair<string, JobSchedule> w)
        {
            DateTime nextRunDate = nextRunDatesByScheduleNames.GetValueOrDefault(w.Key);
            return (byTime
                ? w.Value.ScheduleType != EScheduleType.AtStart && nextRunDate != default && nextRunDate <= nowDateTime
                : w.Value.ScheduleType == EScheduleType.AtStart) && w.Value.Enabled;
        }).ToDictionary(k => k.Key, v => v.Value);
    }

    public bool RunAllSteps(ILogger logger, IHttpClientFactory httpClientFactory, bool useConsole, string scheduleName,
        IProcesses processes, string procLogFilesFolder)
    {
        if (!JobSchedules.ContainsKey(scheduleName))
        {
            StShared.WriteErrorLine($"Schedules with name {scheduleName} not found", true, logger);
        }

        //თუ აქ მოვედით შედულეს ბარიერი გავლილია, ან პირდაპირ არის მოთხოვნილი ამ შედულეს შესაბამისი ჯობების გაშევბა
        //შედულეს ბარიერის რეალიზება უნდა მოხდეს ბექპროცესის ტაიმერში
        Dictionary<string, JobStep> steps = GetSteps();
        List<string> jobStepNames = JobsBySchedules.Where(s => s.ScheduleName == scheduleName)
            .OrderBy(o => o.SequentialNumber).Select(s => s.JobStepName).ToList();

        List<string> missingJobStepNames = jobStepNames.Except(steps.Keys).ToList();

        if (missingJobStepNames.Count <= 0)
        {
            // ReSharper disable once using
            using ProcessManager processManager = processes.GetNewProcessManager();
            try
            {
                foreach (ProcessesToolAction? stepToolAction in jobStepNames.Select(name =>
                             steps[name].GetToolAction(logger, httpClientFactory, useConsole, processManager, this,
                                 procLogFilesFolder)))
                {
                    if (stepToolAction is not null)
                    {
                        processManager.Run(stepToolAction);
                    }
                }

                return true;
            }
            catch (OperationCanceledException e)
            {
                logger.LogError(e, "OperationCanceledException");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception");
            }

            return false;
        }

        foreach (string stepName in missingJobStepNames)
        {
            StShared.WriteErrorLine($"Step with name {stepName} not found", true, logger);
        }

        return false;
    }
}
