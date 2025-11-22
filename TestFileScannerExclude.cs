using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageInfo.Services;

namespace ImageInfo.Tests
{
    /// <summary>
    /// 快速测试文件扫描的排除功能
    /// 验证 .bf, .preview 等文件夹会被正确跳过
    /// </summary>
    public class TestFileScannerExclude
    {
        public static void Main()
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║  FileScanner 排除文件夹功能测试        ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            // 创建测试目录结构
            const string testRoot = @"C:\temp\test_scan";
            SetupTestDirectories(testRoot);

            try
            {
                Console.WriteLine(">>> 测试1：扫描包含被排除文件夹的目录");
                Console.WriteLine($"根目录: {testRoot}\n");

                var files = FileScanner.GetImageFiles(testRoot).ToList();
                
                Console.WriteLine($"✓ 扫描完成，找到 {files.Count} 个文件\n");
                
                Console.WriteLine("发现的文件:");
                foreach (var file in files)
                {
                    Console.WriteLine($"  • {file}");
                }

                Console.WriteLine("\n>>> 测试2：验证排除的文件夹名称");
                var excluded = FileScanner.GetExcludedFolders();
                Console.WriteLine("被排除的文件夹名称:");
                foreach (var folder in excluded)
                {
                    Console.WriteLine($"  • {folder}");
                }

                Console.WriteLine("\n>>> 验证结果:");
                
                // 验证：.bf 和 .preview 中的文件不应该出现
                var hasBfFiles = files.Any(f => f.Contains(@"\.bf\") || f.Contains(@"\.bf" + Path.DirectorySeparatorChar));
                var hasPreviewFiles = files.Any(f => f.Contains(@"\.preview\") || f.Contains(@"\.preview" + Path.DirectorySeparatorChar));
                
                if (!hasBfFiles)
                    Console.WriteLine("✓ .bf 文件夹中的文件被正确排除");
                else
                    Console.WriteLine("✗ .bf 文件夹中的文件未被排除（有问题）");
                
                if (!hasPreviewFiles)
                    Console.WriteLine("✓ .preview 文件夹中的文件被正确排除");
                else
                    Console.WriteLine("✗ .preview 文件夹中的文件未被排除（有问题）");

                // 验证：根目录和正常子目录的文件应该出现
                var hasRootFiles = files.Any(f => f.EndsWith("root.png"));
                var hasSubFiles = files.Any(f => f.Contains(@"\normal_subfolder\") && f.EndsWith("sub.png"));
                
                if (hasRootFiles)
                    Console.WriteLine("✓ 根目录中的文件被正确包含");
                else
                    Console.WriteLine("✗ 根目录中的文件未被包含（有问题）");
                
                if (hasSubFiles)
                    Console.WriteLine("✓ 正常子目录中的文件被正确包含");
                else
                    Console.WriteLine("✗ 正常子目录中的文件未被包含（有问题）");

                Console.WriteLine("\n╔════════════════════════════════════════╗");
                Console.WriteLine("║         测试完成                       ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
            }
            finally
            {
                // 清理测试目录
                if (Directory.Exists(testRoot))
                {
                    Console.WriteLine("\n清理测试目录...");
                    Directory.Delete(testRoot, true);
                    Console.WriteLine("✓ 测试目录已删除");
                }
            }
        }

        /// <summary>
        /// 创建测试目录结构
        /// </summary>
        private static void SetupTestDirectories(string root)
        {
            Console.WriteLine(">>> 创建测试目录结构...\n");
            
            // 创建目录
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "normal_subfolder"));
            Directory.CreateDirectory(Path.Combine(root, ".bf"));
            Directory.CreateDirectory(Path.Combine(root, ".preview"));
            Directory.CreateDirectory(Path.Combine(root, ".bf", ".preview"));  // 嵌套的排除文件夹

            // 创建测试文件
            CreateDummyImage(Path.Combine(root, "root.png"));
            CreateDummyImage(Path.Combine(root, "normal_subfolder", "sub.png"));
            CreateDummyImage(Path.Combine(root, ".bf", "excluded_bf.png"));
            CreateDummyImage(Path.Combine(root, ".preview", "excluded_preview.png"));
            CreateDummyImage(Path.Combine(root, ".bf", ".preview", "excluded_nested.png"));

            Console.WriteLine("目录结构:");
            Console.WriteLine($"  {root}");
            Console.WriteLine($"    ├─ root.png (✓ 应该被扫描)");
            Console.WriteLine($"    ├─ normal_subfolder/");
            Console.WriteLine($"    │  └─ sub.png (✓ 应该被扫描)");
            Console.WriteLine($"    ├─ .bf/ (✗ 应该被跳过)");
            Console.WriteLine($"    │  ├─ excluded_bf.png");
            Console.WriteLine($"    │  └─ .preview/");
            Console.WriteLine($"    │     └─ excluded_nested.png");
            Console.WriteLine($"    └─ .preview/ (✗ 应该被跳过)");
            Console.WriteLine($"       └─ excluded_preview.png\n");
        }

        /// <summary>
        /// 创建一个虚拟的PNG文件（仅用于测试）
        /// </summary>
        private static void CreateDummyImage(string filePath)
        {
            // PNG 文件签名（最小的有效PNG文件）
            byte[] pngSignature = new byte[] 
            { 
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,  // PNG signature
                // IHDR chunk
                0x00, 0x00, 0x00, 0x0D,  // chunk length
                0x49, 0x48, 0x44, 0x52,  // "IHDR"
                0x00, 0x00, 0x00, 0x01,  // width: 1
                0x00, 0x00, 0x00, 0x01,  // height: 1
                0x08, 0x02, 0x00, 0x00, 0x00,  // bit depth, color type, etc.
                0x90, 0x77, 0x53, 0xDE,  // CRC
                // IEND chunk
                0x00, 0x00, 0x00, 0x00,
                0x49, 0x45, 0x4E, 0x44,
                0xAE, 0x42, 0x60, 0x82
            };

            File.WriteAllBytes(filePath, pngSignature);
        }
    }
}
