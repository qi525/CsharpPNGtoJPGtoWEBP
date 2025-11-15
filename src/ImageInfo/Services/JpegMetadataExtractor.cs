using System;
using System.Collections.Generic;
using System.Linq;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ImageInfo.Services
{
    /// <summary>
    /// JPEG/JPG 图片元数据提取器（特化实现）。
    /// JPEG 通过 EXIF 数据段存储元数据（包括相机信息、位置、自定义注释等）。
    /// 某些工具会在 ImageDescription 或 UserComment 字段存储生成提示。
    /// 
    /// 实现读→写→验证三步流程。
    /// </summary>
    public static class JpegMetadataExtractor
    {
        /// <summary>
        /// 【步骤 1：读】从 JPEG EXIF 段读取 AI 元数据。
        /// EXIF 数据结构复杂，通常包含：
        /// - ImageDescription (Tag 0x010E)
        /// - UserComment (Tag 0x8972 in IFD1)
        /// - Software (Tag 0x0131)
        /// 等多个字段。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                ExtractFromExif(directories, metadata);
            }
            catch
            {
                // 元数据读取失败时返回空对象
            }

            return metadata;
        }

        /// <summary>
        /// 【步骤 2：写】将 AI 元数据写入到 JPEG EXIF 段。
        /// 注：JPEG EXIF 写入较复杂，需要使用专门的库如 ImageMagick 或手动 EXIF 编码。
        /// 当前实现为占位符。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            // TODO: 实现 JPEG EXIF 写入
            // 可使用 ImageMagick 或 SixLabors.ImageSharp 扩展
        }

        /// <summary>
        /// 【步骤 3：验证】验证元数据是否成功写入。
        /// 重新读取 JPEG 并比对关键 EXIF 字段。
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
        /// 从 JPEG EXIF 数据段提取 AI 元数据。
        /// EXIF 格式严格，需要按照标准标签号访问（IFD0 等）。
        /// 重点关注：ImageDescription、UserComment、Software 等字段。
        /// </summary>
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

                    if (string.IsNullOrEmpty(desc))
                        continue;

                    // 匹配常见的 EXIF 字段
                    if (tagName.Contains("description") || tagName.Contains("imagedescription"))
                    {
                        // ImageDescription (0x010E) 常用于存储 prompt
                        if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.Prompt = desc;
                    }
                    else if (tagName.Contains("usercomment"))
                    {
                        // UserComment 可能包含工具生成的元数据
                        if (string.IsNullOrEmpty(metadata.Prompt) && desc.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.Prompt = desc;
                        else if (string.IsNullOrEmpty(metadata.OtherInfo))
                            metadata.OtherInfo = desc;
                    }
                    else if (tagName.Contains("software"))
                    {
                        // Software 字段可能标识生成工具
                        if (string.IsNullOrEmpty(metadata.Model))
                            metadata.Model = desc;
                    }
                    else if (tagName.Contains("makernote") || tagName.Contains("maker"))
                    {
                        // MakerNote 可能包含工具特定的元数据
                        if (string.IsNullOrEmpty(metadata.OtherInfo))
                            metadata.OtherInfo = desc;
                    }
                }
            }
        }
    }
}
