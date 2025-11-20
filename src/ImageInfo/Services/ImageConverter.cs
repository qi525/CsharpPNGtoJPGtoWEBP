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
    /// 图片格式转换工具（PNG/JPEG/WebP）。
    /// </summary>
    public static class ImageConverter
    {
        private const int DefaultTimeout = 30000;
        /// <summary>PNG → JPEG 转换（透明背景转白色，质量 1-100）。</summary>
        public static string ConvertPngToJpeg(string pngPath, string? outPath = null, int quality = 85)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(pngPath, ".jpg");

            using var image = new MagickImage(pngPath);
            image.Format = MagickFormat.Jpeg;
            image.Quality = (uint)quality;
            image.BackgroundColor = MagickColor.FromRgba(255, 255, 255, 255);
            image.Write(outPath);
            return outPath;
        }

        /// <summary>PNG → WebP 转换（质量 1-100）。</summary>
        public static string ConvertPngToWebP(string pngPath, string? outPath = null, int quality = 80)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(pngPath, ".webp");

            using var image = new MagickImage(pngPath);
            image.Format = MagickFormat.WebP;
            image.Quality = (uint)quality;
            image.Write(outPath);
            return outPath;
        }

        /// <summary>JPEG → WebP 转换（质量 1-100）。</summary>
        public static string ConvertJpegToWebP(string jpgPath, string? outPath = null, int quality = 80)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(jpgPath, ".webp");

            using var image = new MagickImage(jpgPath);
            image.Format = MagickFormat.WebP;
            image.Quality = (uint)quality;
            image.Write(outPath);
            return outPath;
        }

        /// <summary>WebP → JPEG 转换（质量 1-100）。</summary>
        public static string ConvertWebPToJpeg(string webpPath, string? outPath = null, int quality = 85)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                outPath = Path.ChangeExtension(webpPath, ".jpg");
            using var image = new MagickImage(webpPath);
            image.Format = MagickFormat.Jpeg;
            image.Quality = (uint)quality;
            image.Write(outPath);
            return outPath;
        }
    }
}
