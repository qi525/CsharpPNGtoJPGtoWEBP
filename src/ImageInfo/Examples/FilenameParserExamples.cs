using System;
using ImageInfo.Services;

namespace ImageInfo.Examples;

/// <summary>
/// FilenameParser 使用示例
/// 
/// 演示如何使用 FilenameParser 来解析和处理文件名
/// </summary>
public class FilenameParserExamples
{
    /// <summary>
    /// 运行所有示例（不用 Main 作为入口点）
    /// </summary>
    public static void RunAllExamples()
    {
        Console.WriteLine("=== FilenameParser 使用示例 ===\n");

        // 示例 1: 完整解析
        Example1_FullParsing();

        // 示例 2: 快速提取
        Example2_QuickExtraction();

        // 示例 3: 文件路径解析
        Example3_PathParsing();

        // 示例 4: 错误处理
        Example4_ErrorHandling();

        // 示例 5: 批量处理
        Example5_BatchProcessing();
    }

    /// <summary>
    /// 示例 1: 完整解析文件名
    /// </summary>
    public static void Example1_FullParsing()
    {
        Console.WriteLine("--- 示例 1: 完整解析 ---");
        
        var filename = "00000-2365214977___blue_archive___whip___mari___track___commentaries___kit___aid___archive___milkshakework___highleg___dominatrix@@@评分88.jpg";
        var result = FilenameParser.ParseFilename(filename);

        if (result.IsSuccess)
        {
            Console.WriteLine($"✓ 解析成功");
            Console.WriteLine($"  原名: {result.OriginalName}");
            Console.WriteLine($"  扩展名: {result.Extension}");
            Console.WriteLine($"  后缀: {result.Suffix}");
            Console.WriteLine($"  重建文件名: {result.RebuiltFilename}");
            Console.WriteLine($"  是否完全匹配原始: {result.RebuiltFilename == filename}");
        }
        else
        {
            Console.WriteLine($"✗ 解析失败: {result.ErrorMessage}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 2: 快速提取单个信息
    /// </summary>
    public static void Example2_QuickExtraction()
    {
        Console.WriteLine("--- 示例 2: 快速提取 ---");

        var testCases = new[]
        {
            "photo___tag1___tag2.jpg",
            "image_001@@@评分95.png",
            "simple_image.webp",
            "图片001___中文标签___english_tag.jpeg"
        };

        foreach (var filename in testCases)
        {
            var originalName = FilenameParser.GetOriginalName(filename);
            var extension = FilenameParser.GetExtension(filename);
            var suffix = FilenameParser.GetSuffix(filename);

            Console.WriteLine($"文件名: {filename}");
            Console.WriteLine($"  原名: {originalName}");
            Console.WriteLine($"  扩展名: {extension}");
            Console.WriteLine($"  后缀: {suffix ?? "(无)"}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 3: 从文件路径解析
    /// </summary>
    public static void Example3_PathParsing()
    {
        Console.WriteLine("--- 示例 3: 文件路径解析 ---");

        var filePath = @"C:\Users\test\Documents\my_photo___landscape___nature___sunset.jpg";
        var result = FilenameParser.ParseFilenamePath(filePath);

        if (result.IsSuccess)
        {
            Console.WriteLine($"✓ 从路径解析成功");
            Console.WriteLine($"  完整路径: {filePath}");
            Console.WriteLine($"  原始文件名: {result.RawFilename}");
            Console.WriteLine($"  提取的原名: {result.OriginalName}");
            Console.WriteLine($"  提取的扩展名: {result.Extension}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 4: 错误处理
    /// </summary>
    public static void Example4_ErrorHandling()
    {
        Console.WriteLine("--- 示例 4: 错误处理 ---");

        var testCases = new[]
        {
            ("", "空文件名"),
            ("no_extension", "缺少扩展名"),
            ("___only_suffix.jpg", "没有有效原名"),
            ("valid_image___tag.png", "有效的文件名")
        };

        foreach (var (filename, description) in testCases)
        {
            var result = FilenameParser.ParseFilename(filename);
            
            if (result.IsSuccess)
            {
                Console.WriteLine($"✓ {description}: {result.OriginalName}{result.Extension}");
            }
            else
            {
                Console.WriteLine($"✗ {description}: {result.ErrorMessage}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 5: 批量处理文件名
    /// </summary>
    public static void Example5_BatchProcessing()
    {
        Console.WriteLine("--- 示例 5: 批量处理 ---");

        var filenames = new[]
        {
            "artwork_2025___anime___girl___cute@@@评分88.jpg",
            "photo_001___landscape___nature.png",
            "screenshot___desktop___ui.webp",
            "document_scan.pdf"
        };

        Console.WriteLine("处理文件列表:");
        
        var successCount = 0;
        var failureCount = 0;

        foreach (var filename in filenames)
        {
            var result = FilenameParser.ParseFilename(filename);
            
            if (result.IsSuccess)
            {
                successCount++;
                Console.WriteLine($"  ✓ {result.OriginalName} ({result.Extension})");
            }
            else
            {
                failureCount++;
                Console.WriteLine($"  ✗ {filename} - {result.ErrorMessage}");
            }
        }

        Console.WriteLine($"\n统计: 成功 {successCount}, 失败 {failureCount}");
        Console.WriteLine();
    }
}
