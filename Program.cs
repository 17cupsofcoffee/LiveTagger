using System.CommandLine;
using System.Xml.Linq;
using LiveTagger.XmlConstants;

namespace LiveTagger;

class Program
{
    public static string XmlTemplate = """
        <?xml version="1.0" encoding="utf-8"?>
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="XMP Core 5.6.0">
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description rdf:about="" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:ablFR="https://ns.ableton.com/xmp/fs-resources/1.0/" xmlns:xmp="http://ns.adobe.com/xap/1.0/">
                <dc:format>application/vnd.ableton.folder</dc:format>
                <ablFR:resource>folder</ablFR:resource>
                <ablFR:items>
                    <rdf:Bag></rdf:Bag>
                </ablFR:items>
                </rdf:Description>
            </rdf:RDF>
            </x:xmpmeta>
    """;

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

        addTagCommand.SetHandler(tagFiles, tagOption, filesOption, commitOption);

        rootCommand.AddCommand(addTagCommand);

        rootCommand.Invoke(args);
    }

    private static void tagFiles(string tag, string files, bool commit)
    {
        var folders = new Dictionary<string, List<string>>();

        foreach (string file in Directory.EnumerateFiles(files, "*", SearchOption.AllDirectories))
        {
            if (isAbletonMetadata(file))
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

        foreach (var (folder, samples) in folders)
        {
            if (hasExistingAbletonXmpFile(folder))
            {
                updateExistingAbletonXmpFile(folder, samples, tag, commit);
            }
            else
            {
                createAbletonXmpFile(folder, samples, tag, commit);
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

    private static bool isAbletonMetadata(string path)
    {
        return path.Contains("Ableton Folder Info") || Path.GetExtension(path) == ".asd";
    }

    private static string getAbletonXmpFilePath(string folder)
    {
        return Path.Join(folder, "Ableton Folder Info/dc66a3fa-0fe1-5352-91cf-3ec237e9ee90.xmp");
    }

    private static bool hasExistingAbletonXmpFile(string folder)
    {
        return File.Exists(getAbletonXmpFilePath(folder));
    }

    private static void createAbletonXmpFile(string folder, List<string> files, string tag, bool commit)
    {
        Console.WriteLine($"=== Creating new metadata for {folder} ===\n");

        var xmp = XElement.Parse(XmlTemplate.Trim());

        addTagsToAbletonXmpFile(files, tag, xmp);

        if (commit)
        {
            var xmpFilePath = getAbletonXmpFilePath(folder);

            xmp.Save(xmpFilePath);
        }

        Console.WriteLine();
    }

    private static void updateExistingAbletonXmpFile(string folder, List<string> files, string tag, bool commit)
    {
        Console.WriteLine($"=== Updating existing metadata for {folder} ===\n");

        var xmpFilePath = getAbletonXmpFilePath(folder);

        var xmp = XElement.Load(xmpFilePath);
        addTagsToAbletonXmpFile(files, tag, xmp);

        if (commit)
        {
            File.Copy(xmpFilePath, xmpFilePath + ".bak", true);

            xmp.Save(xmpFilePath);
        }

        Console.WriteLine();
    }

    private static void addTagsToAbletonXmpFile(List<string> files, string tag, XElement xmp)
    {
        var itemsBag = xmp.Descendants(Ableton.Items).First().Element(Rdf.Bag);

        foreach (string file in files)
        {
            var existingItem = itemsBag.Elements(Rdf.Li).FirstOrDefault(e => e.Element(Ableton.FilePath).Value == file);

            if (existingItem != null)
            {
                // Add keyword to existing items
                var keywordBag = existingItem.Element(Ableton.Keywords).Element(Rdf.Bag);

                if (keywordBag.Elements(Rdf.Li).Any(e => e.Value == tag))
                {
                    Console.WriteLine($"Tag '{tag}' already exists for {file}");
                }
                else
                {
                    keywordBag.Add(new XElement(Rdf.Li, tag));

                    Console.WriteLine($"Added tag '{tag}' to {file}");
                }
            }
            else
            {
                // Create new item
                var newItem = new XElement(Rdf.Li, new XAttribute(Rdf.ParseType, "Resource"),
                    new XElement(Ableton.FilePath, file),
                    new XElement(Ableton.Keywords,
                        new XElement(Rdf.Bag,
                            new XElement(Rdf.Li, tag)
                        )
                    )
                );

                itemsBag.Add(newItem);

                Console.WriteLine($"Added tag '{tag}' to {file}");
            }
        }
    }
}
