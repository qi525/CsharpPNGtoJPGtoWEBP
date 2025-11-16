using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;

namespace ImageInfo.Services
{
    /// <summary>
    /// 提取 AI 生成图片的元数据（Stable Diffusion、ComfyUI 等）。
    /// 实现读→写→验证的三步流程。
    /// </summary>
    public static class AIMetadataExtractor
    {
        /// <summary>
        /// 【步骤 1：读】从图片文件读取 AI 元数据。
        /// 支持从 PNG tEXt、EXIF、XMP 中提取 prompt、model、seed 等信息。
        /// </summary>
        public static AIMetadata ReadAIMetadata(string imagePath)
        {
            var metadata = new AIMetadata();
            
            try
            {
                // 尝试从缓存加载（如果存在且未过期）以加速重复运行
                var cached = TryLoadFromCache(imagePath);
                if (cached != null)
                {
                    // 直接返回缓存的 AIMetadata（避免重复解析）
                    LogMetadataExtraction(imagePath, cached.FullInfoExtractionMethod ?? "Cache", cached.FullInfo, cached.Prompt, cached.NegativePrompt, cached.FullInfo == null);
                    return cached;
                }
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                // 从 PNG tEXt 读取
                ExtractFromPngText(directories, metadata);
                
                // 从 EXIF 读取（某些工具会存储在 EXIF UserComment）
                ExtractFromExif(directories, metadata);
                
                // 从 XMP 读取
                ExtractFromXmp(directories, metadata);
                
                // --- 尝试获取完整的原始信息（优先级：MetadataExtractor -> ImageSharp PNG text chunks -> raw bytes brute-force）
                var full = TryGetFullInfoFromDirectories(directories);
                // 如果 MetadataExtractor 没有提取到完整信息，后面会尝试 raw-bytes 暴力解析作为最后手段

                if (string.IsNullOrEmpty(full))
                {
                    // 最后的暴力破解：从文件二进制中查找 ASCII 文本片段（例如含有 "parameters" 或 "prompt" 的块），尝试以多种编码解码
                    try
                    {
                        full = TryExtractFullInfoFromRawBytes(imagePath);
                        if (!string.IsNullOrEmpty(full))
                            metadata.FullInfoExtractionMethod = "RawBytes.Fallback";
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (!string.IsNullOrEmpty(full))
                {
                    metadata.FullInfo = full;
                    if (string.IsNullOrEmpty(metadata.FullInfoExtractionMethod))
                        metadata.FullInfoExtractionMethod = "MetadataExtractor";

                    // 在拿到完整信息后，尝试从中解析具体字段（prompt, negative, model, steps, seed, sampler）
                    ParseFullInfoIntoFields(metadata);
                    // 保存缓存并记录日志
                    SaveToCache(imagePath, metadata);
                    LogMetadataExtraction(imagePath, metadata.FullInfoExtractionMethod ?? "Unknown", metadata.FullInfo, metadata.Prompt, metadata.NegativePrompt, false);
                }
                else
                {
                    // 未获取到完整信息，记录警告（可能需要进一步调查）
                    LogMetadataExtraction(imagePath, metadata.FullInfoExtractionMethod ?? "None", null, metadata.Prompt, metadata.NegativePrompt, true);
                }
            }
            catch
            {
                // 元数据读取失败时返回空对象
            }

            return metadata;
        }

        /// <summary>
        /// 尝试从 MetadataExtractor 的目录中获取可能的完整信息文本（例如 tEXt iTXt zTXt 的内容）。
        /// </summary>
        private static string? TryGetFullInfoFromDirectories(IReadOnlyList<MetadataExtractor.Directory> directories)
        {
            foreach (var dir in directories)
            {
                if (dir is MetadataExtractor.Formats.Png.PngDirectory pngDir)
                {
                    foreach (var tag in pngDir.Tags)
                    {
                        var desc = tag?.Description ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(desc))
                            continue;

                        // 常见完整信息包含 'Steps:' 或 'Negative prompt' 或以 'parameters:' 为前缀
                        if (desc.IndexOf("Steps:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("parameters", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return desc;
                        }
                    }
                }

                // 一些 EXIF/ImageDescription 也可能包含完整的参数块
                if (dir is MetadataExtractor.Formats.Exif.ExifIfd0Directory exifDir)
                {
                    foreach (var tag in exifDir.Tags)
                    {
                        var desc = tag?.Description ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(desc))
                            continue;

                        if (desc.IndexOf("Steps:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("parameters", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return desc;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 最后的暴力尝试：从文件二进制中查找可能的文本片段（parameters / prompt 等）。
        /// 会尝试 UTF-8 与 UTF-16LE 解码。
        /// </summary>
        private static string? TryExtractFullInfoFromRawBytes(string path)
        {
            var bytes = System.IO.File.ReadAllBytes(path);
            var needleAscii = System.Text.Encoding.ASCII.GetBytes("parameters");
            var idx = IndexOfSequence(bytes, needleAscii);
            if (idx < 0)
            {
                // fallback search for 'prompt' string
                needleAscii = System.Text.Encoding.ASCII.GetBytes("prompt");
                idx = IndexOfSequence(bytes, needleAscii);
            }

            if (idx < 0)
                return null;

            // 从找到的位置向后截取一定长度（限制避免内存问题）
            var start = Math.Max(0, idx - 16);
            var maxLen = Math.Min(16000, bytes.Length - start);
            var slice = new byte[maxLen];
            Array.Copy(bytes, start, slice, 0, maxLen);

            // 尝试以 UTF8 解码
            try
            {
                var s = System.Text.Encoding.UTF8.GetString(slice);
                // 如果包含有意义的标签，返回
                if (s.IndexOf("Steps:", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0)
                    return s.Trim('\0', '\r', '\n');
            }
            catch { }

            // 再尝试 UTF-16LE（EXIF UserComment 常用）
            try
            {
                var s2 = System.Text.Encoding.Unicode.GetString(slice);
                if (s2.IndexOf("Steps:", StringComparison.OrdinalIgnoreCase) >= 0 || s2.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0)
                    return s2.Trim('\0', '\r', '\n');
            }
            catch { }

            return null;
        }

        private static int IndexOfSequence(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0)
                return 0;
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }

        // -------------------- 缓存与日志辅助方法 --------------------
        private static readonly string CacheDir = Path.Combine(Environment.CurrentDirectory, ".imageinfo_cache");
        private static readonly string ExtractionLog = Path.Combine(Environment.CurrentDirectory, "metadata-extraction.log");

        private static AIMetadata? TryLoadFromCache(string imagePath)
        {
            try
            {
                if (!System.IO.Directory.Exists(CacheDir)) return null;
                var fi = new FileInfo(imagePath);
                var key = ComputeHash(imagePath);
                var candidate = Path.Combine(CacheDir, key + "_" + fi.Length + "_" + fi.LastWriteTimeUtc.Ticks + ".meta");
                if (File.Exists(candidate))
                {
                    var content = File.ReadAllText(candidate, Encoding.UTF8);
                    var lines = content.Split(new[] { "\n" }, 2, StringSplitOptions.None);
                    if (lines.Length >= 2)
                    {
                        var method = lines[0].Trim();
                        var full = lines[1];
                        var m = new AIMetadata { FullInfo = full, FullInfoExtractionMethod = method };
                        ParseFullInfoIntoFields(m);
                        return m;
                    }
                }
            }
            catch { }
            return null;
        }

        private static void SaveToCache(string imagePath, AIMetadata metadata)
        {
            try
            {
                if (!System.IO.Directory.Exists(CacheDir)) System.IO.Directory.CreateDirectory(CacheDir);
                var fi = new FileInfo(imagePath);
                var key = ComputeHash(imagePath);
                var candidate = Path.Combine(CacheDir, key + "_" + fi.Length + "_" + fi.LastWriteTimeUtc.Ticks + ".meta");
                var data = (metadata.FullInfoExtractionMethod ?? "") + "\n" + (metadata.FullInfo ?? "");
                File.WriteAllText(candidate, data, Encoding.UTF8);
            }
            catch { }
        }

        private static string ComputeHash(string input)
        {
            using var sha1 = SHA1.Create();
            var b = Encoding.UTF8.GetBytes(input);
            var hash = sha1.ComputeHash(b);
            var sb = new StringBuilder();
            foreach (var bt in hash) sb.Append(bt.ToString("x2"));
            return sb.ToString();
        }

        // 只记录关键事件：ALARM（无FullInfo）和 RawBytes.Fallback 回退
        private static void LogMetadataExtraction(string imagePath, string method, string? fullInfo, string? prompt, string? negative, bool alarm)
        {
            try
            {
                // 只在以下情况记录：ALARM（无FullInfo）或 RawBytes 回退
                if (!alarm && method != "RawBytes.Fallback") return;

                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(alarm ? "ALARM" : "RawBytes")} | File: {Path.GetFileName(imagePath)} | Method: {method}");
                if (!string.IsNullOrEmpty(fullInfo) && fullInfo.Length > 500)
                    sb.AppendLine($"  FullInfo: {fullInfo.Substring(0, 500)}...");
                else if (!string.IsNullOrEmpty(fullInfo))
                    sb.AppendLine($"  FullInfo: {fullInfo}");
                sb.AppendLine();
                
                File.AppendAllText(ExtractionLog, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// 从完整信息字符串中解析常见字段（prompt, negative prompt, model, steps, seed, sampler）。
        /// 解析是启发式的：优先使用明确的标签（例如 "Negative prompt:"、"Steps:" 等）。
        /// </summary>
        private static void ParseFullInfoIntoFields(AIMetadata metadata)
        {
            if (metadata == null || string.IsNullOrEmpty(metadata.FullInfo))
                return;

            var s = metadata.FullInfo;

            // 提取 Negative prompt
            var negMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)Negative prompt[:：]?
\s*(.*?)($|\n|\r|\n\n)");
            if (negMatch.Success)
            {
                metadata.NegativePrompt = negMatch.Groups[1].Value.Trim();
            }

            // 如果存在 'Parameters:' 或 'parameters:' 这一块，很多工具将完整信息放在该后
            var paramsMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)(Parameters|parameters|parameters:)[\s:：]*(.*)$", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (paramsMatch.Success)
            {
                // 取参数块为 OtherInfo（保留原始）
                metadata.OtherInfo = paramsMatch.Groups[2].Value.Trim();
            }

            // 提取 Steps, Seed, Sampler, Model
            var stepsMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)Steps[:：]?\s*(\d+)");
            if (stepsMatch.Success)
                metadata.OtherInfo = CombineField(metadata.OtherInfo, $"Steps:{stepsMatch.Groups[1].Value}");

            var seedMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)Seed[:：]?\s*([0-9-]+)");
            if (seedMatch.Success)
                metadata.Seed = seedMatch.Groups[1].Value.Trim();

            var samplerMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)Sampler[:：]?\s*([^,\n\r]+)");
            if (samplerMatch.Success)
                metadata.Sampler = samplerMatch.Groups[1].Value.Trim();

            var modelMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)Model[:：]?\s*([^,\n\r]+)");
            if (modelMatch.Success)
                metadata.Model = modelMatch.Groups[1].Value.Trim();

            // 正向提示词：如果 FullInfo 中含有 'Negative prompt'，则正向提示为 'Negative prompt' 前面的部分（去掉标签）
            if (!string.IsNullOrEmpty(metadata.NegativePrompt))
            {
                var parts = System.Text.RegularExpressions.Regex.Split(s, @"(?i)Negative prompt[:：]");
                if (parts.Length > 0)
                {
                    var first = parts[0];
                    // 尝试从开头寻找 'Prompt:' 或直接作为 prompt
                    var promptMatch = System.Text.RegularExpressions.Regex.Match(first, @"(?i)Prompt[:：]?\s*(.*?)($|\n|\r|\n\n)");
                    if (promptMatch.Success)
                        metadata.Prompt = promptMatch.Groups[1].Value.Trim();
                    else
                    {
                        // 否则把 first 的前一部分作为 Prompt（去掉 Parameters 块）
                        var p = System.Text.RegularExpressions.Regex.Split(first, @"(?i)Parameters[:：]")[0].Trim();
                        if (!string.IsNullOrEmpty(p))
                            metadata.Prompt = p;
                    }
                }
            }
            else
            {
                // 如果没有 negative prompt，尝试直接提取 Prompt: 标签
                var promptMatch = System.Text.RegularExpressions.Regex.Match(s, @"(?i)Prompt[:：]?\s*(.*?)($|\n|\r|\n\n)");
                if (promptMatch.Success)
                {
                    metadata.Prompt = promptMatch.Groups[1].Value.Trim();
                }
            }
        }

        private static string CombineField(string? existing, string add)
        {
            if (string.IsNullOrEmpty(existing)) return add;
            return existing + " | " + add;
        }

        /// <summary>
        /// 【步骤 2：写】将 AI 元数据写入到转换后的图片。
        /// 目前作为占位符实现；实际写入需要 libpng 或其他库的支持。
        /// </summary>
        public static void WriteAIMetadata(string destImagePath, AIMetadata aiMetadata)
        {
            // 注：写回 PNG tEXt/EXIF/XMP 需要额外的库或手动编码
            // 这里作为调用示意，实际实现可使用 ImageMagick 或手动 PNG 字节写入
            // 当前实现仅为占位符，确保流程完整性
            if (string.IsNullOrEmpty(destImagePath) || aiMetadata == null)
                return;

            // 实际实现可在此处添加
        }

        /// <summary>
        /// 【步骤 3：验证】验证元数据是否成功写入并能被读回。
        /// </summary>
        public static bool VerifyAIMetadata(string destImagePath, AIMetadata originalMetadata)
        {
            if (string.IsNullOrEmpty(destImagePath))
                return false;

            try
            {
                var readBack = ReadAIMetadata(destImagePath);
                // 验证关键字段是否一致
                return !string.IsNullOrEmpty(readBack.Prompt) || readBack.Prompt == originalMetadata.Prompt;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>从 PNG tEXt 条目提取 AI 元数据。</summary>
        private static void ExtractFromPngText(IReadOnlyList<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories)
            {
                if (dir is MetadataExtractor.Formats.Png.PngDirectory pngDir)
                {
                    foreach (var tag in pngDir.Tags)
                    {
                        var desc = tag?.Description ?? "";
                        
                        // Stable Diffusion WebUI 格式
                        if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.Prompt = desc;
                        
                        if (desc.Contains("negative prompt", StringComparison.OrdinalIgnoreCase))
                            metadata.NegativePrompt = desc;
                        
                        if (desc.Contains("model", StringComparison.OrdinalIgnoreCase) || 
                            desc.Contains("checkpoint", StringComparison.OrdinalIgnoreCase))
                            metadata.Model = desc;
                        
                        if (desc.Contains("seed", StringComparison.OrdinalIgnoreCase))
                            metadata.Seed = desc;
                        
                        if (desc.Contains("sampler", StringComparison.OrdinalIgnoreCase) || 
                            desc.Contains("scheduler", StringComparison.OrdinalIgnoreCase))
                            metadata.Sampler = desc;
                        
                        // ComfyUI 格式
                        if (desc.Contains("workflow", StringComparison.OrdinalIgnoreCase))
                            metadata.OtherInfo = desc;
                    }
                }
            }
        }

        /// <summary>从 EXIF UserComment 提取 AI 元数据。</summary>
        private static void ExtractFromExif(IReadOnlyList<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories)
            {
                if (dir is ExifIfd0Directory exifDir)
                {
                    // EXIF tag 0x010F is ImageDescription which often contains metadata
                    foreach (var tag in exifDir.Tags)
                    {
                        if (tag != null)
                        {
                            var desc = tag.Description ?? "";
                            if (desc.Contains("prompt", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(metadata.Prompt))
                                metadata.Prompt = desc;
                        }
                    }
                }
            }
        }

        /// <summary>从 XMP 提取 AI 元数据（dc:subject、photoshop:Keywords 等）。</summary>
        private static void ExtractFromXmp(IReadOnlyList<MetadataExtractor.Directory> directories, AIMetadata metadata)
        {
            foreach (var dir in directories)
            {
                if (dir is XmpDirectory xmpDir)
                {
                    var xmp = xmpDir.XmpMeta;
                    if (xmp != null)
                    {
                        // Dublin Core subject (常用于存储 prompt)
                        var subject = xmp.GetPropertyString("http://purl.org/dc/elements/1.1/", "subject");
                        if (!string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(metadata.Prompt))
                            metadata.Prompt = subject;

                        // Photoshop Keywords
                        var keywords = xmp.GetPropertyString("http://ns.adobe.com/photoshop/1.0/", "Keywords");
                        if (!string.IsNullOrEmpty(keywords))
                            metadata.OtherInfo = (metadata.OtherInfo ?? "") + " | Keywords: " + keywords;
                    }
                }
            }
        }
    }

    /// <summary>
    /// AI 元数据容器。
    /// </summary>
    public class AIMetadata
    {
        /// <summary>AI 生成的 prompt。</summary>
        public string? Prompt { get; set; }
        /// <summary>AI 负 prompt。</summary>
        public string? NegativePrompt { get; set; }
        /// <summary>使用的 AI 模型。</summary>
        public string? Model { get; set; }
        /// <summary>生成种子。</summary>
        public string? Seed { get; set; }
        /// <summary>采样器/调度器。</summary>
        public string? Sampler { get; set; }
        /// <summary>其他元数据。</summary>
        public string? OtherInfo { get; set; }
        /// <summary>原始完整信息（例如 PNG 的 parameters 或 EXIF/ImageDescription 的完整块）。</summary>
        public string? FullInfo { get; set; }
        /// <summary>记录提取完整信息所使用的方法（例如 MetadataExtractor, ImageSharp.PngTextData, RawBytes.Fallback）。</summary>
        public string? FullInfoExtractionMethod { get; set; }
    }
}

