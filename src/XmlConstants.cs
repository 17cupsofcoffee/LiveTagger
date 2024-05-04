using System.Xml.Linq;

namespace LiveTagger.XmlConstants;

public static class Rdf
{
    public static XNamespace Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    public static XName Bag = Namespace + "Bag";
    public static XName Li = Namespace + "li";
    public static XName ParseType = Namespace + "parseType";
}

public static class Ableton
{
    public static XNamespace Namespace => "https://ns.ableton.com/xmp/fs-resources/1.0/";

    public static XName Items => Namespace + "items";
    public static XName FilePath => Namespace + "filePath";
    public static XName Keywords => Namespace + "keywords";
}
