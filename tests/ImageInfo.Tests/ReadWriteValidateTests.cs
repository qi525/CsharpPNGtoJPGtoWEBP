using System;
using System.IO;
using ImageInfo.Services;
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

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
