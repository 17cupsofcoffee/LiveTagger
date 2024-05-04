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
    /// Whether any changes have been made to the document since the last save.
    /// </summary>
    public bool IsDirty { get; private set; } = false;

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
    /// If the file hasn't actually been changed, this will have no effect.
    /// If the file already exists, a backup will be made.
    /// </summary>
    /// <param name="path">The path to save the file to.</param>
    public void Save(string path)
    {

        if (Path.Exists(path))
        {
            File.Copy(path, path + ".bak", true);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        xml.Save(path);

        IsDirty = false;
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
            var tagsAdded = new List<string>();

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
                        tagsAdded.Add(tag);
                        IsDirty = true;
                    }
                }
            }
            else
            {
                // Create new item
                var newItem = new XElement(Rdf.Li, new XAttribute(Rdf.ParseType, "Resource"),
                    new XElement(Ableton.FilePath, file),
                    new XElement(Ableton.Keywords,
                        new XElement(Rdf.Bag,
                            tags.Select(tag => new XElement(Rdf.Li, tag))
                        )
                    )
                );

                itemsBag.Add(newItem);
                tagsAdded = tags;
                IsDirty = true;
            }

            if (tagsAdded.Count > 0)
            {
                Console.WriteLine($"Added tags to {file}: {string.Join(", ", tagsAdded)}");
            }

        }
    }

    /// <summary>
    /// Remove a specific set of tags from a list of files.
    /// </summary>
    /// <param name="files">The files to untag.</param>
    /// <param name="tags">The tags to remove.</param>
    public void RemoveTags(List<string> files, List<string> tags)
    {
        var itemsBag = xml.Descendants(Ableton.Items).First().Element(Rdf.Bag);

        foreach (string file in files)
        {
            var item = itemsBag.Elements(Rdf.Li).FirstOrDefault(e => e.Element(Ableton.FilePath).Value == file);

            if (item != null)
            {
                var keywordBag = item.Element(Ableton.Keywords).Element(Rdf.Bag);

                var tagsRemoved = new List<string>();
                var elementsToRemove = new List<XElement>();

                foreach (XElement keyword in keywordBag.Elements(Rdf.Li))
                {
                    if (tags.Contains(keyword.Value))
                    {
                        tagsRemoved.Add(keyword.Value);
                        elementsToRemove.Add(keyword);
                    }
                }

                if (elementsToRemove.Count > 0)
                {
                    if (elementsToRemove.Count == keywordBag.Elements(Rdf.Li).Count())
                    {
                        item.Remove();
                    }
                    else
                    {
                        elementsToRemove.Remove();
                    }

                    IsDirty = true;
                    Console.WriteLine($"Removed tags from {file}: {string.Join(", ", tagsRemoved)}");
                }
            }
        }
    }

    /// <summary>
    /// Remove all tags from a list of files.
    /// </summary>
    /// <param name="files">The files to untag.</param>
    public void RemoveTags(List<string> files)
    {
        var itemsBag = xml.Descendants(Ableton.Items).First().Element(Rdf.Bag);

        foreach (string file in files)
        {
            var item = itemsBag.Elements(Rdf.Li).FirstOrDefault(e => e.Element(Ableton.FilePath).Value == file);

            if (item != null)
            {
                item.Remove();
                IsDirty = true;
                Console.WriteLine($"Removed all tags from {file}");
            }
        }
    }
}