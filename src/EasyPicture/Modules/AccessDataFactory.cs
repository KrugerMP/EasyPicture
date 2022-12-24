using EasyPicture.Models;
using System.Data.Odbc;

namespace EasyPicture.Modules
{
  public class AccessDataFactory : IAccessDataFactory
  {
    private readonly string _databaseConnectionString;
    private readonly ILogger<AccessDataFactory> _logger;

    /// <summary>
    /// Base constructor
    /// </summary>
    /// <param name="configuration"></param>
    public AccessDataFactory(IConfiguration configuration, ILogger<AccessDataFactory> logger)
    {
      _databaseConnectionString = configuration["LocalAccess:DatabaseConnectionString"];
      _logger = logger;
    }

    /// <summary>
    /// Validates if a database connection could be made and a table query was possible
    /// </summary>
    /// <returns></returns>
    public async Task<bool> HasDataBaseConnectionAsync()
    {
      string queryText = "SELECT TOP 1 * FROM PictureLocations";

      return (await RunQueryAsync(queryText) != null);
    }

    /// <summary>
    /// Inserts picture location data
    /// </summary>
    /// <param name="pictureLocations"></param>
    /// <returns></returns>
    public async Task InsertPictureDataAsync(PictureLocations pictureLocations)
    {
      string query = $"INSERT INTO PictureLocations(ImageLocation, MD5) VALUES ('{pictureLocations.ImageLocation}', '{pictureLocations.MD5}')";

      _ = await RunQueryAsync(query);
    }

    /// <summary>
    /// Check if the MD5 is not already downloaded
    /// </summary>
    /// <param name="md5"></param>
    /// <returns></returns>
    public async Task<PictureLocations> IsAlreadyDownloadedAsync(string md5)
    {
      string queryText = $"SELECT * FROM PictureLocations WHERE MD5 = '{md5}'";

      return (await RunQueryAsync(queryText)).FirstOrDefault();
    }

    /// <summary>
    /// Runs a query against the access database and return any selected result of type <see cref="PictureLocations"/>
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private async Task<List<PictureLocations>> RunQueryAsync(string query)
    {
      try
      {
        OdbcCommand command = new(query);
        List<PictureLocations> pictureLocations = new();

        using OdbcConnection connection = new(_databaseConnectionString);
        command.Connection = connection;
        connection.Open();
        var reader = await command.ExecuteReaderAsync();

        while (reader.Read())
        {
          pictureLocations.Add(new PictureLocations
          {
            ID = reader.GetInt32(reader.GetOrdinal("ID")),
            ImageLocation = reader.GetString(reader.GetOrdinal("ImageLocation")),
            MD5 = reader.GetString(reader.GetOrdinal("MD5"))
          });
        }

        reader.Close();
        command.Dispose();
        connection.Close();
        connection.Dispose();

        return pictureLocations;
      }
      catch (Exception ex)
      {
        _logger.LogError($"DbConnection Used:{_databaseConnectionString} with exception:{ex.Message}");
        return null;
      }
    }
  }
}