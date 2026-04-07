using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReplicatorShared.Data.Models;

public sealed class DuplicateFilesStorage
{
    private readonly List<ComparedFilesModel> _comparedFiles = [];
    private readonly List<DuplicateFilesModel> _duplicateFiles = [];

    public ComparedFilesModel? GetComparedFiles(string fileFullName, string modelFileFullName)
    {
        Tuple<string, string> fileNamesTuple = GetOrderedTuple(fileFullName, modelFileFullName);
        return _comparedFiles.SingleOrDefault(s =>
            s.FirstFile.FileFullName == fileNamesTuple.Item1 && s.SecondFile.FileFullName == fileNamesTuple.Item2);
    }

    private static Tuple<string, string> GetOrderedTuple(string file1, string file2)
    {
        return string.CompareOrdinal(file1, file2) < 1
            ? new Tuple<string, string>(file1, file2)
            : new Tuple<string, string>(file2, file1);
    }

    private static Tuple<FileModel, FileModel> GetOrderedTuple(FileModel fileModel1, FileModel fileModel2)
    {
        return string.CompareOrdinal(fileModel1.FileFullName, fileModel2.FileFullName) < 1
            ? new Tuple<FileModel, FileModel>(fileModel1, fileModel2)
            : new Tuple<FileModel, FileModel>(fileModel2, fileModel1);
    }

    public void AddComparedFiles(FileModel fileModel1, FileModel fileModel2, bool isEqual)
    {
        Tuple<FileModel, FileModel> fileNamesTuple = GetOrderedTuple(fileModel1, fileModel2);
        _comparedFiles.Add(new ComparedFilesModel(fileNamesTuple.Item1, fileNamesTuple.Item2, isEqual));
    }

    public void CountMultiDuplicates()
    {
        foreach (ComparedFilesModel comparedFiles in _comparedFiles.Where(x => x.IsEqual))
        {
            FileModel? file1 = null;
            FileModel? file2 = null;
            foreach (DuplicateFilesModel duplicateFiles in _duplicateFiles)
            {
                file1 =
                    duplicateFiles.Files.SingleOrDefault(s => s.FileFullName == comparedFiles.FirstFile.FileFullName);
                file2 = duplicateFiles.Files.SingleOrDefault(s =>
                    s.FileFullName == comparedFiles.SecondFile.FileFullName);
                if (file1 != null && file2 == null)
                {
                    duplicateFiles.Files.Add(comparedFiles.SecondFile);
                    break;
                }

                if (file1 != null || file2 == null)
                {
                    continue;
                }

                duplicateFiles.Files.Add(comparedFiles.FirstFile);
                break;
            }

            if (file1 == null && file2 == null)
            {
                _duplicateFiles.Add(new DuplicateFilesModel
                {
                    Files = [comparedFiles.FirstFile, comparedFiles.SecondFile]
                });
            }
        }
    }

    public void RemoveDuplicates(List<string> priorityList)
    {
        foreach (DuplicateFilesModel duplicateFiles in _duplicateFiles)
        {
            if (duplicateFiles.Files.Count < 2)
            {
                continue;
            }

            FileModel? saveFile = null;
            List<FileModel> priorityFiles = [];
            if (priorityList is not null)
            {
                foreach (string p in priorityList)
                {
                    priorityFiles.AddRange(duplicateFiles.Files.Where(w =>
                        w.FileFullName.StartsWith(p, StringComparison.Ordinal)));
                }
            }

            if (priorityFiles.Count > 0)
            {
                saveFile = priorityFiles[0];
            }

            if (saveFile == null)
            {
                List<FileModel> orderedAllFiles = duplicateFiles.Files.OrderBy(o => o.FileFullName.Length)
                    .ThenBy(o => o.FileFullName).ToList();
                saveFile = orderedAllFiles[0];
            }

            //if (saveFile == null)
            //    continue;

            foreach (FileModel fileModel in duplicateFiles.Files.Where(w => w.FileFullName != saveFile.FileFullName))
            {
                File.Delete(fileModel.FileFullName);
            }
        }
    }
}
