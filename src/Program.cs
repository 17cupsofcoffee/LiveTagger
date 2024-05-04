using System.CommandLine;

namespace LiveTagger;

class Program
{
    static void Main(string[] args)
    {
        var commitOption = new Option<bool>(["--commit", "-c"], "Saves changes to the filesystem. Run without this first, to make sure you're tagging the correct files!");

        var rootCommand = new RootCommand("LiveTagger");
        rootCommand.AddGlobalOption(commitOption);

        var tagsArg = new Argument<List<string>>("tags", "The tags to apply to the matched files.");
        var dirOption = new Option<string>(["--dir", "-d"], () => ".", "The directory to process.");
        var recursiveOption = new Option<bool>(["--recursive", "-r"], () => false, "Process files that are in subfolders.");

        var addTagCommand = new Command("add", "Adds tags to a set of files.");
        addTagCommand.AddArgument(tagsArg);
        addTagCommand.AddOption(dirOption);
        addTagCommand.AddOption(recursiveOption);

        addTagCommand.SetHandler(addTags, dirOption, tagsArg, recursiveOption, commitOption);

        rootCommand.AddCommand(addTagCommand);

        var removeTagCommand = new Command("remove", "Removes tags from a set of files.");
        removeTagCommand.AddArgument(tagsArg);
        removeTagCommand.AddOption(dirOption);
        removeTagCommand.AddOption(recursiveOption);

        removeTagCommand.SetHandler(removeTags, dirOption, tagsArg, recursiveOption, commitOption);

        rootCommand.AddCommand(removeTagCommand);

        var removeAllTagsCommand = new Command("remove-all", "Removes all tags from a set of files.");
        removeAllTagsCommand.AddOption(dirOption);
        removeAllTagsCommand.AddOption(recursiveOption);

        removeAllTagsCommand.SetHandler(removeAllTags, dirOption, recursiveOption, commitOption);

        rootCommand.AddCommand(removeAllTagsCommand);

        rootCommand.Invoke(args);
    }

    /// <summary>
    /// Adds the specified tags to all files under the given parent directory. 
    /// </summary>
    /// <param name="dir">The directory to process.</param>
    /// <param name="tags">The tags to add.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void addTags(string dir, List<string> tags, bool recursive, bool commit)
    {
        processXmp(dir, recursive, commit, (xmp, files) => xmp.AddTags(files, tags));
    }

    /// <summary>
    /// Removes the specified tags from all files under the given parent directory. 
    /// </summary>
    /// <param name="dir">The directory to process.</param>
    /// <param name="tags">The tags to remove.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void removeTags(string dir, List<string> tags, bool recursive, bool commit)
    {
        processXmp(dir, recursive, commit, (xmp, files) => xmp.RemoveTags(files, tags));
    }

    /// <summary>
    /// Removes all tags from all files under the given parent directory. 
    /// </summary>
    /// <param name="dir">The directory to process.</param>
    /// <param name="tags">The tags to remove.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void removeAllTags(string dir, bool recursive, bool commit)
    {
        processXmp(dir, recursive, commit, (xmp, files) => xmp.RemoveTags(files));
    }

    /// <summary>
    /// Run an action on the XMP files for a given directory.
    /// </summary>
    /// <param name="dir">The directory to process.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    /// <param name="action">The action to run.</param>
    private static void processXmp(string dir, bool recursive, bool commit, Action<Xmp, List<string>> action)
    {
        var folders = searchForFiles(dir, recursive);

        foreach (var (folder, files) in folders)
        {
            var xmpFilePath = AbletonMetadata.GetXmpFilePath(folder);
            Xmp xmp;

            if (Path.Exists(xmpFilePath))
            {
                Console.WriteLine($"=== Updating existing metadata for {folder} ===\n");
                xmp = Xmp.FromFile(xmpFilePath);
            }
            else
            {
                Console.WriteLine($"=== Creating new metadata for {folder} ===\n");
                xmp = new Xmp();
            }

            action(xmp, files);

            if (commit)
            {
                xmp.Save(xmpFilePath);
            }

            Console.WriteLine();
        }

        if (commit)
        {
            Console.WriteLine("Done!");
        }
        else
        {
            Console.WriteLine("Re-run with --commit to apply the above changes.");
        }
    }

    /// <summary>
    /// Searches for files to process.
    /// </summary>
    /// <param name="path">The path to search.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <returns>A mapping of directories to individual files.</returns>
    private static Dictionary<string, List<string>> searchForFiles(string path, bool recursive)
    {
        var folders = new Dictionary<string, List<string>>();
        var searchOption = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        foreach (string file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            if (AbletonMetadata.IsMetadata(file))
            {
                continue;
            }

            var parent = Directory.GetParent(file)!.FullName;
            var filename = Path.GetFileName(file);

            if (folders.ContainsKey(parent))
            {
                folders[parent].Add(filename);
            }
            else
            {
                folders.Add(parent, new List<string>() { filename });
            }
        }

        return folders;
    }

}
