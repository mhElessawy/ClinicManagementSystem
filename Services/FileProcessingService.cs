using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ClinicManagementSystem.Services
{
    public interface IFileProcessingService
    {
        Task<byte[]> ResizeImageAsync(IFormFile file, int maxWidth = 800, int maxHeight = 800, int quality = 75);
        Task<byte[]> ResizeImageAsync(byte[] imageData, int maxWidth = 800, int maxHeight = 800, int quality = 75);
        Task<string> SaveResizedFileAsync(IFormFile file, string uploadsFolder, int maxWidth = 1200, int maxHeight = 1200, int quality = 80);
        Task<string> SaveDoctorPictureAsync(IFormFile file, string webRootPath, int maxWidth = 400, int maxHeight = 400, int quality = 75);
        void DeleteDoctorPicture(string? picturePath, string webRootPath);
        bool IsImageFile(string fileName);
        bool IsPdfFile(string fileName);
    }

    public class FileProcessingService : IFileProcessingService
    {
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private readonly string[] _pdfExtensions = { ".pdf" };

        public async Task<byte[]> ResizeImageAsync(IFormFile file, int maxWidth = 800, int maxHeight = 800, int quality = 75)
        {
            if (file == null || file.Length == 0)
                return Array.Empty<byte>();

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!IsImageFile(file.FileName))
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                return ms.ToArray();
            }

            using var inputStream = file.OpenReadStream();
            return await ProcessImageAsync(inputStream, maxWidth, maxHeight, quality, extension);
        }

        public async Task<byte[]> ResizeImageAsync(byte[] imageData, int maxWidth = 800, int maxHeight = 800, int quality = 75)
        {
            if (imageData == null || imageData.Length == 0)
                return Array.Empty<byte>();

            using var inputStream = new MemoryStream(imageData);
            return await ProcessImageAsync(inputStream, maxWidth, maxHeight, quality, ".jpg");
        }

        public async Task<string> SaveResizedFileAsync(IFormFile file, string uploadsFolder, int maxWidth = 1200, int maxHeight = 1200, int quality = 80)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}_{originalFileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (IsImageFile(originalFileName))
            {
                var resizedBytes = await ResizeImageAsync(file, maxWidth, maxHeight, quality);
                await File.WriteAllBytesAsync(filePath, resizedBytes);
            }
            else
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public bool IsImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _imageExtensions.Contains(extension);
        }

        public bool IsPdfFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _pdfExtensions.Contains(extension);
        }

        public async Task<string> SaveDoctorPictureAsync(IFormFile file, string webRootPath, int maxWidth = 400, int maxHeight = 400, int quality = 75)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            if (!IsImageFile(file.FileName))
                return string.Empty;

            // Create the uploads/doctors directory if it doesn't exist
            var uploadsFolder = Path.Combine(webRootPath, "uploads", "doctors");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            // Always save as jpg for consistency
            var fileName = $"{Guid.NewGuid()}.jpg";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Resize and save the image
            var resizedBytes = await ResizeImageAsync(file, maxWidth, maxHeight, quality);
            await File.WriteAllBytesAsync(filePath, resizedBytes);

            // Return relative path for storage in database
            return $"/uploads/doctors/{fileName}";
        }

        public void DeleteDoctorPicture(string? picturePath, string webRootPath)
        {
            if (string.IsNullOrEmpty(picturePath))
                return;

            try
            {
                // Convert relative URL path to physical path
                var relativePath = picturePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(webRootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Silently ignore delete errors
            }
        }

        private async Task<byte[]> ProcessImageAsync(Stream inputStream, int maxWidth, int maxHeight, int quality, string extension)
        {
            using var image = await Image.LoadAsync(inputStream);

            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            if (ratio < 1)
            {
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            using var outputStream = new MemoryStream();

            if (extension == ".png")
            {
                var encoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                };
                await image.SaveAsync(outputStream, encoder);
            }
            else
            {
                var encoder = new JpegEncoder
                {
                    Quality = quality
                };
                await image.SaveAsync(outputStream, encoder);
            }

            return outputStream.ToArray();
        }
    }
}
