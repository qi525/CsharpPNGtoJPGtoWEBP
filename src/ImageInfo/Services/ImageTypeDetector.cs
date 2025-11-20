using System;
using System.IO;

namespace ImageInfo.Services
{
    /// <summary>
    /// 图片类型检测（扩展名+Magic Number）。
    /// </summary>
    public static class ImageTypeDetector
    {
        public enum ImageFormat
        {
            Unknown,
            PNG,
            JPEG,
            WebP
        }

        /// <summary>
        /// 检测图片类型（先扩展名后Magic Number）。
        /// </summary>
        /// <param name="filePath">图片文件路径</param>
        /// <returns>识别的图片格式</returns>
        public static ImageFormat DetectImageFormat(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return ImageFormat.Unknown;

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            ImageFormat formatFromExt = GetFormatFromExtension(extension);
            try
            {
                var formatFromMagic = VerifyByMagicNumber(filePath);
                if (formatFromMagic != ImageFormat.Unknown)
                    return formatFromMagic;
            }
            catch { }

            return formatFromExt;
        }

        /// <summary>
        /// 根据扩展名确定图片格式（快速路径）。
        /// </summary>
        private static ImageFormat GetFormatFromExtension(string extension)
        {
            return extension switch
            {
                "png" => ImageFormat.PNG,
                "jpg" or "jpeg" => ImageFormat.JPEG,
                "webp" => ImageFormat.WebP,
                _ => ImageFormat.Unknown
            };
        }

        /// <summary>
        /// 通过文件头 Magic Number 验证图片格式（前 12 字节）。
        /// </summary>
        private static ImageFormat VerifyByMagicNumber(string filePath)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                byte[] buffer = new byte[12];
                int bytesRead = fileStream.Read(buffer, 0, 12);

                if (bytesRead < 4)
                    return ImageFormat.Unknown;

                // PNG: 89 50 4E 47
                if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                    return ImageFormat.PNG;

                // JPEG: FF D8
                if (buffer[0] == 0xFF && buffer[1] == 0xD8)
                    return ImageFormat.JPEG;

                // WebP: 52 49 46 46 (RIFF) ... 57 45 42 50 (WEBP)
                if (bytesRead >= 12 && 
                    buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                    buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    return ImageFormat.WebP;

                return ImageFormat.Unknown;
            }
            catch
            {
                return ImageFormat.Unknown;
            }
        }

        /// <summary>
        /// 将 ImageFormat 枚举转换为字符串。
        /// </summary>
        public static string FormatToString(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.PNG => "PNG",
                ImageFormat.JPEG => "JPEG",
                ImageFormat.WebP => "WEBP",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 将字符串转换为 ImageFormat 枚举。
        /// </summary>
        public static ImageFormat StringToFormat(string formatString)
        {
            return formatString?.ToUpperInvariant() switch
            {
                "PNG" => ImageFormat.PNG,
                "JPG" or "JPEG" => ImageFormat.JPEG,
                "WEBP" => ImageFormat.WebP,
                _ => ImageFormat.Unknown
            };
        }
    }
}
