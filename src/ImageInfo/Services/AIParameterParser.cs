using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ImageInfo.Models;

namespace ImageInfo.Services
{
    /// <summary>
    /// AI 元数据参数提取器。
    /// 统一处理 PNG、JPEG、WebP 等不同格式中的元数据解析。
    /// 
    /// 职责：将各种格式读出的文本内容进行统一的参数解析，
    /// 提取出 Prompt、NegativePrompt、Model、Seed、Sampler 等标准字段。
    /// 
    /// 使用流程：
    /// 1. 各格式的提取器（PngMetadataExtractor、JpegMetadataExtractor 等）读取原始文本
    /// 2. 调用本类的静态方法进行参数解析
    /// 3. 返回结构化的 AIMetadata 对象
    /// </summary>
    public static class AIParameterParser
    {
        /// <summary>
        /// 从完整的信息文本中提取结构化参数。
        /// 支持多种常见格式（Stable Diffusion、ComfyUI 等）。
        /// </summary>
        /// <param name="fullInfo">原始文本信息</param>
        /// <param name="targetMetadata">目标元数据对象（用于填充字段）</param>
        public static void ParseFullInfoIntoFields(string fullInfo, AIMetadata targetMetadata)
        {
            if (string.IsNullOrWhiteSpace(fullInfo) || targetMetadata == null)
                return;

            // 尝试识别格式类型
            var format = DetectInfoFormat(fullInfo);

            switch (format)
            {
                case InfoFormat.StableDiffusionWebUI:
                    ParseStableDiffusionFormat(fullInfo, targetMetadata);
                    break;
                case InfoFormat.ComfyUI:
                    ParseComfyUIFormat(fullInfo, targetMetadata);
                    break;
                case InfoFormat.KeyValuePairs:
                    ParseKeyValueFormat(fullInfo, targetMetadata);
                    break;
                default:
                    ParseGenericFormat(fullInfo, targetMetadata);
                    break;
            }
        }

        /// <summary>
        /// 检测信息文本的格式类型。
        /// </summary>
        private static InfoFormat DetectInfoFormat(string fullInfo)
        {
            // Stable Diffusion WebUI 特征：通常以 Prompt 开头，包含 "Steps:", "Sampler:" 等参数
            if (Regex.IsMatch(fullInfo, @"(?i)Steps\s*:|Sampler\s*:|CFG scale\s*:"))
                return InfoFormat.StableDiffusionWebUI;

            // ComfyUI 特征：通常是 JSON 格式或特定的键名结构
            if (fullInfo.Contains("\"") && (fullInfo.Contains("ckpt_name") || fullInfo.Contains("seed")))
                return InfoFormat.ComfyUI;

            // 简单的键值对格式（Prompt=..., Model=... 等）
            if (Regex.IsMatch(fullInfo, @"^\w+\s*[=:]\s*.+", RegexOptions.Multiline))
                return InfoFormat.KeyValuePairs;

            return InfoFormat.Generic;
        }

        /// <summary>
        /// 解析 Stable Diffusion WebUI 格式的参数。
        /// 典型格式：
        /// prompt text here
        /// Negative prompt: negative text
        /// Steps: 20, Sampler: Euler a, CFG scale: 7.0, Seed: 123456, Size: 512x512, ...
        /// </summary>
        private static void ParseStableDiffusionFormat(string fullInfo, AIMetadata metadata)
        {
            var lines = fullInfo.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.None);

            // 第一行通常是 Prompt（没有冒号）
            if (lines.Length > 0 && !lines[0].Contains(":") && !string.IsNullOrWhiteSpace(lines[0]))
            {
                if (string.IsNullOrEmpty(metadata.Prompt))
                    metadata.Prompt = lines[0].Trim();
            }

            // 处理剩余行
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 处理 "Negative prompt:" 行
                if (line.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var colonIdx = line.IndexOf(':');
                    if (colonIdx >= 0 && string.IsNullOrEmpty(metadata.NegativePrompt))
                    {
                        metadata.NegativePrompt = line.Substring(colonIdx + 1).Trim();
                    }
                }
                // 处理参数行（通常包含多个参数用逗号分隔）
                else if (line.IndexOf("Steps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("Sampler", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // 这一行包含多个参数，需要逐个解析
                    ParseParameterLine(line, metadata);
                }
            }
        }

