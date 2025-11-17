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

            // Header (Chinese) - 新增转换后文件的元数据和时间戳对比
            var headers = new[] {
                "源文件路径", "目标文件路径",
                "源宽度", "源高度", "源格式", 
                "目标宽度", "目标高度", "目标格式",
                "转换成功", "错误信息",
                // 源文件元数据
                "源完整AI元数据 (FullInfo)", "源元数据提取方法", 
                "源AI Prompt (正向)", "源AI 负 Prompt (负向)", 
                "源AI 模型", "源AI 种子", "源AI 采样器", "源AI 其他信息",
                // 转换后文件元数据 - 新增
                "转换后完整AI元数据 (FullInfo)", "转换后元数据提取方法",
                "转换后AI Prompt (正向)", "转换后AI 负 Prompt (负向)",
                "转换后AI 模型", "转换后AI 种子", "转换后AI 采样器", "转换后AI 其他信息",
                // 元数据写入验证
                "元数据已写入", "元数据已验证",
                // 时间戳信息
                "源创建时间", "源修改时间",
                "转换后创建时间", "转换后修改时间",
                "创建时间一致性", "修改时间一致性",
                "报告时间戳"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // 设置列宽和格式
            ws.Column(1).Width = 30;   // 源文件路径
            ws.Column(2).Width = 30;   // 目标文件路径
            ws.Column(3).Width = 10;   // 源宽度
            ws.Column(4).Width = 10;   // 源高度
            ws.Column(5).Width = 10;   // 源格式
            ws.Column(6).Width = 10;   // 目标宽度
            ws.Column(7).Width = 10;   // 目标高度
            ws.Column(8).Width = 10;   // 目标格式
            ws.Column(9).Width = 10;   // 转换成功
            ws.Column(10).Width = 20;  // 错误信息
            ws.Column(11).Width = 60;  // 源完整AI元数据
            ws.Column(12).Width = 15;  // 源元数据提取方法
            ws.Column(13).Width = 40;  // 源Prompt
            ws.Column(14).Width = 40;  // 源NegativePrompt
            ws.Column(15).Width = 20;  // 源模型
            ws.Column(16).Width = 15;  // 源种子
            ws.Column(17).Width = 15;  // 源采样器
            ws.Column(18).Width = 20;  // 源其他信息
            ws.Column(19).Width = 60;  // 转换后完整AI元数据
            ws.Column(20).Width = 15;  // 转换后元数据提取方法
            ws.Column(21).Width = 40;  // 转换后Prompt
            ws.Column(22).Width = 40;  // 转换后NegativePrompt
            ws.Column(23).Width = 20;  // 转换后模型
            ws.Column(24).Width = 15;  // 转换后种子
            ws.Column(25).Width = 15;  // 转换后采样器
            ws.Column(26).Width = 20;  // 转换后其他信息
            ws.Column(27).Width = 12;  // 元数据已写入
            ws.Column(28).Width = 12;  // 元数据已验证
            ws.Column(29).Width = 20;  // 源创建时间
            ws.Column(30).Width = 20;  // 源修改时间
            ws.Column(31).Width = 20;  // 转换后创建时间
            ws.Column(32).Width = 20;  // 转换后修改时间
            ws.Column(33).Width = 12;  // 创建时间一致性
            ws.Column(34).Width = 12;  // 修改时间一致性
            ws.Column(35).Width = 20;  // 报告时间戳

            // 设置表头样式（粗体、背景色）
            for (int i = 1; i <= headers.Length; i++)
            {
                var headerCell = ws.Cell(1, i);
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            int r = 2;
            var reportTimestamp = DateTime.UtcNow.ToString("o"); // ISO 8601
            foreach (var row in rows)
            {
                int col = 1;
                ws.Cell(r, col++).Value = row.SourcePath;
                ws.Cell(r, col++).Value = row.DestPath;
                ws.Cell(r, col++).Value = row.SourceWidth;
                ws.Cell(r, col++).Value = row.SourceHeight;
                ws.Cell(r, col++).Value = row.SourceFormat;
                ws.Cell(r, col++).Value = row.DestWidth;
                ws.Cell(r, col++).Value = row.DestHeight;
                ws.Cell(r, col++).Value = row.DestFormat;
                ws.Cell(r, col++).Value = row.Success;
                ws.Cell(r, col++).Value = row.ErrorMessage;
                
                // 源文件元数据
                ws.Cell(r, col++).Value = row.FullAIMetadata ?? string.Empty;
                ws.Cell(r, col++).Value = row.FullAIMetadataExtractionMethod;
                ws.Cell(r, col++).Value = row.AIPrompt ?? string.Empty;
                ws.Cell(r, col++).Value = row.AINegativePrompt ?? string.Empty;
                ws.Cell(r, col++).Value = row.AIModel;
                ws.Cell(r, col++).Value = row.AISeed;
                ws.Cell(r, col++).Value = row.AISampler;
                ws.Cell(r, col++).Value = row.AIMetadata;
                
                // 转换后文件元数据 - 新增
                ws.Cell(r, col++).Value = row.DestFullAIMetadata ?? string.Empty;
                ws.Cell(r, col++).Value = row.DestFullAIMetadataExtractionMethod;
                ws.Cell(r, col++).Value = row.DestAIPrompt ?? string.Empty;
                ws.Cell(r, col++).Value = row.DestAINegativePrompt ?? string.Empty;
                ws.Cell(r, col++).Value = row.DestAIModel;
                ws.Cell(r, col++).Value = row.DestAISeed;
                ws.Cell(r, col++).Value = row.DestAISampler;
                ws.Cell(r, col++).Value = row.DestAIMetadata;
                
                // 元数据写入验证
                ws.Cell(r, col++).Value = row.MetadataWritten;
                ws.Cell(r, col++).Value = row.MetadataVerified;
                
                // 时间戳信息
                ws.Cell(r, col++).Value = row.SourceCreatedUtc?.ToString("u");
                ws.Cell(r, col++).Value = row.SourceModifiedUtc?.ToString("u");
                ws.Cell(r, col++).Value = row.DestCreatedUtc?.ToString("u");
                ws.Cell(r, col++).Value = row.DestModifiedUtc?.ToString("u");
                
                // 时间戳一致性 - 新增
                ws.Cell(r, col++).Value = row.CreatedTimeMatches == true ? "是" : (row.CreatedTimeMatches == false ? "否" : "N/A");
                ws.Cell(r, col++).Value = row.ModifiedTimeMatches == true ? "是" : (row.ModifiedTimeMatches == false ? "否" : "N/A");
                
                ws.Cell(r, col++).Value = reportTimestamp;
                r++;
            }

            // 设置列宽并启用文本换行
            ws.Column(1).Width = 40;   // 源文件路径
            ws.Column(2).Width = 40;   // 目标文件路径
            ws.Column(10).Width = 50;  // 错误信息
            ws.Column(11).Width = 80;  // 源完整AI元数据
            ws.Column(12).Width = 25;  // 源元数据提取方法
            ws.Column(13).Width = 80;  // 源Prompt
            ws.Column(14).Width = 80;  // 源负Prompt
            ws.Column(19).Width = 80;  // 转换后完整AI元数据
            ws.Column(20).Width = 25;  // 转换后元数据提取方法
            ws.Column(21).Width = 80;  // 转换后Prompt
            ws.Column(22).Width = 80;  // 转换后负Prompt

            // 启用所有包含文本的单元格的换行，并设置对齐方式
            for (int col = 1; col <= headers.Length; col++)
            {
                for (int row = 2; row <= r - 1; row++)
                {
                    var cell = ws.Cell(row, col);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }
            }

            // 设置行高
            ws.Row(1).Height = 30; // 表头行高
            for (int row = 2; row <= r - 1; row++)
            {
                ws.Row(row).Height = 50; // 数据行高（允许文本换行显示）
            }

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
