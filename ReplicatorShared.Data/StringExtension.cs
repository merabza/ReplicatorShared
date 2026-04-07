using System.IO;
using System.Linq;

namespace ReplicatorShared.Data;

public static class StringExtension
{
    public const string AppAgentAppKey = "8959D94B-596E-48C1-A644-29667AEE2250";

    public static string PrepareFileName(this string fileName)
    {
        const string restrictedSymbols = "<>:\"/\\|?*'«»";
        return fileName.Intersect(restrictedSymbols)
            .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }

    public static string PreparedFileNameConsideringLength(this string fileName, int fileMaxLength)
    {
        string preparedFileName = fileName.PrepareFileName().Trim();
        string extension = Path.GetExtension(preparedFileName).Trim();
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(preparedFileName).Trim();
        preparedFileName = fileNameWithoutExtension.GetNewFileNameWithMaxLength(0, extension, fileMaxLength);
        return preparedFileName;
    }

    public static string GetNewFileName(this string fileNameWithoutExtension, int i, string fileExtension)
    {
        return $"{fileNameWithoutExtension}{(i == 0 ? string.Empty : $"({i})")}{fileExtension}";
    }

    public static string GetNewFileNameWithMaxLength(this string fileNameWithoutExtension, int i, string fileExtension,
        int maxLength = 255)
    {
        string oneTry = fileNameWithoutExtension.GetNewFileName(i, fileExtension);
        int more = oneTry.Length - maxLength;
        if (more <= 0)
        {
            return oneTry;
        }

        int take = fileNameWithoutExtension.Length - more;
        oneTry = fileNameWithoutExtension[..take].GetNewFileName(i, fileExtension);
        return oneTry;
    }
}
