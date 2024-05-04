using System.CommandLine;

namespace LiveTagger;

class Program
{
    static void Main(string[] args)
    {
        var commitOption = new Option<bool>("--commit", ".");

        var rootCommand = new RootCommand("LiveTagger");
        rootCommand.AddGlobalOption(commitOption);

        var tagOption = new Option<string>("--tag", "The tag to apply to the matched files.") { IsRequired = true };
        var filesOption = new Option<string>("--files", "The files to tag.") { IsRequired = true };

        var addTagCommand = new Command("add", "Adds tags to the specified files.");
        addTagCommand.AddOption(filesOption);
        addTagCommand.AddOption(tagOption);

        addTagCommand.SetHandler(addTag, tagOption, filesOption, commitOption);

        rootCommand.AddCommand(addTagCommand);

        rootCommand.Invoke(args);
    }

    private static void addTag(string tag, string path, bool commit)
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

            xmp.AddTag(files, tag);

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