        /// <summary>
        /// 解析参数行（通常是逗号分隔的键值对）。
        /// 例：Steps: 20, Sampler: Euler a, CFG scale: 7.0, Seed: 123456
        /// </summary>
        private static void ParseParameterLine(string paramLine, AIMetadata metadata)
        {
            var parameters = paramLine.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in parameters)
            {
                var trimmed = param.Trim();
                var colonIdx = trimmed.IndexOf(':');

                if (colonIdx < 0)
                    continue;

                var key = trimmed.Substring(0, colonIdx).Trim().ToLowerInvariant();
                var value = trimmed.Substring(colonIdx + 1).Trim();

                AssignParameterValue(key, value, metadata);
            }
        }

        /// <summary>
        /// 解析 ComfyUI 格式的参数（可能是 JSON 或特殊格式）。
        /// ComfyUI 通常生成 JSON 格式的元数据。
        /// </summary>
        private static void ParseComfyUIFormat(string fullInfo, AIMetadata metadata)
        {
            // 简单的 JSON 键值提取（不使用完整的 JSON 解析以避免额外依赖）
            ExtractJsonFieldValues(fullInfo, metadata);
        }

        /// <summary>
        /// 从 JSON 格式的文本中提取字段值。
        /// </summary>
        private static void ExtractJsonFieldValues(string jsonText, AIMetadata metadata)
        {
            // 提取 "prompt": "..." 的值
            var promptMatch = Regex.Match(jsonText, @"""prompt\s*""?\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (promptMatch.Success && string.IsNullOrEmpty(metadata.Prompt))
                metadata.Prompt = promptMatch.Groups[1].Value;

            // 提取 "negative": "..." 或 "negative_prompt": "..." 的值
            var negativeMatch = Regex.Match(jsonText, @"""negative(?:_prompt)?\s*""?\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (negativeMatch.Success && string.IsNullOrEmpty(metadata.NegativePrompt))
                metadata.NegativePrompt = negativeMatch.Groups[1].Value;

            // 提取 "ckpt_name": "..." 或 "model": "..." 的值
            var modelMatch = Regex.Match(jsonText, @"""(?:ckpt_name|model)\s*""?\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (modelMatch.Success && string.IsNullOrEmpty(metadata.Model))
                metadata.Model = modelMatch.Groups[1].Value;

            // 提取 "seed": ... 的值
            var seedMatch = Regex.Match(jsonText, @"""seed\s*""?\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (seedMatch.Success && string.IsNullOrEmpty(metadata.Seed))
                metadata.Seed = seedMatch.Groups[1].Value;

            // 提取 "sampler": "..." 的值
            var samplerMatch = Regex.Match(jsonText, @"""sampler\s*""?\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (samplerMatch.Success && string.IsNullOrEmpty(metadata.Sampler))
                metadata.Sampler = samplerMatch.Groups[1].Value;
        }

        /// <summary>
        /// 解析键值对格式的参数。
        /// 格式：Key=Value 或 Key: Value
        /// </summary>
        private static void ParseKeyValueFormat(string fullInfo, AIMetadata metadata)
        {
            var lines = fullInfo.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 尝试按 = 或 : 分割
                var colonIdx = line.IndexOf(':');
                var equalIdx = line.IndexOf('=');

                int separatorIdx;
                if (colonIdx >= 0 && (equalIdx < 0 || colonIdx < equalIdx))
                    separatorIdx = colonIdx;
                else if (equalIdx >= 0)
                    separatorIdx = equalIdx;
                else
                    continue;

                var key = line.Substring(0, separatorIdx).Trim().ToLowerInvariant();
                var value = line.Substring(separatorIdx + 1).Trim();

                AssignParameterValue(key, value, metadata);
            }
        }

        /// <summary>
        /// 通用格式解析（尽力提取，容错性强）。
        /// </summary>
        private static void ParseGenericFormat(string fullInfo, AIMetadata metadata)
        {
            // 按行处理
            var lines = fullInfo.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.None);

            // 第一行作为 Prompt（如果不包含冒号）
            if (lines.Length > 0 && !lines[0].Contains(":") && !string.IsNullOrWhiteSpace(lines[0]))
            {
                if (string.IsNullOrEmpty(metadata.Prompt))
                    metadata.Prompt = lines[0].Trim();
            }

            // 其他行尝试解析
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var colonIdx = line.IndexOf(':');
                if (colonIdx < 0)
                    continue;

                var key = line.Substring(0, colonIdx).Trim().ToLowerInvariant();
                var value = line.Substring(colonIdx + 1).Trim();

                AssignParameterValue(key, value, metadata);
            }
        }

        /// <summary>
        /// 根据参数键名分配到对应的元数据字段。
        /// </summary>
        private static void AssignParameterValue(string key, string value, AIMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            // 移除多余的引号和空格
            value = value.Trim('"', '\'').Trim();

            if (key.Contains("prompt") && !key.Contains("negative"))
            {
                if (string.IsNullOrEmpty(metadata.Prompt))
                    metadata.Prompt = value;
            }
            else if (key.Contains("negative"))
            {
                if (string.IsNullOrEmpty(metadata.NegativePrompt))
                    metadata.NegativePrompt = value;
            }
            else if (key.Contains("model") || key.Contains("ckpt") || key.Contains("checkpoint"))
            {
                if (string.IsNullOrEmpty(metadata.Model))
                    metadata.Model = value;
            }
            else if (key.Contains("seed"))
            {
                if (string.IsNullOrEmpty(metadata.Seed))
                    metadata.Seed = value;
            }
            else if (key.Contains("sampler") || key.Contains("scheduler"))
            {
                if (string.IsNullOrEmpty(metadata.Sampler))
                    metadata.Sampler = value;
            }
            else if (key.Contains("steps") || key.Contains("cfg") || key.Contains("scale") || 
                     key.Contains("size") || key.Contains("width") || key.Contains("height"))
            {
                // 其他参数汇总到 OtherInfo
                if (string.IsNullOrEmpty(metadata.OtherInfo))
                    metadata.OtherInfo = $"{key}={value}";
                else
                    metadata.OtherInfo += $"; {key}={value}";
            }
            else
            {
                // 未分类的字段
                if (string.IsNullOrEmpty(metadata.OtherInfo))
                    metadata.OtherInfo = value;
                else if (!metadata.OtherInfo.Contains(value))
                    metadata.OtherInfo += $"; {value}";
            }
        }

        /// <summary>
        /// 参数格式类型枚举。
        /// </summary>
        private enum InfoFormat
        {
            StableDiffusionWebUI,  // SD WebUI 标准格式
            ComfyUI,               // ComfyUI JSON 格式
            KeyValuePairs,         // 简单键值对
            Generic                // 通用格式
        }
    }
}
