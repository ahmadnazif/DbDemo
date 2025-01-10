using DbDemo.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using static Pipelines.Sockets.Unofficial.Threading.MutexSlim;

namespace DbDemo.Controllers;

[Route("api/file")]
public class FileController(ILogger<FileController> logger, IWebHostEnvironment env, DocTokenService docToken, IRedisDb db) : ControllerBase
{
    private readonly ILogger<FileController> logger = logger;
    private readonly IWebHostEnvironment env = env;
    private readonly DocTokenService docToken = docToken;
    private readonly IRedisDb db = db;

    private readonly string userUploadFolderName = ServerFileHelper.FolderName.UserUploadFiles.ToString();
    private readonly static bool encrypt = ENCRYPT;
    private readonly static string encExt = ENC_EXT;

    [HttpPost("upload")]
    public async Task<UploadFileResponse> UploadFile([FromQuery] string username, [FromForm] List<IFormFile> files)
    {
        try
        {
            if (files.Count > 1)
            {
                return new UploadFileResponse
                {
                    IsSuccess = false,
                    Message = "Only 1 file can be accepted",
                };
            }

            var file = files[0];

            var ext = Path.GetExtension(file.FileName);

            string fileFrontName = "FILE";

            var trustedFileName = ServerFileHelper.GenerateTrustedFileNameWithExt(file.FileName, fileFrontName);
            var path = Path.Combine(env.ContentRootPath, userUploadFolderName, trustedFileName);

            var resp = await ServerFileHelper.UploadFileAsync(file, trustedFileName, path);

            if (resp.IsSuccess)
            {
                resp.Message = "File uploaded to storage";
                await db.SaveDocAsync(username, resp.StoredFileName);
            }

            return resp;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new UploadFileResponse
            {
                IsSuccess = false,
                Message = $"{ex.Message}",
            };
        }
    }

    [HttpGet("download")]
    public async Task<ActionResult<ResponseBase>> Download([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("Token is NULL or empty");
            return Unauthorized();
        }

        var data = docToken.Decrypt(token);

        if (data == null)
        {
            logger.LogError("Decryption failed");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(data.Username))
        {
            logger.LogError("Username not supplied");
            return Unauthorized();
        }

        var filename = await db.GetDocAsync(data.Username);

        if (string.IsNullOrEmpty(filename))
        {
            logger.LogError("Filename is not set in DB yet");
            return NotFound();
        }

        var path = Path.Combine(env.ContentRootPath, userUploadFolderName, filename);

        if (encrypt)
            path += encExt;

        logger.LogError(path);

        var exist = System.IO.File.Exists(path);

        if (!exist)
        {
            logger.LogError("File not found in storage");
            return NotFound();
        }

        return await DownloadAsync(path, false);
    }

    [HttpGet("generate-doc-token")]
    public async Task<ActionResult<ResponseBase>> GenDownloadToken([FromQuery] string username, [FromQuery] bool urlEncode)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new ResponseBase { IsSuccess = false, Message = "Username is required" };

        var exist = await db.IsUserExistAsync(username);

        if (!exist)
            return new ResponseBase { IsSuccess = false, Message = "User not found" };

        DocToken data = new()
        {
            Username = username
        };

        var tokenRaw = docToken.GenerateDocToken(data);
        var token = urlEncode ? WebUtility.UrlEncode(tokenRaw) : tokenRaw;

        return new ResponseBase
        {
             IsSuccess = true,
             Message = token
        };
    }

    private async Task<FileResult> DownloadAsync(string path, bool exportAsPhysicalFile)
    {
        var resp = await ServerFileHelper.DownloadFileAsync(path);

        if (exportAsPhysicalFile)
            return PhysicalFile(resp.FilePath, resp.ContentType, fileDownloadName: resp.FileName);
        else
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(resp.FilePath);
            return new FileContentResult(bytes, resp.ContentType);
        }
    }

}
