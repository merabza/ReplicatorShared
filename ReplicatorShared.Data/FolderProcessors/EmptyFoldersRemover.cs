using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class EmptyFoldersRemover : FolderProcessor
{
    public EmptyFoldersRemover(FileManager fileManager) : base("Empty Folders Remover", "Removes Empty Folders",
        fileManager, null, true, null, true, false)
    {
    }
}
