using System;
using System.Collections.Generic;
using ImageInfo.Services;

namespace ImageInfo.Examples
{
    /// <summary>
    /// PNG 图片信息读取演示。
    /// 展示如何使用 SixLabors.ImageSharp 读取 PNG 的完整信息。
    /// 使用方法：通过其他主程序类的入口点调用本类的静态方法。
    /// </summary>
    public static class PngInfoReaderDemo
    {
        /// <summary>
        /// 演示读取单个 PNG 文件的完整信息。
        /// 调用此方法来查看 PNG 的所有详细信息。
        /// </summary>
        public static void RunSingleFileDemo(string filePath)
        {
            Console.WriteLine("=== PNG Information Reader Demo ===\n");

            if (!System.IO.File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found: {filePath}");
                Console.ResetColor();
                return;
            }

            if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: File does not appear to be a PNG: {filePath}");
                Console.ResetColor();
            }

            // 读取 PNG 信息
            var pngInfo = PngInfoReader.ReadPngInfo(filePath);
            if (pngInfo == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to read PNG information.");
                Console.ResetColor();
                return;
            }

            // 显示摘要信息
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(pngInfo.ToString());
            Console.ResetColor();

            // 显示详细的文本元数据
            if (pngInfo.TextMetadata?.Count > 0)
            {
                Console.WriteLine("\n--- Detailed Text Metadata ---");
                foreach (var (keyword, value) in pngInfo.TextMetadata)
                {
                    Console.WriteLine($"\nKeyword: {keyword}");
                    Console.WriteLine($"Value:\n{value}\n");
                }
            }

            // 显示 EXIF 数据
            if (pngInfo.HasExif && pngInfo.ExifData?.Count > 0)
            {
                Console.WriteLine("\n--- EXIF Data ---");
                foreach (var (key, value) in pngInfo.ExifData)
                {
                    Console.WriteLine($"{key}: {value}");
                }
            }

            // 检查透明度
            Console.WriteLine("\n--- Transparency Check ---");
            var hasTransparency = PngInfoReader.HasTransparency(filePath);
            if (hasTransparency.HasValue)
            {
                Console.WriteLine($"Has transparent pixels: {(hasTransparency.Value ? "Yes" : "No")}");
            }

            // 显示基本信息
            Console.WriteLine("\n--- Basic Image Info ---");
            var basicInfo = PngInfoReader.GetBasicImageInfo(filePath);
            if (basicInfo.HasValue)
            {
                Console.WriteLine($"Format: {basicInfo.Value.Format}");
                Console.WriteLine($"Dimensions: {basicInfo.Value.Width}x{basicInfo.Value.Height}");
            }
        }

