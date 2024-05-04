using System.CommandLine;

namespace LiveTagger;

class Program
{
    static void Main(string[] args)
    {
        var commitOption = new Option<bool>("--commit", ".");

        var rootCommand = new RootCommand("LiveTagger");
        rootCommand.AddGlobalOption(commitOption);

        var tagsOption = new Option<List<string>>("--tag", "The tag to apply to the matched files. This option can be repeated to add multiple tags at once.") { IsRequired = true };
        var filesOption = new Option<string>("--files", "The files to tag.") { IsRequired = true };

        var addTagCommand = new Command("add", "Adds tags to the specified files.");
        addTagCommand.AddOption(filesOption);
        addTagCommand.AddOption(tagsOption);

        addTagCommand.SetHandler(addTags, filesOption, tagsOption, commitOption);

        rootCommand.AddCommand(addTagCommand);

        var removeTagCommand = new Command("remove", "Removes tags from the specified files.");
        removeTagCommand.AddOption(filesOption);
        removeTagCommand.AddOption(tagsOption);

        removeTagCommand.SetHandler(removeTags, filesOption, tagsOption, commitOption);

        rootCommand.AddCommand(removeTagCommand);

        rootCommand.Invoke(args);
    }

    /// <summary>
    /// Adds the specified tags to all files under the given parent directory. 
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <param name="tags">The tags to add.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void addTags(string path, List<string> tags, bool commit)
    {
        processXmp(path, commit, (xmp, files) => xmp.AddTags(files, tags));
    }

    /// <summary>
    /// Removes the specified tags from all files under the given parent directory. 
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <param name="tags">The tags to remove.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void removeTags(string path, List<string> tags, bool commit)
    {
        processXmp(path, commit, (xmp, files) => xmp.RemoveTags(files, tags));
    }

    /// <summary>
    /// Run an action on the XMP files for a given directory.
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    /// <param name="action">The action to run.</param>
    private static void processXmp(string path, bool commit, Action<Xmp, List<string>> action)
    {
        var folders = searchForFiles(path);

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
    /// <returns>A mapping of directories to individual files.</returns>
    private static Dictionary<string, List<string>> searchForFiles(string path)
    {
        var folders = new Dictionary<string, List<string>>();

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
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
