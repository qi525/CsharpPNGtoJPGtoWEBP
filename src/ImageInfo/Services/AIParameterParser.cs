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
        /// 
        /// 数据格式说明：
        /// 1. parameters: [正向提示词] - "parameters:"后直接跟正向提示词（可能多行），提示词本身不含参数含义
        /// 2. Negative prompt: [负向提示词] - 负向提示词标记
        /// 3. Steps: ..., Sampler: ..., ... - 参数行（逗号分隔的键值对）
        /// 
        /// 特殊情况：某些JPEG/WEBP的元数据中，全部内容在一块，可能没有明确的 "parameters:" 标记
        /// 此时需要通过识别 "Negative prompt:" 来反向确定正向提示词
        /// 
        /// 示例：
        /// parameters: artlist:betabeet,(artist:konya karasue:0.8),masterpiece,1girl,solo
        /// Negative prompt: NSFW,lowres,bad anatomy
        /// Steps: 28, Sampler: DPM++ 2M, CFG scale: 6, Seed: 127923918, Size: 832x1264
        /// </summary>
        private static void ParseStableDiffusionFormat(string fullInfo, AIMetadata metadata)
        {
            var lines = fullInfo.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            // 按照数据块类型分类处理
            ExtractPromptBlock(lines, metadata);
            ExtractNegativePromptBlock(lines, metadata);
            ExtractParameterBlock(lines, metadata);
            
            // 如果 Prompt 仍然为空，尝试从 FullInfo 的开头提取（某些格式将Prompt放在最前面）
            if (string.IsNullOrEmpty(metadata.Prompt) && !string.IsNullOrEmpty(fullInfo))
            {
                // 查找第一个明确的标记
                var negIdx = fullInfo.IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase);
                var stepsIdx = fullInfo.IndexOf("Steps:", StringComparison.OrdinalIgnoreCase);
                
                if (negIdx > 0)
                {
                    // 从开头提取到 "Negative prompt:" 之前的内容
                    var promptContent = fullInfo.Substring(0, negIdx).Trim();
                    if (!string.IsNullOrWhiteSpace(promptContent))
                    {
                        metadata.Prompt = promptContent;
                    }
                }
            }
        }

        /// <summary>
        /// 提取正向提示词块（从 "parameters:" 开始，直到 "Negative prompt:"）
        /// </summary>
        private static void ExtractPromptBlock(string[] lines, AIMetadata metadata)
        {
            int startIdx = -1;
            int endIdx = -1;

            // 找到 "parameters:" 的行索引
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("parameters:", StringComparison.OrdinalIgnoreCase))
                {
                    startIdx = i;
                    break;
                }
            }

            if (startIdx < 0)
                return;

            // 找到 "Negative prompt:" 的行索引
            for (int i = startIdx + 1; i < lines.Length; i++)
            {
                if (lines[i].Trim().IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    endIdx = i;
                    break;
                }
            }

            if (endIdx < 0)
                endIdx = lines.Length;

            // 提取 "parameters:" 后的内容
            var promptLines = new List<string>();
            var firstLine = lines[startIdx].Trim();
            var colonIdx = firstLine.IndexOf(':');
            if (colonIdx >= 0)
            {
                var promptStart = firstLine.Substring(colonIdx + 1).Trim();
                if (!string.IsNullOrWhiteSpace(promptStart))
                    promptLines.Add(promptStart);
            }

            // 收集中间的所有行作为正向提示词的延续
            for (int i = startIdx + 1; i < endIdx; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line))
                    promptLines.Add(line);
            }

            if (promptLines.Count > 0 && string.IsNullOrEmpty(metadata.Prompt))
                metadata.Prompt = string.Join("\n", promptLines).Trim();
        }

        /// <summary>
        /// 提取负向提示词块（从 "Negative prompt:" 开始，直到 "Steps:" 或 "Sampler:"）
        /// </summary>
        private static void ExtractNegativePromptBlock(string[] lines, AIMetadata metadata)
        {
            int startIdx = -1;
            int endIdx = -1;

            // 找到 "Negative prompt:" 的行索引
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().IndexOf("Negative prompt", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    startIdx = i;
                    break;
                }
            }

            if (startIdx < 0)
                return;

            // 找到参数行（Steps: 或 Sampler:）的行索引
            for (int i = startIdx + 1; i < lines.Length; i++)
            {
                if (lines[i].Trim().IndexOf("Steps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    lines[i].Trim().IndexOf("Sampler", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    endIdx = i;
                    break;
                }
            }

            if (endIdx < 0)
                endIdx = lines.Length;

            // 提取 "Negative prompt:" 后的内容
            var negPromptLines = new List<string>();
            var firstLine = lines[startIdx].Trim();
            var colonIdx = firstLine.IndexOf(':');
            if (colonIdx >= 0)
            {
                var negPromptStart = firstLine.Substring(colonIdx + 1).Trim();
                if (!string.IsNullOrWhiteSpace(negPromptStart))
                    negPromptLines.Add(negPromptStart);
            }

            // 收集中间的所有行作为负向提示词的延续
            for (int i = startIdx + 1; i < endIdx; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line))
                    negPromptLines.Add(line);
            }

            if (negPromptLines.Count > 0 && string.IsNullOrEmpty(metadata.NegativePrompt))
                metadata.NegativePrompt = string.Join("\n", negPromptLines).Trim();
        }

        /// <summary>
        /// 提取参数块（包含 "Steps:" 或 "Sampler:" 的行）
        /// </summary>
        private static void ExtractParameterBlock(string[] lines, AIMetadata metadata)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.IndexOf("Steps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    trimmed.IndexOf("Sampler", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ParseParameterLine(trimmed, metadata);
                    break;  // 参数行通常只有一行
                }
            }
        }

        /// <summary>
        /// 解析参数行（逗号分隔的键值对）。
        /// 
        /// 参数行格式说明：
        /// - 包含 "Steps:" 或 "Sampler:" 标记，表示这是真正的参数行
        /// - 参数以逗号分隔，每个参数是 "Key: Value" 的形式
        /// - 通常位于负向提示词之后
        /// 
        /// 示例：Steps: 28, Sampler: DPM++ 2M, CFG scale: 6, Seed: 127923918, Size: 832x1264
        /// </summary>
        private static void ParseParameterLine(string paramLine, AIMetadata metadata)
        {
            // 按逗号分割参数
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
        /// 关键字段（Prompt、Model、Seed、Sampler）分别提取，
        /// 所有其他参数完整地存储在 OtherInfo 中（逗号分隔的键值对）。
        /// </summary>
        private static void AssignParameterValue(string key, string value, AIMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            // 移除多余的引号和空格
            value = value.Trim('"', '\'').Trim();

            // 定义关键字段的匹配规则
            AssignKeyField(key, value, metadata);
            
            // 所有参数都保存到 OtherInfo
            AppendToOtherInfo(key, value, metadata);
        }

        /// <summary>
        /// 提取关键字段到对应的属性
        /// </summary>
        private static void AssignKeyField(string key, string value, AIMetadata metadata)
        {
            switch (key)
            {
                case "prompt" or "positive prompt" or "parameters":
                    if (string.IsNullOrEmpty(metadata.Prompt))
                        metadata.Prompt = value;
                    break;

                case "negative prompt":
                    if (string.IsNullOrEmpty(metadata.NegativePrompt))
                        metadata.NegativePrompt = value;
                    break;

                case "model" or "ckpt" or "checkpoint" or "ckpt name":
                    if (string.IsNullOrEmpty(metadata.Model))
                        metadata.Model = value;
                    break;

                case "model hash" or "model_hash":
                    if (string.IsNullOrEmpty(metadata.ModelHash))
                        metadata.ModelHash = value;
                    break;

                case "seed":
                    if (string.IsNullOrEmpty(metadata.Seed))
                        metadata.Seed = value;
                    break;

                case "sampler" or "scheduler" or "sampler name":
                    if (string.IsNullOrEmpty(metadata.Sampler))
                        metadata.Sampler = value;
                    break;

                default:
                    // 其他关键字也需要检查是否包含 "model"
                    if (key.Contains("model", StringComparison.OrdinalIgnoreCase) && 
                        string.IsNullOrEmpty(metadata.Model))
                        metadata.Model = value;
                    break;
            }
        }

        /// <summary>
        /// 将参数添加到 OtherInfo（避免重复）
        /// </summary>
        private static void AppendToOtherInfo(string key, string value, AIMetadata metadata)
        {
            string paramEntry = $"{key}: {value}";
            
            if (string.IsNullOrEmpty(metadata.OtherInfo))
            {
                metadata.OtherInfo = paramEntry;
            }
            else if (!metadata.OtherInfo.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                metadata.OtherInfo += ", " + paramEntry;
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
