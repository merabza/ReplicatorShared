using System.Collections.Generic;

namespace ReplicatorShared.Data.Models;

public sealed class DuplicateFilesModel
{
    public List<FileModel> Files { get; set; } = [];
}
