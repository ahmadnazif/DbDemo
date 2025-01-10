namespace DbDemo.Models;

public class UploadFileResponse : ResponseBase
{
    public bool IsEncrypted { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
}
