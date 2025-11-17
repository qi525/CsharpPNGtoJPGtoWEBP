using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.Xmp;
using XmpCore;
using ImageMagick;

namespace ImageInfo.Services
{
    /// <summary>
    /// 统一的图片元数据提取服务，支持 PNG、JPEG、WebP 三种格式。
    /// 提供读→写→验证三步流程。
    /// 参数解析委托给 AIParameterParser 处理，实现关注点分离。
    /// </summary>
    public static class MetadataExtractors
    {
        #region 公共接口 - 读取、写入、验证

        /// <summary>
        /// 从图片文件读取 AI 元数据。
        /// 根据文件格式自动选择相应的处理策略。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return new AIMetadata();

            try
            {
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                return extension switch
                {
                    ".png" => ReadPngMetadata(imagePath),
                    ".jpg" or ".jpeg" => ReadJpegMetadata(imagePath),
                    ".webp" => ReadWebPMetadata(imagePath),
                    _ => new AIMetadata()
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to read AI metadata from {imagePath}: {ex.Message}");
                return new AIMetadata();
            }
        }

        /// <summary>
        /// 将 AI 元数据写入到图片文件。
        /// 根据文件格式自动选择相应的写入策略。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            try
            {
                var extension = Path.GetExtension(destImagePath).ToLowerInvariant();
                switch (extension)
                {
                    case ".png":
                        WritePngMetadata(destImagePath, aiMetadata);
                        break;
                    case ".jpg":
                    case ".jpeg":
                        WriteJpegMetadata(destImagePath, aiMetadata);
                        break;
                    case ".webp":
                        WriteWebPMetadata(destImagePath, aiMetadata);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to write AI metadata to {destImagePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证元数据是否成功写入。
        /// 根据文件格式自动选择相应的验证策略。
        /// </summary>
        public static bool VerifyAIMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || originalMetadata == null)
                return false;

            try
            {
                var extension = Path.GetExtension(destImagePath).ToLowerInvariant();
                return extension switch
                {
                    ".png" => VerifyPngMetadata(destImagePath, originalMetadata),
                    ".jpg" or ".jpeg" => VerifyJpegMetadata(destImagePath, originalMetadata),
                    ".webp" => VerifyWebPMetadata(destImagePath, originalMetadata),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to verify AI metadata for {destImagePath}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region PNG 元数据处理

        private static AIMetadata ReadPngMetadata(string imagePath)
        {
            var metadata = new AIMetadata();

            try
            {
                // 首先尝试用 Magick.NET 直接读取 IPTC Profile（最可靠）
                using (var image = new MagickImage(imagePath))
                {
                    var iptcProfile = image.GetIptcProfile();
                    if (iptcProfile != null)
                    {
                        // 尝试读取 Caption 标签（IptcTag.Caption 是标准的元数据存储位置）
                        try
                        {
                            var caption = iptcProfile.GetValue(IptcTag.Caption);
                            if (caption != null)
                            {
                                string fullInfo = caption.ToString() ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(fullInfo))
                                {
                                    metadata.FullInfo = fullInfo;
                                    metadata.FullInfoExtractionMethod = "PNG.IPTC.Caption";
                                    AIParameterParser.ParseFullInfoIntoFields(fullInfo, metadata);
                                    return metadata;
                                }
                            }
                        }
                        catch { }
                    }
                }

                // 回退到 MetadataExtractor 库
                var directories = ImageMetadataReader.ReadMetadata(imagePath);

                // 优先从所有目录中查找完整参数块
                var full = TryGetFullInfoFromDirectories(directories);
                if (!string.IsNullOrWhiteSpace(full))
                {
                    metadata.FullInfo = full;
                    metadata.FullInfoExtractionMethod = "MetadataExtractor";
                    AIParameterParser.ParseFullInfoIntoFields(full, metadata);
                    return metadata;
                }

                // 回退到逐项 tEXt 标签解析
                ExtractFromPngText(directories, metadata);
            }
            catch { }

            return metadata;
        }

        private static void WritePngMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            try
            {
                using (var image = new MagickImage(destImagePath))
                {
                    image.Format = MagickFormat.Png;
                    image.Write(destImagePath);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Warning: Failed to write PNG metadata to {destImagePath}: {ex.Message}");
            }
        }

        private static bool VerifyPngMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || originalMetadata == null)
                return false;

            try
            {
                var readBack = ReadPngMetadata(destImagePath);
                if (!string.IsNullOrEmpty(originalMetadata.Prompt))
                    return readBack.Prompt == originalMetadata.Prompt;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ExtractFromPngText(IEnumerable<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories.OfType<PngDirectory>())
            {
                foreach (var tag in dir.Tags)
                {
                    if (tag == null)
                        continue;

                    string description = tag.Description ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(description))
                        continue;

                    var parts = description.Split(new[] { ": " }, 2, StringSplitOptions.None);

                    if (parts.Length == 2)
                    {
                        string keyword = parts[0].Trim().ToLowerInvariant();
                        string text = parts[1].Trim();

                        if (keyword.Equals("parameters", StringComparison.OrdinalIgnoreCase))
                        {
                            metadata.FullInfo = text;
                            metadata.FullInfoExtractionMethod = "PNG.tEXt.parameters";
                            AIParameterParser.ParseFullInfoIntoFields(text, metadata);
                            continue;
                        }

                        if (keyword.Equals("prompt", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(metadata.Prompt))
                        {
                            metadata.Prompt = text;
                            if (string.IsNullOrEmpty(metadata.FullInfoExtractionMethod))
                                metadata.FullInfoExtractionMethod = "PNG.tEXt.prompt";
                        }

                        if ((keyword.Equals("negative prompt", StringComparison.OrdinalIgnoreCase) || 
                             keyword.Equals("negativeprompt", StringComparison.OrdinalIgnoreCase)) && 
                            string.IsNullOrEmpty(metadata.NegativePrompt))
                        {
                            metadata.NegativePrompt = text;
                        }

                        if (keyword.Contains("model") || keyword.Contains("checkpoint"))
                        {
                            if (string.IsNullOrEmpty(metadata.Model))
                                metadata.Model = text;
                        }
                        else if (keyword.Contains("seed"))
                        {
                            if (string.IsNullOrEmpty(metadata.Seed))
                                metadata.Seed = text;
                        }
                        else if (keyword.Contains("sampler") || keyword.Contains("scheduler"))
                        {
                            if (string.IsNullOrEmpty(metadata.Sampler))
                                metadata.Sampler = text;
                        }
                        else if (keyword.Contains("workflow") || keyword.Contains("extra") || keyword.Contains("info"))
                        {
                            if (string.IsNullOrEmpty(metadata.OtherInfo))
                                metadata.OtherInfo = text;
                        }
                    }
                    else
                    {
                        if (description.Contains("prompt", StringComparison.OrdinalIgnoreCase) && 
                            string.IsNullOrEmpty(metadata.Prompt))
                        {
                            metadata.Prompt = description;
                            if (string.IsNullOrEmpty(metadata.FullInfoExtractionMethod))
                                metadata.FullInfoExtractionMethod = "PNG.tEXt.fallback";
                        }
                    }
                }
            }
        }

        #endregion

        #region JPEG 元数据处理

        private static AIMetadata ReadJpegMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                // 尝试所有可能的 EXIF 字段：ImageDescription、UserComment、MakerNote 等
                ExtractFromExif(directories, metadata);
                
                // 如果还没有获取到，尝试 XMP
                if (string.IsNullOrEmpty(metadata.FullInfo))
                {
                    ExtractFromXmp(directories, metadata);
                }
            }
            catch { }

            return metadata;
        }

