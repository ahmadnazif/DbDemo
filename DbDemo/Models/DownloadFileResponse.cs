namespace DbDemo.Models;

public class DownloadFileResponse : ResponseBase
{
    public bool IsDecrypted { get; set; }
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
}
