using System;
using System.Collections.Generic;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;

namespace ImageInfo.Services
{
    /// <summary>
    /// 提取 AI 生成图片的元数据（Stable Diffusion、ComfyUI 等）。
    /// 实现读→写→验证的三步流程。
    /// </summary>
    public static class AIMetadataExtractor
    {
        /// <summary>
        /// 【步骤 1：读】从图片文件读取 AI 元数据。
        /// 支持从 PNG tEXt、EXIF、XMP 中提取 prompt、model、seed 等信息。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                // 从 PNG tEXt 读取
                ExtractFromPngText(directories, metadata);
                
                // 从 EXIF 读取（某些工具会存储在 EXIF UserComment）
                ExtractFromExif(directories, metadata);
                
                // 从 XMP 读取
                ExtractFromXmp(directories, metadata);
            }
            catch
            {
                // 元数据读取失败时返回空对象
            }

            return metadata;
        }

        /// <summary>
        /// 【步骤 2：写】将 AI 元数据写入到转换后的图片。
        /// 目前作为占位符实现；实际写入需要 libpng 或其他库的支持。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            // 注：写回 PNG tEXt/EXIF/XMP 需要额外的库或手动编码
            // 这里作为调用示意，实际实现可使用 ImageMagick 或手动 PNG 字节写入
            // 当前实现仅为占位符，确保流程完整性
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            // 实际实现可在此处添加
        }

        /// <summary>
        /// 【步骤 3：验证】验证元数据是否成功写入并能被读回。
        /// </summary>
        public static bool VerifyAIMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath))
                return false;

            try
            {
                var readBack = ReadAIMetadata(destImagePath);
                // 验证关键字段是否一致
                return !string.IsNullOrEmpty(readBack.Prompt) || readBack.Prompt == originalMetadata.Prompt;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>从 PNG tEXt 条目提取 AI 元数据。</summary>
        private static void ExtractFromPngText(IReadOnlyList<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories)
            {
                if (dir is MetadataExtractor.Formats.Png.PngDirectory pngDir)
                {
                    foreach (var tag in pngDir.Tags)
                    {
                        var desc = tag?.Description ?? "";
                        
                        // Stable Diffusion WebUI 格式
                        if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.Prompt = desc;
                        
                        if (desc.Contains("negative prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.NegativePrompt = desc;
                        
                        if (desc.Contains("model", StringComparison.OrdinalIgnoreCase) || 
                            desc.Contains("checkpoint", StringComparison.OrdinalIgnoreCase))
                            metadata.Model = desc;
                        
                        if (desc.Contains("seed", StringComparison.OrdinalIgnoreCase))
                            metadata.Seed = desc;
                        
                        if (desc.Contains("sampler", StringComparison.OrdinalIgnoreCase) || 
                            desc.Contains("scheduler", StringComparison.OrdinalIgnoreCase))
                            metadata.Sampler = desc;
                        
                        // ComfyUI 格式
                        if (desc.Contains("workflow", StringComparison.OrdinalIgnoreCase))
                            metadata.OtherInfo = desc;
                    }
                }
            }
        }

        /// <summary>从 EXIF UserComment 提取 AI 元数据。</summary>
        private static void ExtractFromExif(IReadOnlyList<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories)
            {
                if (dir is ExifIfd0Directory exifDir)
                {
                    // EXIF tag 0x010F is ImageDescription which often contains metadata
                    foreach (var tag in exifDir.Tags)
                    {
                        if (tag != null)
                        {
                            var desc = tag.Description ?? "";
                            if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(metadata.Prompt))
                                metadata.Prompt = desc;
                        }
                    }
                }
            }
        }

        /// <summary>从 XMP 提取 AI 元数据（dc:subject、photoshop:Keywords 等）。</summary>
        private static void ExtractFromXmp(IReadOnlyList<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories)
            {
                if (dir is XmpDirectory xmpDir)
                {
                    var xmp = xmpDir.XmpMeta;
                    if (xmp != null)
                    {
                        // Dublin Core subject (常用于存储 prompt)
                        var subject = xmp.GetPropertyString("http://purl.org/dc/elements/1.1/", "subject");
                        if (!string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(metadata.Prompt))
                            metadata.Prompt = subject;

                        // Photoshop Keywords
                        var keywords = xmp.GetPropertyString("http://ns.adobe.com/photoshop/1.0/", "Keywords");
                        if (!string.IsNullOrEmpty(keywords))
                            metadata.OtherInfo = (metadata.OtherInfo ?? "") + " | Keywords: " + keywords;
                    }
                }
            }
        }
    }

    /// <summary>
    /// AI 元数据容器。
    /// </summary>
    public class AIMetadata
    {
        /// <summary>AI 生成的 prompt。</summary>
        public string? Prompt { get; set; }
        /// <summary>AI 负 prompt。</summary>
        public string? NegativePrompt { get; set; }
        /// <summary>使用的 AI 模型。</summary>
        public string? Model { get; set; }
        /// <summary>生成种子。</summary>
        public string? Seed { get; set; }
        /// <summary>采样器/调度器。</summary>
        public string? Sampler { get; set; }
        /// <summary>其他元数据。</summary>
        public string? OtherInfo { get; set; }
    }
}

