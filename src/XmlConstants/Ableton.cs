using System.Xml.Linq;

namespace LiveTagger.XmlConstants;

public static class Ableton
{
    public static readonly XNamespace Namespace = "https://ns.ableton.com/xmp/fs-resources/1.0/";

    public static readonly XName Items = Namespace + "items";
    public static readonly XName FilePath = Namespace + "filePath";
    public static readonly XName Keywords = Namespace + "keywords";
}
