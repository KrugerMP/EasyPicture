using System.Text;
using System.Xml.Serialization;

namespace EasyPicture.Modules
{
  public class RuleApiController : IRuleApiController
  {
    private Posts _posts;
    private int CurrentPageCount = 0;
    private int DownloadConter = 0;

    private readonly string _rule34ApiUrl;
    private readonly int _maxDegreeOfParallelism = 0;
    private readonly bool _auditDownloads = false;
    private readonly string _downloadPath;
    private readonly string _auditPath;

    private readonly ILogger _logger;
    private readonly IAccessDataFactory _accessDataFactory;

    private IAuditHandler auditHandler;

    /// <summary>
    /// Base constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="accessDataFactory"></param>
    /// <param name="configuration"></param>
    public RuleApiController(
      ILogger<RuleApiController> logger,
      IAccessDataFactory accessDataFactory,
      IConfiguration configuration)
    {
      _posts = new();
      _logger = logger;
      _accessDataFactory = accessDataFactory;

      _maxDegreeOfParallelism = int.Parse((string.IsNullOrEmpty(configuration["MaxDegreeOfParallelism"]) ? "4" : configuration["MaxDegreeOfParallelism"]));
      _rule34ApiUrl = configuration["Rule34:DownloadBaseUrl"];
      _auditDownloads = bool.TryParse(configuration["SaveAuditHistory:Option"], out _);
      _downloadPath = configuration["Directories:DownloadDirectory"];
      _auditPath = configuration["Directories:AuditDirectory"];
    }

    /// <summary>
    /// Returns true/false to know when the download should be started.
    /// Only check if there are posts to download.
    /// </summary>
    /// <returns></returns>
    public bool ContinueDownloadLoop()
    {
      if (_posts.Post.Count != 0)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Downloads the tags and creates a sub-folder under "Downloads".
    /// Also build a folder with the tags as "food-hands-".
    /// Will also save data to the access database to help keep track of not downloading duplicates
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public async Task GetPostByTagsAsync(string[] tags)
    {
      auditHandler = new AuditHandler(_logger, _auditPath, _auditDownloads);

      tags ??= (Array.Empty<string>());

      Array.Sort(tags);

      StringBuilder tagList = new();
      foreach (var tag in tags)
        tagList.Append(tag + "%20");

      _posts = await RequestPageAsync(tagList.ToString(), CurrentPageCount.ToString());

      if (_posts.Post.Count != 0)
      {
        CurrentPageCount++;

        await auditHandler.CreateFileAsync(tagList.ToString(), $"Starting download of page:{CurrentPageCount}{Environment.NewLine}Currently downloaded:{DownloadConter}");

        string location = $"{_downloadPath}{tagList.Replace("%20", "-")}\\";
        bool databaseOnline = await _accessDataFactory.HasDataBaseConnectionAsync();

        Parallel.ForEach(_posts.Post, new ParallelOptions()
        {
          MaxDegreeOfParallelism = _maxDegreeOfParallelism
        }, post =>
        {
          var preDownloadResult = _accessDataFactory.IsAlreadyDownloadedAsync(post.Md5).Result;

          if (string.IsNullOrEmpty(preDownloadResult?.MD5))
          {
            DownloadConter++;

            var downloadedResult = DownloadImageAsync(location, post.Md5, new Uri(post.FileUrl)).Result;

            if (databaseOnline)
            {
              _accessDataFactory.InsertPictureDataAsync(new Models.PictureLocations()
              {
                MD5 = post.Md5,
                ImageLocation = downloadedResult.Item2
              }).Wait();
            }

            if (downloadedResult.Item1)
            {
              auditHandler.AuditInfoMessageAsync($"Downloaded {post.FileUrl} to {location}").Wait();
            }
            else
            {
              auditHandler.AuditInfoMessageAsync($"Could not download/save {post.FileUrl} to {location}").Wait();
            }
          }
          else
          {
            auditHandler.AuditInfoMessageAsync($"Already downloaded {post.FileUrl} with name {preDownloadResult.ImageLocation}").Wait();
          }
        });

        auditHandler.AuditInfoMessageAsync("Finished downloading").Wait();
        auditHandler.DropFileStreamAsync().Wait();
      }
    }

    /// <summary>
    /// Downloads the image async and return the path of the downloaded image.
    /// When the image is downloaded successfully, the path will be returned, else the path will be null.
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <param name="fileName"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    private async Task<Tuple<bool, string>> DownloadImageAsync(string directoryPath, string fileName, Uri uri)
    {
      try
      {
        using var httpClient = new HttpClient();

        // Get the file extension
        var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
        var fileExtension = Path.GetExtension(uriWithoutQuery);

        // Create file path and ensure directory exists
        var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
        Directory.CreateDirectory(directoryPath);

        // Download the image and write to the file
        var stream = await httpClient.GetStreamAsync(uri);
        await File.WriteAllBytesAsync(path, ReadFully(stream));

        stream.Dispose();

        return new Tuple<bool, string>(true, path);
      }
      catch (Exception ex)
      {
        await auditHandler.AuditExceptionMessageAsync(ex);
        return new Tuple<bool, string>(false, null);
      }
    }

    /// <summary>
    /// Read the stream and converts to a byte[]
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static byte[] ReadFully(Stream input)
    {
      byte[] buffer = new byte[16 * 1024];
      using MemoryStream ms = new();
      int read;
      while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
      {
        ms.Write(buffer, 0, read);
      }
      return ms.ToArray();
    }

    /// <summary>
    /// Creates an API request to the page to return the URL's and information of all content on the page.
    /// </summary>
    /// <param name="tags">The tags in URL encoded format</param>
    /// <param name="pid">The page id which we are on</param>
    /// <param name="page">The type of page request. API's use "dapi"</param>
    /// <param name="searchBy">The type of search request we are doing</param>
    /// <param name="queryBy">How we are querying the data</param>
    /// <returns></returns>
    private async Task<Posts> RequestPageAsync(string tags, string pid = "0", string page = "dapi", string searchBy = "post", string queryBy = "index")
    {
      HttpClient client = new();
      // Sample URL: https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&tags=food
      string queryUrl = $"{_rule34ApiUrl}/index.php?page={page}&s={searchBy}&q={queryBy}&tags={tags}&pid={pid}";

      HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, queryUrl);

      HttpResponseMessage httpResponseMessage = await client.SendAsync(httpRequestMessage);

      XmlSerializer serializer = new(typeof(Posts));
      using StringReader reader = new(httpResponseMessage.Content.ReadAsStringAsync().Result);
      return (Posts)serializer.Deserialize(reader);
    }
  }
}