using System;
using ImageInfo.Models;
using ImageInfo.Services;
using System.IO;
using System.Collections.Generic;

namespace ImageInfo;

class Program
{
    /// <summary>
    /// 程序入口：示例展示如何扫描目录、读取元数据并生成一个简单的转换报告。
    /// 注意：该 Main 仅为示例用途，实际生产可将逻辑提取为可测试的服务/命令行参数解析。
    /// </summary>
    static int Main(string[] args)
    {
        var folder = args.Length > 0 ? args[0] : @"C:\\Users\\10374\\Desktop\\test";
        Console.WriteLine($"Scanning folder: {folder}");

        var files = FileScanner.GetImageFiles(folder).ToList();
        Console.WriteLine($"Found {files.Count} image(s)");

        foreach (var path in files)
        {
            var meta = MetadataService.ExtractTagsAndTimes(path);
            Console.WriteLine($"---\nFile: {path}");
            Console.WriteLine($"Created: {meta.CreatedUtc:u}");
            Console.WriteLine($"Modified: {meta.ModifiedUtc:u}");
            Console.WriteLine($"Tags: {string.Join(", ", meta.Tags)}");
        }

        // Example: write a simple conversion report into the scanned folder and open it when done.
        var rows = new List<ConversionReportRow>();
        foreach (var path in files)
        {
            rows.Add(new ConversionReportRow
            {
                SourcePath = path,
                DestPath = string.Empty,
                SourceFormat = Path.GetExtension(path)?.TrimStart('.')?.ToUpperInvariant(),
                Success = true
            });
        }

        var outReport = Path.Combine(folder, "conversion-report.xlsx");
        ReportService.WriteConversionReport(rows, outReport, openAfterSave: true);

        return 0;
    }
}
