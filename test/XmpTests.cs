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
            ["bd1.wav", "bd2.wav"],
            ["Drums|Kick"]
        );

        xmp.AddTags(
            ["ch.wav"],
            ["Drums|Hihat", "Drums|Hihat|Closed Hihat"]
        );

        xmp.AddTags(
            ["bd1.wav", "bd2.wav", "ch.wav"],
            ["Creator|17cupsofcoffee"]
        );

        Assert.Equal(
            XElement.Parse(TestUtils.ReadFileAsString("TestData/WithTagsAdded.xml")),
            xmp.Xml,
            XNode.EqualityComparer
        );

        Assert.True(xmp.IsDirty);
    }

    [Fact]
    public void ShouldRemoveTags()
    {
        var xmp = Xmp.FromString(TestUtils.ReadFileAsString("TestData/WithTagsAdded.xml"));

        xmp.RemoveTags(
            ["bd1.wav", "bd2.wav", "ch.wav"],
            ["Creator|17cupsofcoffee"]
        );

        xmp.RemoveTags(
            ["ch.wav"],
            ["Drums|Hihat", "Drums|Hihat|Closed Hihat"]
        );

        Assert.Equal(
            XElement.Parse(TestUtils.ReadFileAsString("TestData/WithTagsRemoved.xml")),
            xmp.Xml,
            XNode.EqualityComparer
        );

        Assert.True(xmp.IsDirty);
    }

    [Fact]
    public void ShouldRemoveAllTags()
    {
        var xmp = Xmp.FromString(TestUtils.ReadFileAsString("TestData/WithTagsAdded.xml"));

        xmp.RemoveTags(
            ["ch.wav"]
        );

        Assert.Equal(
            XElement.Parse(TestUtils.ReadFileAsString("TestData/WithAllTagsRemoved.xml")),
            xmp.Xml,
            XNode.EqualityComparer
        );

        Assert.True(xmp.IsDirty);
    }
}
