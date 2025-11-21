using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImageInfo.Models;
using ImageMagick;
using ClosedXML.Excel;

namespace ImageInfo.Services
{
    /// <summary>
    /// 开发模式服务：处理各种开发调试功能
    /// 包括：只读诊断、元数据测试等
    /// </summary>
    public static class DevelopmentModeService
    {
        /// <summary>
        /// 扫描模式：扫描文件夹，读取所有图片的元数据，生成 Excel 报告
        /// </summary>
        public static void RunScanMode(string folder)
        {
            Console.WriteLine($"扫描模式：读取元数据并生成 Excel 报告\n");
            Console.WriteLine($"扫描文件夹: {folder}\n");
            
            var allFiles = FileScanner.GetImageFiles(folder).ToList();
            Console.WriteLine($"找到 {allFiles.Count} 个图片文件\n");

            if (allFiles.Count == 0)
            {
                Console.WriteLine("未找到任何图片文件");
                return;
            }

            // 收集元数据
            Console.WriteLine("[步骤1] 读取元数据...");
            var metadataList = new List<MetadataRecord>();
            int processed = 0;

            foreach (var filePath in allFiles)
            {
                try
                {
                    var metadata = MetadataExtractors.ReadAIMetadata(filePath);
                    metadataList.Add(new MetadataRecord
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        FileFormat = Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.'),
                        Prompt = metadata.Prompt ?? string.Empty,
                        NegativePrompt = metadata.NegativePrompt ?? string.Empty,
                        Model = metadata.Model ?? string.Empty,
                        Seed = metadata.Seed ?? string.Empty,
                        Sampler = metadata.Sampler ?? string.Empty,
                        OtherInfo = metadata.OtherInfo ?? string.Empty,
                        FullInfo = metadata.FullInfo ?? string.Empty,
                        ExtractionMethod = metadata.FullInfoExtractionMethod ?? string.Empty
                    });

                    processed++;
                    if (processed % 10 == 0)
                        Console.Write($"已处理: {processed}/{allFiles.Count}\r");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"警告: 读取 {Path.GetFileName(filePath)} 时出错: {ex.Message}");
                }
            }
            Console.WriteLine($"已处理: {processed}/{allFiles.Count}     \n");

            // 生成 Excel 报告
            Console.WriteLine("[步骤2] 生成 Excel 报告...");
            string reportPath = GenerateExcelReport(metadataList, folder);

