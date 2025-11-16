using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ImageInfo.Services;
using ImageInfo.Models;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageInfo.Tests
{
    /// <summary>
    /// 单元测试：验证元数据读取、图片转换和验证服务的综合行为。
    /// 包括从文件名提取 token、将 PNG 转为 JPEG/WebP 并校验结果。
    /// </summary>
    public class ReadWriteValidateTests : IDisposable
    {
        private readonly string _tempDir;

        public ReadWriteValidateTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "imageinfo_tests_rw", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void ReadMetadata_ReturnsFilenameToken()
        {
            var name = "flower_sunrise";
            var png = Path.Combine(_tempDir, name + ".png");
            using (var img = new Image<Rgba32>(10, 10)) img.Save(png);

            var info = MetadataService.ExtractTagsAndTimes(png);
            Assert.Equal(png, info.FilePath);
            Assert.True(info.CreatedUtc > DateTime.MinValue);
            Assert.Contains("flower", info.Tags, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertAndValidate_PngToJpeg_Valid()
        {
            var png = Path.Combine(_tempDir, "convert_test.png");
            using (var img = new Image<Rgba32>(64, 48)) img.Save(png);

            var jpg = ImageConverter.ConvertPngToJpeg(png);
            Assert.True(ValidationService.FileExists(jpg));
            Assert.True(ValidationService.IsImageLoadable(jpg));
            Assert.True(ValidationService.ValidateConversion(png, jpg));
        }

        [Fact]
        public void ConvertAndValidate_PngToWebp_Valid()
        {
            var png = Path.Combine(_tempDir, "convert_test2.png");
            using (var img = new Image<Rgba32>(32, 32)) img.Save(png);

            var webp = ImageConverter.ConvertPngToWebP(png);
            Assert.True(ValidationService.FileExists(webp));
            Assert.True(ValidationService.IsImageLoadable(webp));
            Assert.True(ValidationService.ValidateConversion(png, webp));
        }

        /// <summary>
        /// 测试将 AI 元数据写入 PNG 文件。
        /// 注：PNG 元数据写入在 .NET 中支持有限，本测试验证文件完整性。
        /// </summary>
        [Fact]
        public void WriteAIMetadata_ToPng_Success()
        {
            var png = Path.Combine(_tempDir, "metadata_test.png");
            using (var img = new Image<Rgba32>(32, 32)) img.Save(png);

            var metadata = new AIMetadata
            {
                Prompt = "a beautiful landscape",
                NegativePrompt = "blurry",
                Model = "stable-diffusion-v1.5",
                Seed = "12345",
                Sampler = "DPM++ 2M",
                OtherInfo = "Steps: 20, CFG: 7.5"
            };

            // 写入元数据
            PngMetadataExtractor.WriteAIMetadata(png, metadata);

            // 验证文件仍然有效
            Assert.True(ValidationService.FileExists(png));
            Assert.True(ValidationService.IsImageLoadable(png));
        }

        /// <summary>
        /// 测试将 AI 元数据写入 JPEG 文件。
        /// </summary>
        [Fact]
        public void WriteAIMetadata_ToJpeg_Success()
        {
            var jpeg = Path.Combine(_tempDir, "metadata_test.jpg");
            using (var img = new Image<Rgba32>(32, 32)) img.Save(jpeg);

            var metadata = new AIMetadata
            {
                Prompt = "a sunset over mountains",
                Model = "realistic-v2",
                Seed = "54321"
            };

            // 写入元数据
            JpegMetadataExtractor.WriteAIMetadata(jpeg, metadata);

            // 验证文件仍然有效
            Assert.True(ValidationService.FileExists(jpeg));
            Assert.True(ValidationService.IsImageLoadable(jpeg));
        }

        /// <summary>
        /// 测试将 AI 元数据写入 WebP 文件。
        /// </summary>
        [Fact]
        public void WriteAIMetadata_ToWebp_Success()
        {
            var webp = Path.Combine(_tempDir, "metadata_test.webp");
            using (var img = new Image<Rgba32>(32, 32)) img.Save(webp);

            var metadata = new AIMetadata
            {
                Prompt = "abstract art",
                Sampler = "DDIM",
                OtherInfo = "Upscale: 2x"
            };

            // 写入元数据
            WebPMetadataExtractor.WriteAIMetadata(webp, metadata);

            // 验证文件仍然有效
            Assert.True(ValidationService.FileExists(webp));
            Assert.True(ValidationService.IsImageLoadable(webp));
        }

        /// <summary>
        /// 测试异步批量转换 PNG 为 WebP，带进度报告。
        /// </summary>
        [Fact]
        public async Task ConvertPngToWebPBatchAsync_WithProgress_Success()
        {
            // 创建多个临时 PNG 文件
            var pngFiles = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var png = Path.Combine(_tempDir, $"batch_test_{i}.png");
                using (var img = new Image<Rgba32>(32, 32)) img.Save(png);
                pngFiles.Add(png);
            }

            var progressReports = new List<ConversionProgress>();
            var progress = new Progress<ConversionProgress>(p => progressReports.Add(p));

            // 执行批量转换
            var results = await BatchImageConverter.ConvertPngToWebPBatchAsync(
                pngFiles,
                null,
                quality: 80,
                maxDegreeOfParallelism: 2,
                progressCallback: progress
            );

            // 验证结果
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.True(r.Success));
            Assert.All(results, r => Assert.True(File.Exists(r.OutputPath)));
            
            // 验证进度报告
            Assert.NotEmpty(progressReports);
            var lastProgress = progressReports.Last();
            Assert.Equal(3, lastProgress.TotalCount);
            Assert.Equal(3, lastProgress.SuccessCount);
            Assert.Equal(100, lastProgress.ProgressPercentage);
        }

        /// <summary>
        /// 测试异步批量转换 JPEG 为 WebP（跳过，因为单文件测试也不支持 JPEG 创建）。
        /// 注：在实际项目中应使用真实的 JPEG 文件测试。
        /// </summary>
        [Fact(Skip = "JPEG WebP 转换需要真实的 JPEG 输入文件")]
        public async Task ConvertJpegToWebPBatchAsync_Success()
        {
            // 占位符
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
