namespace ReplicatorShared.Data.Models;

public sealed class FileModel
{
    public FileModel(string fileFullName, long size)
    {
        FileFullName = fileFullName;
        Size = size;
    }

    public string FileFullName { get; set; }
    public long Size { get; set; }
    public string? Sha256 { get; set; }
}
