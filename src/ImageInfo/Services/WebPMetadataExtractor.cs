using System;
using System.Collections.Generic;
using System.Linq;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor.Formats.Exif;
using XmpCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageInfo.Services
{
    /// <summary>
    /// WebP 图片元数据提取器（特化实现）。
    /// WebP 容器支持多种元数据格式：
    /// - XMP：通用 XML 格式，支持自定义命名空间（Dublin Core、Photoshop Keywords 等）
    /// - EXIF：与 JPEG 类似的元数据段
    /// - ICCP：色彩配置文件
    /// 
    /// 实现读→写→验证三步流程。
    /// </summary>
    public static class WebPMetadataExtractor
    {
        /// <summary>
        /// 【步骤 1：读】从 WebP 文件读取 AI 元数据。
        /// WebP 通常通过 XMP 和 EXIF 存储元数据。
        /// 优先级：XMP > EXIF。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                // 优先从 XMP 读取（ComfyUI 等工具常用 XMP）
                ExtractFromXmp(directories, metadata);
                
                // 如果 XMP 中无关键字段，尝试 EXIF
                if (string.IsNullOrEmpty(metadata.Prompt))
                    ExtractFromExif(directories, metadata);
            }
            catch
            {
                // 元数据读取失败时返回空对象
            }

            return metadata;
        }

        /// <summary>
        /// 【步骤 2：写】将 AI 元数据写入到 WebP 文件。
        /// WebP 支持 XMP 和 EXIF，这里使用 ImageSharp 的 WebP 编码器保存。
        /// 注：完整的 XMP 元数据写入需要专门库支持。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            try
            {
                // 使用 ImageSharp 加载 WebP
                using (var image = Image.Load(destImagePath))
                {
                    // 保存回文件，使用 WebP 编码器
                    var encoder = new WebpEncoder { Quality = 95 };
                    image.Save(destImagePath, encoder);
                }

                // 注：由于 SixLabors.ImageSharp 对 WebP XMP 元数据的写入支持有限，
                // 更完整的 XMP 写入需要使用 ExifTool 或其他专门工具。
                // 这里采用的方案是保存图片本身，具体的元数据追加需要额外的库支持。
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Warning: Failed to write WebP metadata to {destImagePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 【步骤 3：验证】验证元数据是否成功写入。
        /// 重新读取 WebP 并比对关键字段。
        /// </summary>
        public static bool VerifyAIMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || originalMetadata == null)
                return false;

            try
            {
                var readBack = ReadAIMetadata(destImagePath);
                // 至少验证 Prompt 字段一致性
                if (!string.IsNullOrEmpty(originalMetadata.Prompt))
                    return readBack.Prompt == originalMetadata.Prompt;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从 WebP 的 XMP 元数据提取 AI 信息。
        /// XMP 是结构化 XML 格式，支持多种命名空间：
        /// - Dublin Core (dc:subject)
        /// - Photoshop (photoshop:Keywords)
        /// - 自定义命名空间
        /// </summary>
        private static void ExtractFromXmp(IEnumerable<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var xmpDir in directories.OfType<XmpDirectory>())
            {
                if (xmpDir.XmpMeta == null)
                    continue;

                // 尝试从 Dublin Core 的 subject 字段提取 prompt
                var subject = xmpDir.XmpMeta.GetPropertyString("http://purl.org/dc/elements/1.1/", "subject");
                if (!string.IsNullOrEmpty(subject))
                {
                    metadata.Prompt = subject;
                }

                // 从 Photoshop Keywords 提取
                var keywords = xmpDir.XmpMeta.GetPropertyString("http://ns.adobe.com/photoshop/1.0/", "Keywords");
                if (!string.IsNullOrEmpty(keywords))
                {
                    if (string.IsNullOrEmpty(metadata.OtherInfo))
                        metadata.OtherInfo = "Keywords: " + keywords;
                    else
                        metadata.OtherInfo += " | Keywords: " + keywords;
                }

                // 尝试从更多自定义字段提取
                // 这些字段可能由生成工具（如 ComfyUI）自定义设置
                ExtractFromCustomXmpFields(xmpDir.XmpMeta, metadata);
            }
        }

        /// <summary>
        /// 从 WebP 的 EXIF 段提取 AI 元数据。
        /// 如果 XMP 中没有关键信息，可以尝试 EXIF 字段。
        /// </summary>
        private static void ExtractFromExif(IEnumerable<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var exifDir in directories.OfType<ExifIfd0Directory>())
            {
                foreach (var tag in exifDir.Tags)
                {
                    if (tag == null)
                        continue;

                    var tagName = tag.Name?.ToLowerInvariant() ?? "";
                    var desc = tag.Description ?? "";

                    if (string.IsNullOrEmpty(desc))
                        continue;

                    if (tagName.Contains("description"))
                    {
                        if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.Prompt = desc;
                    }
                    else if (tagName.Contains("usercomment"))
                    {
                        if (string.IsNullOrEmpty(metadata.OtherInfo))
                            metadata.OtherInfo = desc;
                    }
                }
            }
        }

        /// <summary>
        /// 从 XMP 元数据中提取可能的自定义字段。
        /// 不同工具（ComfyUI、Automatic1111 等）可能存储在不同的命名空间。
        /// 这是一个辅助方法，尝试通过启发式方法查找相关字段。
        /// </summary>
        private static void ExtractFromCustomXmpFields(IXmpMeta xmpMeta, AIMetadata metadata)
        {
            if (xmpMeta == null)
                return;

            // 常见的自定义命名空间尝试
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
                    // 尝试获取 "prompt" 属性
                    var prompt = xmpMeta.GetPropertyString(ns, "prompt");
                    if (!string.IsNullOrEmpty(prompt) && string.IsNullOrEmpty(metadata.Prompt))
                    {
                        metadata.Prompt = prompt;
                    }

                    // 尝试获取 "model" 属性
                    var model = xmpMeta.GetPropertyString(ns, "model");
                    if (!string.IsNullOrEmpty(model) && string.IsNullOrEmpty(metadata.Model))
                    {
                        metadata.Model = model;
                    }

                    // 尝试获取 "seed" 属性
                    var seed = xmpMeta.GetPropertyString(ns, "seed");
                    if (!string.IsNullOrEmpty(seed) && string.IsNullOrEmpty(metadata.Seed))
                    {
                        metadata.Seed = seed;
                    }
                }
                catch
                {
                    // 命名空间不存在或属性不存在，继续下一个
                }
            }
        }
    }
}
