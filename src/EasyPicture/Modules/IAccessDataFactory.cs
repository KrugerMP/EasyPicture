using EasyPicture.Models;

namespace EasyPicture.Modules
{
  public interface IAccessDataFactory
  {
    /// <summary>
    /// Validates if a database connection could be made and a table query was possible
    /// </summary>
    /// <returns></returns>
    public Task<bool> HasDataBaseConnectionAsync();

    /// <summary>
    /// Inserts picture location data
    /// </summary>
    /// <param name="pictureLocations">Location where image was downloaded</param>
    /// <returns></returns>
    public Task InsertPictureDataAsync(PictureLocations pictureLocations);

    /// <summary>
    /// Check if the MD5 is not already downloaded
    /// </summary>
    /// <param name="md5">The image MD5 from the API request</param>
    /// <returns></returns>
    public Task<PictureLocations> IsAlreadyDownloadedAsync(string md5);
  }
}
