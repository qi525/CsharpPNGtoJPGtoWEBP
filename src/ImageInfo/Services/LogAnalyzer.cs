using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageInfo.Services
{
    /// <summary>
    /// 日志分析器：检测元数据提取和转换的问题。
    /// </summary>
    public static class LogAnalyzer
    {
        public class DiagnosisReport
        {
            public int Total { get; set; }
            public int Missing { get; set; }
            public int RawBytes { get; set; }
            public string? GeneratedReportPath { get; set; }
        }

        /// <summary>
        /// 执行诊断分析（扫描 metadata-extraction.log 和 conversion.log）。
        /// </summary>
        public static DiagnosisReport Analyze(string sourceFolder)
        {
            var report = new DiagnosisReport();

            // 元数据提取日志在工作目录（仅关键事件：ALARM 和 RawBytes）
            var extractionLog = Path.Combine(Environment.CurrentDirectory, "metadata-extraction.log");

            if (File.Exists(extractionLog))
            {
                AnalyzeExtractionLog(extractionLog, report);
            }

            // 转换统计日志在 sourceFolder
            var conversionLog = Path.Combine(sourceFolder, "conversion.log");
            if (File.Exists(conversionLog))
            {
                AnalyzeConversionLog(conversionLog, report);
            }

            // 生成诊断报告文件（保存到 sourceFolder）
            report.GeneratedReportPath = GenerateDiagnosisReport(report, sourceFolder);

            return report;
        }

        private static void AnalyzeExtractionLog(string logPath, DiagnosisReport report)
        {
            try
            {
                var lines = File.ReadAllLines(logPath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (line.Contains("ALARM"))
                        report.Missing++;
                    else if (line.Contains("RawBytes"))
                        report.RawBytes++;
                }
            }
            catch { }
        }

        private static void AnalyzeConversionLog(string logPath, DiagnosisReport report)
        {
            try
            {
                var lines = File.ReadAllLines(logPath, Encoding.UTF8);
                int maxTotal = 0;
                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"Total:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int total))
                        maxTotal = Math.Max(maxTotal, total);
                }
                report.Total = maxTotal;
            }
            catch { }
        }

        private static string? GenerateDiagnosisReport(DiagnosisReport report, string sourceFolder)
        {
            try
            {
                var reportPath = Path.Combine(sourceFolder, "diagnosis.log");
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Diagnosis Report");
                sb.AppendLine($"Total: {report.Total} | FullInfo: {report.Total - report.Missing} | Missing: {report.Missing} | RawBytes: {report.RawBytes}");
                if (report.Total > 0)
                {
                    double missingPct = (double)report.Missing / report.Total * 100;
                    double rawBytesPct = (double)report.RawBytes / report.Total * 100;
                    if (missingPct > 10)
                        sb.AppendLine($"WARNING: Missing FullInfo > 10% ({missingPct:F1}%). Check metadata storage or upgrade libraries.");
                    if (rawBytesPct > 5)
                        sb.AppendLine($"WARNING: RawBytes.Fallback > 5% ({rawBytesPct:F1}%). Consider upgrading MetadataExtractor or adding format handlers.");
                    if (report.Missing == 0 && report.RawBytes == 0)
                        sb.AppendLine("OK: All files extracted successfully.");
                }
                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
                return reportPath;
            }
            catch { return null; }
        }

        /// <summary>将诊断报告输出到控制台。</summary>
        public static void PrintDiagnosisToConsole(DiagnosisReport report)
        {
            if (report == null) return;
            Console.WriteLine("\n【诊断摘要】");
            Console.WriteLine($"  处理: {report.Total} | 缺失: {report.Missing} | RawBytes: {report.RawBytes}");
            if (!string.IsNullOrEmpty(report.GeneratedReportPath))
                Console.WriteLine($"  报告: {report.GeneratedReportPath}");
        }
    }
}
