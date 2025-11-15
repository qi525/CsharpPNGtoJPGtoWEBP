using System;
using System.IO;
using ImageInfo.Services;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageInfo.Tests
{
    /// <summary>
    /// 单元测试：验证图片转换函数行为（主要生成转换后的文件）。
    /// 每个测试在临时目录中创建图片并在完成后清理。
    /// </summary>
    public class ConvertersTests : IDisposable
    {
        private readonly string _tempDir;

        public ConvertersTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "imageinfo_tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void PngToJpeg_CreatesFile()
        {
            var png = Path.Combine(_tempDir, "test.png");
            using (var img = new Image<Rgba32>(100, 100))
            {
                img.Save(png);
            }

            var outJpg = ImageConverter.ConvertPngToJpeg(png);
            Assert.True(File.Exists(outJpg));
        }

        [Fact]
        public void PngToWebp_CreatesFile()
        {
            var png = Path.Combine(_tempDir, "test2.png");
            using (var img = new Image<Rgba32>(50, 50))
            {
                img.Save(png);
            }

            var outWebp = ImageConverter.ConvertPngToWebP(png);
            Assert.True(File.Exists(outWebp));
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
