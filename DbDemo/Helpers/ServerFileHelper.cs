using Microsoft.Extensions.FileProviders.Physical;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using MimeKit;
using PhysicalFileInfo = DbDemo.Models.PhysicalFileInfo;

namespace DbDemo.Helpers;

public class ServerFileHelper
{
    public enum FolderName
    {
        UserUploadFiles = 1,
        UserArchiveFiles = 2,
        UserGenFiles = 3
    }

    private readonly static bool encrypt =  ENCRYPT;
    private readonly static string encExt = ENC_EXT;

    public static string GenerateTrustedFileNameWithExt(string originalFilename, string fileCode)
    {
        var ext = Path.GetExtension(originalFilename);
        var fn = Path.GetFileNameWithoutExtension(originalFilename);
        var id = Guid.NewGuid().ToString("N").ToUpper()[..5];
        var trustedFileName = $"{CreateValidFileName(fn)}-{fileCode}{id}{ext}";

        return trustedFileName;
    }

    private static string CreateValidFileName(string originalFilename)
    {
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return Regex.Replace(originalFilename, invalidRegStr, "_");
    }

    private static byte[] GenerateRandomSalt()
    {
        byte[] data = new byte[32];
        using var r = RandomNumberGenerator.Create();

        for (int i = 0; i < 10; i++)
            r.GetBytes(data);

        return data;
    }

    /// <summary>
    /// If encrypted, the .enc file will be created, but the .enc extension is removed for filename returned
    /// </summary>
    /// <param name="file"></param>
    /// <param name="trustedFileName"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static async Task<UploadFileResponse> UploadFileAsync(IFormFile file, string trustedFileName, string filePath)
    {
        try
        {
            await using FileStream fs = new(filePath, FileMode.Create);
            await file.CopyToAsync(fs);
            fs.Close();

            if (encrypt)
            {
                var salt = GenerateRandomSalt();

                await using FileStream fsCrypt = new($"{filePath}{encExt}", FileMode.Create);

                var passwordBytes = Encoding.UTF8.GetBytes(CRYPTO.KEY);

                using var aes = Aes.Create();

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;

                var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);
                aes.Mode = CipherMode.CFB;

                fsCrypt.Write(salt, 0, salt.Length);

                await using CryptoStream cs = new(fsCrypt, aes.CreateEncryptor(), CryptoStreamMode.Write);
                await using FileStream fsIn = new($"{filePath}", FileMode.Open);

                var buffer = new byte[1048576];
                int read;

                try
                {
                    while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cs.Write(buffer, 0, read);
                    }

                    return new UploadFileResponse
                    {
                        IsEncrypted = encrypt,
                        IsSuccess = true,
                        OriginalFileName = file.Name,
                        StoredFileName = $"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}" //$"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}{ENC_EXT}",
                    };
                }
                catch (Exception ex)
                {
                    return new UploadFileResponse
                    {
                        IsEncrypted = encrypt,
                        IsSuccess = false,
                        Message = ex.Message
                    };
                }
                finally
                {
                    fsIn.Close();
                    File.Delete(filePath);
                }
            }

