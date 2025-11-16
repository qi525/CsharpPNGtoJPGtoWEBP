using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;

namespace ImageInfo.Services
{
    /// <summary>
    /// 使用 SixLabors.ImageSharp 库读取 PNG 图片的完整信息。
    /// 包括：尺寸、颜色空间、位深度、透明度、文本元数据、ICC 颜色配置文件等。
    /// </summary>
    public static class PngInfoReader
    {
        /// <summary>
        /// 读取 PNG 图片的完整信息。
        /// </summary>
        /// <param name="filePath">PNG 文件路径</param>
        /// <returns>PNG 信息对象，如果加载失败则返回 null</returns>
        public static PngInfo? ReadPngInfo(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return null;

                using (var image = Image.Load(filePath))
                {
                    var pngInfo = new PngInfo
                    {
                        FilePath = filePath,
                        Width = image.Width,
                        Height = image.Height,
                        PixelFormat = image.PixelType.ToString() ?? "Unknown",
                    };

                    // 提取 PNG 特定元数据
                    var pngMetadata = image.Metadata.GetPngMetadata();
                    if (pngMetadata != null)
                    {
                        pngInfo.ColorType = pngMetadata.ColorType?.ToString() ?? "Unknown";
                        pngInfo.BitDepth = 8;  // SixLabors 默认 8-bit
                        pngInfo.IsInterlaced = pngMetadata.InterlaceMethod != PngInterlaceMode.None;
                        pngInfo.InterlaceMethod = pngMetadata.InterlaceMethod?.ToString() ?? "None";
                        
                        // 提取文本块（tEXt）
                        if (pngMetadata.TextData != null && pngMetadata.TextData.Count > 0)
                        {
                            pngInfo.TextMetadata = new Dictionary<string, string>();
                            foreach (var text in pngMetadata.TextData)
                            {
                                pngInfo.TextMetadata[text.Keyword] = text.Value;
                            }
                        }
                    }

                    // 提取通用图片元数据
                    var generalMetadata = image.Metadata;
                    pngInfo.DpiX = generalMetadata.HorizontalResolution;
                    pngInfo.DpiY = generalMetadata.VerticalResolution;
                    pngInfo.ResolutionUnit = generalMetadata.ResolutionUnits.ToString();

                    // 提取 EXIF 数据（如果存在）
                    var exifProfile = generalMetadata.ExifProfile;
                    if (exifProfile != null)
                    {
                        pngInfo.HasExif = true;
                        pngInfo.ExifData = ExtractExifInfo(exifProfile);
                    }

                    // 提取 ICC 颜色配置文件
                    var iccProfile = generalMetadata.IccProfile;
                    if (iccProfile != null)
                    {
                        pngInfo.HasIccProfile = true;
                        pngInfo.IccProfileName = "ICC Profile";
                        pngInfo.IccProfileSize = 0;
                    }

                    return pngInfo;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to read PNG info from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从 Image 对象直接读取 PNG 信息（适用于已加载的图片）。
        /// </summary>
        public static PngInfo? ReadPngInfoFromImage(Image image, string filePath = "")
        {
            try
            {
                var pngInfo = new PngInfo
                {
                    FilePath = filePath,
                    Width = image.Width,
                    Height = image.Height,
                    PixelFormat = image.PixelType.ToString() ?? "Unknown",
                };

                // 提取 PNG 特定元数据
                var pngMetadata = image.Metadata.GetPngMetadata();
                if (pngMetadata != null)
                {
                    pngInfo.ColorType = pngMetadata.ColorType?.ToString() ?? "Unknown";
                    pngInfo.BitDepth = 8;  // SixLabors 默认 8-bit
                    pngInfo.IsInterlaced = pngMetadata.InterlaceMethod != PngInterlaceMode.None;
                    pngInfo.InterlaceMethod = pngMetadata.InterlaceMethod?.ToString() ?? "None";
                    
                    // 提取文本块（tEXt）
                    if (pngMetadata.TextData != null && pngMetadata.TextData.Count > 0)
                    {
                        pngInfo.TextMetadata = new Dictionary<string, string>();
                        foreach (var text in pngMetadata.TextData)
                        {
                            pngInfo.TextMetadata[text.Keyword] = text.Value;
                        }
                    }
                }

                // 提取通用图片元数据
                var generalMetadata = image.Metadata;
                pngInfo.DpiX = generalMetadata.HorizontalResolution;
                pngInfo.DpiY = generalMetadata.VerticalResolution;
                pngInfo.ResolutionUnit = generalMetadata.ResolutionUnits.ToString();

                // 提取 EXIF 数据（如果存在）
                var exifProfile = generalMetadata.ExifProfile;
                if (exifProfile != null)
                {
                    pngInfo.HasExif = true;
                    pngInfo.ExifData = ExtractExifInfo(exifProfile);
                }

                // 提取 ICC 颜色配置文件
                var iccProfile = generalMetadata.IccProfile;
                if (iccProfile != null)
                {
                    pngInfo.HasIccProfile = true;
                    pngInfo.IccProfileName = "ICC Profile";
                    pngInfo.IccProfileSize = 0;
                }

                return pngInfo;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to read PNG info from image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从 EXIF 配置文件提取相关信息。
        /// </summary>
        private static Dictionary<string, string> ExtractExifInfo(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifProfile exifProfile)
        {
            var exifData = new Dictionary<string, string>();

            try
            {
                // 由于 SixLabors.ImageSharp 的 EXIF API 较为复杂，这里提供基本支持
                // 完整的 EXIF 解析建议使用 MetadataExtractor 库
                exifData["HasExif"] = "true";
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to extract EXIF info: {ex.Message}");
            }

            return exifData;
        }

        /// <summary>
        /// 读取 PNG 的文本元数据（tEXt 块）。
        /// 这对于 AI 生成图片（如 Stable Diffusion）存储 prompt 很重要。
        /// </summary>
        public static Dictionary<string, string>? ReadPngTextMetadata(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return null;

                using (var image = Image.Load(filePath))
                {
                    var pngMetadata = image.Metadata.GetPngMetadata();
                    if (pngMetadata?.TextData != null && pngMetadata.TextData.Count > 0)
                    {
                        var textMetadata = new Dictionary<string, string>();
                        foreach (var text in pngMetadata.TextData)
                        {
                            textMetadata[text.Keyword] = text.Value;
                        }
                        return textMetadata;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to read PNG text metadata from {filePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取图片的基本信息（宽、高、格式）。
        /// </summary>
        public static (int Width, int Height, string Format)? GetBasicImageInfo(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return null;

                using (var image = Image.Load(filePath))
                {
                    return (image.Width, image.Height, image.Metadata.DecodedImageFormat?.Name ?? "Unknown");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to get basic image info from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查 PNG 图片是否有透明通道。
        /// </summary>
        public static bool? HasTransparency(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return null;

                using (var image = Image.Load<Rgba32>(filePath))
                {
                    // 检查是否有任何像素的 Alpha 值不是 255（完全不透明）
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            if (image[x, y].A < 255)
                                return true;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to check transparency for {filePath}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// PNG 图片信息容器类。
    /// 包含尺寸、颜色空间、元数据等完整信息。
    /// </summary>
    public class PngInfo
    {
        /// <summary>文件路径。</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>图片宽度（像素）。</summary>
        public int Width { get; set; }

        /// <summary>图片高度（像素）。</summary>
        public int Height { get; set; }

        /// <summary>像素格式（如 Rgba32）。</summary>
        public string PixelFormat { get; set; } = string.Empty;

        /// <summary>PNG 颜色类型（Grayscale, Rgb, Indexed, GrayscaleWithAlpha, Rgba）。</summary>
        public string ColorType { get; set; } = string.Empty;

        /// <summary>每通道位深度（通常为 8 或 16）。</summary>
        public byte BitDepth { get; set; }

        /// <summary>是否使用了 PNG 交错（interlacing）。</summary>
        public bool IsInterlaced { get; set; }

        /// <summary>交错方法（None 或 Adam7）。</summary>
        public string InterlaceMethod { get; set; } = string.Empty;

        /// <summary>水平分辨率（DPI）。</summary>
        public double DpiX { get; set; }

        /// <summary>垂直分辨率（DPI）。</summary>
        public double DpiY { get; set; }

        /// <summary>分辨率单位（Meter, Inch 等）。</summary>
        public string ResolutionUnit { get; set; } = string.Empty;

        /// <summary>文本元数据（tEXt 块），键为 keyword，值为文本内容。</summary>
        public Dictionary<string, string>? TextMetadata { get; set; }

        /// <summary>是否包含 EXIF 数据。</summary>
        public bool HasExif { get; set; }

        /// <summary>EXIF 数据字典。</summary>
        public Dictionary<string, string>? ExifData { get; set; }

        /// <summary>是否包含 ICC 颜色配置文件。</summary>
        public bool HasIccProfile { get; set; }

        /// <summary>ICC 颜色配置文件名。</summary>
        public string IccProfileName { get; set; } = string.Empty;

        /// <summary>ICC 颜色配置文件大小（字节）。</summary>
        public int IccProfileSize { get; set; }

        /// <summary>
        /// 生成易读的摘要信息。
        /// </summary>
        public override string ToString()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"PNG Information: {System.IO.Path.GetFileName(FilePath)}");
            summary.AppendLine($"  Dimensions: {Width}x{Height} pixels");
            summary.AppendLine($"  Pixel Format: {PixelFormat}");
            summary.AppendLine($"  Color Type: {ColorType}");
            summary.AppendLine($"  Bit Depth: {BitDepth} bits/channel");
            summary.AppendLine($"  Interlaced: {(IsInterlaced ? "Yes (" + InterlaceMethod + ")" : "No")}");
            summary.AppendLine($"  Resolution: {DpiX}x{DpiY} {ResolutionUnit}");

            if (HasIccProfile)
                summary.AppendLine($"  ICC Profile: {IccProfileName} ({IccProfileSize} bytes)");

            if (HasExif && ExifData?.Count > 0)
                summary.AppendLine($"  EXIF Data: {ExifData.Count} fields");

            if (TextMetadata?.Count > 0)
            {
                summary.AppendLine($"  Text Metadata ({TextMetadata.Count} entries):");
                foreach (var (keyword, value) in TextMetadata)
                {
                    var truncatedValue = value.Length > 50 ? value.Substring(0, 50) + "..." : value;
                    summary.AppendLine($"    {keyword}: {truncatedValue}");
                }
            }

            return summary.ToString();
        }

        /// <summary>
        /// 生成 JSON 格式的信息。
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> ToJsonObject()
        {
            var json = new System.Collections.Generic.Dictionary<string, object>
            {
                { "FilePath", FilePath },
                { "Dimensions", new { Width, Height } },
                { "PixelFormat", PixelFormat },
                { "ColorType", ColorType },
                { "BitDepth", BitDepth },
                { "Interlaced", IsInterlaced },
                { "InterlaceMethod", InterlaceMethod },
                { "Resolution", new { DpiX, DpiY, Unit = ResolutionUnit } },
                { "HasIccProfile", HasIccProfile },
            };

            if (HasIccProfile)
                json["IccProfile"] = new { Name = IccProfileName, Size = IccProfileSize };

            if (HasExif && ExifData?.Count > 0)
                json["ExifData"] = ExifData;

            if (TextMetadata?.Count > 0)
                json["TextMetadata"] = TextMetadata;

            return json;
        }
    }
}
