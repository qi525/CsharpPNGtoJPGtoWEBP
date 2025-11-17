using System;
using System.IO;
using Xunit;
using ImageInfo.Services;
using ImageInfo.Models;
using ImageMagick;

namespace ImageInfo.Tests
{
    /// <summary>
    /// 元数据写入测试套件
    /// 持续测试直到JPG/WebP的元数据写入功能成功实现
    /// </summary>
    public class MetadataWriteTests
    {
        private readonly string _testDir = Path.Combine(Path.GetTempPath(), "ImageInfoMetadataWriteTests");
        private readonly AIMetadata _testMetadata;

        public MetadataWriteTests()
        {
            // 创建测试目录
            if (!Directory.Exists(_testDir))
                Directory.CreateDirectory(_testDir);

            // 准备测试元数据
            _testMetadata = new AIMetadata
            {
                Prompt = "a beautiful landscape with mountains and sunset, 8k, ultra detailed",
                NegativePrompt = "blurry, low quality, distorted, ugly",
                Model = "sd-xl-1.0",
                Seed = "123456789",
                Sampler = "DPM++ 2M Karras",
                OtherInfo = "steps: 25, cfg_scale: 7.5",
                FullInfo = "Prompt: a beautiful landscape with mountains and sunset, 8k, ultra detailed\n" +
                          "Negative prompt: blurry, low quality, distorted, ugly\n" +
                          "Steps: 25, Sampler: DPM++ 2M Karras, CFG scale: 7.5, Seed: 123456789, Model: sd-xl-1.0, Size: 1024x1024",
                FullInfoExtractionMethod = "Test"
            };
        }

