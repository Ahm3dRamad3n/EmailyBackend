using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Emaily.BLL.DTOs;
using Emaily.BLL.Helpers.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Emaily.BLL.Helpers.Services
{
    public class AttachmentManager(IConfiguration configuration, ILoggerService logger) :  IAttachmentManager
    {
        private readonly ILoggerService _logger = logger;
        private readonly string _connectionString = configuration["BS_CONNECTION_STRING"] ??
                  throw new Exception("BS_CONNECTION_STRING is not configured in appsettings.json or environment variables.");
        private readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".docx"];

        public enum AttachmentSource
        {
            Invoice,
            Template,
        }

        private static string GetContainerName(AttachmentSource source)
        {
            return source switch
            {
                AttachmentSource.Invoice => "invoices",
                AttachmentSource.Template => "templates",
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }

        public string? Add(IFormFile file, AttachmentSource attachmentSource)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > 5 * 1024 * 1024) return null; // الحد الأقصى لحجم الملف 5 ميغابايت
            if (!IsValidFileSignature(file)) return null; // التحقق من صحة توقيع الملف

            string containerName = GetContainerName(attachmentSource);
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // يتأكد من إنشاء الحاوية إن لم تكن موجودة، ويجعلها عامة للقراءة
            containerClient.CreateIfNotExists(PublicAccessType.Blob);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            using (var stream = file.OpenReadStream())
            {
                // الرفع المباشر المتزامن
                blobClient.Upload(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            return blobClient.Uri.ToString();
        }

        public bool Delete(string fileUrl, AttachmentSource attachmentSource)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return true;

            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
                return true;

            string containerName = GetContainerName(attachmentSource);
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var fileName = Path.GetFileName(uri.LocalPath);
            var blobClient = containerClient.GetBlobClient(fileName);

            blobClient.DeleteIfExists();
            return true;
        }

        public IFormFile? ConvertToIFormFile(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return null;

            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                _logger.LogError(new Log(Guid.Empty, $"Invalid URL format: {fileUrl}"));
                return null;
            }

            // استخراج اسم الحاوية ديناميكياً من الرابط (مثال: /invoices/file.jpg)
            var containerName = uri.Segments.Length > 1 ? uri.Segments[1].TrimEnd('/') : "";
            var fileName = Path.GetFileName(uri.LocalPath);

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.Exists())
            {
                _logger.LogError(new Log(Guid.Empty, $"File not found in blob storage: {fileUrl}"));
                return null;
            }

            // تحميل الملف في الذاكرة لإنشاء الـ IFormFile
            var memoryStream = new MemoryStream();
            blobClient.DownloadTo(memoryStream);
            memoryStream.Position = 0; // إعادة المؤشر للبداية لقراءته

            var properties = blobClient.GetProperties().Value;

            return new FormFile(memoryStream, 0, memoryStream.Length, "attachment", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = properties.ContentType ?? GetContentType(fileName)
            };
        }

        public async Task<FileDto?> ConvertToFileDtoAsync(IFormFile? file)
        {
            if (file == null || !IsValidFileSignature(file))
                return null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            return new FileDto
            {
                Content = memoryStream.ToArray(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Name = file.Name
            };
        }

        public IFormFile? ConvertToIFormFile(FileDto? fileDto)
        {
            if (fileDto == null || fileDto.Content == null)
                return null;

            var stream = new MemoryStream(fileDto.Content);

            var formFile = new FormFile(stream, 0, stream.Length, fileDto.Name ?? "file", fileDto.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = fileDto.ContentType
            };

            return formFile;
        }

        public bool IsValidFileSignature(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                return false;

            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream);
            var fileBytes = reader.ReadBytes(8);

            if (fileBytes.Length == 0) return false;

            return extension switch
            {
                ".png" => fileBytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),

                ".jpg" or ".jpeg" => fileBytes.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),

                ".pdf" => fileBytes.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }), // تبدأ بـ %PDF

                // ملفات DOCX هي في الواقع ملفات ZIP وتبدأ بحروف PK
                ".docx" => fileBytes.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }) ||
                           fileBytes.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x05, 0x06 }) ||
                           fileBytes.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x07, 0x08 }),

                _ => false,
            };
        }

        private static string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}