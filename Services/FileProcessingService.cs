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
        bool IsImageFile(string fileName);
        bool IsPdfFile(string fileName);
    }

    public class FileProcessingService : IFileProcessingService
    {
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private readonly string[] _pdfExtensions = { ".pdf" };

        /// <summary>
        /// Resizes an image from an IFormFile and returns the resized image as a byte array.
        /// Used for doctor pictures stored in database.
        /// </summary>
        public async Task<byte[]> ResizeImageAsync(IFormFile file, int maxWidth = 800, int maxHeight = 800, int quality = 75)
        {
            if (file == null || file.Length == 0)
                return Array.Empty<byte>();

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // If not an image, just return original bytes
            if (!IsImageFile(file.FileName))
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                return ms.ToArray();
            }

            using var inputStream = file.OpenReadStream();
            return await ProcessImageAsync(inputStream, maxWidth, maxHeight, quality, extension);
        }

        /// <summary>
        /// Resizes an image from byte array and returns the resized image as a byte array.
        /// </summary>
        public async Task<byte[]> ResizeImageAsync(byte[] imageData, int maxWidth = 800, int maxHeight = 800, int quality = 75)
        {
            if (imageData == null || imageData.Length == 0)
                return Array.Empty<byte>();

            using var inputStream = new MemoryStream(imageData);
            return await ProcessImageAsync(inputStream, maxWidth, maxHeight, quality, ".jpg");
        }

        /// <summary>
        /// Saves a resized file to disk and returns the relative path.
        /// Used for patient diagnosis files stored on disk.
        /// </summary>
        public async Task<string> SaveResizedFileAsync(IFormFile file, string uploadsFolder, int maxWidth = 1200, int maxHeight = 1200, int quality = 80)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}_{originalFileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // For images, resize before saving
            if (IsImageFile(originalFileName))
            {
                var resizedBytes = await ResizeImageAsync(file, maxWidth, maxHeight, quality);
                await File.WriteAllBytesAsync(filePath, resizedBytes);
            }
            // For PDFs and other files, save as-is (PDF compression requires specialized libraries)
            else
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        /// <summary>
        /// Checks if a file is an image based on its extension.
        /// </summary>
        public bool IsImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _imageExtensions.Contains(extension);
        }

        /// <summary>
        /// Checks if a file is a PDF based on its extension.
        /// </summary>
        public bool IsPdfFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _pdfExtensions.Contains(extension);
        }

        private async Task<byte[]> ProcessImageAsync(Stream inputStream, int maxWidth, int maxHeight, int quality, string extension)
        {
            using var image = await Image.LoadAsync(inputStream);

            // Calculate new dimensions while maintaining aspect ratio
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            // Only resize if image is larger than max dimensions
            if (ratio < 1)
            {
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            using var outputStream = new MemoryStream();

            // Save with compression based on file type
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
                // Use JPEG for all other formats (including jpg, gif, bmp) for better compression
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
