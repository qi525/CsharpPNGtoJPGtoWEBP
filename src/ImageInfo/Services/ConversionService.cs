using System;
using System.Collections.Generic;
using System.IO;
using ImageInfo.Models;
using SixLabors.ImageSharp;

namespace ImageInfo.Services
{
    /// <summary>
    /// 协调图片格式转换与报告生成的服务。将转换逻辑与报告生成分离，便于测试和复用。
    /// </summary>
    public static class ConversionService
    {
        /// <summary>
        /// 扫描目录、进行格式转换（PNG->JPEG/WebP、JPEG->WebP）、生成报告。
        /// </summary>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="openReport">是否在生成后打开报告（默认 false）</param>
        public static void ScanConvertAndReport(string sourceFolder, bool openReport = false)
        {
            var files = FileScanner.GetImageFiles(sourceFolder);
            var rows = GenerateConversionRows(sourceFolder, files);

            if (rows.Count > 0)
            {
                var outReport = Path.Combine(sourceFolder, "conversion-report.xlsx");
                ReportService.WriteConversionReport(rows, outReport, openAfterSave: openReport);
                Console.WriteLine($"Report generated: {outReport} ({rows.Count} conversions)");
                Console.WriteLine($"Log file: {Path.Combine(sourceFolder, "conversion-log.txt")}");
            }
            else
            {
                Console.WriteLine("No conversions to report.");
            }
        }

