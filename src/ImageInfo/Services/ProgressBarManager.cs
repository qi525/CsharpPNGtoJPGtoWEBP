using System;
using System.Diagnostics;
using Spectre.Console;

namespace ImageInfo.Services
{
    /// <summary>
    /// 进度条管理器，使用 Spectre.Console 库提供丰富的进度显示。
    /// 包括实时进度、运行时间、剩余时间、处理速度等信息。
    /// 
    /// 功能特性：
    /// - 📊 实时进度百分比显示
    /// - ⏱️ 已运行时间跟踪
    /// - ⏳ 剩余时间估算
    /// - 📈 文件处理速度计算（文件/秒）
    /// - 📉 数据大小处理速度（MB/秒）
    /// - 🎯 压缩率统计
    /// - ✅ 成功/失败统计
    /// </summary>
    public class ProgressBarManager : IDisposable
    {
        private readonly Stopwatch _totalStopwatch;
        private int _totalFiles;
        private int _processedFiles;
        private int _successCount;
        private int _failureCount;
        private long _totalProcessedBytes;
        private Stopwatch? _itemStopwatch;

        /// <summary>
        /// 初始化进度条管理器。
        /// </summary>
        /// <param name="totalFiles">总文件数</param>
        public ProgressBarManager(int totalFiles)
        {
            _totalFiles = totalFiles;
            _processedFiles = 0;
            _successCount = 0;
            _failureCount = 0;
            _totalProcessedBytes = 0;
            _totalStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// 更新进度（适用于单个文件）。
        /// </summary>
        /// <param name="currentFile">当前文件名</param>
        /// <param name="success">是否成功处理</param>
        /// <param name="fileSize">文件大小（字节）</param>
        public void UpdateProgress(string currentFile, bool success = true, long fileSize = 0)
        {
            _itemStopwatch?.Stop();
            _itemStopwatch = Stopwatch.StartNew();

            _processedFiles++;
            if (success)
                _successCount++;
            else
                _failureCount++;

            if (fileSize > 0)
                _totalProcessedBytes += fileSize;

            var percentage = (int)((_processedFiles * 100) / _totalFiles);

            var elapsed = _totalStopwatch.Elapsed;
            var estimated = EstimateRemainingTime();
            var fileSpeed = CalculateFileProcessingSpeed();
            var dataSpeed = CalculateDataProcessingSpeed();

            // 构建进度条信息行
            AnsiConsole.MarkupLine(
                $"[bold green]进度:[/] [yellow]{percentage:D3}%[/] " +
                $"({_processedFiles}/{_totalFiles}) | " +
                $"✓ {_successCount} ✗ {_failureCount} | " +
                $"[blue]耗时:[/] {FormatTime(elapsed)} | " +
                $"[magenta]剩余:[/] {FormatTime(estimated)} | " +
                $"[cyan]速度:[/] {fileSpeed:F2} 文件/秒 ({dataSpeed:F2} MB/秒)"
            );
        }

        /// <summary>
        /// 开始文件处理（用于计时单个文件）。
        /// </summary>
        public void StartFileProcessing()
        {
            _itemStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// 结束文件处理，返回处理耗时（毫秒）。
        /// </summary>
        public long StopFileProcessing()
        {
            _itemStopwatch?.Stop();
            return _itemStopwatch?.ElapsedMilliseconds ?? 0;
        }

        /// <summary>
        /// 显示批量处理完成汇总（进度条、速度统计等）。
        /// </summary>
        /// <param name="successCount">成功转换文件数</param>
        /// <param name="failureCount">失败文件数</param>
        public void ShowSummary(int successCount, int failureCount)
        {
            _totalStopwatch.Stop();

            var elapsed = _totalStopwatch.Elapsed;
            var avgFileSpeed = elapsed.TotalSeconds > 0 ? _totalFiles / elapsed.TotalSeconds : 0;
            var avgDataSpeed = elapsed.TotalSeconds > 0 ? (_totalProcessedBytes / 1024.0 / 1024.0) / elapsed.TotalSeconds : 0;
            var successRate = _totalFiles > 0 ? (successCount * 100.0 / _totalFiles) : 0;

            var table = new Table();
            table.AddColumn(new TableColumn("[bold]项目[/]").Centered());
            table.AddColumn(new TableColumn("[bold]值[/]").Centered());

            table.AddRow("[yellow]总文件数[/]", $"[cyan]{_totalFiles}[/]");
            table.AddRow("[green]成功数[/]", $"[lime]{successCount}[/]");
            table.AddRow("[red]失败数[/]", $"[red]{failureCount}[/]");
            table.AddRow("[magenta]成功率[/]", $"[yellow]{successRate:F1}%[/]");

            table.AddRow("[blue]总耗时[/]", $"[cyan]{FormatTime(elapsed)}[/]");
            table.AddRow("[blue]已运行时间[/]", $"[cyan]{FormatTime(elapsed)}[/]");
            table.AddRow("[cyan]平均文件速度[/]", $"[yellow]{avgFileSpeed:F2} 文件/秒[/]");
            table.AddRow("[cyan]平均数据速度[/]", $"[yellow]{avgDataSpeed:F2} MB/秒[/]");
            table.AddRow("[magenta]总处理数据量[/]", $"[yellow]{FormatBytes(_totalProcessedBytes)}[/]");

            AnsiConsole.Write(new Panel(table)
            {
                Header = new PanelHeader("[bold green]✓ 处理完成[/]"),
                Border = BoxBorder.Double,
                BorderStyle = new Style(Color.Green)
            });
        }

        /// <summary>
        /// 显示单个文件转换详情（带进度条和颜色）。
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="success">是否成功</param>
        /// <param name="sourceSize">源文件大小（字节）</param>
        /// <param name="outputSize">输出文件大小（字节）</param>
        /// <param name="elapsedMs">处理耗时（毫秒）</param>
        public void ShowFileDetails(string fileName, bool success, long sourceSize, long outputSize, long elapsedMs)
        {
            var compressionRate = sourceSize > 0 ? (1 - (double)outputSize / sourceSize) * 100 : 0;
            var speedMbps = elapsedMs > 0 ? (sourceSize / 1024.0 / 1024.0) / (elapsedMs / 1000.0) : 0;

            if (success)
            {
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] {fileName} | " +
                    $"[cyan]{FormatBytes(sourceSize)}[/] → [yellow]{FormatBytes(outputSize)}[/] | " +
                    $"[magenta]{compressionRate:F1}% 压缩[/] | " +
                    $"[blue]{elapsedMs}ms[/] ({speedMbps:F2} MB/s)"
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]✗[/] {fileName} | [red]转换失败[/]"
                );
            }
        }

