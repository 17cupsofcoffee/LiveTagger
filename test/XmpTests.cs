using System.Xml.Linq;
using System.Xml.XPath;
using LiveTagger.XmlConstants;

namespace LiveTagger.Tests;

public class XmpTests
{
    [Fact]
    public void ShouldAddTags()
    {
        var xmp = new Xmp();

        xmp.AddTags(
            new List<string>() { "bd1.wav", "bd2.wav" },
            new List<string>() { "Drums|Kick" }
        );

        xmp.AddTags(
            new List<string>() { "ch.wav" },
            new List<string>() { "Drums|Hihat", "Drums|Hihat|Closed Hihat" }
        );

        xmp.AddTags(
            new List<string>() { "bd1.wav", "bd2.wav", "ch.wav" },
            new List<string>() { "Creator|17cupsofcoffee" }
        );

        var bd1 = getItem(xmp.Xml, "bd1.wav");
        var bd2 = getItem(xmp.Xml, "bd2.wav");
        var ch = getItem(xmp.Xml, "ch.wav");

        // We should only ever have a single item per file.
        Assert.Single(bd1);
        Assert.Single(bd2);
        Assert.Single(ch);

        var bd1Tags = getTags(bd1.First());
        var bd2Tags = getTags(bd2.First());
        var chTags = getTags(ch.First());

        Assert.Contains("Drums|Kick", bd1Tags);
        Assert.Contains("Creator|17cupsofcoffee", bd1Tags);

        Assert.Contains("Drums|Kick", bd2Tags);
        Assert.Contains("Creator|17cupsofcoffee", bd2Tags);

        Assert.Contains("Drums|Hihat", chTags);
        Assert.Contains("Drums|Hihat|Closed Hihat", chTags);
        Assert.Contains("Creator|17cupsofcoffee", chTags);
    }

    [Fact]
    public void ShouldRemoveTags()
    {
        var xmp = new Xmp();

        xmp.AddTags(
            new List<string>() { "bd.wav", "ch.wav" },
            new List<string>() { "Drums|Kick", "Drums|Snare" }
        );

        xmp.RemoveTags(
            new List<string>() { "bd.wav", "ch.wav" },
            new List<string>() { "Drums|Snare" }
        );

        xmp.RemoveTags(
            new List<string>() { "ch.wav" },
            new List<string>() { "Drums|Kick" }
        );

        var bd = getItem(xmp.Xml, "bd.wav");
        var ch = getItem(xmp.Xml, "ch.wav");

        // Removing all tags from a file should remove the entire item.
        Assert.Single(bd);
        Assert.Empty(ch);

        var bdTags = getTags(bd.First());

        Assert.Contains("Drums|Kick", bdTags);
    }

    [Fact]
    public void ShouldRemoveAllTags()
    {
        var xmp = new Xmp();

        xmp.AddTags(
            new List<string>() { "bd.wav", "ch.wav" },
            new List<string>() { "Drums|Kick" }
        );

        xmp.RemoveTags(
            new List<string>() { "ch.wav" }
        );

        var bd = getItem(xmp.Xml, "bd.wav");
        var ch = getItem(xmp.Xml, "ch.wav");

        // Removing all tags from a file should remove the entire item.
        Assert.Single(bd);
        Assert.Empty(ch);
    }

    private IEnumerable<XElement> getItem(XElement xml, string file)
    {
        return xml.Descendants(Ableton.Items)!
            .First()!
            .Element(Rdf.Bag)!
            .Elements(Rdf.Li)!
            .Where(e => e.Element(Ableton.FilePath)!.Value == file);
    }

    private IEnumerable<string> getTags(XElement item)
    {
        return item
            .Element(Ableton.Keywords)!
            .Element(Rdf.Bag)!
            .Elements(Rdf.Li)
            .Select(e => e.Value);
    }
}