using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageInfo.Services;
using Spectre.Console;
using ImageMagick;

namespace ImageInfo
{
    /// <summary>
    /// 批量转换演示程序，展示使用 AdvancedBatchConverter 和 ProgressBarManager 的功能。
    /// 包括显示运行时间、剩余时间、处理速度等。
    /// </summary>
    public class BatchConversionDemo
    {
        public static async Task Main(string[] args)
        {
            AnsiConsole.Write(new FigletText("ImageInfo Batch Converter")
            {
                Color = Color.Green
            });

            // 示例 1: 批量转换 PNG 到 WebP（带进度显示）
            await DemoPngToWebPConversion();

            // 示例 2: 演示进度条管理器的独立功能
            DemoProgressBarManager();

            AnsiConsole.MarkupLine("[green bold]✓ 所有演示完成![/]");
        }

        /// <summary>
        /// 演示 PNG 到 WebP 的批量转换。
        /// </summary>
        private static async Task DemoPngToWebPConversion()
        {
            AnsiConsole.MarkupLine("\n[bold blue]演示 1: PNG → WebP 批量转换（带进度显示）[/]");
            AnsiConsole.MarkupLine("[yellow]─────────────────────────────────────────[/]\n");

            // 创建临时 PNG 文件用于演示
            var tempDir = Path.Combine(Path.GetTempPath(), "imageinfo_demo");
            Directory.CreateDirectory(tempDir);

            try
            {
                var pngFiles = CreateSamplePngFiles(tempDir, 5);

                AnsiConsole.MarkupLine($"[cyan]已创建 {pngFiles.Count} 个示例 PNG 文件[/]");

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // 执行批量转换（使用进度显示）
                var results = await AdvancedBatchConverter.ConvertPngToWebPWithProgressAsync(
                    pngFiles,
                    outputDir,
                    quality: 80,
                    maxDegreeOfParallelism: 2,
                    showProgress: true
                );

                // 显示转换结果统计
                AnsiConsole.MarkupLine("\n[bold green]转换结果统计:[/]");
                var successCount = results.FindAll(r => r.Success).Count;
                var totalSize = 0L;
                var compressedSize = 0L;

                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        totalSize += result.SourceSize;
                        compressedSize += result.OutputSize;
                    }
                }

                var overallCompression = totalSize > 0 ? (1 - (double)compressedSize / totalSize) * 100 : 0;
                AnsiConsole.MarkupLine($"[lime]总体压缩率: {overallCompression:F1}%[/]");

                Console.WriteLine();
            }
            finally
            {
                // 清理临时文件
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        /// <summary>
        /// 演示 ProgressBarManager 的独立功能。
        /// </summary>
        private static void DemoProgressBarManager()
        {
            AnsiConsole.MarkupLine("\n[bold blue]演示 2: 进度条管理器功能[/]");
            AnsiConsole.MarkupLine("[yellow]─────────────────────────────────────────[/]\n");

            using (var progress = new ProgressBarManager(10))
            {
                ProgressBarManager.ShowInfo("模拟处理 10 个文件");

                for (int i = 0; i < 10; i++)
                {
                    progress.StartFileProcessing();
                    System.Threading.Thread.Sleep(500); // 模拟处理
                    var elapsed = progress.StopFileProcessing();

                    progress.UpdateProgress($"file_{i}.png");
                    
                    // 模拟显示文件详情
                    if (i % 3 == 0)
                    {
                        progress.ShowFileDetails(
                            $"file_{i}.png",
                            true,
                            1024 * 1024,     // 1 MB
                            512 * 1024,      // 512 KB
                            elapsed
                        );
                    }
                }

                progress.ShowSummary(10, 0);
            }

            AnsiConsole.MarkupLine("[green]进度条演示完成[/]");

            // 演示警告、错误等消息
            ProgressBarManager.ShowSuccess("处理成功完成");
            ProgressBarManager.ShowWarning("这是一个警告消息");
            ProgressBarManager.ShowInfo("这是一个信息消息");
            ProgressBarManager.ShowError("这是一个错误消息");
        }

        /// <summary>
        /// 创建示例 PNG 文件。
        /// </summary>
        private static List<string> CreateSamplePngFiles(string directory, int count)
        {
            var files = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var filePath = Path.Combine(directory, $"sample_{i:D2}.png");
                
                // 使用 Magick.NET 创建示例图像
                try
                {
                    using (var img = new ImageMagick.MagickImage(ImageMagick.MagickColors.White, (uint)(64 + i * 32), (uint)(64 + i * 32)))
                    {
                        img.Format = ImageMagick.MagickFormat.Png;
                        img.Write(filePath);
                        files.Add(filePath);
                    }
                }
                catch { }
            }

            return files;
        }
    }
}
