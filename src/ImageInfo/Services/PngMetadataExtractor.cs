using System;
using System.Collections.Generic;
using System.Linq;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Png;

namespace ImageInfo.Services
{
    /// <summary>
    /// PNG 图片元数据提取器（特化实现）。
    /// PNG 通过 tEXt 块存储文本元数据，支持自定义键值对。
    /// 常用于 Stable Diffusion 等工具存储 prompt 信息。
    /// 
    /// 实现读→写→验证三步流程。
    /// </summary>
    public static class PngMetadataExtractor
    {
        /// <summary>
        /// 【步骤 1：读】从 PNG tEXt 块读取 AI 元数据。
        /// PNG tEXt 块格式：键名=值（通常键名为 "prompt", "model" 等）。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                ExtractFromPngText(directories, metadata);
            }
            catch
            {
                // 元数据读取失败时返回空对象
            }

            return metadata;
        }

        /// <summary>
        /// 【步骤 2：写】将 AI 元数据写入到 PNG tEXt 块。
        /// 注：需要使用专门的 PNG 库（如 SixLabors.ImageSharp 扩展）或手动编码。
        /// 当前实现为占位符。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            // TODO: 实现 PNG tEXt 块写入
            // 可使用第三方库如 ImageMagick 或手动 PNG 字节操作
        }

        /// <summary>
        /// 【步骤 3：验证】验证元数据是否成功写入。
        /// 重新读取 PNG 并比对关键字段。
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
        /// 从 PNG tEXt 条目提取 AI 元数据。
        /// PNG tEXt 块由键名和文本值组成，通常结构为：keyword=value。
        /// </summary>
        private static void ExtractFromPngText(IEnumerable<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories.OfType<PngDirectory>())
            {
                foreach (var tag in dir.Tags)
                {
                    if (tag == null)
                        continue;

                    var tagName = tag.Name?.ToLowerInvariant() ?? "";
                    var desc = tag.Description ?? "";

                    // 匹配 Stable Diffusion WebUI 标准 tEXt 键名
                    if (tagName.Contains("prompt") || tagName.Equals("prompt", StringComparison.OrdinalIgnoreCase))
                    {
                        metadata.Prompt = desc;
                    }
                    else if (tagName.Contains("negative") || tagName.Equals("negative prompt", StringComparison.OrdinalIgnoreCase))
                    {
                        metadata.NegativePrompt = desc;
                    }
                    else if (tagName.Contains("model") || tagName.Contains("checkpoint"))
                    {
                        metadata.Model = desc;
                    }
                    else if (tagName.Contains("seed"))
                    {
                        metadata.Seed = desc;
                    }
                    else if (tagName.Contains("sampler") || tagName.Contains("scheduler"))
                    {
                        metadata.Sampler = desc;
                    }
                    else if (tagName.Contains("workflow") || tagName.Contains("extra"))
                    {
                        metadata.OtherInfo = desc;
                    }
                }
            }
        }
    }
}
