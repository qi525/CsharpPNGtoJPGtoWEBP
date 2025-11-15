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
        /// 将给定的 <paramref name="rows"/> 写入一个 XLSX 文件（路径为 <paramref name="outPath"/>）。
        /// 可选参数 <paramref name="openAfterSave"/> 控制在保存后是否尝试用系统默认应用打开该文件（最佳努力）。
        /// </summary>
        /// <param name="rows">要写入到表格的行集合</param>
        /// <param name="outPath">输出文件路径（.xlsx）</param>
        /// <param name="openAfterSave">是否在保存后尝试打开（默认 false）</param>
        public static void WriteConversionReport(IEnumerable<ConversionReportRow> rows, string outPath, bool openAfterSave = false)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Conversions");

            // Header
            var headers = new[] {
                "SourcePath", "DestPath",
                "SourceWidth", "SourceHeight", "SourceFormat", "SourceParams",
                "DestWidth", "DestHeight", "DestFormat", "DestParams",
                "Success", "ErrorMessage"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            int r = 2;
            foreach (var row in rows)
            {
                ws.Cell(r, 1).Value = row.SourcePath;
                ws.Cell(r, 2).Value = row.DestPath;
                ws.Cell(r, 3).Value = row.SourceWidth;
                ws.Cell(r, 4).Value = row.SourceHeight;
                ws.Cell(r, 5).Value = row.SourceFormat;
                ws.Cell(r, 6).Value = row.SourceParams;
                ws.Cell(r, 7).Value = row.DestWidth;
                ws.Cell(r, 8).Value = row.DestHeight;
                ws.Cell(r, 9).Value = row.DestFormat;
                ws.Cell(r, 10).Value = row.DestParams;
                ws.Cell(r, 11).Value = row.Success;
                ws.Cell(r, 12).Value = row.ErrorMessage;
                r++;
            }

            // Auto-fit columns for readability
            ws.Columns().AdjustToContents();

            // Ensure directory exists
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            wb.SaveAs(outPath);

            if (openAfterSave)
            {
                try
                {
                    // On Windows, launch with UseShellExecute so the default app opens the file.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var psi = new ProcessStartInfo(outPath)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        // Try xdg-open on Linux
                        Process.Start("xdg-open", outPath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // macOS open command
                        Process.Start("open", outPath);
                    }
                }
                catch
                {
                    // best-effort: do not throw from report opening
                }
            }
        }
    }
}
