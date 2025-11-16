using System;
using System.IO;

namespace ImageInfo.Services
{
    /// <summary>
    /// 图片类型检测服务。
    /// 根据文件扩展名和 Magic Number（文件头）识别图片格式。
    /// </summary>
    public static class ImageTypeDetector
    {
        /// <summary>
        /// 支持的图片格式枚举。
        /// </summary>
        public enum ImageFormat
        {
            Unknown,
            PNG,
            JPEG,
            WebP
        }

        /// <summary>
        /// 根据文件路径检测图片类型。
        /// 
        /// 检测顺序：
        /// 1. 先检查文件扩展名（快速路径）
        /// 2. 如果扩展名不可靠，读取文件头 Magic Number 进行确认
        /// 
        /// Magic Number 参考：
        /// - PNG:  0x89 0x50 0x4E 0x47 (89 50 4E 47)
        /// - JPEG: 0xFF 0xD8 ... 0xFF 0xD9
        /// - WebP: 52 49 46 46 ... 57 45 42 50 ("RIFF" ... "WEBP")
        /// </summary>
        /// <param name="filePath">图片文件路径</param>
        /// <returns>识别的图片格式</returns>
        public static ImageFormat DetectImageFormat(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return ImageFormat.Unknown;

            // 【步骤 1】基于扩展名的快速检测
            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            ImageFormat formatFromExt = GetFormatFromExtension(extension);

            // 【步骤 2】通过 Magic Number 验证（可选，提高准确性）
            try
            {
                ImageFormat formatFromMagic = VerifyByMagicNumber(filePath);
                
                // 如果 Magic Number 检测成功，使用其结果（优先级更高）
                if (formatFromMagic != ImageFormat.Unknown)
                    return formatFromMagic;
            }
            catch
            {
                // Magic Number 读取失败时，回退到扩展名结果
            }

            return formatFromExt;
        }

        /// <summary>
        /// 根据扩展名确定图片格式。
        /// 快速路径，不涉及磁盘 I/O。
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
        /// 通过文件头 Magic Number 验证图片格式。
        /// 读取文件前 12 字节进行检测。
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
