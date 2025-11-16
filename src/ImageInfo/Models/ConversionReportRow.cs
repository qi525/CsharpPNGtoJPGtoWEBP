using System;

namespace ImageInfo.Models
{
    /// <summary>
    /// 表示一行转换报告：源/目标路径、尺寸、格式、参数、以及是否成功与错误信息。
    /// 由 ReportService 写入 Excel 并在测试中用于验证与断言。
    /// </summary>
    public class ConversionReportRow
    {
        /// <summary>源文件绝对路径。</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>目标文件绝对路径（如果有）。</summary>
        public string DestPath { get; set; } = string.Empty;

        /// <summary>源图片宽度（像素）。</summary>
        public int? SourceWidth { get; set; }
        /// <summary>源图片高度（像素）。</summary>
        public int? SourceHeight { get; set; }
        /// <summary>源图片格式字符串（例如 "png"）。</summary>
        public string? SourceFormat { get; set; }
        /// <summary>源图片额外参数（例如 encoder 参数的文本描述）。</summary>
        public string? SourceParams { get; set; }

        /// <summary>目标图片宽度（像素）。</summary>
        public int? DestWidth { get; set; }
        /// <summary>目标图片高度（像素）。</summary>
        public int? DestHeight { get; set; }
        /// <summary>目标图片格式字符串（例如 "webp"）。</summary>
        public string? DestFormat { get; set; }
        /// <summary>目标图片额外参数（例如质量值等）。</summary>
        public string? DestParams { get; set; }

        /// <summary>转换是否成功。</summary>
        public bool Success { get; set; }
        /// <summary>转换失败时的错误信息（如果有）。</summary>
        public string? ErrorMessage { get; set; }

        // AI Metadata fields
        /// <summary>AI 生成的 prompt（Stable Diffusion 或其它文生图模型）。</summary>
        public string? AIPrompt { get; set; }
        /// <summary>AI 生成的负 prompt。</summary>
        public string? AINegativePrompt { get; set; }
        /// <summary>使用的 AI 模型名称（如 "sd-1.5", "sd-xl" 等）。</summary>
        public string? AIModel { get; set; }
        /// <summary>生成时的随机种子。</summary>
        public string? AISeed { get; set; }
        /// <summary>采样器/调度器名称。</summary>
        public string? AISampler { get; set; }
        /// <summary>其他 AI 相关信息（步数、guidance scale 等）。</summary>
        public string? AIMetadata { get; set; }
        /// <summary>原始完整 AI 元数据块（例如 PNG parameters / EXIF ImageDescription）。</summary>
        public string? FullAIMetadata { get; set; }
        /// <summary>完整元数据提取方法（MetadataExtractor / RawBytes.Fallback / ImageSharp.PngTextData / 等）。</summary>
        public string? FullAIMetadataExtractionMethod { get; set; }
        /// <summary>是否成功写入元数据到输出文件。</summary>
        public bool MetadataWritten { get; set; }
        /// <summary>写入后是否通过验证（读回比对）。</summary>
        public bool MetadataVerified { get; set; }

        /// <summary>源文件创建时间（UTC）。</summary>
        public DateTime? SourceCreatedUtc { get; set; }
        /// <summary>源文件修改时间（UTC）。</summary>
        public DateTime? SourceModifiedUtc { get; set; }

        /// <summary>报告生成的时间戳（ISO 8601 格式，UTC）。</summary>
        public string? ReportTimestamp { get; set; }
    }
}

