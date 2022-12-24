namespace EasyPicture.Modules
{
  public interface IAuditHandler
  {
    /// <summary>
    /// Creates a file based on the name in downloads
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Task CreateFileAsync(string name, string firstLineAudit = "");

    /// <summary>
    /// Audits the information message
    /// </summary>
    /// <param name="auditMessage"></param>
    /// <returns></returns>
    public Task AuditInfoMessageAsync(string auditMessage);

    /// <summary>
    /// Audits the exception message
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public Task AuditExceptionMessageAsync(Exception ex);

    /// <summary>
    /// Disposes the streamwriter
    /// </summary>
    /// <returns></returns>
    public Task DropFileStreamAsync();
  }
}
