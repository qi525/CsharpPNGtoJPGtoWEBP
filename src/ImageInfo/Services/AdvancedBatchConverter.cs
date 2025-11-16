using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace ImageInfo.Services
{
    /// <summary>
    /// 增强的批量图片转换器，使用 ProgressBarManager 提供丰富的进度显示。
    /// 支持显示运行时间、剩余时间、处理速度等。
    /// </summary>
    public static class AdvancedBatchConverter
    {
        /// <summary>
        /// 异步批量转换 PNG 文件为 WebP，带详细进度显示。
        /// </summary>
        /// <param name="pngPaths">PNG 文件路径列表</param>
        /// <param name="outputDirectory">输出目录（可选，默认同源目录）</param>
        /// <param name="quality">WebP 压缩质量（1-100，默认80）</param>
        /// <param name="maxDegreeOfParallelism">最大并发数（默认逻辑核心数）</param>
        /// <param name="showProgress">是否显示详细进度（默认 true）</param>
        /// <param name="cancellationToken">取消令牌（可选）</param>
        /// <returns>转换结果列表</returns>
        public static async Task<List<ConversionResult>> ConvertPngToWebPWithProgressAsync(
            IEnumerable<string> pngPaths,
            string? outputDirectory = null,
            int quality = 80,
            int maxDegreeOfParallelism = 0,
            bool showProgress = true,
            CancellationToken cancellationToken = default)
        {
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = Environment.ProcessorCount;

            var paths = pngPaths.ToList();
            if (paths.Count == 0)
            {
                if (showProgress) ProgressBarManager.ShowWarning("未找到任何 PNG 文件");
                return new List<ConversionResult>();
            }

            var results = new List<ConversionResult>();
            var lockObj = new object();

            using (var progressMgr = new ProgressBarManager(paths.Count))
            {
                if (showProgress)
                {
                    ProgressBarManager.ShowInfo($"开始批量转换 PNG → WebP，共 {paths.Count} 个文件，并发数: {maxDegreeOfParallelism}");
                }

                try
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(paths, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = maxDegreeOfParallelism,
                            CancellationToken = cancellationToken
                        }, (pngPath, state, index) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                state.Stop();

                            try
                            {
                                progressMgr.StartFileProcessing();

                                var outPath = string.IsNullOrEmpty(outputDirectory)
                                    ? Path.ChangeExtension(pngPath, ".webp")
                                    : Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(pngPath) + ".webp");

                                var result = ConvertPngToWebPInternal(pngPath, outPath, quality);
                                var elapsed = progressMgr.StopFileProcessing();

                                lock (lockObj)
                                {
                                    results.Add(result);
                                    if (showProgress)
                                    {
                                        progressMgr.UpdateProgress(Path.GetFileName(pngPath));
                                        if (result.Success)
                                        {
                                            progressMgr.ShowFileDetails(
                                                Path.GetFileName(pngPath),
                                                true,
                                                result.SourceSize,
                                                result.OutputSize,
                                                elapsed
                                            );
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var result = new ConversionResult
                                {
                                    SourcePath = pngPath,
                                    Success = false,
                                    ErrorMessage = ex.Message
                                };

                                lock (lockObj)
                                {
                                    results.Add(result);
                                    if (showProgress)
                                    {
                                        progressMgr.UpdateProgress(Path.GetFileName(pngPath));
                                        ProgressBarManager.ShowError($"{Path.GetFileName(pngPath)}: {ex.Message}");
                                    }
                                }
                            }
                        });
                    }, cancellationToken);

                    // 显示完成汇总
                    if (showProgress)
                    {
                        var successCount = results.Count(r => r.Success);
                        var failureCount = results.Count(r => !r.Success);
                        progressMgr.ShowSummary(successCount, failureCount);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (showProgress)
                        ProgressBarManager.ShowWarning("用户取消了转换操作");
                }
            }

            return results;
        }

        /// <summary>
        /// 异步批量转换 JPEG 文件为 WebP，带详细进度显示。
        /// </summary>
        /// <param name="jpegPaths">JPEG 文件路径列表</param>
        /// <param name="outputDirectory">输出目录（可选，默认同源目录）</param>
        /// <param name="quality">WebP 压缩质量（1-100，默认80）</param>
        /// <param name="maxDegreeOfParallelism">最大并发数（默认逻辑核心数）</param>
        /// <param name="showProgress">是否显示详细进度（默认 true）</param>
        /// <param name="cancellationToken">取消令牌（可选）</param>
        /// <returns>转换结果列表</returns>
        public static async Task<List<ConversionResult>> ConvertJpegToWebPWithProgressAsync(
            IEnumerable<string> jpegPaths,
            string? outputDirectory = null,
            int quality = 80,
            int maxDegreeOfParallelism = 0,
            bool showProgress = true,
            CancellationToken cancellationToken = default)
        {
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = Environment.ProcessorCount;

            var paths = jpegPaths.ToList();
            if (paths.Count == 0)
            {
                if (showProgress) ProgressBarManager.ShowWarning("未找到任何 JPEG 文件");
                return new List<ConversionResult>();
            }

            var results = new List<ConversionResult>();
            var lockObj = new object();

            using (var progressMgr = new ProgressBarManager(paths.Count))
            {
                if (showProgress)
                {
                    ProgressBarManager.ShowInfo($"开始批量转换 JPEG → WebP，共 {paths.Count} 个文件，并发数: {maxDegreeOfParallelism}");
                }

                try
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(paths, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = maxDegreeOfParallelism,
                            CancellationToken = cancellationToken
                        }, (jpegPath, state, index) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                state.Stop();

                            try
                            {
                                progressMgr.StartFileProcessing();

                                var outPath = string.IsNullOrEmpty(outputDirectory)
                                    ? Path.ChangeExtension(jpegPath, ".webp")
                                    : Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(jpegPath) + ".webp");

                                var result = ConvertJpegToWebPInternal(jpegPath, outPath, quality);
                                var elapsed = progressMgr.StopFileProcessing();

                                lock (lockObj)
                                {
                                    results.Add(result);
                                    if (showProgress)
                                    {
                                        progressMgr.UpdateProgress(Path.GetFileName(jpegPath));
                                        if (result.Success)
                                        {
                                            progressMgr.ShowFileDetails(
                                                Path.GetFileName(jpegPath),
                                                true,
                                                result.SourceSize,
                                                result.OutputSize,
                                                elapsed
                                            );
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var result = new ConversionResult
                                {
                                    SourcePath = jpegPath,
                                    Success = false,
                                    ErrorMessage = ex.Message
                                };

                                lock (lockObj)
                                {
                                    results.Add(result);
                                    if (showProgress)
                                    {
                                        progressMgr.UpdateProgress(Path.GetFileName(jpegPath));
                                        ProgressBarManager.ShowError($"{Path.GetFileName(jpegPath)}: {ex.Message}");
                                    }
                                }
                            }
                        });
                    }, cancellationToken);

                    // 显示完成汇总
                    if (showProgress)
                    {
                        var successCount = results.Count(r => r.Success);
                        var failureCount = results.Count(r => !r.Success);
                        progressMgr.ShowSummary(successCount, failureCount);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (showProgress)
                        ProgressBarManager.ShowWarning("用户取消了转换操作");
                }
            }

            return results;
        }

        /// <summary>
        /// PNG 转 WebP 的内部实现。
        /// </summary>
        private static ConversionResult ConvertPngToWebPInternal(string pngPath, string outPath, int quality)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sourceInfo = new FileInfo(pngPath);

            try
            {
                using var inputBitmap = SKBitmap.Decode(pngPath);
                using var data = inputBitmap.Encode(SKEncodedImageFormat.Webp, quality);
                using var outFile = File.Create(outPath);
                data.SaveTo(outFile);

                sw.Stop();
                var outputInfo = new FileInfo(outPath);

                return new ConversionResult
                {
                    SourcePath = pngPath,
                    OutputPath = outPath,
                    Success = true,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    SourceSize = sourceInfo.Length,
                    OutputSize = outputInfo.Length
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ConversionResult
                {
                    SourcePath = pngPath,
                    OutputPath = outPath,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    SourceSize = sourceInfo.Length
                };
            }
        }

        /// <summary>
        /// JPEG 转 WebP 的内部实现。
        /// </summary>
        private static ConversionResult ConvertJpegToWebPInternal(string jpegPath, string outPath, int quality)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sourceInfo = new FileInfo(jpegPath);

            try
            {
                using var inputBitmap = SKBitmap.Decode(jpegPath);
                using var data = inputBitmap.Encode(SKEncodedImageFormat.Webp, quality);
                using var outFile = File.Create(outPath);
                data.SaveTo(outFile);

                sw.Stop();
                var outputInfo = new FileInfo(outPath);

                return new ConversionResult
                {
                    SourcePath = jpegPath,
                    OutputPath = outPath,
                    Success = true,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    SourceSize = sourceInfo.Length,
                    OutputSize = outputInfo.Length
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ConversionResult
                {
                    SourcePath = jpegPath,
                    OutputPath = outPath,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    SourceSize = sourceInfo.Length
                };
            }
        }
    }
}
