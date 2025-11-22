using System;
using System.Collections.Generic;
using ImageInfo.Services;

namespace TestSafeMoveProtection
{
    /// <summary>
    /// 安全移动保护功能测试程序
    /// 验证 SafeMoveProtection 的所有功能
    /// </summary>
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║   SafeMoveProtection 安全移动保护 - 功能测试       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝\n");

            // 测试用例集合
            var testCases = new List<(string Path, bool ShouldBeProtected, string Description)>
            {
                // 包含单个保护关键词的路径
                (@"C:\Images\[超清]\photo.png", true, "文件名包含保护关键词'超'"),
                (@"C:\Archive\[绝版]\important.jpg", true, "文件名包含保护关键词'绝'"),
                (@"D:\Projects\[精选]\selection.webp", true, "文件名包含保护关键词'精'"),
                (@"E:\Files\[特殊]\special.gif", true, "文件名包含保护关键词'特'"),
                (@"F:\Work\[待处理]\pending.bmp", true, "文件名包含保护关键词'待'"),
                
                // 包含保护关键词的文件夹路径
                (@"C:\[超清录制]\videos\movie.mp4", true, "文件夹名包含保护关键词'超'"),
                (@"C:\[绝版存档]\old files\document.txt", true, "文件夹路径包含保护关键词'绝'"),
                (@"D:\精选集合\[重要]\archive.zip", true, "路径中间包含保护关键词'精'"),
                (@"E:\特定目录\photos\image.png", true, "路径包含保护关键词'特'"),
                (@"待审核\files\待归档\data.xlsx", true, "路径包含保护关键词'待'"),
                
                // 组合情况
                (@"C:\[超清绝版]\[精选特待]\image.png", true, "包含多个保护关键词"),
                (@"C:\超清\绝版\精选\特别\待处理\file.jpg", true, "路径中多个位置包含保护关键词"),
                
                // 不受保护的正常路径
                (@"C:\Images\photo.png", false, "完全正常的路径"),
                (@"C:\Normal folder\image.jpg", false, "普通文件夹名"),
                (@"D:\Archive\backup\important.zip", false, "没有保护关键词的路径"),
                (@"E:\Images\photo2024.png", false, "包含数字但无保护关键词"),
                (@"F:\Files\test file (copy).txt", false, "包含括号但无保护关键词"),
                
                // 边界情况
                ("", false, "空路径"),
                ("  ", false, "纯空格路径"),
                (@"C:\超", true, "最后是保护关键词"),
                (@"超\file.txt", true, "最前是保护关键词"),
            };

            Console.WriteLine(">>> 测试1：基础保护检测\n");
            TestBasicProtection(testCases);

            Console.WriteLine("\n>>> 测试2：反向检查（CanMove）\n");
            TestCanMove(testCases);

            Console.WriteLine("\n>>> 测试3：获取保护关键词列表\n");
            TestGetProtectedKeywords();

            Console.WriteLine("\n>>> 测试4：获取详细保护状态\n");
            TestGetProtectionStatus();

            Console.WriteLine("\n>>> 测试5：批量文件过滤\n");
            TestFilterMultipleFiles();

            Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║              所有测试完成                           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// 测试1：基础保护检测
        /// </summary>
        private static void TestBasicProtection(List<(string Path, bool ShouldBeProtected, string Description)> testCases)
        {
            int passCount = 0;
            int totalCount = 0;

            foreach (var (path, shouldBeProtected, description) in testCases)
            {
                totalCount++;
                var isProtected = SafeMoveProtection.IsProtectedPath(path);
                bool passed = isProtected == shouldBeProtected;
                passCount += passed ? 1 : 0;

                string status = passed ? "✓" : "✗";
                string pathDisplay = string.IsNullOrWhiteSpace(path) ? "[空路径]" : path;
                Console.WriteLine($"{status} {description}");
                Console.WriteLine($"  路径: {pathDisplay}");
                Console.WriteLine($"  预期: {(shouldBeProtected ? "受保护" : "不受保护")}, 实际: {(isProtected ? "受保护" : "不受保护")}");

                if (!passed)
                {
                    Console.WriteLine($"  ⚠️ 测试失败！");
                }
                Console.WriteLine();
            }

            Console.WriteLine($"总测试数: {totalCount}, 通过: {passCount}, 失败: {totalCount - passCount}");
            if (passCount == totalCount)
                Console.WriteLine("✅ 基础保护检测全部通过");
            else
                Console.WriteLine($"⚠️  有 {totalCount - passCount} 个测试失败");
        }

