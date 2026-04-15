using System.IO;

namespace ReciteHelper.Utils;

public static class Extensions
{
    /// <summary>
    /// Finds the next non-whitespace character starting from the specified index.
    /// </summary>
    /// <param name="str">The string to search.</param>
    /// <param name="index">The starting index.</param>
    /// <returns>The next non-whitespace character, or space if none found.</returns>
    public static char FindNextChar(this string str, int index)
    {
        for (int i = index; i < str.Length; i++)
            if (!char.IsWhiteSpace(str[i])) return str[i];
        return ' ';
    }
}

public static class DirectoryExtensions
{
    /// <summary>
    /// Deletes all files in the specified directory.
    /// </summary>
    /// <remarks>This method removes all files directly within the specified directory but does not
    /// delete subdirectories or their contents. If a file cannot be deleted (for example, due to being in use), the
    /// method continues processing the remaining files.</remarks>
    /// <param name="targetDirectory">The full path of the directory whose files are to be deleted. Cannot be null or an empty string.</param>
    public static void Clear(string targetDirectory)
    {
        var files = Directory.GetFiles(targetDirectory, "*");
        foreach (string file in files)
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch (IOException)
            {
                // ignored
                Console.WriteLine($"无为在歧路，儿女共沾巾。");
            }
        }
    }
}
