using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace ImageInfo.Services
{
    /// <summary>
    /// 转换进度回调信息。
    /// </summary>
    public class ConversionProgress
    {
        /// <summary>
        /// 当前处理的文件索引（0-based）。
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// 总文件数。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前处理的文件路径。
        /// </summary>
        public string CurrentFilePath { get; set; } = "";

        /// <summary>
        /// 完成进度百分比（0-100）。
        /// </summary>
        public int ProgressPercentage => TotalCount > 0 ? (int)((CurrentIndex + 1) * 100 / TotalCount) : 0;

        /// <summary>
        /// 已成功转换的文件数。
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 转换失败的文件数。
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 当前操作的状态消息。
        /// </summary>
        public string StatusMessage { get; set; } = "";

        /// <summary>
        /// 总耗时（秒）。
        /// </summary>
        public double ElapsedSeconds { get; set; }

        /// <summary>
        /// 输出友好的进度字符串。
        /// </summary>
        public override string ToString()
        {
            return $"[{ProgressPercentage:D3}%] {CurrentIndex + 1}/{TotalCount} | " +
                   $"成功: {SuccessCount} | 失败: {FailureCount} | {StatusMessage}";
        }
    }

    /// <summary>
    /// 转换结果信息。
    /// </summary>
    public class ConversionResult
    {
        /// <summary>
        /// 源文件路径。
        /// </summary>
        public string SourcePath { get; set; } = "";

        /// <summary>
        /// 输出文件路径。
        /// </summary>
        public string OutputPath { get; set; } = "";

        /// <summary>
        /// 是否转换成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（如果失败）。
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）。
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 源文件大小（字节）。
        /// </summary>
        public long SourceSize { get; set; }

        /// <summary>
        /// 输出文件大小（字节）。
        /// </summary>
        public long OutputSize { get; set; }

        /// <summary>
        /// 压缩率百分比。
        /// </summary>
        public double CompressionRate => SourceSize > 0 ? (1 - (double)OutputSize / SourceSize) * 100 : 0;
    }

    /// <summary>
    /// 批量图片转换器，支持多线程和进度报告。
    /// 专门用于批量处理 PNG/JPEG 到 WebP 的转换。
    /// </summary>
    public static class BatchImageConverter
    {
        /// <summary>
        /// 异步批量转换 PNG 文件为 WebP（支持多线程和进度报告）。
        /// </summary>
        /// <param name="pngPaths">PNG 文件路径列表</param>
        /// <param name="outputDirectory">输出目录（可选，默认同源目录）</param>
        /// <param name="quality">WebP 压缩质量（1-100，默认80）</param>
        /// <param name="maxDegreeOfParallelism">最大并发数（默认逻辑核心数）</param>
        /// <param name="progressCallback">进度回调（可选）</param>
        /// <param name="cancellationToken">取消令牌（可选）</param>
        /// <returns>转换结果列表</returns>
        public static async Task<List<ConversionResult>> ConvertPngToWebPBatchAsync(
            IEnumerable<string> pngPaths,
            string? outputDirectory = null,
            int quality = 80,
            int maxDegreeOfParallelism = 0,
            IProgress<ConversionProgress>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = Environment.ProcessorCount;

            var paths = pngPaths.ToList();
            var results = new List<ConversionResult>();
            var lockObj = new object();

            var progress = new ConversionProgress { TotalCount = paths.Count };
            var startTime = DateTime.UtcNow;

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
                            var outPath = string.IsNullOrEmpty(outputDirectory)
                                ? Path.ChangeExtension(pngPath, ".webp")
                                : Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(pngPath) + ".webp");

                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            var result = ConvertPngToWebPInternal(pngPath, outPath, quality);
                            sw.Stop();

                            lock (lockObj)
                            {
                                results.Add(result);
                                progress.CurrentIndex = results.Count - 1;
                                progress.CurrentFilePath = pngPath;
                                progress.SuccessCount = results.Count(r => r.Success);
                                progress.FailureCount = results.Count(r => !r.Success);
                                progress.ElapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                                progress.StatusMessage = result.Success
                                    ? $"转换成功 ({result.CompressionRate:F1}% 压缩)"
                                    : $"转换失败: {result.ErrorMessage}";

                                progressCallback?.Report(progress);
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
                                progress.CurrentIndex = results.Count - 1;
                                progress.CurrentFilePath = pngPath;
                                progress.FailureCount = results.Count(r => !r.Success);
                                progress.StatusMessage = $"异常: {ex.Message}";
                                progressCallback?.Report(progress);
                            }
                        }
                    });
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                progress.StatusMessage = "用户取消了操作";
                progressCallback?.Report(progress);
            }

            return results;
        }

        /// <summary>
        /// 异步批量转换 JPEG 文件为 WebP（支持多线程和进度报告）。
        /// </summary>
        /// <param name="jpegPaths">JPEG 文件路径列表</param>
        /// <param name="outputDirectory">输出目录（可选，默认同源目录）</param>
        /// <param name="quality">WebP 压缩质量（1-100，默认80）</param>
        /// <param name="maxDegreeOfParallelism">最大并发数（默认逻辑核心数）</param>
        /// <param name="progressCallback">进度回调（可选）</param>
        /// <param name="cancellationToken">取消令牌（可选）</param>
        /// <returns>转换结果列表</returns>
        public static async Task<List<ConversionResult>> ConvertJpegToWebPBatchAsync(
            IEnumerable<string> jpegPaths,
            string? outputDirectory = null,
            int quality = 80,
            int maxDegreeOfParallelism = 0,
            IProgress<ConversionProgress>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = Environment.ProcessorCount;

            var paths = jpegPaths.ToList();
            var results = new List<ConversionResult>();
            var lockObj = new object();

            var progress = new ConversionProgress { TotalCount = paths.Count };
            var startTime = DateTime.UtcNow;

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
                            var outPath = string.IsNullOrEmpty(outputDirectory)
                                ? Path.ChangeExtension(jpegPath, ".webp")
                                : Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(jpegPath) + ".webp");

                            var result = ConvertJpegToWebPInternal(jpegPath, outPath, quality);

                            lock (lockObj)
                            {
                                results.Add(result);
                                progress.CurrentIndex = results.Count - 1;
                                progress.CurrentFilePath = jpegPath;
                                progress.SuccessCount = results.Count(r => r.Success);
                                progress.FailureCount = results.Count(r => !r.Success);
                                progress.ElapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                                progress.StatusMessage = result.Success
                                    ? $"转换成功 ({result.CompressionRate:F1}% 压缩)"
                                    : $"转换失败: {result.ErrorMessage}";

                                progressCallback?.Report(progress);
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
                                progress.CurrentIndex = results.Count - 1;
                                progress.CurrentFilePath = jpegPath;
                                progress.FailureCount = results.Count(r => !r.Success);
                                progress.StatusMessage = $"异常: {ex.Message}";
                                progressCallback?.Report(progress);
                            }
                        }
                    });
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                progress.StatusMessage = "用户取消了操作";
                progressCallback?.Report(progress);
            }

            return results;
        }

        #region 内部辅助方法

        /// <summary>
        /// PNG 转 WebP 的内部实现（带文件大小统计）。
        /// </summary>
        private static ConversionResult ConvertPngToWebPInternal(string pngPath, string outPath, int quality)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sourceInfo = new FileInfo(pngPath);

            try
            {
                using var image = new MagickImage(pngPath);
                image.Format = MagickFormat.WebP;
                image.Quality = (uint)quality;
                image.Write(outPath);

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
        /// JPEG 转 WebP 的内部实现（带文件大小统计）。
        /// </summary>
        private static ConversionResult ConvertJpegToWebPInternal(string jpegPath, string outPath, int quality)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sourceInfo = new FileInfo(jpegPath);

            try
            {
                using var image = new MagickImage(jpegPath);
                image.Format = MagickFormat.WebP;
                image.Quality = (uint)quality;
                image.Write(outPath);

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

        #endregion
    }
}

