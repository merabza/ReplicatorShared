namespace ReplicatorShared.Data.Models;

public sealed class JobStepBySchedule
{
    public JobStepBySchedule(string jobStepName, string scheduleName, int sequentialNumber)
    {
        JobStepName = jobStepName;
        ScheduleName = scheduleName;
        SequentialNumber = sequentialNumber;
    }

    public string JobStepName { get; }
    public string ScheduleName { get; }
    public int SequentialNumber { get; set; }
}
