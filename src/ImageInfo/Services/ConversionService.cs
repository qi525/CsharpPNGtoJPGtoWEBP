using System;
using System.Collections.Generic;
using System.IO;
using ImageInfo.Models;
using SixLabors.ImageSharp;
using System.Linq;

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
                    
                    // 使用 MetadataExtractorFactory 统一提取所有格式的元数据
                    var aiMetadata = MetadataExtractorFactory.GetImageInfo(srcPath);

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
        /// 
        /// 工作流：
        /// 1. 【读】源文件加载、元数据读取
        /// 2. 【写】格式转换、时间戳同步、元数据写入
        /// 3. 【验证】结果校验、时间戳验证、元数据验证、创建时间验证
        /// 4. 【恢复】失败时备份源文件，成功时设置创建时间（Windows P/Invoke）
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
                
                // 使用 CreationTimeService 设置创建时间（Windows P/Invoke）
                CreationTimeService.SetCreationTime(destPath, createdUtc);

                // 【验证】读取并验证结果
                using var destImg = Image.Load(destPath);
                var isValidConversion = ValidationService.ValidateConversion(srcPath, destPath);
                var isTimeValid = FileTimeService.VerifyFileTimes(destPath, modifiedUtc);
                var isCreationTimeValid = CreationTimeService.VerifyCreationTime(destPath, createdUtc);
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
                    Success = isValidConversion && isTimeValid && isCreationTimeValid && isMetadataValid,
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
                // 转换失败时，将源文件备份到目标目录
                var backupFilePath = FileBackupService.CreateBackupFile(srcPath, reportDir);
                var backupVerified = backupFilePath != null && FileBackupService.VerifyBackupFile(srcPath, backupFilePath);
                
                Console.WriteLine($"Conversion failed for {Path.GetFileName(srcPath)}: {ex.Message}");
                if (backupVerified)
                {
                    Console.WriteLine($"Source file backed up: {backupFilePath}");
                }

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
                    ErrorMessage = backupVerified ? $"{ex.Message} (source backed up)" : ex.Message,
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
        /// 根据输出目录模式和源文件路径，计算实际输出目录。
        /// 
        /// 模式 1 (SiblingDirectoryWithStructure):
        ///   源: D:/Pictures/folder/subfolder/photo.png
        ///   根: D:/Pictures
        ///   输出前缀: PNG转JPG
        ///   结果: D:/PNG转JPG/folder/subfolder/
        /// 
        /// 模式 2 (LocalSubdirectory):
        ///   源: D:/Pictures/folder/subfolder/photo.png
        ///   输出前缀: PNG转JPG
        ///   结果: D:/Pictures/folder/subfolder/PNG转JPG/
        /// </summary>
        private static string GetOutputDirectory(string sourceFilePath, string outputDirPrefix, 
            string? rootFolder, OutputDirectoryMode mode)
        {
            var sourceFileDir = Path.GetDirectoryName(sourceFilePath) ?? string.Empty;

            return mode switch
            {
                OutputDirectoryMode.SiblingDirectoryWithStructure =>
                    GetSiblingDirectoryPath(sourceFilePath, outputDirPrefix, rootFolder),
                
                OutputDirectoryMode.LocalSubdirectory =>
                    Path.Combine(sourceFileDir, outputDirPrefix),
                
                _ => Path.Combine(sourceFileDir, outputDirPrefix)
            };
        }

        /// <summary>
        /// 计算兄弟目录 + 复刻结构的输出路径。
        /// 
        /// 工作流：
        /// 1. 获取根文件夹的父目录
        /// 2. 在父目录下创建输出前缀目录
        /// 3. 在该目录下复刻源目录结构
        /// </summary>
        private static string GetSiblingDirectoryPath(string sourceFilePath, string outputDirPrefix, string? rootFolder)
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                // 如果没有提供根目录，回退到本地模式
                var sourceDir = Path.GetDirectoryName(sourceFilePath) ?? string.Empty;
                return Path.Combine(sourceDir, outputDirPrefix);
            }

            var rootFolderAbs = Path.GetFullPath(rootFolder);
            var sourceFileAbs = Path.GetFullPath(sourceFilePath);
            
            // 计算源文件相对于根目录的相对路径
            var relativePath = GetRelativePath(rootFolderAbs, sourceFileAbs);
            var relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;

            // 获取根文件夹名称
            var rootFolderName = new DirectoryInfo(rootFolderAbs).Name;

            // 在父目录中创建输出前缀目录
            var parentOfRoot = Directory.GetParent(rootFolderAbs)?.FullName ?? string.Empty;
            var outputBase = Path.Combine(parentOfRoot, outputDirPrefix);
            
            // 复刻完整结构：outputBase/根目录名/相对目录
            return Path.Combine(outputBase, rootFolderName, relativeDir);
        }

        /// <summary>
        /// 计算相对路径（.NET Standard 兼容版本）。
        /// </summary>
        private static string GetRelativePath(string basePath, string fullPath)
        {
            basePath = Path.GetFullPath(basePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            var relativePath = fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            return relativePath;
        }

        /// <summary>
        /// 根据格式选择相应的元数据提取器。
        /// 
        /// 【优化说明】
        /// 此方法已被 MetadataExtractorFactory.GetImageInfo() 替代。
        /// 新方法集中了类型判断和分派逻辑，更符合 Factory Pattern。
        /// 保留此方法用于向后兼容。
        /// </summary>
        [Obsolete("Use MetadataExtractorFactory.GetImageInfo() instead", false)]
        private static AIMetadata ExtractAIMetadata(string imagePath, string format)
        {
            // 回退到工厂方法
            return MetadataExtractorFactory.GetImageInfo(imagePath);
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
