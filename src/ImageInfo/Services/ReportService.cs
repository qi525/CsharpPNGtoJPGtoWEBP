using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ClosedXML.Excel;
using ImageInfo.Models;

namespace ImageInfo.Services
{
    /// <summary>
    /// 生成转换报告（XLSX）的服务。将每一行写入 Excel，包括源/目标路径、参数、尺寸和转换是否成功。
    /// 依赖：ClosedXML（第三方）。
    /// </summary>
    public static class ReportService
    {
        /// <summary>
        /// 将给定的 <paramref name="rows"/> 写入一个 XLSX 文件，自动按时间戳命名。
        /// 可选参数 <paramref name="openAfterSave"/> 控制在保存后是否尝试用系统默认应用打开该文件（最佳努力）。
        /// 长文本字段（>1000字符）将被截断并添加省略号，以避免超过 Excel 单元格限制。
        /// 同时生成日志文件记录报告统计信息。
        /// </summary>
        /// <param name="rows">要写入到表格的行集合</param>
        /// <param name="outPath">输出文件路径基础（将自动添加时间戳，如 conversion-report-20251116-143022.xlsx）</param>
        /// <param name="openAfterSave">是否在保存后尝试打开（默认 false）</param>
        public static void WriteConversionReport(IEnumerable<ConversionReportRow> rows, string outPath, bool openAfterSave = false)
        {
            // 生成时间戳并更新输出路径
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var dir = Path.GetDirectoryName(outPath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(outPath);
            var timestampedPath = Path.Combine(dir, $"{fileName}-{timestamp}.xlsx");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Conversions");

            // Header (Chinese) - 添加报告时间戳列、AI元数据完整信息、元数据写入状态
            var headers = new[] {
                "源文件路径", "目标文件路径",
                "源宽度", "源高度", "源格式", "源参数",
                "目标宽度", "目标高度", "目标格式", "目标参数",
                "转换成功", "错误信息",
                "AI Prompt", "AI 负 Prompt", "AI 模型", "AI 种子", "AI 采样器", "AI 其他信息",
                "完整AI元数据", "元数据提取方法", "元数据已写入", "元数据已验证",
                "源创建时间", "源修改时间", "报告时间戳"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            int r = 2;
            var reportTimestamp = DateTime.UtcNow.ToString("o"); // ISO 8601
            foreach (var row in rows)
            {
                ws.Cell(r, 1).Value = TruncateIfNeeded(row.SourcePath, 1000);
                ws.Cell(r, 2).Value = TruncateIfNeeded(row.DestPath, 1000);
                ws.Cell(r, 3).Value = row.SourceWidth;
                ws.Cell(r, 4).Value = row.SourceHeight;
                ws.Cell(r, 5).Value = TruncateIfNeeded(row.SourceFormat, 100);
                ws.Cell(r, 6).Value = TruncateIfNeeded(row.SourceParams, 200);
                ws.Cell(r, 7).Value = row.DestWidth;
                ws.Cell(r, 8).Value = row.DestHeight;
                ws.Cell(r, 9).Value = TruncateIfNeeded(row.DestFormat, 100);
                ws.Cell(r, 10).Value = TruncateIfNeeded(row.DestParams, 200);
                ws.Cell(r, 11).Value = row.Success;
                ws.Cell(r, 12).Value = TruncateIfNeeded(row.ErrorMessage, 500);
                ws.Cell(r, 13).Value = TruncateIfNeeded(row.AIPrompt, 1000);
                ws.Cell(r, 14).Value = TruncateIfNeeded(row.AINegativePrompt, 1000);
                ws.Cell(r, 15).Value = TruncateIfNeeded(row.AIModel, 200);
                ws.Cell(r, 16).Value = TruncateIfNeeded(row.AISeed, 100);
                ws.Cell(r, 17).Value = TruncateIfNeeded(row.AISampler, 100);
                ws.Cell(r, 18).Value = TruncateIfNeeded(row.AIMetadata, 1000);
                ws.Cell(r, 19).Value = TruncateIfNeeded(row.FullAIMetadata, 1000);
                ws.Cell(r, 20).Value = TruncateIfNeeded(row.FullAIMetadataExtractionMethod, 100);
                ws.Cell(r, 21).Value = row.MetadataWritten;
                ws.Cell(r, 22).Value = row.MetadataVerified;
                ws.Cell(r, 23).Value = row.SourceCreatedUtc?.ToString("u");
                ws.Cell(r, 24).Value = row.SourceModifiedUtc?.ToString("u");
                ws.Cell(r, 25).Value = reportTimestamp;
                r++;
            }

            // Auto-fit columns for readability
            ws.Columns().AdjustToContents();

            // Ensure directory exists
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            wb.SaveAs(timestampedPath);
            
            // Write log entry
            LogReportGeneration(timestampedPath, rows);

            if (openAfterSave)
            {
                try
                {
                    // On Windows, launch with UseShellExecute so the default app opens the file.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var psi = new ProcessStartInfo(timestampedPath)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        // Try xdg-open on Linux
                        Process.Start("xdg-open", timestampedPath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // macOS open command
                        Process.Start("open", timestampedPath);
                    }
                }
                catch
                {
                    // best-effort: do not throw from report opening
                }
            }
        }

        /// <summary>
        /// 截断字符串以避免超过 XLSX 单元格限制（32767 字符）。
        /// 如果字符串长度超过 maxLength，返回截断后的字符串加省略号。
        /// </summary>
        private static string TruncateIfNeeded(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// 将报告生成信息记录到日志文件。
        /// 日志文件位置：{reportDir}/conversion.log
        /// 记录内容：简洁统计（总数、成功/失败数、成功率）。
        /// </summary>
        private static void LogReportGeneration(string reportPath, IEnumerable<ConversionReportRow> rows)
        {
            try
            {
                var reportDir = Path.GetDirectoryName(reportPath) ?? string.Empty;
                var logFile = Path.Combine(reportDir, "conversion.log");

                var rowList = rows is List<ConversionReportRow> list ? list : new List<ConversionReportRow>(rows);
                var successCount = rowList.Count(r => r.Success);
                var failureCount = rowList.Count(r => !r.Success);

                var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Total: {rowList.Count} | Success: {successCount} | Failed: {failureCount} | Rate: {(rowList.Count > 0 ? (double)successCount / rowList.Count * 100 : 0):F1}%\n";

                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // 日志写入失败不应中断主流程
            }
        }
    }
}