        /// <summary>
        /// 测试2：反向检查（CanMove）
        /// </summary>
        private static void TestCanMove(List<(string Path, bool ShouldBeProtected, string Description)> testCases)
        {
            Console.WriteLine("验证 CanMove() 与 IsProtectedPath() 的逆关系：\n");

            int passCount = 0;
            foreach (var (path, shouldBeProtected, _) in testCases)
            {
                var isProtected = SafeMoveProtection.IsProtectedPath(path);
                var canMove = SafeMoveProtection.CanMove(path);

                // CanMove 应该是 IsProtectedPath 的反函数
                bool correct = canMove == !isProtected;
                passCount += correct ? 1 : 0;

                string pathDisplay = string.IsNullOrWhiteSpace(path) ? "[空路径]" : path;
                if (!correct)
                {
                    Console.WriteLine($"✗ 关系验证失败: {pathDisplay}");
                }
            }

            Console.WriteLine($"验证完成: {passCount}/{testCases.Count} 正确");
            if (passCount == testCases.Count)
                Console.WriteLine("✅ CanMove 反向检查全部通过");
        }

        /// <summary>
        /// 测试3：获取保护关键词列表
        /// </summary>
        private static void TestGetProtectedKeywords()
        {
            Console.WriteLine("当前的保护关键词列表：\n");

            var keywords = SafeMoveProtection.GetProtectedKeywords();
            var keywordList = new List<string>(keywords);

            // 验证是否包含所有5个关键词
            var expectedKeywords = new[] { "超", "绝", "精", "特", "待" };
            int foundCount = 0;

            foreach (var keyword in keywordList)
            {
                Console.WriteLine($"  • {keyword}");
                if (System.Array.Exists(expectedKeywords, e => e == keyword))
                    foundCount++;
            }

            Console.WriteLine($"\n总数: {keywordList.Count} 个");
            Console.WriteLine($"预期关键词: {string.Join(", ", expectedKeywords)}");

            if (foundCount == expectedKeywords.Length && keywordList.Count == expectedKeywords.Length)
                Console.WriteLine("✅ 关键词列表完整正确");
            else
                Console.WriteLine($"⚠️ 关键词列表不完整，找到 {foundCount}/{expectedKeywords.Length}");
        }

        /// <summary>
        /// 测试4：获取详细保护状态
        /// </summary>
        private static void TestGetProtectionStatus()
        {
            var testPaths = new[]
            {
                (@"C:\[超清]\photo.png", "包含保护关键词的文件"),
                (@"C:\Normal\image.jpg", "正常文件"),
                ("", "空路径")
            };

            foreach (var (path, description) in testPaths)
            {
                Console.WriteLine($"测试: {description}");
                Console.WriteLine($"路径: {(string.IsNullOrEmpty(path) ? "[空]" : path)}");

                var status = SafeMoveProtection.GetProtectionStatus(path);

                Console.WriteLine($"受保护: {status.IsProtected}");
                if (!string.IsNullOrEmpty(status.TriggeredKeyword))
                    Console.WriteLine($"触发关键词: {status.TriggeredKeyword}");
                Console.WriteLine($"原因: {status.Reason}");
                Console.WriteLine();
            }

            Console.WriteLine("✅ 详细状态查询完成");
        }

        /// <summary>
        /// 测试5：批量文件过滤
        /// </summary>
        private static void TestFilterMultipleFiles()
        {
            var filePaths = new[]
            {
                @"C:\[超清]\photo1.png",
                @"C:\Normal\photo2.jpg",
                @"D:\[精选]\photo3.webp",
                @"E:\Archive\image4.gif",
                @"F:\[待处理]\file.txt",
            };

            Console.WriteLine("待过滤的文件列表：");
            for (int i = 0; i < filePaths.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {filePaths[i]}");
            }

            var result = SafeMoveProtection.FilterProtectedFiles(filePaths);

            Console.WriteLine($"\n✅ 受保护的文件 ({result.Protected.Count} 个)：");
            foreach (var file in result.Protected)
            {
                Console.WriteLine($"  • {file}");
            }

            Console.WriteLine($"\n✓ 可移动的文件 ({result.Unprotected.Count} 个)：");
            foreach (var file in result.Unprotected)
            {
                Console.WriteLine($"  • {file}");
            }

            Console.WriteLine($"\n汇总: 总共 {result.Protected.Count + result.Unprotected.Count} 个文件，" +
                            $"其中 {result.Protected.Count} 个受保护，{result.Unprotected.Count} 个可移动");
        }
    }
}