        /// <summary>
        /// 创建测试用的临时PNG文件
        /// </summary>
        private string CreateTestPngFile(string filename)
        {
            var filePath = Path.Combine(_testDir, filename);
            using (var image = new MagickImage(new MagickColor(100, 150, 200), 100, 100))
            {
                image.Format = MagickFormat.Png;
                image.Write(filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 从PNG创建JPEG转换（用于测试）
        /// </summary>
        private string ConvertPngToJpeg(string pngPath, string jpegFilename)
        {
            var jpegPath = Path.Combine(_testDir, jpegFilename);
            using (var image = new MagickImage(pngPath))
            {
                image.Format = MagickFormat.Jpeg;
                image.Quality = 95u;
                image.Write(jpegPath);
            }
            return jpegPath;
        }

        /// <summary>
        /// 从PNG创建WebP转换（用于测试）
        /// </summary>
        private string ConvertPngToWebP(string pngPath, string webpFilename)
        {
            var webpPath = Path.Combine(_testDir, webpFilename);
            using (var image = new MagickImage(pngPath))
            {
                image.Format = MagickFormat.WebP;
                image.Quality = 80u;
                image.Write(webpPath);
            }
            return webpPath;
        }

        /// <summary>
        /// 测试 1: JPEG 元数据写入
        /// 失败时提示实现进度
        /// </summary>
        [Fact]
        public void TestJpegMetadataWrite()
        {
            // Arrange
            var pngPath = CreateTestPngFile("test_source.png");
            var jpegPath = ConvertPngToJpeg(pngPath, "test_metadata_write.jpg");

            try
            {
                // Act - 写入元数据
                MetadataExtractors.WriteAIMetadata(jpegPath, _testMetadata);

                // Assert - 读回验证
                var readBack = MetadataExtractors.ReadAIMetadata(jpegPath);
                
                // 元数据读回应该包含写入的内容
                Assert.NotNull(readBack);
                Assert.NotEmpty(readBack.FullInfo ?? string.Empty);
                Assert.NotEmpty(readBack.Prompt ?? string.Empty);
                Assert.Equal(_testMetadata.Prompt, readBack.Prompt);
                Assert.Equal(_testMetadata.NegativePrompt, readBack.NegativePrompt);
                Assert.Equal(_testMetadata.Model, readBack.Model);

                // 成功标记
                Console.WriteLine("✓ JPEG 元数据写入测试 PASSED");
            }
            catch (AssertionException ex)
            {
                Console.WriteLine($"✗ JPEG 元数据写入测试 FAILED: {ex.Message}");
                Console.WriteLine("  当前状态: JPEG 元数据写入功能尚未实现");
                Console.WriteLine("  需要完成:");
                Console.WriteLine("    1. 使用 MetadataExtractor 库读取现有 EXIF");
                Console.WriteLine("    2. 修改 ImageDescription 或 UserComment 字段");
                Console.WriteLine("    3. 写回 JPEG 文件");
                throw;
            }
            finally
            {
                // Cleanup
                if (File.Exists(pngPath)) File.Delete(pngPath);
                if (File.Exists(jpegPath)) File.Delete(jpegPath);
            }
        }

        /// <summary>
        /// 测试 2: WebP 元数据写入
        /// </summary>
        [Fact]
        public void TestWebPMetadataWrite()
        {
            // Arrange
            var pngPath = CreateTestPngFile("test_source_webp.png");
            var webpPath = ConvertPngToWebP(pngPath, "test_metadata_write.webp");

            try
            {
                // Act - 写入元数据
                MetadataExtractors.WriteAIMetadata(webpPath, _testMetadata);

                // Assert - 读回验证
                var readBack = MetadataExtractors.ReadAIMetadata(webpPath);
                
                // 元数据读回应该包含写入的内容
                Assert.NotNull(readBack);
                Assert.NotEmpty(readBack.FullInfo ?? string.Empty);
                Assert.NotEmpty(readBack.Prompt ?? string.Empty);
                Assert.Equal(_testMetadata.Prompt, readBack.Prompt);

                Console.WriteLine("✓ WebP 元数据写入测试 PASSED");
            }
            catch (AssertionException ex)
            {
                Console.WriteLine($"✗ WebP 元数据写入测试 FAILED: {ex.Message}");
                Console.WriteLine("  当前状态: WebP 元数据写入功能尚未实现");
                Console.WriteLine("  需要完成:");
                Console.WriteLine("    1. 使用 MetadataExtractor 库读取现有 XMP");
                Console.WriteLine("    2. 创建或修改 XMP 数据");
                Console.WriteLine("    3. 写回 WebP 文件");
                throw;
            }
            finally
            {
                // Cleanup
                if (File.Exists(pngPath)) File.Delete(pngPath);
                if (File.Exists(webpPath)) File.Delete(webpPath);
            }
        }

        /// <summary>
        /// 测试 3: 元数据完整性验证
        /// 确保所有字段都被正确写入
        /// </summary>
        [Fact]
        public void TestMetadataCompleteness()
        {
            var pngPath = CreateTestPngFile("test_completeness_src.png");
            var jpegPath = ConvertPngToJpeg(pngPath, "test_completeness.jpg");

            try
            {
                MetadataExtractors.WriteAIMetadata(jpegPath, _testMetadata);
                var readBack = MetadataExtractors.ReadAIMetadata(jpegPath);

                // 验证所有字段
                Assert.NotEmpty(readBack.Prompt ?? string.Empty);
                Assert.NotEmpty(readBack.NegativePrompt ?? string.Empty);
                Assert.NotEmpty(readBack.Model ?? string.Empty);
                Assert.NotEmpty(readBack.Seed ?? string.Empty);
                Assert.NotEmpty(readBack.Sampler ?? string.Empty);

                Console.WriteLine("✓ 元数据完整性验证 PASSED");
            }
            catch (AssertionException ex)
            {
                Console.WriteLine($"✗ 元数据完整性验证 FAILED");
                Console.WriteLine($"  错误: {ex.Message}");
                throw;
            }
            finally
            {
                if (File.Exists(pngPath)) File.Delete(pngPath);
                if (File.Exists(jpegPath)) File.Delete(jpegPath);
            }
        }

        /// <summary>
        /// 测试 4: 转换后文件元数据读取
        /// 检查转换后的文件是否能正确读取元数据
        /// </summary>
        [Fact]
        public void TestConvertedFileMetadataRead()
        {
            var pngPath = CreateTestPngFile("test_read_src.png");
            var jpegPath = ConvertPngToJpeg(pngPath, "test_read.jpg");

            try
            {
                // 读取转换后文件
                var metadata = MetadataExtractors.ReadAIMetadata(jpegPath);
                
                // 如果文件没有元数据，这是正常的
                // 但读取应该不会出错
                Assert.NotNull(metadata);
                Console.WriteLine("✓ 转换后文件元数据读取 PASSED (无元数据或读取成功)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 转换后文件元数据读取 FAILED: {ex.Message}");
                throw;
            }
            finally
            {
                if (File.Exists(pngPath)) File.Delete(pngPath);
                if (File.Exists(jpegPath)) File.Delete(jpegPath);
            }
        }

        /// <summary>
        /// 性能测试：批量写入元数据
        /// </summary>
        [Fact]
        public void TestBatchMetadataWrite()
        {
            const int batchSize = 5;
            var createdFiles = new List<string>();

            try
            {
                // 创建多个测试文件
                for (int i = 0; i < batchSize; i++)
                {
                    var pngPath = CreateTestPngFile($"batch_src_{i}.png");
                    var jpegPath = ConvertPngToJpeg(pngPath, $"batch_test_{i}.jpg");
                    createdFiles.Add(jpegPath);

                    // 写入元数据
                    MetadataExtractors.WriteAIMetadata(jpegPath, _testMetadata);
                }

                // 验证所有文件
                foreach (var filePath in createdFiles)
                {
                    var metadata = MetadataExtractors.ReadAIMetadata(filePath);
                    Assert.NotNull(metadata);
                }

                Console.WriteLine($"✓ 批量写入 {batchSize} 个文件的元数据成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 批量写入失败: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup
                foreach (var file in createdFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                    Directory.Delete(_testDir, true);
            }
            catch { }
        }
    }
}