            if (!string.IsNullOrEmpty(reportPath) && File.Exists(reportPath))
            {
                Console.WriteLine($"✓ 报告已生成: {reportPath}\n");
                
                // 自动打开
                Console.WriteLine("[步骤3] 自动打开报告...");
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = reportPath,
                        UseShellExecute = true
                    });
                    Console.WriteLine("✓ 报告已打开");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ 无法自动打开报告: {ex.Message}");
                    Console.WriteLine($"请手动打开: {reportPath}");
                }
            }
            else
            {
                Console.WriteLine("✗ 生成报告失败");
            }
        }

        /// <summary>
        /// 运行完整的元数据写入/读取/验证测试
        /// </summary>
        public static void RunFullMetadataTest()
        {
            string testDir = @"C:\Users\SNOW\Desktop\test\";
            if (!Directory.Exists(testDir))
                Directory.CreateDirectory(testDir);

            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("   完整元数据测试：写入 → 读取 → 验证");
            Console.WriteLine("═══════════════════════════════════════════════════\n");

            string pngPath = Path.Combine(testDir, "test_meta_write.png");
            string jpgPath = Path.Combine(testDir, "test_meta_write.jpg");
            string webpPath = Path.Combine(testDir, "test_meta_write.webp");

            // 清理旧文件
            foreach (var f in new[] { pngPath, jpgPath, webpPath })
                if (File.Exists(f)) 
                    File.Delete(f);

            // 步骤1：生成测试图像
            GenerateTestImages(pngPath, jpgPath, webpPath);

            // 步骤2：创建测试元数据
            var testMetadata = new AIMetadata
            {
                FullInfo = "Steps: 25, Sampler: euler_ancestral, CFG: 8.0, Model: stable-diffusion-v1-5",
                Prompt = "a beautiful landscape with mountains",
                NegativePrompt = "ugly, distorted, blurry",
                Sampler = "euler_ancestral",
                Model = "stable-diffusion-v1-5",
                Seed = "12345"
            };

            // 步骤3：写入元数据
            WriteTestMetadata(pngPath, jpgPath, webpPath, testMetadata);

            // 步骤4：读取已写入的元数据
            ReadTestMetadata(pngPath, jpgPath, webpPath);

            // 步骤5：验证字节级编码
            VerifyByteLevel(jpgPath, webpPath);

            // 步骤6：总结
            PrintTestSummary();
        }

        /// <summary>
        /// 运行开发模式的完整转换流程
        /// </summary>
        public static void RunFullConversionMode(string folder)
        {
            Console.WriteLine("[开发模式-转换] 将自动批量运行所有三种转换模式...\n");
            var mode = OutputDirectoryMode.SiblingDirectoryWithStructure;

            LogAnalyzer.DiagnosisReport? lastDiagnosis = null;

            Console.WriteLine("\n=== 模式 1: PNG -> JPG ===");
            lastDiagnosis = ConversionService.ScanConvertAndReport(folder, 1, mode, openReport: true);

            Console.WriteLine("\n=== 模式 2: PNG -> WEBP ===");
            lastDiagnosis = ConversionService.ScanConvertAndReport(folder, 2, mode, openReport: true);

            Console.WriteLine("\n=== 模式 3: JPG -> WEBP ===");
            lastDiagnosis = ConversionService.ScanConvertAndReport(folder, 3, mode, openReport: true);

            Console.WriteLine("\n[开发模式完成] 所有三种转换已执行，报告已逐次自动打开。");
            
            if (lastDiagnosis != null)
            {
                LogAnalyzer.PrintDiagnosisToConsole(lastDiagnosis);
            }
        }

        #region 私有辅助方法

        private static void GenerateTestImages(string pngPath, string jpgPath, string webpPath)
        {
            Console.WriteLine("[步骤1] 生成测试图像...");
            try
            {
                using (var img = new MagickImage(MagickColors.Gray, 100, 100))
                {
                    img.Format = MagickFormat.Png;
                    img.Write(pngPath);
                }
                Console.WriteLine($"  ✓ PNG已生成: {Path.GetFileName(pngPath)}");

                using (var img = new MagickImage(MagickColors.Gray, 100, 100))
                {
                    img.Format = MagickFormat.Jpeg;
                    img.Quality = 95u;
                    img.Write(jpgPath);
                }
                Console.WriteLine($"  ✓ JPG已生成: {Path.GetFileName(jpgPath)}");

                using (var img = new MagickImage(MagickColors.Gray, 100, 100))
                {
                    img.Format = MagickFormat.WebP;
                    img.Quality = 80u;
                    img.Write(webpPath);
                }
                Console.WriteLine($"  ✓ WebP已生成: {Path.GetFileName(webpPath)}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ 生成失败: {ex.Message}\n");
            }
        }

        private static void WriteTestMetadata(string pngPath, string jpgPath, string webpPath, AIMetadata testMetadata)
        {
            Console.WriteLine("[步骤2] 写入元数据...");
            var formats = new[] {
                (path: pngPath, format: ".png", name: "PNG"),
                (path: jpgPath, format: ".jpg", name: "JPEG"),
                (path: webpPath, format: ".webp", name: "WebP")
            };

            foreach (var (path, format, name) in formats)
            {
                Console.WriteLine($"\n  [{name}]");
                try
                {
                    var (written, verified) = MetadataWriter.WriteMetadata(path, format, testMetadata);
                    Console.WriteLine($"    写入: {(written ? "✓" : "✗")}");
                    Console.WriteLine($"    验证: {(verified ? "✓" : "✗")}");
                    Console.WriteLine($"    → {GetWriteStatus(written, verified)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ 异常: {ex.Message}");
                }
            }
        }

        private static void ReadTestMetadata(string pngPath, string jpgPath, string webpPath)
        {
            Console.WriteLine("\n[步骤3] 读取已写入的元数据...");
            var formats = new[] {
                (path: pngPath, name: "PNG"),
                (path: jpgPath, name: "JPEG"),
                (path: webpPath, name: "WebP")
            };

            foreach (var (path, name) in formats)
            {
                Console.WriteLine($"\n  [{name}]");
                try
                {
                    var read = MetadataExtractors.ReadAIMetadata(path);
                    if (!string.IsNullOrEmpty(read.FullInfo))
                        Console.WriteLine($"    ✓ FullInfo: {TruncateText(read.FullInfo, 60)}");
                    else
                        Console.WriteLine($"    ✗ 未读到 FullInfo");

                    if (!string.IsNullOrEmpty(read.Prompt))
                        Console.WriteLine($"    ✓ Prompt: {TruncateText(read.Prompt, 40)}");

                    if (!string.IsNullOrEmpty(read.Model))
                        Console.WriteLine($"    ✓ Model: {read.Model}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ 异常: {ex.Message}");
                }
            }
        }

        private static void VerifyByteLevel(string jpgPath, string webpPath)
        {
            Console.WriteLine("\n[步骤4] 字节级验证...");
            VerifyByteLevelMetadata(jpgPath, "JPEG");
            VerifyByteLevelMetadata(webpPath, "WebP");
        }

        private static void VerifyByteLevelMetadata(string filePath, string format)
        {
            Console.WriteLine($"\n  [{format}]");
            try
            {
                using (var image = new MagickImage(filePath))
                {
                    var exif = image.GetExifProfile();
                    if (exif != null)
                    {
                        var userComment = exif.GetValue(ExifTag.UserComment);
                        if (userComment != null)
                        {
                            var bytes = userComment.GetValue() as byte[];
                            if (bytes != null)
                            {
                                Console.WriteLine($"    字节长度: {bytes.Length}");
                                string decoded = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                                Console.WriteLine($"    解码内容: {decoded}");
                                Console.WriteLine($"    ✓ 成功读取");
                            }
                            else
                            {
                                Console.WriteLine($"    ✗ UserComment不是字节数组");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"    ✗ 未找到 UserComment 标签");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"    ✗ 没有 EXIF 信息");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ✗ 异常: {ex.Message}");
            }
        }

        private static void PrintTestSummary()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════");
            Console.WriteLine("   测试完成");
            Console.WriteLine("═══════════════════════════════════════════════════\n");
        }

        private static string TruncateText(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "[空]";
            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }

        private static string GetWriteStatus(bool written, bool verified)
        {
            if (written && verified) return "成功！";
            if (written) return "警告：已写入但验证失败";
            return "失败";
        }

        private static string GenerateExcelReport(List<MetadataRecord> records, string scanFolder)
        {
            try
            {
                string reportName = $"metadata_scan_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
                string reportPath = Path.Combine(Path.GetTempPath(), reportName);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("元数据扫描报告");

                    // 设置列头
                    var headers = new[] { "文件名", "路径", "格式", "Prompt", "NegativePrompt", "Model", "Seed", "Sampler", "其他信息", "完整信息", "提取方法" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    // 填充数据
                    int row = 2;
                    foreach (var record in records)
                    {
                        worksheet.Cell(row, 1).Value = record.FileName;
                        worksheet.Cell(row, 2).Value = record.FilePath;
                        worksheet.Cell(row, 3).Value = record.FileFormat;
                        worksheet.Cell(row, 4).Value = record.Prompt;
                        worksheet.Cell(row, 5).Value = record.NegativePrompt;
                        worksheet.Cell(row, 6).Value = record.Model;
                        worksheet.Cell(row, 7).Value = record.Seed;
                        worksheet.Cell(row, 8).Value = record.Sampler;
                        worksheet.Cell(row, 9).Value = record.OtherInfo;
                        worksheet.Cell(row, 10).Value = record.FullInfo;
                        worksheet.Cell(row, 11).Value = record.ExtractionMethod;
                        row++;
                    }

                    // 调整列宽
                    worksheet.Columns().AdjustToContents();
                    worksheet.Column(2).Width = 15;  // 路径列宽度
                    worksheet.Column(4).Width = 15;  // Prompt 列
                    worksheet.Column(10).Width = 15; // 完整信息列

                    // 添加摘要页
                    var summary = workbook.Worksheets.Add("摘要");
                    summary.Cell(1, 1).Value = "扫描摘要";
                    summary.Cell(1, 1).Style.Font.Bold = true;
                    summary.Cell(1, 1).Style.Font.FontSize = 14;

                    summary.Cell(3, 1).Value = "扫描文件夹:";
                    summary.Cell(3, 2).Value = scanFolder;

                    summary.Cell(4, 1).Value = "扫描时间:";
                    summary.Cell(4, 2).Value = DateTime.Now;

                    summary.Cell(5, 1).Value = "文件总数:";
                    summary.Cell(5, 2).Value = records.Count;

                    var formatCount = records.GroupBy(r => r.FileFormat).ToDictionary(g => g.Key, g => g.Count());
                    int summaryRow = 7;
                    summary.Cell(summaryRow, 1).Value = "格式统计";
                    summary.Cell(summaryRow, 1).Style.Font.Bold = true;
                    summaryRow++;

                    foreach (var (format, count) in formatCount)
                    {
                        summary.Cell(summaryRow, 1).Value = format;
                        summary.Cell(summaryRow, 2).Value = count;
                        summaryRow++;
                    }

                    var methodCount = records.GroupBy(r => r.ExtractionMethod).ToDictionary(g => g.Key, g => g.Count());
                    summaryRow += 2;
                    summary.Cell(summaryRow, 1).Value = "提取方法统计";
                    summary.Cell(summaryRow, 1).Style.Font.Bold = true;
                    summaryRow++;

                    foreach (var (method, count) in methodCount)
                    {
                        summary.Cell(summaryRow, 1).Value = string.IsNullOrEmpty(method) ? "[未知]" : method;
                        summary.Cell(summaryRow, 2).Value = count;
                        summaryRow++;
                    }

                    summary.Columns().AdjustToContents();

                    workbook.SaveAs(reportPath);
                }

                return reportPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 生成 Excel 报告失败: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion
    }

    /// <summary>
    /// 元数据记录：用于 Excel 报告
    /// </summary>
    public class MetadataRecord
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileFormat { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Seed { get; set; } = string.Empty;
        public string Sampler { get; set; } = string.Empty;
        public string OtherInfo { get; set; } = string.Empty;
        public string FullInfo { get; set; } = string.Empty;
        public string ExtractionMethod { get; set; } = string.Empty;
    }
}
