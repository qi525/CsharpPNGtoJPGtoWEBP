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
    }
}
