using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.Xmp;
using XmpCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

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
                using (var image = Image.Load(destImagePath))
                {
                    var encoder = new PngEncoder();
                    image.Save(destImagePath, encoder);
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
                ExtractFromExif(directories, metadata);
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
                using (var image = Image.Load(destImagePath))
                {
                    var encoder = new JpegEncoder { Quality = 95 };
                    image.Save(destImagePath, encoder);
                }
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

                    if (tagName.Contains("description") || tagName.Contains("imagedescription"))
                    {
                        if (!string.IsNullOrEmpty(desc))
                        {
                            if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase) || 
                                desc.Contains("negative", StringComparison.OrdinalIgnoreCase) ||
                                desc.Contains("steps:", StringComparison.OrdinalIgnoreCase))
                            {
                                metadata.FullInfo = desc;
                                metadata.FullInfoExtractionMethod = "JPEG.EXIF.ImageDescription";
                                AIParameterParser.ParseFullInfoIntoFields(desc, metadata);
                                continue;
                            }
                            else if (string.IsNullOrEmpty(metadata.Prompt))
                            {
                                metadata.Prompt = desc;
                            }
                        }
                    }
                    else if (tagName.Contains("usercomment"))
                    {
                        if (!string.IsNullOrEmpty(desc))
                        {
                            if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase) || 
                                desc.Contains("negative", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.IsNullOrEmpty(metadata.FullInfo))
                                    metadata.FullInfo = desc;
                                if (string.IsNullOrEmpty(metadata.FullInfoExtractionMethod))
                                    metadata.FullInfoExtractionMethod = "JPEG.EXIF.UserComment";
                                AIParameterParser.ParseFullInfoIntoFields(desc, metadata);
                            }
                            else if (string.IsNullOrEmpty(metadata.OtherInfo))
                            {
                                metadata.OtherInfo = desc;
                            }
                        }
                    }
                    else if (tagName.Contains("software"))
                    {
                        if (string.IsNullOrEmpty(metadata.Model))
                            metadata.Model = desc;
                    }
                    else if (tagName.Contains("makernote") || tagName.Contains("maker"))
                    {
                        if (string.IsNullOrEmpty(metadata.OtherInfo))
                            metadata.OtherInfo = desc;
                    }
                }
            }
        }

        #endregion

        #region WebP 元数据处理

        private static AIMetadata ReadWebPMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                ExtractFromXmp(directories, metadata);
                
                if (string.IsNullOrEmpty(metadata.Prompt))
                    ExtractFromExif(directories, metadata);
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
                using (var image = Image.Load(destImagePath))
                {
                    var encoder = new WebpEncoder { Quality = 95 };
                    image.Save(destImagePath, encoder);
                }
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

        #endregion
    }
}
