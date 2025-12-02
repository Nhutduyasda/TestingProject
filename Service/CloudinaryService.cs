using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace MyProject.Service
{
    /// <summary>
    /// Interface cho Cloud Storage - Quản lý upload/delete ảnh
    /// </summary>
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder = "products");
        Task<bool> DeleteImageAsync(string imageUrl);
        Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folder = "products");
    }

    /// <summary>
    /// Cloudinary Service - Dịch vụ lưu trữ ảnh trên Cloudinary Cloud
    /// </summary>
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            // Support both CloudinaryUrl format and separate config
            var cloudinaryUrl = configuration["Cloudinary:CloudinaryUrl"];
            
            if (!string.IsNullOrEmpty(cloudinaryUrl))
            {
                // Use CloudinaryUrl format: cloudinary://API_KEY:API_SECRET@CLOUD_NAME
                _cloudinary = new Cloudinary(cloudinaryUrl);
                _logger.LogInformation("Cloudinary initialized with CloudinaryUrl format");
            }
            else
            {
                // Use separate config format
                var cloudName = configuration["Cloudinary:CloudName"];
                var apiKey = configuration["Cloudinary:ApiKey"];
                var apiSecret = configuration["Cloudinary:ApiSecret"];
                
                if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    var errorMsg = "Cloudinary configuration is missing. Please add either:\n" +
                        "1. CloudinaryUrl: 'cloudinary://API_KEY:API_SECRET@CLOUD_NAME'\n" +
                        "2. CloudName, ApiKey, and ApiSecret separately to appsettings.json";
                    _logger.LogError(errorMsg);
                    throw new ArgumentException(errorMsg);
                }

                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
                _logger.LogInformation($"Cloudinary initialized successfully for cloud: {cloudName}");
            }
            
            _cloudinary.Api.Secure = true;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "products")
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Empty file attempted to upload");
                    return null;
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning($"File too large: {file.FileName} ({file.Length / 1024 / 1024}MB)");
                    return null;
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Invalid file type: {extension}");
                    return null;
                }

                // Upload to Cloudinary
                using var stream = file.OpenReadStream();
                
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"myproject/{folder}", // Tổ chức thư mục trên Cloudinary
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Width(1000)
                        .Height(1000)
                        .Crop("limit")
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Image uploaded successfully: {uploadResult.SecureUrl}");
                    return uploadResult.SecureUrl.ToString();
                }
                else
                {
                    _logger.LogError($"Cloudinary upload failed: {uploadResult.Error?.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image to Cloudinary: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogWarning("Empty image URL provided for deletion");
                    return false;
                }

                // Extract public ID from Cloudinary URL
                // URL format: https://res.cloudinary.com/{cloud_name}/image/upload/v{version}/{folder}/{public_id}.{format}
                if (imageUrl.StartsWith("http"))
                {
                    var uri = new Uri(imageUrl);
                    var pathSegments = uri.AbsolutePath.Split('/');
                    var uploadIndex = Array.IndexOf(pathSegments, "upload");
                    
                    if (uploadIndex >= 0 && uploadIndex < pathSegments.Length - 1)
                    {
                        // Lấy các phần sau "upload", bỏ qua version (vXXXXXX) nếu có
                        var publicIdParts = pathSegments.Skip(uploadIndex + 1).ToList();
                        
                        if (publicIdParts.Count > 0 && publicIdParts[0].StartsWith("v"))
                        {
                            publicIdParts.RemoveAt(0); // Bỏ version
                        }
                        
                        // Ghép lại và bỏ extension
                        var publicIdWithExt = string.Join("/", publicIdParts);
                        var publicId = Path.GetFileNameWithoutExtension(publicIdWithExt);
                        
                        // Nếu có folder, thêm lại đầy đủ path
                        if (publicIdParts.Count > 1)
                        {
                            publicId = string.Join("/", publicIdParts.Take(publicIdParts.Count - 1)) + "/" + publicId;
                        }

                        var deletionParams = new DeletionParams(publicId);
                        var result = await _cloudinary.DestroyAsync(deletionParams);

                        if (result.Result == "ok")
                        {
                            _logger.LogInformation($"Image deleted successfully from Cloudinary: {imageUrl}");
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to delete image from Cloudinary: {result.Result} - {imageUrl}");
                            return false;
                        }
                    }
                }

                _logger.LogWarning($"Invalid Cloudinary URL format: {imageUrl}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image from Cloudinary: {imageUrl}");
                return false;
            }
        }

        public async Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folder = "products")
        {
            if (files == null || files.Count == 0)
            {
                _logger.LogWarning("Empty file list provided for upload");
                return new List<string>();
            }

            var uploadedUrls = new List<string>();
            var successCount = 0;
            var failCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var url = await UploadImageAsync(file, folder);
                    if (!string.IsNullOrEmpty(url))
                    {
                        uploadedUrls.Add(url);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload file: {file.FileName}");
                    failCount++;
                }
            }

            _logger.LogInformation($"Batch upload completed: {successCount} succeeded, {failCount} failed");
            return uploadedUrls;
        }
    }
}
