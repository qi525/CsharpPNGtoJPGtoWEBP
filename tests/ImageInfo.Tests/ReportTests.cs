using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using ImageInfo.Models;
using ImageInfo.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ImageInfo.Tests
{
    /// <summary>
    /// 单元测试：验证 ReportService 能将转换行写入 XLSX，并且工作表与行数大致匹配。
    /// 同时此测试也会调用转换器以生成样例源/目标文件。
    /// </summary>
    public class ReportTests : IDisposable
    {
        private readonly string _tempDir;

        public ReportTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "imageinfo_report_tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void WriteConversionReport_CreatesXlsx()
        {
            // Prepare sample image and conversions
            var src = Path.Combine(_tempDir, "sample.png");
            using (var img = new Image<Rgba32>(80, 60)) img.Save(src);

            var jpg = ImageConverter.ConvertPngToJpeg(src, Path.Combine(_tempDir, "sample_out.jpg"), 85);
            var webp = ImageConverter.ConvertPngToWebP(src, Path.Combine(_tempDir, "sample_out.webp"), 80);

            var rows = new List<ConversionReportRow>();

            void AddRow(string s, string d)
            {
                var row = new ConversionReportRow { SourcePath = s, DestPath = d };
                try
                {
                    using var sImg = Image.Load(s);
                    row.SourceWidth = sImg.Width; row.SourceHeight = sImg.Height; row.SourceFormat = Path.GetExtension(s).TrimStart('.');
                }
                catch { }

                try
                {
                    using var dImg = Image.Load(d);
                    row.DestWidth = dImg.Width; row.DestHeight = dImg.Height; row.DestFormat = Path.GetExtension(d).TrimStart('.');
                }
                catch { }

                row.Success = ValidationService.ValidateConversion(s, d);
                rows.Add(row);
            }

            AddRow(src, jpg);
            AddRow(src, webp);

            var outXlsx = Path.Combine(_tempDir, "report.xlsx");
            ReportService.WriteConversionReport(rows, outXlsx);

            // 文件名现在包含时间戳后缀，例如 report-20251116-143022.xlsx
            var reportFiles = Directory.GetFiles(_tempDir, "report-*.xlsx");
            Assert.NotEmpty(reportFiles);
            var actualXlsx = reportFiles[0];

            Assert.True(File.Exists(actualXlsx));

            // Basic content check using ClosedXML
            using var wb = new XLWorkbook(actualXlsx);
            var ws = wb.Worksheet("Conversions");
            Assert.NotNull(ws);
            var used = ws.RangeUsed();
            Assert.True(used.RowCount() >= rows.Count + 1);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