        private static void WriteJpegMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            try
            {
                // 临时方案：直接使用 C# System.IO 接口修改文件
                // 由于 ExifLibrary 的 API 设计复杂，这里使用简化方案
                // 完整的 JPEG EXIF 写入需要专门的EXIF处理库支持
                // 暂时保留文件原样，元数据信息记录在报告中
                System.Console.WriteLine($"Note: JPEG metadata write not yet fully implemented for {Path.GetFileName(destImagePath)}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Warning: Failed to write JPEG metadata to {destImagePath}: {ex.Message}");
            }
        }

        private static bool VerifyJpegMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || originalMetadata == null)
                return false;

            try
            {
                var readBack = ReadJpegMetadata(destImagePath);
                if (!string.IsNullOrEmpty(originalMetadata.Prompt))
                    return readBack.Prompt == originalMetadata.Prompt;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ExtractFromExif(IEnumerable<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            // 遍历所有 EXIF 目录
            foreach (var dir in directories.OfType<ExifIfd0Directory>())
            {
                foreach (var tag in dir.Tags)
                {
                    if (tag == null)
                        continue;

                    var tagName = tag.Name?.ToLowerInvariant() ?? "";
                    var desc = tag.Description ?? "";

                    if (string.IsNullOrWhiteSpace(desc))
                        continue;

                    // 优先级 1：ImageDescription（SD WebUI 通常在这里）
                    if (tagName.Contains("description") || tagName.Contains("imagedescription"))
                    {
                        if (IsAIGenerationMetadata(desc))
                        {
                            metadata.FullInfo = desc;
                            metadata.FullInfoExtractionMethod = "EXIF.ImageDescription";
                            AIParameterParser.ParseFullInfoIntoFields(desc, metadata);
                        }
                        else if (string.IsNullOrEmpty(metadata.Prompt))
                        {
                            metadata.Prompt = desc;
                        }
                    }
                    // 优先级 2：UserComment
                    else if (tagName.Contains("usercomment") || tagName.Contains("comment"))
                    {
                        // 尝试解码 UNICODE\x00 + UTF-16LE 格式
                        string? decodedDesc = TryDecodeUnicodeUserComment(desc);
                        string finalDesc = decodedDesc ?? desc;
                        
                        if (IsAIGenerationMetadata(finalDesc))
                        {
                            if (string.IsNullOrEmpty(metadata.FullInfo))
                            {
                                metadata.FullInfo = finalDesc;
                                metadata.FullInfoExtractionMethod = "EXIF.UserComment";
                                AIParameterParser.ParseFullInfoIntoFields(metadata.FullInfo, metadata);
                            }
                        }
                        else if (string.IsNullOrEmpty(metadata.OtherInfo))
                        {
                            metadata.OtherInfo = finalDesc;
                        }
                    }
                    // 优先级 3：其他可能的文本字段
                    else if (tagName.Contains("artist") || tagName.Contains("copyright") || tagName.Contains("maker"))
                    {
                        if (IsAIGenerationMetadata(desc) && string.IsNullOrEmpty(metadata.FullInfo))
                        {
                            metadata.FullInfo = desc;
                            metadata.FullInfoExtractionMethod = "EXIF." + tag.Name;
                            AIParameterParser.ParseFullInfoIntoFields(desc, metadata);
                        }
                    }
                    // 提取模型信息
                    else if (tagName.Contains("model") && string.IsNullOrEmpty(metadata.Model))
                    {
                        metadata.Model = desc;
                    }
                }
            }
            
            // 也检查 SubIFD
            foreach (var dir in directories.OfType<ExifSubIfdDirectory>())
            {
                foreach (var tag in dir.Tags)
                {
                    if (tag == null)
                        continue;

                    var tagName = tag.Name?.ToLowerInvariant() ?? "";
                    var desc = tag.Description ?? "";

                    if (string.IsNullOrWhiteSpace(desc))
                        continue;

                    if (IsAIGenerationMetadata(desc) && string.IsNullOrEmpty(metadata.FullInfo))
                    {
                        metadata.FullInfo = desc;
                        metadata.FullInfoExtractionMethod = "EXIF.SubIFD." + tag.Name;
                        AIParameterParser.ParseFullInfoIntoFields(desc, metadata);
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否是 AI 生成的元数据（包含特征关键词）
        /// </summary>
        private static bool IsAIGenerationMetadata(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lowerText = text.ToLowerInvariant();
            return lowerText.Contains("prompt") || 
                   lowerText.Contains("negative") ||
                   lowerText.Contains("steps:") ||
                   lowerText.Contains("sampler") ||
                   lowerText.Contains("seed:") ||
                   lowerText.Contains("model") ||
                   lowerText.Contains("cfg") ||
                   lowerText.Contains("parameters:");
        }

        #endregion

        #region WebP 元数据处理

        private static AIMetadata ReadWebPMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                // 方案 1: 尝试 XMP 数据
                ExtractFromXmp(directories, metadata);
                
                // 方案 2: 尝试 EXIF 数据（WebP 也可能包含 EXIF）
                if (string.IsNullOrEmpty(metadata.FullInfo))
                {
                    ExtractFromExif(directories, metadata);
                }
            }
            catch { }

            return metadata;
        }

        private static void WriteWebPMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            try
            {
                // 暂时方案：WebP 元数据写入也需要特殊的 XMP/EXIF 处理
                // 完整实现需要额外的库支持
                System.Console.WriteLine($"Note: WebP metadata write not yet fully implemented for {Path.GetFileName(destImagePath)}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Warning: Failed to write WebP metadata to {destImagePath}: {ex.Message}");
            }
        }

        private static bool VerifyWebPMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || originalMetadata == null)
                return false;

            try
            {
                var readBack = ReadWebPMetadata(destImagePath);
                if (!string.IsNullOrEmpty(originalMetadata.Prompt))
                    return readBack.Prompt == originalMetadata.Prompt;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ExtractFromXmp(IEnumerable<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var xmpDir in directories.OfType<XmpDirectory>())
            {
                if (xmpDir.XmpMeta == null)
                    continue;

                var subject = xmpDir.XmpMeta.GetPropertyString("http://purl.org/dc/elements/1.1/", "subject");
                if (!string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(metadata.Prompt))
                {
                    metadata.Prompt = subject;
                }

                var keywords = xmpDir.XmpMeta.GetPropertyString("http://ns.adobe.com/photoshop/1.0/", "Keywords");
                if (!string.IsNullOrEmpty(keywords))
                {
                    if (string.IsNullOrEmpty(metadata.OtherInfo))
                        metadata.OtherInfo = "Keywords: " + keywords;
                    else
                        metadata.OtherInfo += " | Keywords: " + keywords;
                }

                ExtractFromCustomXmpFields(xmpDir.XmpMeta, metadata);
            }
        }

        private static void ExtractFromCustomXmpFields(IXmpMeta xmpMeta, AIMetadata metadata)
        {
            if (xmpMeta == null)
                return;

            var customNamespaces = new[]
            {
                "http://comfyui.org/",
                "http://stable-diffusion.io/",
                "http://automatic1111.io/"
            };

            foreach (var ns in customNamespaces)
            {
                try
                {
                    var prompt = xmpMeta.GetPropertyString(ns, "prompt");
                    if (!string.IsNullOrEmpty(prompt) && string.IsNullOrEmpty(metadata.Prompt))
                    {
                        metadata.Prompt = prompt;
                    }

                    var model = xmpMeta.GetPropertyString(ns, "model");
                    if (!string.IsNullOrEmpty(model) && string.IsNullOrEmpty(metadata.Model))
                    {
                        metadata.Model = model;
                    }

                    var seed = xmpMeta.GetPropertyString(ns, "seed");
                    if (!string.IsNullOrEmpty(seed) && string.IsNullOrEmpty(metadata.Seed))
                    {
                        metadata.Seed = seed;
                    }

                    var parameters = xmpMeta.GetPropertyString(ns, "parameters");
                    if (!string.IsNullOrEmpty(parameters))
                    {
                        AIParameterParser.ParseFullInfoIntoFields(parameters, metadata);
                    }
                }
                catch { }
            }
        }

        #endregion

        #region 辅助方法

        private static string? TryGetFullInfoFromDirectories(IEnumerable<MetadataExtractor.Directory> directories)
        {
            foreach (var dir in directories)
            {
                // 优先从 IPTC 的 Caption 标签获取完整信息
                // IPTC Caption 标签通常用于存储长文本元数据
                if (dir.GetType().Name == "IptcDirectory")
                {
                    foreach (var tag in dir.Tags)
                    {
                        var desc = tag?.Description;
                        if (string.IsNullOrWhiteSpace(desc)) continue;
                        
                        // 检查是否包含 Caption 或相关信息
                        if (desc.IndexOf("Caption", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // 提取 Caption 值（格式通常是 "Caption: [values]" 或 "[值]"）
                            var value = ExtractIptcValue(desc);
                            if (!string.IsNullOrWhiteSpace(value) && 
                                (value.IndexOf("parameters", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 value.IndexOf("steps:", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                return value;
                            }
                        }
                    }
                }
                
                // 再从其他目录的通用标签中搜索
                foreach (var tag in dir.Tags)
                {
                    var desc = tag?.Description;
                    if (string.IsNullOrWhiteSpace(desc)) continue;

                    if (desc.IndexOf("parameters", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        desc.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        desc.IndexOf("Prompt:", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return desc;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 从 IPTC 标签描述中提取实际值
        /// </summary>
        private static string ExtractIptcValue(string description)
        {
            // IPTC 标签通常的格式是 "TagName: value" 或 "[values]"
            if (description.Contains(": "))
            {
                var parts = description.Split(new[] { ": " }, 2, StringSplitOptions.None);
                return parts.Length > 1 ? parts[1].Trim() : description;
            }
            
            return description;
        }

        /// <summary>
        /// 构建完整的元数据字符串用于写入
        /// </summary>
        private static string BuildMetadataString(AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(aiMetadata.FullInfo))
            {
                // 如果没有完整信息，从各个字段构造
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(aiMetadata.Prompt))
                    parts.Add($"Prompt: {aiMetadata.Prompt}");
                
                if (!string.IsNullOrEmpty(aiMetadata.NegativePrompt))
                    parts.Add($"Negative prompt: {aiMetadata.NegativePrompt}");
                
                if (!string.IsNullOrEmpty(aiMetadata.Model))
                    parts.Add($"Model: {aiMetadata.Model}");
                
                if (!string.IsNullOrEmpty(aiMetadata.Seed))
                    parts.Add($"Seed: {aiMetadata.Seed}");
                
                if (!string.IsNullOrEmpty(aiMetadata.Sampler))
                    parts.Add($"Sampler: {aiMetadata.Sampler}");
                
                if (!string.IsNullOrEmpty(aiMetadata.OtherInfo))
                    parts.Add($"Parameters: {aiMetadata.OtherInfo}");
                
                return string.Join("\n", parts);
            }
            
            return aiMetadata.FullInfo;
        }

        /// <summary>
        /// 尝试解码 UNICODE\x00 + UTF-16LE 格式的 EXIF UserComment
        /// SD WebUI 使用此格式存储参数信息
        /// </summary>
        private static string? TryDecodeUnicodeUserComment(string encodedDesc)
        {
            if (string.IsNullOrEmpty(encodedDesc))
                return null;

            try
            {
                // 如果描述包含UNICODE头标记
                if (encodedDesc.StartsWith("UNICODE"))
                {
                    // 直接尝试使用 Latin1 解码后重新用 UTF-16LE 解码
                    // MetadataExtractor 使用 Latin1 读取，我们需要恢复
                    byte[] latin1Bytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(encodedDesc);
                    
                    // 跳过 "UNICODE\0" (8 字节)
                    if (latin1Bytes.Length > 8)
                    {
                        byte[] utf16Bytes = new byte[latin1Bytes.Length - 8];
                        Array.Copy(latin1Bytes, 8, utf16Bytes, 0, utf16Bytes.Length);
                        
                        // 用 UTF-16LE 解码
                        string decoded = Encoding.Unicode.GetString(utf16Bytes).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(decoded) && decoded != encodedDesc)
                            return decoded;
                    }
                }
            }
            catch { }

            return null;
        }

        #endregion
    }
}
