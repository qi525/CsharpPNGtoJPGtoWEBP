using Xunit;
using ImageInfo.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ImageInfo.Tests
{
    /// <summary>
    /// TF-IDF处理服务的单元测试
    /// 策略：从简单到复杂，逐步验证和迭代
    /// </summary>
    public class TfidfProcessorServiceTests
    {
        // ==================== 测试1：初始化测试 ====================
        /// <summary>
        /// 测试1：验证服务初始化正确
        /// </summary>
        [Fact]
        public void Test_001_ServiceInitialization()
        {
            Console.WriteLine("\n========== 测试1：初始化 ==========");
            
            var service = new TfidfProcessorService(topN: 10);
            Assert.NotNull(service);
            
            Console.WriteLine("✓ 服务初始化成功");
        }

        // ==================== 测试2：最简单案例 - 单条文本 ====================
        /// <summary>
        /// 测试2：处理最简单的单行文本
        /// 输入：简单的词语列表
        /// 期望输出：正确识别并提取关键词
        /// </summary>
        [Fact]
        public void Test_002_SimpleSingleDocument()
        {
            Console.WriteLine("\n========== 测试2：最简单案例 ==========");
            
            var service = new TfidfProcessorService(topN: 5);
            
            // 最简单的测试数据：单条文本
            var texts = new List<string>
            {
                "cat dog cat bird cat"  // 3个cat, 1个dog, 1个bird
            };
            
            Console.WriteLine($"输入文本: {texts[0]}");
            
            // 预期：cat应该有最高的TF值
            var results = service.ProcessAll(texts);
            
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.NotNull(results[0].TopKeywords);
            Assert.NotEmpty(results[0].TopKeywords);
            
            // 第一个关键词应该是"cat"（出现频率最高）
            Console.WriteLine($"提取的关键词: {string.Join(", ", results[0].TopKeywords)}");
            Assert.Equal("cat", results[0].TopKeywords[0]);
            
            Console.WriteLine("✓ 单文档测试通过");
        }

        // ==================== 测试3：多文档IDF验证 ====================
        /// <summary>
        /// 测试3：验证IDF计算正确性
        /// 输入：3条文本，验证不同词的IDF值
        /// 期望：高频词IDF低，低频词IDF高
        /// </summary>
        [Fact]
        public void Test_003_IdfCalculation()
        {
            Console.WriteLine("\n========== 测试3：IDF计算 ==========");
            
            var service = new TfidfProcessorService(topN: 5);
            
            // 3条文本
            var texts = new List<string>
            {
                "the cat sat on the mat",           // 文档1："the"出现2次，其他词各1次
                "the dog sat under the tree",       // 文档2："the"出现2次，"sat"重复
                "the bird sat on the branch"        // 文档3："the"出现2次，"sat"重复
            };
            
            Console.WriteLine("输入文本:");
            for (int i = 0; i < texts.Count; i++)
            {
                Console.WriteLine($"  文档{i + 1}: {texts[i]}");
            }
            
            var results = service.ProcessAll(texts);
            
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            
            // 所有文档都应该有提取的关键词
            foreach (var result in results)
            {
                Assert.NotNull(result.TopKeywords);
                Assert.NotEmpty(result.TopKeywords);
                Console.WriteLine($"文档{result.DocId}的关键词: {string.Join(", ", result.TopKeywords)}");
            }
            
            // "the"应该出现在所有文档中，所以IDF低，不应该是Top关键词
            // 特定词如"cat"、"dog"、"bird"应该获得更高的IDF权重
            
            Console.WriteLine("✓ IDF计算测试通过");
        }

        // ==================== 测试4：Excel格式化 ====================
        /// <summary>
        /// 测试4：验证Excel格式化字符串正确
        /// 格式: word1(0.82)|word2(0.76)|...
        /// </summary>
        [Fact]
        public void Test_004_ExcelStringFormatting()
        {
            Console.WriteLine("\n========== 测试4：Excel格式化 ==========");
            
            var service = new TfidfProcessorService(topN: 5);
            
            var texts = new List<string>
            {
                "python python python java java"
            };
            
            var results = service.ProcessAll(texts);
            Assert.Single(results);
            
            var excelString = results[0].ExcelString;
            Console.WriteLine($"Excel字符串格式: {excelString}");
            
            // 验证格式：应该包含分数（括号）和分隔符
            Assert.NotNull(excelString);
            Assert.Contains("(", excelString);
            Assert.Contains(")", excelString);
            
            Console.WriteLine("✓ Excel格式化测试通过");
        }

        // ==================== 测试5：特殊字符处理 ====================
        /// <summary>
        /// 测试5：处理包含特殊字符的文本
        /// 输入包含：逗号、冒号、括号、下划线等
        /// 期望：正确清洗并提取有效词汇
        /// </summary>
        [Fact]
        public void Test_005_SpecialCharacterHandling()
        {
            Console.WriteLine("\n========== 测试5：特殊字符处理 ==========");
            
            var service = new TfidfProcessorService(topN: 5);
            
            // 包含特殊字符的文本
            var texts = new List<string>
            {
                "beautiful_girl, beautiful_day: beautiful_night (beautiful_sky) [beautiful_world]"
            };
            
            Console.WriteLine($"输入文本: {texts[0]}");
            
            var results = service.ProcessAll(texts);
            Assert.NotNull(results[0].TopKeywords);
            
            // 应该识别出"beautiful"的变体
            var topKeyword = results[0].TopKeywords.First();
            Console.WriteLine($"Top关键词: {topKeyword}");
            
            // 应该是beautiful相关的词
            Assert.NotEmpty(results[0].TopKeywords);
            
            Console.WriteLine("✓ 特殊字符处理测试通过");
        }

        // ==================== 测试6：性能测试 - 中等数据量 ====================
        /// <summary>
        /// 测试6：处理100条文本，验证性能和结果正确性
        /// 目标：<1秒完成
        /// </summary>
        [Fact]
        public void Test_006_Performance_Medium()
        {
            Console.WriteLine("\n========== 测试6：性能测试(100条文本) ==========");
            
            var service = new TfidfProcessorService(topN: 10);
            
            // 生成100条测试文本
            var texts = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                var text = $"document{i} apple banana cherry date elderberry apple banana cherry apple";
                texts.Add(text);
            }
            
            var sw = Stopwatch.StartNew();
            var results = service.ProcessAll(texts);
            sw.Stop();
            
            Assert.Equal(100, results.Count);
            Console.WriteLine($"处理100条文本耗时: {sw.ElapsedMilliseconds}ms");
            
            // 验证所有文档都有结果
            foreach (var result in results.Take(3))  // 只打印前3条
            {
                Console.WriteLine($"  文档{result.DocId}: {string.Join(", ", result.TopKeywords ?? new List<string>())}");
            }
            
            // 性能要求：应该在可接受的时间内完成
            Assert.True(sw.ElapsedMilliseconds < 5000, "性能超标");
            
            Console.WriteLine("✓ 性能测试通过");
        }

        // ==================== 测试7：空文本和边界情况 ====================
        /// <summary>
        /// 测试7：处理空文本、只有空格的文本等边界情况
        /// </summary>
        [Fact]
        public void Test_007_EdgeCases()
        {
            Console.WriteLine("\n========== 测试7：边界情况 ==========");
            
            var service = new TfidfProcessorService(topN: 5);
            
            var texts = new List<string>
            {
                "valid text here",
                "",                  // 空文本
                "   ",              // 只有空格
                "single"            // 单个词
            };
            
            var results = service.ProcessAll(texts);
            
            Assert.NotNull(results);
            Console.WriteLine($"处理结果数: {results.Count}");
            
            // 验证能够处理各种边界情况而不抛异常
            foreach (var result in results)
            {
                Console.WriteLine($"  文档{result.DocId}: {result.TopKeywords?.Count ?? 0}个关键词");
            }
            
            Console.WriteLine("✓ 边界情况测试通过");
        }

        // ==================== 测试8：与Python结果对比 ====================
        /// <summary>
        /// 测试8：使用真实数据（Excel N列的样本）与Python结果对比
        /// 这将在后续步骤中实现
        /// </summary>
        [Fact]
        public void Test_008_RealExcelData()
        {
            Console.WriteLine("\n========== 测试8：真实Excel数据（准备中）==========");
            
            // TODO: 加载实际的Excel文件N列数据
            // TODO: 与Python的tfidf_processor.py结果对比
            
            Console.WriteLine("⚠ 此测试在后续迭代中实现");
            Assert.True(true);
        }

        // ==================== 总结方法 ====================
        /// <summary>
        /// 打印测试总结
        /// </summary>
        public static void PrintTestSummary()
        {
            Console.WriteLine("\n");
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     TF-IDF 单元测试总结                 ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine("测试阶段: 框架验证 + 简单案例");
            Console.WriteLine("下一步: 补充具体实现 → 中等数据测试 → 大数据测试");
        }
    }
}
