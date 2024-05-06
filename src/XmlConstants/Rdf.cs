using System.Xml.Linq;

namespace LiveTagger.XmlConstants;

public static class Rdf
{
    public static readonly XNamespace Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    public static readonly XName RdfWrapper = Namespace + "RDF";
    public static readonly XName Description = Namespace + "Description";
    public static readonly XName Bag = Namespace + "Bag";
    public static readonly XName Li = Namespace + "li";
    public static readonly XName ParseType = Namespace + "parseType";
}