namespace DbDemo.Models;

public class PhysicalFileInfo : ResponseBase
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
}
