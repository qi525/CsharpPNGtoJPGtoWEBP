using System;
using System.IO;
using ImageInfo.Models;

namespace ImageInfo.Services
{
    /// <summary>
    /// AI 元数据提取的兼容性封装。
    /// 为了向后兼容，保留 AIMetadataExtractor 作为 MetadataExtractors 的简单代理。
    /// 所有逻辑已移至 MetadataExtractors.cs。
    /// </summary>
    public static class AIMetadataExtractor
    {
        /// <summary>
        /// 从图片文件读取 AI 元数据。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            return MetadataExtractors.ReadAIMetadata(imagePath);
        }

        /// <summary>
        /// 将 AI 元数据写入到图片文件。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            MetadataExtractors.WriteAIMetadata(destImagePath, aiMetadata);
        }

        /// <summary>
        /// 验证元数据是否成功写入。
        /// </summary>
        public static bool VerifyAIMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            return MetadataExtractors.VerifyAIMetadata(destImagePath, originalMetadata);
        }
    }
}

