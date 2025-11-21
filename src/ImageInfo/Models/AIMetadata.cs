namespace ImageInfo.Models
{
    /// <summary>
    /// AI 生成图片的元数据容器。
    /// 用于存储从各种格式（PNG tEXt、JPEG EXIF、WebP XMP）中提取的元数据。
    /// </summary>
    public class AIMetadata
    {
        /// <summary>AI 生成的正向提示词。</summary>
        public string? Prompt { get; set; }

        /// <summary>AI 生成的负向提示词。</summary>
        public string? NegativePrompt { get; set; }

        /// <summary>使用的 AI 模型/检查点名称。</summary>
        public string? Model { get; set; }

        /// <summary>模型的哈希值。</summary>
        public string? ModelHash { get; set; }

        /// <summary>生成使用的随机种子。</summary>
        public string? Seed { get; set; }

        /// <summary>采样器或调度器类型。</summary>
        public string? Sampler { get; set; }

        /// <summary>其他补充元数据（Steps、CFG Scale 等）。</summary>
        public string? OtherInfo { get; set; }

        /// <summary>原始完整信息块（例如 PNG 的 parameters tEXt 块或 EXIF ImageDescription）。</summary>
        public string? FullInfo { get; set; }

        /// <summary>完整信息的提取方法（例如 PNG.tEXt.parameters、JPEG.EXIF.ImageDescription 等）。</summary>
        public string? FullInfoExtractionMethod { get; set; }
    }
}