            return new UploadFileResponse
            {
                IsEncrypted = encrypt,
                IsSuccess = true,
                OriginalFileName = file.Name,
                StoredFileName = trustedFileName
            };
        }
        catch (Exception ex)
        {
            return new UploadFileResponse
            {
                IsEncrypted = encrypt,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public static async Task<DownloadFileResponse> DownloadFileAsync(string filePath)
    {
        try
        {
            if (encrypt)
            {
                var passwordBytes = Encoding.UTF8.GetBytes(CRYPTO.KEY);
                var salt = new byte[32];

                await using FileStream fsCrypt = new(filePath, FileMode.Open);
                fsCrypt.Read(salt, 0, salt.Length);

                using var aes = Aes.Create();

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;

                var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);
                aes.Mode = CipherMode.CFB;

                var encryptedFilename = fsCrypt.Name;
                var originalFilename = $"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}";
                var originalFilepath = Path.Combine(Path.GetDirectoryName(filePath), originalFilename);

                await using CryptoStream cs = new(fsCrypt, aes.CreateDecryptor(), CryptoStreamMode.Read);
                await using FileStream fsOut = new(originalFilepath, FileMode.Create);

                int read;
                byte[] buffer = new byte[1048576];

                try
                {
                    while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fsOut.Write(buffer, 0, read);
                    }

                    //BackgroundJob.Schedule(() => File.Delete(originalFilepath), new TimeSpan(0, 0, 10));
                    //File.Delete(originalFilepath);
                    DeleteFileAsync(originalFilepath, new TimeSpan(0, 0, 10));

                    return new DownloadFileResponse
                    {
                        IsDecrypted = encrypt,
                        IsSuccess = true,
                        FileName = originalFilename,
                        FilePath = originalFilepath,
                        ContentType = MimeTypes.GetMimeType(originalFilepath)
                    };
                }
                catch (CryptographicException ex)
                {
                    return new DownloadFileResponse
                    {
                        IsSuccess = false,
                        Message = $"CryptographicException error: {ex.Message}"
                    };
                }
                catch (Exception ex)
                {
                    return new DownloadFileResponse
                    {
                        IsSuccess = false,
                        Message = $"Error: {ex.Message}"
                    };
                }
            }
            else
            {
                return new DownloadFileResponse
                {
                    IsSuccess = true,
                    IsDecrypted = encrypt,
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    ContentType = MimeTypes.GetMimeType(filePath)
                };
            }
        }
        catch (Exception ex)
        {
            return new DownloadFileResponse
            {
                IsDecrypted = encrypt,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    private static async Task DeleteFileAsync(string filePath, TimeSpan delay)
    {
        await Task.Run(async () =>
        {
            await Task.Delay(delay);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                }
            }
            else
            {
            }
        });
    }

    public static async Task<ServerSaveFileResponse> EncryptFileAsync(string filePath, bool deleteOriginalFile = true)
    {
        try
        {
            var salt = GenerateRandomSalt();

            await using FileStream fsCrypt = new($"{filePath}{encExt}", FileMode.Create);

            var passwordBytes = Encoding.UTF8.GetBytes(CRYPTO.KEY);

            using var aes = Aes.Create();

            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;

            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);
            aes.Mode = CipherMode.CFB;

            fsCrypt.Write(salt, 0, salt.Length);

            await using CryptoStream cs = new(fsCrypt, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await using FileStream fsIn = new($"{filePath}", FileMode.Open);

            var buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }

                if (deleteOriginalFile)
                    File.Delete(filePath);
                //BackgroundJob.Schedule(() => File.Delete(filePath), new TimeSpan(0, 0, 10));

                return new ServerSaveFileResponse
                {
                    IsEncrypted = encrypt,
                    IsSuccess = true,
                    OriginalFileName = Path.GetFileName(filePath),
                    StoredFileName = $"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}" //$"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}{ENC_EXT}",
                };
            }
            catch (Exception ex)
            {
                return new ServerSaveFileResponse
                {
                    IsEncrypted = encrypt,
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
            finally
            {
                fsIn.Close();
                File.Delete(filePath);
            }

        }
        catch (Exception ex)
        {
            return new ServerSaveFileResponse
            {
                IsEncrypted = encrypt,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public static async Task<ServerSaveFileResponse> DecryptFileAsync(string filePath)
    {
        try
        {
            var passwordBytes = Encoding.UTF8.GetBytes(CRYPTO.KEY);
            var salt = new byte[32];

            await using FileStream fsCrypt = new(filePath, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);

            using var aes = Aes.Create();

            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;

            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);
            aes.Mode = CipherMode.CFB;

            var encryptedFilename = fsCrypt.Name;
            var originalFilename = $"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}";
            var originalFilepath = Path.Combine(Path.GetDirectoryName(filePath), originalFilename);

            await using CryptoStream cs = new(fsCrypt, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await using FileStream fsOut = new(originalFilepath, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fsOut.Write(buffer, 0, read);
                }

                return new ServerSaveFileResponse
                {
                    IsEncrypted = encrypt,
                    IsSuccess = true,
                    OriginalFileName = originalFilename,
                    StoredFileName = $"{Path.GetFileNameWithoutExtension(fsCrypt.Name)}"
                };
            }
            catch (CryptographicException ex)
            {
                return new ServerSaveFileResponse
                {
                    IsEncrypted = encrypt,
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ServerSaveFileResponse
                {
                    IsEncrypted = encrypt,
                    IsSuccess = false,
                    Message = ex.Message
                };
            }

        }
        catch (Exception ex)
        {
            return new ServerSaveFileResponse
            {
                IsEncrypted = encrypt,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public static ResponseBase DeleteFileFromStorage(IWebHostEnvironment env, string fileName, FolderName folder = FolderName.UserUploadFiles)
    {
        try
        {
            var path = Path.Combine(env.ContentRootPath, folder.ToString(), fileName);

            if (encrypt)
                path += encExt; // expand .enc extension

            File.Delete(path);

            return new ResponseBase
            {
                IsSuccess = true,
                Message = "File successfully deleted from storage"
            };
        }
        catch (Exception ex)
        {
            return new ResponseBase
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public static PhysicalFileInfo GetPhysicalFileInfo(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new PhysicalFileInfo
                {
                    IsSuccess = false,
                    Message = "File not found"
                };

            FileInfo fi = new(filePath);

            return new PhysicalFileInfo
            {
                IsSuccess = true,
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                ContentType = MimeTypes.GetMimeType(filePath),
                FileSize = fi.Length
            };

        }
        catch (Exception ex)
        {
            return new PhysicalFileInfo
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }
}

