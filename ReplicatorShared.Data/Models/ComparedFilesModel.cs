namespace ReplicatorShared.Data.Models;

public sealed class ComparedFilesModel
{
    public ComparedFilesModel(FileModel firstFile, FileModel secondFile, bool isEqual)
    {
        FirstFile = firstFile;
        SecondFile = secondFile;
        IsEqual = isEqual;
    }

    public FileModel FirstFile { get; }
    public FileModel SecondFile { get; }
    public bool IsEqual { get; }
}
