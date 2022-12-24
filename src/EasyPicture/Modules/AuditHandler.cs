namespace EasyPicture.Modules
{
  public class AuditHandler : IAuditHandler
  {
    /// <summary>
    /// The text file name and location
    /// </summary>
    public string TextFileName { get; private set; }

    private readonly bool _audit = false;
    private readonly string _downloadPath;

    private StreamWriter _streamWriter;

    private readonly ILogger _logger;

    /// <summary>
    /// Base constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="audit"></param>
    /// <param name="auditDirectory"></param>
    public AuditHandler(
      ILogger logger,
      string auditDirectory,
      bool audit = false)
    {
      _logger = logger;
      _downloadPath = auditDirectory;
      _audit = audit;
    }

    /// <summary>
    /// Creates a file based on the name in downloads
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task CreateFileAsync(string name, string firstLineAudit = "")
    {
      name = name.Replace("%20", "_");

      string auditMessage = $"{DateTime.UtcNow}:{firstLineAudit}";

      if (_audit)
      {
        TextFileName = _downloadPath + name + ".txt";

        await File.WriteAllTextAsync(TextFileName, $"{DateTime.UtcNow}:Audit Started");

        _streamWriter = new(TextFileName, append: true);
        _streamWriter.AutoFlush = true;

        _logger.LogInformation($"{DateTime.UtcNow}:Audit Started");

        if (!string.IsNullOrEmpty(firstLineAudit))
        {
          await _streamWriter.WriteLineAsync(auditMessage);
        }
      }
      _logger.LogInformation(auditMessage);
    }

    /// <summary>
    /// Disposes the streamwriter
    /// </summary>
    /// <returns></returns>
    public async Task DropFileStreamAsync()
    {
      if (_streamWriter != null)
      {
        await _streamWriter.DisposeAsync();
        _streamWriter = null; 
      }
    }

    /// <summary>
    /// Audits the information message
    /// </summary>
    /// <param name="auditMessage"></param>
    /// <returns></returns>
    public async Task AuditInfoMessageAsync(string auditMessage)
    {
      string auditMessageConcat = $"{DateTime.UtcNow}:{auditMessage}";

      if (_audit)
      {
        try
        {
          await _streamWriter.WriteLineAsync(auditMessageConcat);
        }
        catch (Exception)
        {
          _logger.LogWarning("Stream was already busy being written to");
        }
      }

      _logger.LogInformation(auditMessageConcat);
    }

    /// <summary>
    /// Audits the exception message
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public async Task AuditExceptionMessageAsync(Exception ex)
    {
      if (_audit)
      {
        await AuditInfoMessageAsync($"{ex.Message}, {ex.InnerException}");
      }
      _logger.LogError(ex.Message);
    }
  }
}