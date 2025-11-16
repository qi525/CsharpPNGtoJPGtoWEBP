using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ImageInfo.Services;

namespace ImageInfo.Examples
{
    /// <summary>
    /// 进度条功能演示程序。
    /// 展示 ProgressBarManager 的各种功能。
    /// </summary>
    public static class ProgressBarDemo
    {
        public static void Demo_BasicProgress()
        {
            Console.WriteLine("\n=== 演示 1: 基础进度更新 ===\n");
            
            var imageCount = 10;
            var progressManager = new ProgressBarManager(imageCount);
            var random = new Random();

            for (int i = 0; i < imageCount; i++)
            {
                Thread.Sleep(500); // 模拟处理耗时

                var fileName = $"image_{i + 1}.png";
                var fileSize = (long)(random.NextDouble() * 5 * 1024 * 1024); // 0-5MB
                var success = i % 3 != 0; // 每三个文件有一个失败

                progressManager.UpdateProgress(
                    currentFile: fileName,
                    success: success,
                    fileSize: success ? fileSize : 0
                );
            }

            progressManager.ShowSummary(successCount: 7, failureCount: 3);
            progressManager.Dispose();
        }

        public static void Demo_ProgressPanel()
        {
            Console.WriteLine("\n=== 演示 2: 进度面板展示 ===\n");

            var imageCount = 8;
            var progressManager = new ProgressBarManager(imageCount);

            for (int i = 0; i < imageCount; i++)
            {
                Thread.Sleep(700); // 模拟处理耗时

                var fileName = $"photo_{i + 1:D3}.jpg";
                progressManager.UpdateProgress(fileName, success: true, fileSize: 2048 * 1024);

                // 每 2 个文件显示一次面板
                if ((i + 1) % 2 == 0)
                {
                    Console.WriteLine();
                    progressManager.ShowProgressPanel(currentFile: fileName);
                    Console.WriteLine();
                }
            }

            progressManager.ShowSummary(successCount: 8, failureCount: 0);
            progressManager.Dispose();
        }

        public static void Demo_FileDetails()
        {
            Console.WriteLine("\n=== 演示 3: 文件处理详情 ===\n");

            var progressManager = new ProgressBarManager(5);

            var details = new[]
            {
                (name: "photo1.png", source: 2048L * 1024, output: 512L * 1024, elapsed: 2500L, success: true),
                (name: "photo2.jpg", source: 3072L * 1024, output: 1024L * 1024, elapsed: 1800L, success: true),
                (name: "photo3.webp", source: 1024L * 1024, output: 256L * 1024, elapsed: 3200L, success: true),
                (name: "photo4.png", source: 4096L * 1024, output: 0L, elapsed: 5000L, success: false),
                (name: "photo5.jpg", source: 2560L * 1024, output: 768L * 1024, elapsed: 2100L, success: true),
            };

            foreach (var (name, source, output, elapsed, success) in details)
            {
                progressManager.ShowFileDetails(
                    fileName: name,
                    success: success,
                    sourceSize: source,
                    outputSize: output,
                    elapsedMs: elapsed
                );

                progressManager.UpdateProgress(
                    currentFile: name,
                    success: success,
                    fileSize: source
                );

                Thread.Sleep(300);
            }

            progressManager.ShowSummary(successCount: 4, failureCount: 1);
            progressManager.Dispose();
        }

        public static void Demo_Complete()
        {
            Console.WriteLine("\n=== 演示 4: 完整处理流程 ===\n");

            var imageCount = 15;
            var progressManager = new ProgressBarManager(imageCount);
            var successCount = 0;
            var failureCount = 0;
            var random = new Random();

            for (int i = 0; i < imageCount; i++)
            {
                progressManager.StartFileProcessing();
                Thread.Sleep(random.Next(300, 800)); // 模拟变长处理耗时
                var elapsedMs = progressManager.StopFileProcessing();

                var fileName = $"file_{i + 1:D3}.png";
                var sourceSize = 1024 * 1024 * (1 + (i % 3)); // 1-3MB
                var success = random.Next(0, 10) != 0; // 90% 成功率

                if (success)
                {
                    successCount++;
                    var outputSize = sourceSize / 2; // 假设压缩 50%
                    progressManager.ShowFileDetails(
                        fileName: fileName,
                        success: true,
                        sourceSize: sourceSize,
                        outputSize: outputSize,
                        elapsedMs: elapsedMs
                    );
                }
                else
                {
                    failureCount++;
                    progressManager.ShowFileDetails(
                        fileName: fileName,
                        success: false,
                        sourceSize: sourceSize,
                        outputSize: 0,
                        elapsedMs: elapsedMs
                    );
                }

                progressManager.UpdateProgress(
                    currentFile: fileName,
                    success: success,
                    fileSize: sourceSize
                );

                // 每 5 个文件显示面板
                if ((i + 1) % 5 == 0)
                {
                    Console.WriteLine();
                    progressManager.ShowProgressPanel(fileName);
                    Console.WriteLine();
                }
            }

            progressManager.ShowSummary(successCount: successCount, failureCount: failureCount);
            progressManager.Dispose();
        }

        /// <summary>
        /// 运行所有演示。
        /// 使用方式：new ProgressBarDemo().RunAllDemos();
        /// </summary>
        public static void RunAllDemos()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   进度条管理器 (ProgressBarManager) 功能演示                ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

            try
            {
                Demo_BasicProgress();
                Console.WriteLine("\n按任意键继续...\n");
                Console.ReadKey();

                Demo_FileDetails();
                Console.WriteLine("\n按任意键继续...\n");
                Console.ReadKey();

                Demo_ProgressPanel();
                Console.WriteLine("\n按任意键继续...\n");
                Console.ReadKey();

                Demo_Complete();

                Console.WriteLine("\n\n✓ 所有演示完成！");
            }
            catch (Exception ex)
            {
                ProgressBarManager.ShowError($"演示过程中出错: {ex.Message}");
            }
        }
    }
}
