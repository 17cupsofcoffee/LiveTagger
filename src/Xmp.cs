using System.Xml.Linq;
using LiveTagger.XmlConstants;

namespace LiveTagger;

public class Xmp
{
    /// <summary>
    /// Base template for new XMP files.
    /// </summary>
    private static string XmlTemplate = """
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

    /// <summary>
    /// The in-memory XML document.
    /// </summary>
    private XElement xml;

    /// <summary>
    /// Creates a new XMP document.
    /// </summary>
    /// <param name="xml">The XML to populate the document with.</param>
    private Xmp(XElement xml)
    {
        this.xml = xml;
    }

    /// <summary>
    /// Creates a new XMP document, with no metadata.
    /// </summary>
    public Xmp() : this(XElement.Parse(XmlTemplate.Trim())) { }

    /// <summary>
    /// Loads an XMP document from the filesystem.
    /// </summary>
    /// <param name="path">The path to the XMP file to load.</param>
    /// <returns>The loaded XMP document.</returns>
    public static Xmp FromFile(string path)
    {
        return new Xmp(XElement.Load(path));
    }

    /// <summary>
    /// Saves the XMP document to the filesystem, including any changes.
    /// If the file already exists, a backup will be made.
    /// </summary>
    /// <param name="path">The path to save the file to.</param>
    public void Save(string path)
    {
        if (Path.Exists(path))
        {
            File.Copy(path, path + ".bak", true);
        }

        xml.Save(path);
    }

    /// <summary>
    /// Tag a list of files.
    /// </summary>
    /// <param name="files">The files to tag.</param>
    /// <param name="tags">The tags to apply.</param>
    public void AddTags(List<string> files, List<string> tags)
    {
        var itemsBag = xml.Descendants(Ableton.Items).First().Element(Rdf.Bag);

        foreach (string file in files)
        {
            var existingItem = itemsBag.Elements(Rdf.Li).FirstOrDefault(e => e.Element(Ableton.FilePath).Value == file);

            if (existingItem != null)
            {
                // Add keyword to existing items
                var keywordBag = existingItem.Element(Ableton.Keywords).Element(Rdf.Bag);

                foreach (string tag in tags)
                {
                    if (!keywordBag.Elements(Rdf.Li).Any(e => e.Value == tag))
                    {
                        keywordBag.Add(new XElement(Rdf.Li, tag));

                        Console.WriteLine($"Added tag '{tag}' to {file}");
                    }
                }
            }
            else
            {
                // Create new item
                foreach (string tag in tags)
                {
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
}