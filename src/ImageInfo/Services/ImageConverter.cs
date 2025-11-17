using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace ImageInfo.Services
{

    /// <summary>
    /// 图片格式转换工具，支持 PNG/JPEG/WebP 互转。
    /// 包含单文件转换和批量异步转换功能。
    /// </summary>
    public static class ImageConverter
    {
        /// <summary>
        /// 单个文件转换时的默认超时时间（毫秒）。
        /// </summary>
        private const int DefaultTimeout = 30000;
        /// <summary>
        /// 将 PNG 图片转换为 JPEG 格式，自动处理透明背景为白色。
        /// 返回输出文件路径（默认替换扩展名为 .jpg）。
        /// </summary>
        /// <param name="pngPath">源 PNG 文件路径</param>
        /// <param name="outPath">输出 JPEG 路径（可选，默认同名 .jpg）</param>
        /// <param name="quality">JPEG 压缩质量（1-100，默认85）</param>
        /// <returns>输出 JPEG 文件路径</returns>
        public static string ConvertPngToJpeg(string pngPath, string? outPath = null, int quality = 85)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(pngPath, ".jpg");

            using var image = new MagickImage(pngPath);
            image.Format = MagickFormat.Jpeg;
            image.Quality = quality;
            image.BackgroundColor = MagickColor.FromRgba(255, 255, 255, 255);
            image.Write(outPath);
            return outPath;
        }

        /// <summary>
        /// 将 PNG 图片转换为 WebP 格式（使用 Magick.NET）。
        /// 返回输出文件路径（默认同名 .webp）。
        /// </summary>
        /// <param name="pngPath">源 PNG 文件路径</param>
        /// <param name="outPath">输出 WebP 路径（可选，默认同名 .webp）</param>
        /// <param name="quality">WebP 压缩质量（1-100，默认80）</param>
        /// <returns>输出 WebP 文件路径</returns>
        public static string ConvertPngToWebP(string pngPath, string? outPath = null, int quality = 80)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(pngPath, ".webp");

            using var image = new MagickImage(pngPath);
            image.Format = MagickFormat.WebP;
            image.Quality = quality;
            image.Write(outPath);
            return outPath;
        }

        /// <summary>
        /// 将 JPEG 图片转换为 WebP 格式（使用 Magick.NET）。
        /// 返回输出文件路径（默认同名 .webp）。
        /// </summary>
        /// <param name="jpgPath">源 JPEG 文件路径</param>
        /// <param name="outPath">输出 WebP 路径（可选，默认同名 .webp）</param>
        /// <param name="quality">WebP 压缩质量（1-100，默认80）</param>
        /// <returns>输出 WebP 文件路径</returns>
        public static string ConvertJpegToWebP(string jpgPath, string? outPath = null, int quality = 80)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(jpgPath, ".webp");

            using var image = new MagickImage(jpgPath);
            image.Format = MagickFormat.WebP;
            image.Quality = quality;
            image.Write(outPath);
            return outPath;
        }

        /// <summary>
        /// 将 WebP 图片转换为 JPEG 格式（使用 Magick.NET）。
        /// 返回输出文件路径（默认同名 .jpg）。
        /// </summary>
        public static string ConvertWebPToJpeg(string webpPath, string? outPath = null, int quality = 85)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(webpPath, ".jpg");

            using var image = new MagickImage(webpPath);
            image.Format = MagickFormat.Jpeg;
            image.Quality = quality;
            image.Write(outPath);
            return outPath;
        }


    }
}
