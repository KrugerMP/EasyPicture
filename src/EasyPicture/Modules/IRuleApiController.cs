namespace EasyPicture.Modules
{
  public interface IRuleApiController
  {
    public bool ContinueDownloadLoop();

    public Task GetPostByTagsAsync(string[] tags);
  }
}
