using System.Xml.Serialization;

[XmlRoot(ElementName = "posts")]
public class Posts
{

  [XmlElement(ElementName = "post")]
  public List<Post> Post { get; set; }

  [XmlAttribute(AttributeName = "count")]
  public int Count { get; set; }

  [XmlAttribute(AttributeName = "offset")]
  public int Offset { get; set; }
}