namespace LiveTagger;

/// <summary>
/// Helper functions for dealing with Ableton metadata files.
/// </summary>
public static class AbletonMetadata
{
    /// <summary>
    /// Returns whether or not a path represents some kind of Ableton metadata.
    /// 
    /// This is not comprehensive - it's mainly just used to exclude files from
    /// the tagging process.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>Whether or not the path is for metadata.</returns>
    public static bool IsMetadata(string path)
    {
        return path.Contains("Ableton Folder Info") || Path.GetExtension(path) == ".asd";
    }

    /// <summary>
    /// Returns the XMP metadata path for a given folder.
    /// </summary>
    /// <param name="folder">The folder to find the metadata path for.</param>
    /// <returns>The metadata path.</returns>
    public static string GetXmpFilePath(string folder)
    {
        return Path.Join(folder, "Ableton Folder Info/dc66a3fa-0fe1-5352-91cf-3ec237e9ee90.xmp");
    }
}