        /// <summary>
        /// 生成转换报告行集合，包含所有转换操作及结果。
        /// </summary>
        private static List<ConversionReportRow> GenerateConversionRows(string sourceFolder, IEnumerable<string> files)
        {
            var rows = new List<ConversionReportRow>();
            var reportDir = Path.Combine(sourceFolder, "converted");
            if (!Directory.Exists(reportDir))
                Directory.CreateDirectory(reportDir);

            foreach (var srcPath in files)
            {
                try
                {
                    using var srcImg = Image.Load(srcPath);
                    var srcFormat = Path.GetExtension(srcPath).TrimStart('.').ToUpperInvariant();

                    // 读取源文件的时间戳
                    var (createdUtc, modifiedUtc) = FileTimeService.ReadFileTimes(srcPath);
                    
                    // 根据格式使用相应的元数据提取器
                    var aiMetadata = ExtractAIMetadata(srcPath, srcFormat);

                    if (srcFormat == "PNG")
                    {
                        AddConversionRow(rows, srcPath, srcImg, reportDir, "JPG", 85, createdUtc, modifiedUtc, aiMetadata);
                        AddConversionRow(rows, srcPath, srcImg, reportDir, "WEBP", 80, createdUtc, modifiedUtc, aiMetadata);
                    }
                    else if (srcFormat == "JPG" || srcFormat == "JPEG")
                    {
                        AddConversionRow(rows, srcPath, srcImg, reportDir, "WEBP", 80, createdUtc, modifiedUtc, aiMetadata);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {srcPath}: {ex.Message}");
                }
            }

            return rows;
        }

        /// <summary>
        /// 添加一行转换记录到列表中。
        /// 实现读→写→验证的完整流程。
        /// </summary>
        private static void AddConversionRow(List<ConversionReportRow> rows, string srcPath, Image srcImg, 
            string reportDir, string destFormat, int quality, DateTime createdUtc, DateTime modifiedUtc, AIMetadata aiMetadata)
        {
            var srcFormat = Path.GetExtension(srcPath).TrimStart('.').ToUpperInvariant();
            var destExt = destFormat.ToLower();
            var destPath = Path.Combine(reportDir, Path.GetFileNameWithoutExtension(srcPath) + "." + destExt);

            try
            {
                // 【读】已在前面执行（源文件加载、元数据读取）
                
                // 【写】执行格式转换
                if (srcFormat == "PNG" && destFormat == "JPG")
                    ImageConverter.ConvertPngToJpeg(srcPath, destPath, quality);
                else if (srcFormat == "PNG" && destFormat == "WEBP")
                    ImageConverter.ConvertPngToWebP(srcPath, destPath, quality);
                else if ((srcFormat == "JPG" || srcFormat == "JPEG") && destFormat == "WEBP")
                    ImageConverter.ConvertJpegToWebP(srcPath, destPath, quality);

                // 写入 AI 元数据
                WriteAIMetadata(destPath, destFormat, aiMetadata);

                // 复刻文件时间
                FileTimeService.WriteFileTimes(destPath, createdUtc, modifiedUtc);

                // 【验证】读取并验证结果
                using var destImg = Image.Load(destPath);
                var isValidConversion = ValidationService.ValidateConversion(srcPath, destPath);
                var isTimeValid = FileTimeService.VerifyFileTimes(destPath, modifiedUtc);
                var isMetadataValid = VerifyAIMetadata(destPath, destFormat, aiMetadata);

                rows.Add(new ConversionReportRow
                {
                    SourcePath = Path.GetFullPath(srcPath),
                    DestPath = Path.GetFullPath(destPath),
                    SourceWidth = srcImg.Width,
                    SourceHeight = srcImg.Height,
                    SourceFormat = srcFormat,
                    SourceParams = srcFormat == "PNG" ? "RGBA" : "RGB",
                    DestWidth = destImg.Width,
                    DestHeight = destImg.Height,
                    DestFormat = destFormat,
                    DestParams = $"Quality:{quality}",
                    Success = isValidConversion && isTimeValid && isMetadataValid,
                    ErrorMessage = null,
                    AIPrompt = aiMetadata?.Prompt,
                    AINegativePrompt = aiMetadata?.NegativePrompt,
                    AIModel = aiMetadata?.Model,
                    AISeed = aiMetadata?.Seed,
                    AISampler = aiMetadata?.Sampler,
                    AIMetadata = aiMetadata?.OtherInfo,
                    SourceCreatedUtc = createdUtc,
                    SourceModifiedUtc = modifiedUtc
                });
            }
            catch (Exception ex)
            {
                rows.Add(new ConversionReportRow
                {
                    SourcePath = Path.GetFullPath(srcPath),
                    DestPath = Path.GetFullPath(destPath),
                    SourceWidth = srcImg.Width,
                    SourceHeight = srcImg.Height,
                    SourceFormat = srcFormat,
                    SourceParams = srcFormat == "PNG" ? "RGBA" : "RGB",
                    DestWidth = null,
                    DestHeight = null,
                    DestFormat = destFormat,
                    DestParams = $"Quality:{quality}",
                    Success = false,
                    ErrorMessage = ex.Message,
                    AIPrompt = aiMetadata?.Prompt,
                    AINegativePrompt = aiMetadata?.NegativePrompt,
                    AIModel = aiMetadata?.Model,
                    AISeed = aiMetadata?.Seed,
                    AISampler = aiMetadata?.Sampler,
                    AIMetadata = aiMetadata?.OtherInfo,
                    SourceCreatedUtc = createdUtc,
                    SourceModifiedUtc = modifiedUtc
                });
            }
        }

        /// <summary>
        /// 根据格式选择相应的元数据提取器。
        /// </summary>
        private static AIMetadata ExtractAIMetadata(string imagePath, string format)
        {
            return format switch
            {
                "PNG" => PngMetadataExtractor.ReadAIMetadata(imagePath),
                "JPG" or "JPEG" => JpegMetadataExtractor.ReadAIMetadata(imagePath),
                "WEBP" => WebPMetadataExtractor.ReadAIMetadata(imagePath),
                _ => new AIMetadata()
            };
        }

        /// <summary>
        /// 根据格式选择相应的元数据写入方法。
        /// </summary>
        private static void WriteAIMetadata(string destPath, string format, AIMetadata aiMetadata)
        {
            switch (format)
            {
                case "PNG":
                    PngMetadataExtractor.WriteAIMetadata(destPath, aiMetadata);
                    break;
                case "JPG":
                case "JPEG":
                    JpegMetadataExtractor.WriteAIMetadata(destPath, aiMetadata);
                    break;
                case "WEBP":
                    WebPMetadataExtractor.WriteAIMetadata(destPath, aiMetadata);
                    break;
            }
        }

        /// <summary>
        /// 根据格式选择相应的元数据验证方法。
        /// </summary>
        private static bool VerifyAIMetadata(string destPath, string format, AIMetadata aiMetadata)
        {
            return format switch
            {
                "PNG" => PngMetadataExtractor.VerifyAIMetadata(destPath, aiMetadata),
                "JPG" or "JPEG" => JpegMetadataExtractor.VerifyAIMetadata(destPath, aiMetadata),
                "WEBP" => WebPMetadataExtractor.VerifyAIMetadata(destPath, aiMetadata),
                _ => false
            };
        }
    }
}