        /// <summary>
        /// 演示批量读取多个 PNG 文件的信息。
        /// </summary>
        public static void DemoBatchReadPngInfo(string[] filePaths)
        {
            Console.WriteLine("=== Batch PNG Information Reading ===\n");

            foreach (var filePath in filePaths)
            {
                Console.WriteLine($"\nProcessing: {filePath}");
                Console.WriteLine(new string('-', 60));

                var pngInfo = PngInfoReader.ReadPngInfo(filePath);
                if (pngInfo != null)
                {
                    Console.WriteLine($"✓ Dimensions: {pngInfo.Width}x{pngInfo.Height}");
                    Console.WriteLine($"✓ Color Type: {pngInfo.ColorType}");
                    Console.WriteLine($"✓ Bit Depth: {pngInfo.BitDepth}");
                    
                    if (pngInfo.TextMetadata?.Count > 0)
                    {
                        Console.WriteLine($"✓ Text Metadata Keys: {string.Join(", ", pngInfo.TextMetadata.Keys)}");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Failed to read PNG information");
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// 演示提取 AI 生成图片的 prompt 和其他元数据。
        /// </summary>
        public static void DemoExtractAIMetadata(string filePath)
        {
            Console.WriteLine("=== AI Image Metadata Extraction ===\n");

            var textMetadata = PngInfoReader.ReadPngTextMetadata(filePath);
            if (textMetadata == null || textMetadata.Count == 0)
            {
                Console.WriteLine("No text metadata found in the PNG file.");
                return;
            }

            Console.WriteLine($"Found {textMetadata.Count} text metadata entries:\n");

            foreach (var (keyword, value) in textMetadata)
            {
                Console.WriteLine($"【{keyword}】");
                
                // 针对常见的 AI 工具元数据格式进行格式化显示
                if (keyword.Equals("parameters", StringComparison.OrdinalIgnoreCase))
                {
                    DisplayFormattedParameters(value);
                }
                else if (keyword.Equals("prompt", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Prompt: {value}");
                }
                else if (keyword.Equals("negative prompt", StringComparison.OrdinalIgnoreCase) ||
                         keyword.Equals("negativeprompt", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Negative Prompt: {value}");
                }
                else
                {
                    // 显示其他元数据，超长字符串进行截断
                    if (value.Length > 200)
                        Console.WriteLine(value.Substring(0, 200) + "...");
                    else
                        Console.WriteLine(value);
                }

                Console.WriteLine();
            }
        }

    /// <summary>
    /// 格式化显示 Stable Diffusion 风格的 parameters。
    /// </summary>
    private static void DisplayFormattedParameters(string parameters)
    {
        var lines = parameters.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.Contains(":"))
            {
                var parts = line.Split(new[] { ':' }, 2);
                Console.WriteLine($"  {parts[0].Trim()}: {parts[1].Trim()}");
            }
            else
            {
                // 第一行通常是 prompt
                Console.WriteLine($"  Prompt: {line.Trim()}");
            }
        }
    }

        /// <summary>
        /// 演示生成 JSON 格式的 PNG 信息。
        /// </summary>
        public static void DemoJsonExport(string filePath)
        {
            Console.WriteLine("=== Export PNG Info as JSON ===\n");

            var pngInfo = PngInfoReader.ReadPngInfo(filePath);
            if (pngInfo == null)
            {
                Console.WriteLine("Failed to read PNG information.");
                return;
            }

            var jsonObject = pngInfo.ToJsonObject();
            
            // 简单的 JSON 序列化（生产环境建议使用 System.Text.Json 或 Newtonsoft.Json）
            Console.WriteLine(SerializeToJson(jsonObject, 0));
        }

        /// <summary>
        /// 简单的 JSON 序列化函数。
        /// </summary>
        private static string SerializeToJson(object obj, int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            var nextIndentStr = new string(' ', (indent + 1) * 2);

            if (obj == null)
                return "null";

            if (obj is string str)
                return $"\"{str}\"";

            if (obj is bool b)
                return b.ToString().ToLower();

            if (obj is int intVal)
                return intVal.ToString();

            if (obj is double doubleVal)
                return doubleVal.ToString("F2");

            if (obj is Dictionary<string, object> dict)
            {
                var lines = new List<string> { "{" };
                var keys = dict.Keys.ToList();
                for (int idx = 0; idx < keys.Count; idx++)
                {
                    var key = keys[idx];
                    var value = dict[key];
                    var comma = idx < keys.Count - 1 ? "," : "";
                    lines.Add($"{nextIndentStr}\"{key}\": {SerializeToJson(value, indent + 1)}{comma}");
                }
                lines.Add($"{indentStr}}}");
                return string.Join("\n", lines);
            }

            if (obj is Dictionary<string, string> strDict)
            {
                var lines = new List<string> { "{" };
                var keys = strDict.Keys.ToList();
                for (int idx = 0; idx < keys.Count; idx++)
                {
                    var key = keys[idx];
                    var value = strDict[key];
                    var comma = idx < keys.Count - 1 ? "," : "";
                    lines.Add($"{nextIndentStr}\"{key}\": \"{value}\"{comma}");
                }
                lines.Add($"{indentStr}}}");
                return string.Join("\n", lines);
            }

            return obj.ToString() ?? "";
        }
    }
}
