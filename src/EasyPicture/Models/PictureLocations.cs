using System.ComponentModel.DataAnnotations.Schema;

namespace EasyPicture.Models
{
  [Table("PictureLocations")]
  public class PictureLocations
  {
    public virtual int ID { get; set; }

    public virtual string ImageLocation { get; set; }

    public virtual string MD5 { get; set; }
  }
}
