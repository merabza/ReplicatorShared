using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ConnectionTools.ConnectTools;
using ReplicatorShared.Data.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace ReplicatorShared.Data.FolderProcessors;

public sealed class DuplicateFilesFinder : FolderProcessor
{
    //private readonly ConsoleFormatter _consoleFormatter;
    // The cryptographic service provider.
    private readonly SHA256 _sha256 = SHA256.Create();

    public DuplicateFilesFinder(FileManager fileManager) : base("DuplicatesRemover", "Find and remove duplicate files",
        fileManager, null, false, null, true, true)
    {
    }

    public FileListModel FileList { get; } = new();

    protected override bool CheckParameters()
    {
        if (FileManager is not DiskFileManager)
        {
            return false;
        }

        Console.WriteLine("Find Files");
        return true;
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        if (FileManager is not DiskFileManager dFileManager)
        {
            return false;
        }

        string fileFullName = dFileManager.GetPath(afterRootPath, file.FileName);

        Console.WriteLine($"Analyze file {fileFullName}");

        var fileModel = new FileModel(fileFullName, file.FileLength);
        FileList.Files.Add(fileModel);

        List<FileModel> filesWithSameSize = FileList.Files.Where(w => w.Size == fileModel.Size).ToList();

        if (filesWithSameSize.Count <= 1)
        {
            return true;
        }

        foreach (FileModel currentFileModel in filesWithSameSize)
        {
            currentFileModel.Sha256 ??= BytesToString(GetHashSha256(currentFileModel.FileFullName));
        }

        FileModel? lastFileModel = null;
        foreach (FileModel model in filesWithSameSize.OrderBy(o => o.Sha256))
        {
            if (lastFileModel?.Sha256 is not null && lastFileModel.Sha256 == model.Sha256)
            {
                //დავადგინოთ გვაქვს თუ არა საცავი ამ კოდისათვის.
                //და თუ არ არის შევქმნათ
                DuplicateFilesStorage currentFilesStorage;
                if (!FileList.DuplicateFilesStorage.TryGetValue(lastFileModel.Sha256, out DuplicateFilesStorage? value))
                {
                    currentFilesStorage = new DuplicateFilesStorage();
                    value = currentFilesStorage;
                    FileList.DuplicateFilesStorage.Add(lastFileModel.Sha256, value);
                }

                currentFilesStorage = value;

                //დავადგინოთ ეს ფაილები უკვე შედარებული გვაქვს თუ არა
                ComparedFilesModel? comparedFiles =
                    currentFilesStorage.GetComparedFiles(lastFileModel.FileFullName, model.FileFullName);

                if (comparedFiles == null)
                {
                    currentFilesStorage.AddComparedFiles(lastFileModel, model,
                        FileStat.FileCompare(lastFileModel.FileFullName, model.FileFullName));
                }
            }

            lastFileModel = model;
        }

        return true;
    }

    // Return a byte array as a sequence of hex values.
    private static string BytesToString(byte[] bytes)
    {
        return bytes.Aggregate(string.Empty, (current, b) => current + b.ToString("x2", CultureInfo.InvariantCulture));
    }

    // Compute the file's hash.
    private byte[] GetHashSha256(string fileName)
    {
        //_consoleFormatter.WriteInSameLine($"Get Hash Sha256 for file {fileName}");
        Console.WriteLine($"Get Hash Sha256 for file {fileName}");
        // ReSharper disable once using
        using FileStream stream = File.OpenRead(fileName);
        return _sha256.ComputeHash(stream);
    }
}
