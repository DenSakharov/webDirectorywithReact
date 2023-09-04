using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

[Route("api/filemanager")]
[ApiController]
public class FileManagerController : ControllerBase
{
    private const string RootDirectory = @"C:\"; //корневая директория, с которой начнется работа

    private long CalculateDirectorySize(string path)
    {
        long size = 0;

        // Перечислить все файлы в папке
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch (UnauthorizedAccessException)
                {
                    // Обработка ошибки доступа
                    // Пропустить файл, к которому нет доступа
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Обработка ошибки доступа к папке
            // Пропустить папку, к которой нет доступа
        }

        // Рекурсивно пройтись по всем подпапкам
        try
        {
            foreach (var subdirectory in Directory.GetDirectories(path))
            {
                try
                {
                    size += CalculateDirectorySize(subdirectory);
                }
                catch (UnauthorizedAccessException)
                {
                    // Обработка ошибки доступа
                    // Пропустить папку, к которой нет доступа
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Обработка ошибки доступа к папке
            // Пропустить папку, к которой нет доступа
        }

        return size;
    }



    [HttpGet]
    [Route("list")]
    public ActionResult<List<FileSystemItem>> GetFilesAndDirectories(string directoryPath = "")
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                directoryPath = "/";
            }
            string fullPath = Path.Combine(RootDirectory, directoryPath);
            var items = Directory.GetFileSystemEntries(fullPath)
                .Select(path =>
                {
                    var fileInfo = new FileInfo(path);
                    var isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory);

                    long size = isDirectory ? CalculateDirectorySize(path) : fileInfo.Length;

                    return new FileSystemItem
                    {
                        Name = fileInfo.Name,
                        Path = path,
                        Size = size,
                        IsDirectory = isDirectory,
                    };
                })
                .ToList();

            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
        }
    }


    [HttpPost]
    [Route("create-directory")]
    public ActionResult CreateDirectory(string directoryPath, string newDirectoryName)
    {
        try
        {
            string fullPath = Path.Combine(RootDirectory, directoryPath, newDirectoryName);
            Directory.CreateDirectory(fullPath);
            return Ok("Directory created successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost]
    [Route("upload-file")]
    public async Task<ActionResult> UploadFile([FromForm] FileUploadModel model)
    {
        try
        {
            string fullPath = Path.Combine(RootDirectory, model.DirectoryPath, model.File.FileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }
            return Ok("File uploaded successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete]
    [Route("delete")]
    public ActionResult DeleteFileOrDirectory(string path)
    {
        try
        {
            string fullPath = Path.Combine(RootDirectory, path);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
            else
            {
                return NotFound("File or directory not found.");
            }
            return Ok("File or directory deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet]
    [Route("download")]
    public ActionResult DownloadFile(string path)
    {
        try
        {
            string fullPath = Path.Combine(RootDirectory, path);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found.");
            }

            var fileInfo = new FileInfo(fullPath);
            var memory = new MemoryStream();
            using (var stream = new FileStream(fullPath, FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            var response = File(memory, GetContentType(fullPath), fileInfo.Name);
            return response;
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private string GetContentType(string path)
    {
        var types = GetMimeTypes();
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
    }

    private Dictionary<string, string> GetMimeTypes()
    {
        return new Dictionary<string, string>
        {
            { ".txt", "text/plain" },
            { ".pdf", "application/pdf" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            // Добавьте другие MIME-типы по мере необходимости
        };
    }
}
