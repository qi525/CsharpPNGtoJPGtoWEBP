using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ImageInfo.Services
{
    /// <summary>
    /// 提示词清洗服务：从文件读取停用词，清洗提示词并提取核心词
    /// 停用词文件格式：每行一个整体词块，跳过空行和#注释行
    /// </summary>
    public static class PromptCleanerService
    {
        private static readonly Lazy<List<string>> _stopWords =
            new Lazy<List<string>>(LoadStopWords, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 清洗提示词：去掉换行符 → 替换停用词块 → 规范化空白
        /// </summary>
        public static string CleanPositivePrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                return string.Empty;

            var stopWords = _stopWords.Value;
            if (stopWords == null || stopWords.Count == 0)
                return prompt;

            // 去掉所有换行符
            var cleaned = Regex.Replace(prompt, @"[\r\n]+", "");

            // 逐个替换停用词块
            foreach (var word in stopWords)
            {
                if (!string.IsNullOrEmpty(word))
                    cleaned = cleaned.Replace(word, " ", StringComparison.OrdinalIgnoreCase);
            }

            // 规范化空白：多个空白→单个空白，去掉首尾
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            return string.IsNullOrEmpty(cleaned) ? "核心词为空" : cleaned;
        }

        /// <summary>
        /// 从文件加载停用词列表（线程安全懒加载，仅加载一次）
        /// </summary>
        private static List<string> LoadStopWords()
        {
            try
            {
                var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                var stopWordsPath = Path.Combine(basePath, "Resources", "POSITIVE_PROMPT_STOP_WORDS.txt");

                if (!File.Exists(stopWordsPath))
                {
                    Console.WriteLine($"警告: 停用词文件不存在");
                    return new List<string>();
                }

                var stopWords = new List<string>();
                foreach (var line in File.ReadLines(stopWordsPath))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                        stopWords.Add(trimmed);
                }

                Console.WriteLine($"[PromptCleanerService] 已加载 {stopWords.Count} 个停用词块");
                return stopWords;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: 读取停用词文件失败 - {ex.Message}");
                return new List<string>();
            }
        }
    }
}
