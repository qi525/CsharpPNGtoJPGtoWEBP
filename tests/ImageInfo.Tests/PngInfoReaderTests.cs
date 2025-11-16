using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using ImageInfo.Services;

namespace ImageInfo.Tests
{
    /// <summary>
    /// PngInfoReader 的单元测试。
    /// 测试读取 PNG 图片的各种信息（尺寸、颜色空间、元数据等）。
    /// </summary>
    public class PngInfoReaderTests
    {
        private readonly string _testOutputDir = Path.Combine(Path.GetTempPath(), "PngInfoReaderTests");

        public PngInfoReaderTests()
        {
            Directory.CreateDirectory(_testOutputDir);
        }

        /// <summary>
        /// 测试：读取简单 PNG 文件的基本信息。
        /// </summary>
        [Fact]
        public void ReadPngInfo_SimpleImage_ReturnsCorrectDimensions()
        {
            // Arrange
            string testFile = CreateSimplePng(100, 200);

            try
            {
                // Act
                var pngInfo = PngInfoReader.ReadPngInfo(testFile);

                // Assert
                Assert.NotNull(pngInfo);
                Assert.Equal(100, pngInfo.Width);
                Assert.Equal(200, pngInfo.Height);
                Assert.NotEmpty(pngInfo.ColorType);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：读取 PNG 文件的像素格式。
        /// </summary>
        [Fact]
        public void ReadPngInfo_Image_ReturnsPixelFormat()
        {
            // Arrange
            string testFile = CreateSimplePng(64, 64);

            try
            {
                // Act
                var pngInfo = PngInfoReader.ReadPngInfo(testFile);

                // Assert
                Assert.NotNull(pngInfo);
                Assert.NotEmpty(pngInfo.PixelFormat);
                Assert.Contains("32", pngInfo.PixelFormat); // Rgba32
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：读取 PNG 文件的文本元数据（tEXt 块）。
        /// </summary>
        [Fact]
        public void ReadPngInfo_ImageWithTextMetadata_ExtractTextData()
        {
            // Arrange
            string testFile = CreatePngWithTextMetadata(
                new Dictionary<string, string>
                {
                    { "prompt", "a beautiful landscape" },
                    { "model", "Stable Diffusion v1.5" },
                    { "seed", "12345" }
                }
            );

            try
            {
                // Act
                var pngInfo = PngInfoReader.ReadPngInfo(testFile);

                // Assert
                Assert.NotNull(pngInfo);
                Assert.NotNull(pngInfo.TextMetadata);
                Assert.Equal(3, pngInfo.TextMetadata.Count);
                Assert.Equal("a beautiful landscape", pngInfo.TextMetadata["prompt"]);
                Assert.Equal("Stable Diffusion v1.5", pngInfo.TextMetadata["model"]);
                Assert.Equal("12345", pngInfo.TextMetadata["seed"]);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：读取 PNG 文本元数据（专用方法）。
        /// </summary>
        [Fact]
        public void ReadPngTextMetadata_ImageWithText_ReturnsMetadata()
        {
            // Arrange
            string testFile = CreatePngWithTextMetadata(
                new Dictionary<string, string>
                {
                    { "Author", "Test Author" },
                    { "Description", "Test Description" }
                }
            );

            try
            {
                // Act
                var textMetadata = PngInfoReader.ReadPngTextMetadata(testFile);

                // Assert
                Assert.NotNull(textMetadata);
                Assert.Equal(2, textMetadata.Count);
                Assert.Equal("Test Author", textMetadata["Author"]);
                Assert.Equal("Test Description", textMetadata["Description"]);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：读取不存在的文件应返回 null。
        /// </summary>
        [Fact]
        public void ReadPngInfo_NonExistentFile_ReturnsNull()
        {
            // Act
            var pngInfo = PngInfoReader.ReadPngInfo("/nonexistent/path/image.png");

            // Assert
            Assert.Null(pngInfo);
        }

        /// <summary>
        /// 测试：获取基本图片信息。
        /// </summary>
        [Fact]
        public void GetBasicImageInfo_ValidImage_ReturnsCorrectInfo()
        {
            // Arrange
            string testFile = CreateSimplePng(320, 240);

            try
            {
                // Act
                var basicInfo = PngInfoReader.GetBasicImageInfo(testFile);

                // Assert
                Assert.NotNull(basicInfo);
                Assert.Equal(320, basicInfo.Value.Width);
                Assert.Equal(240, basicInfo.Value.Height);
                Assert.NotEmpty(basicInfo.Value.Format);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：检查透明度（无透明像素的图片）。
        /// </summary>
        [Fact]
        public void HasTransparency_OpaqueImage_ReturnsFalse()
        {
            // Arrange
            string testFile = CreateOpaqueImage(100, 100);

            try
            {
                // Act
                var hasTransparency = PngInfoReader.HasTransparency(testFile);

                // Assert
                Assert.NotNull(hasTransparency);
                Assert.False(hasTransparency.Value);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：检查透明度（有透明像素的图片）。
        /// </summary>
        [Fact]
        public void HasTransparency_TransparentImage_ReturnsTrue()
        {
            // Arrange
            string testFile = CreateTransparentImage(100, 100);

            try
            {
                // Act
                var hasTransparency = PngInfoReader.HasTransparency(testFile);

                // Assert
                Assert.NotNull(hasTransparency);
                Assert.True(hasTransparency.Value);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：PngInfo 对象的 ToString 方法。
        /// </summary>
        [Fact]
        public void PngInfo_ToString_ContainsExpectedInfo()
        {
            // Arrange
            string testFile = CreateSimplePng(640, 480);

            try
            {
                // Act
                var pngInfo = PngInfoReader.ReadPngInfo(testFile);
                var summary = pngInfo?.ToString();

                // Assert
                Assert.NotNull(summary);
                Assert.Contains("PNG Information", summary);
                Assert.Contains("640x480", summary);
                Assert.Contains("Dimensions", summary);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：PngInfo 对象的 ToJsonObject 方法。
        /// </summary>
        [Fact]
        public void PngInfo_ToJsonObject_ReturnsValidDictionary()
        {
            // Arrange
            string testFile = CreateSimplePng(100, 100);

            try
            {
                // Act
                var pngInfo = PngInfoReader.ReadPngInfo(testFile);
                var jsonObject = pngInfo?.ToJsonObject();

                // Assert
                Assert.NotNull(jsonObject);
                Assert.Contains("FilePath", jsonObject.Keys);
                Assert.Contains("Dimensions", jsonObject.Keys);
                Assert.Contains("PixelFormat", jsonObject.Keys);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        /// <summary>
        /// 测试：读取多个 PNG 文件。
        /// </summary>
        [Fact]
        public void ReadPngInfo_MultipleDifferentImages_AllReturnCorrectInfo()
        {
            // Arrange
            string file1 = CreateSimplePng(100, 100);
            string file2 = CreateSimplePng(200, 150);
            string file3 = CreateSimplePng(300, 200);

            try
            {
                // Act
                var info1 = PngInfoReader.ReadPngInfo(file1);
                var info2 = PngInfoReader.ReadPngInfo(file2);
                var info3 = PngInfoReader.ReadPngInfo(file3);

                // Assert
                Assert.NotNull(info1);
                Assert.NotNull(info2);
                Assert.NotNull(info3);
                Assert.Equal(100, info1.Width);
                Assert.Equal(200, info2.Width);
                Assert.Equal(300, info3.Width);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
                File.Delete(file3);
            }
        }

        // ==================== 辅助方法 ====================

        /// <summary>
        /// 创建一个简单的 PNG 文件。
        /// </summary>
        private string CreateSimplePng(int width, int height)
        {
            string filePath = Path.Combine(_testOutputDir, $"test_{width}x{height}_{Guid.NewGuid()}.png");
            using (var image = new Image<Rgba32>(width, height))
            {
                image.SaveAsPng(filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 创建一个包含文本元数据的 PNG 文件。
        /// </summary>
        private string CreatePngWithTextMetadata(Dictionary<string, string> textData)
        {
            string filePath = Path.Combine(_testOutputDir, $"test_with_text_{Guid.NewGuid()}.png");
            using (var image = new Image<Rgba32>(100, 100))
            {
                var pngMetadata = image.Metadata.GetPngMetadata();
                if (pngMetadata != null)
                {
                    foreach (var (keyword, value) in textData)
                    {
                        pngMetadata.TextData.Add(new PngTextData(keyword, value, string.Empty, string.Empty));
                    }
                }
                image.SaveAsPng(filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 创建一个完全不透明的 PNG 图片。
        /// </summary>
        private string CreateOpaqueImage(int width, int height)
        {
            string filePath = Path.Combine(_testOutputDir, $"test_opaque_{Guid.NewGuid()}.png");
            using (var image = new Image<Rgba32>(width, height))
            {
                // 填充白色（完全不透明）
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        image[x, y] = new Rgba32(255, 255, 255, 255);
                    }
                }
                image.SaveAsPng(filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 创建一个包含透明像素的 PNG 图片。
        /// </summary>
        private string CreateTransparentImage(int width, int height)
        {
            string filePath = Path.Combine(_testOutputDir, $"test_transparent_{Guid.NewGuid()}.png");
            using (var image = new Image<Rgba32>(width, height))
            {
                // 左半部分不透明，右半部分透明
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (x < width / 2)
                            image[x, y] = new Rgba32(255, 0, 0, 255); // 红色，不透明
                        else
                            image[x, y] = new Rgba32(0, 0, 0, 0); // 黑色，完全透明
                    }
                }
                image.SaveAsPng(filePath);
            }
            return filePath;
        }
    }
}