        /// <summary>
        /// 显示带百分比的进度条（Spectre 风格），支持实时显示运行时间、剩余时间和处理速度。
        /// </summary>
        /// <param name="updateCallback">每次更新时的回调函数，接收当前 Spectre ProgressTask 参数</param>
        public void ShowSpectreProgressBar(Action<ProgressTask>? updateCallback = null)
        {
            AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(),
                })
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]处理中[/]", maxValue: _totalFiles);

                    for (int i = 0; i < _totalFiles; i++)
                    {
                        System.Threading.Thread.Sleep(100); // 模拟处理
                        task.Increment(1);
                        updateCallback?.Invoke(task);
                    }
                });
        }

        /// <summary>
        /// 显示实时进度面板（包含进度条、运行时间、剩余时间、处理速度）。
        /// 适用于长时间运行的批处理任务。
        /// </summary>
        /// <param name="currentFile">当前处理的文件名</param>
        public void ShowProgressPanel(string currentFile = "处理中...")
        {
            var percentage = _totalFiles > 0 ? (int)((_processedFiles * 100) / _totalFiles) : 0;
            var elapsed = _totalStopwatch.Elapsed;
            var estimated = EstimateRemainingTime();
            var fileSpeed = CalculateFileProcessingSpeed();
            var dataSpeed = CalculateDataProcessingSpeed();

            var grid = new Grid();
            grid.AddColumn(new GridColumn().Padding(1, 0));
            grid.AddColumn(new GridColumn().Padding(1, 0));

            // 左列：进度信息
            var progressText = new Text(
                $"{percentage:D3}%\n" +
                $"{_processedFiles}/{_totalFiles}\n" +
                $"✓{_successCount} ✗{_failureCount}",
                new Style(Color.Yellow)
            );
            progressText.Centered();

            var leftPanel = new Panel(progressText)
            {
                Header = new PanelHeader("[bold green]进度[/]"),
                Border = BoxBorder.Rounded
            };

            // 右列：时间和速度信息
            var rightPanel = new Panel(
                new Text(
                    $"📊 {currentFile}\n\n" +
                    $"⏱️  耗时: [cyan]{FormatTime(elapsed)}[/]\n" +
                    $"⏳ 剩余: [magenta]{FormatTime(estimated)}[/]\n" +
                    $"📈 速度: [yellow]{fileSpeed:F2}[/] 文件/秒\n" +
                    $"📉 数据: [yellow]{dataSpeed:F2}[/] MB/秒"
                )
            )
            {
                Header = new PanelHeader("[bold blue]时间统计[/]"),
                Border = BoxBorder.Rounded
            };

            grid.AddRow(leftPanel, rightPanel);
            AnsiConsole.Write(grid);
        }

        /// <summary>
        /// 估算剩余时间。
        /// </summary>
        private TimeSpan EstimateRemainingTime()
        {
            if (_processedFiles == 0) return TimeSpan.Zero;

            var avgTimePerFile = _totalStopwatch.Elapsed.TotalSeconds / _processedFiles;
            var remainingFiles = _totalFiles - _processedFiles;
            var remainingSeconds = avgTimePerFile * remainingFiles;

            return TimeSpan.FromSeconds(remainingSeconds);
        }

        /// <summary>
        /// 计算当前文件处理速度（文件/秒）。
        /// </summary>
        private double CalculateFileProcessingSpeed()
        {
            var elapsedSeconds = _totalStopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds == 0) return 0;

            return _processedFiles / elapsedSeconds;
        }

        /// <summary>
        /// 计算当前数据处理速度（MB/秒）。
        /// </summary>
        private double CalculateDataProcessingSpeed()
        {
            var elapsedSeconds = _totalStopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds == 0 || _totalProcessedBytes == 0) return 0;

            return (_totalProcessedBytes / 1024.0 / 1024.0) / elapsedSeconds;
        }

        /// <summary>
        /// 格式化时间为 HH:MM:SS 格式。
        /// </summary>
        private static string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        /// <summary>
        /// 格式化字节大小为可读格式（KB、MB、GB）。
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        /// <summary>
        /// 显示错误消息（红色警告风格）。
        /// </summary>
        public static void ShowError(string message)
        {
            AnsiConsole.MarkupLine($"[red bold]✗ 错误:[/] {message}");
        }

        /// <summary>
        /// 显示警告信息（黄色风格）。
        /// </summary>
        public static void ShowWarning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow bold]⚠ 警告:[/] {message}");
        }

        /// <summary>
        /// 显示成功信息（绿色风格）。
        /// </summary>
        public static void ShowSuccess(string message)
        {
            AnsiConsole.MarkupLine($"[green bold]✓ 成功:[/] {message}");
        }

        /// <summary>
        /// 显示信息（蓝色风格）。
        /// </summary>
        public static void ShowInfo(string message)
        {
            AnsiConsole.MarkupLine($"[blue bold]ℹ 信息:[/] {message}");
        }

        public void Dispose()
        {
            _totalStopwatch.Stop();
            _itemStopwatch?.Stop();
        }
    }
}
