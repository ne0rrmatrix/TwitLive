using System.Diagnostics;

namespace TwitLive.Services;

public static class FileService
{
    public static void DeleteFile(string url)
    {
        var tempFile = GetFileName(url);
        try
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
                Debug.WriteLine($"Deleted file {tempFile}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    /// <summary>
    /// Get file name from Url <see cref="string"/>
    /// </summary>
    /// <param name="url">A URL <see cref="string"/></param>
    /// <returns>Filename <see cref="string"/> with file extension</returns>
    public static string GetFileName(string url)
    {
        var temp = new Uri(url).LocalPath;
        return Path.GetFileName(temp);
    }
    public static string GetFilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }
}