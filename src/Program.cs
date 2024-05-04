using System.CommandLine;
using Ganss.IO;

namespace LiveTagger;

class Program
{
    static void Main(string[] args)
    {
        var commitOption = new Option<bool>(["--commit", "-c"], "Saves changes to the filesystem. Run without this first, to make sure you're tagging the correct files!");

        var rootCommand = new RootCommand("LiveTagger");
        rootCommand.AddGlobalOption(commitOption);

        var tagsArg = new Argument<List<string>>("tags", "The tags to apply to the matched files.");
        var includeOption = new Option<string>(["--include", "-i"], () => "*", "A glob pattern specifying which files should be processed.");

        var addTagCommand = new Command("add", "Adds tags to a set of files.");
        addTagCommand.AddArgument(tagsArg);
        addTagCommand.AddOption(includeOption);

        addTagCommand.SetHandler(addTags, includeOption, tagsArg, commitOption);

        rootCommand.AddCommand(addTagCommand);

        var removeTagCommand = new Command("remove", "Removes tags from a set of files.");
        removeTagCommand.AddArgument(tagsArg);
        removeTagCommand.AddOption(includeOption);

        removeTagCommand.SetHandler(removeTags, includeOption, tagsArg, commitOption);

        rootCommand.AddCommand(removeTagCommand);

        var removeAllTagsCommand = new Command("remove-all", "Removes all tags from a set of files.");
        removeAllTagsCommand.AddOption(includeOption);

        removeAllTagsCommand.SetHandler(removeAllTags, includeOption, commitOption);

        rootCommand.AddCommand(removeAllTagsCommand);

        rootCommand.Invoke(args);
    }

    /// <summary>
    /// Adds the specified tags to all files under the given parent directory. 
    /// </summary>
    /// <param name="include">A glob pattern specifying which files should be processed.</param>
    /// <param name="tags">The tags to add.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void addTags(string include, List<string> tags, bool commit)
    {
        processXmp(include, commit, (xmp, files) => xmp.AddTags(files, tags));
    }

    /// <summary>
    /// Removes the specified tags from all files under the given parent directory. 
    /// </summary>
    /// <param name="include">A glob pattern specifying which files should be processed.</param>
    /// <param name="tags">The tags to remove.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void removeTags(string include, List<string> tags, bool commit)
    {
        processXmp(include, commit, (xmp, files) => xmp.RemoveTags(files, tags));
    }

    /// <summary>
    /// Removes all tags from all files under the given parent directory. 
    /// </summary>
    /// <param name="include">A glob pattern specifying which files should be processed.</param>
    /// <param name="tags">The tags to remove.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    private static void removeAllTags(string include, bool commit)
    {
        processXmp(include, commit, (xmp, files) => xmp.RemoveTags(files));
    }

    /// <summary>
    /// Run an action on the XMP files for a given directory.
    /// </summary>
    /// <param name="include">A glob pattern specifying which files should be processed.</param>
    /// <param name="commit">Whether changes should be saved.</param>
    /// <param name="action">The action to run.</param>
    private static void processXmp(string include, bool commit, Action<Xmp, List<string>> action)
    {
        var folders = searchForFiles(include);

        foreach (var (folder, files) in folders)
        {
            Console.WriteLine($"Processing {folder}.");

            var xmpFilePath = AbletonMetadata.GetXmpFilePath(folder);

            Xmp xmp = Path.Exists(xmpFilePath)
                ? Xmp.FromFile(xmpFilePath)
                : new Xmp();

            action(xmp, files);

            if (xmp.IsDirty)
            {
                if (commit)
                {
                    xmp.Save(xmpFilePath);
                }

                Console.WriteLine($"Metadata updated for {folder}.");
            }
            else
            {
                Console.WriteLine($"No changes required for {folder}.");
            }
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
    /// <param name="include">A glob pattern specifying which files should be processed.</param>
    /// <returns>A mapping of directories to individual files.</returns>
    private static Dictionary<string, List<string>> searchForFiles(string include)
    {
        var folders = new Dictionary<string, List<string>>();

        foreach (string file in Glob.ExpandNames(include))
        {
            if (AbletonMetadata.IsMetadata(file) || !AbletonMetadata.IsSupportedSampleFormat(file) || Directory.Exists(file))
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
