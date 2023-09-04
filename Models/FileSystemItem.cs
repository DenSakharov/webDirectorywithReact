using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

public class FileSystemItem
{
    public string Name { get; set; }
    public string Path { get; set; }
    public long Size { get; set; }
    public bool IsDirectory { get; set; }
    public List<FileSystemItem> SubItems { get; set; }
}

public class FileUploadModel
{
    public string DirectoryPath { get; set; }
    public IFormFile File { get; set; }
}
