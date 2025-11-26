using System;
using System.Collections.Generic;
using System.Linq;
using ImageInfo.Data;

namespace ImageInfo.Services;

/// <summary>
/// 文件名标签生成服务
/// 
/// 功能：根据关键词列表，从提示词中按顺序匹配关键词，生成标签后缀
/// 格式：用 ___ (三个下划线) 连接匹配的关键词
/// 
/// 示例：
///   关键词表: ["anime", "girl", "cute", "blue_archive"]
///   正向词: "beautiful anime girl, cute style, from blue archive"
///   结果: "anime___girl___cute___blue_archive"
/// </summary>
public class FilenameTaggerService
{
    /// <summary>
    /// 标签生成结果
    /// </summary>
    public class TaggingResult
    {
        /// <summary>
        /// 匹配到的关键词列表 (按词表顺序)
        /// </summary>
        public List<string> MatchedKeywords { get; set; } = new();

        /// <summary>
        /// 生成的标签后缀 (用 ___ 连接)
        /// 如果没有匹配到任何关键词，则为空字符串
        /// </summary>
        public string TagSuffix { get; set; } = string.Empty;

        /// <summary>
        /// 匹配数量
        /// </summary>
        public int MatchCount => MatchedKeywords.Count;

        /// <summary>
        /// 是否成功生成标签 (至少匹配到一个关键词)
        /// </summary>
        public bool HasTags => MatchedKeywords.Count > 0;

        public override string ToString()
        {
            if (!HasTags)
                return "[无匹配关键词]";

            return $"[{MatchCount}个关键词] {TagSuffix}";
        }
    }

    /// <summary>
    /// 从提示词中提取匹配的关键词
    /// </summary>
    /// <param name="promptText">提示词文本 (正向词 + 负向词的组合)</param>
    /// <param name="keywordList">关键词列表 (有序)</param>
    /// <param name="caseSensitive">是否区分大小写 (默认不区分)</param>
    /// <returns>标签生成结果</returns>
    public static TaggingResult ExtractKeywords(
        string promptText,
        List<string> keywordList,
        bool caseSensitive = false)
    {
        var result = new TaggingResult();

        if (string.IsNullOrWhiteSpace(promptText) || keywordList == null || keywordList.Count == 0)
        {
            return result;
        }

        // 预处理提示词文本
        string processedPrompt = promptText;
        if (!caseSensitive)
        {
            processedPrompt = processedPrompt.ToLowerInvariant();
        }

        // 消除可能的转义符
        processedPrompt = processedPrompt.Replace("\\", "");

        // 按顺序匹配关键词
        foreach (var keyword in keywordList)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            string searchKeyword = caseSensitive ? keyword : keyword.ToLowerInvariant();

            // 精确匹配：检查关键词是否存在于提示词中
            if (processedPrompt.Contains(searchKeyword))
            {
                result.MatchedKeywords.Add(keyword); // 保存原始大小写
            }
        }

        // 生成标签后缀
        if (result.MatchedKeywords.Count > 0)
        {
            result.TagSuffix = "___" + string.Join("___", result.MatchedKeywords);
        }

        return result;
    }

    /// <summary>
    /// 结合正向词和负向词提取关键词
    /// </summary>
    /// <param name="positivePrompt">正向提示词</param>
    /// <param name="negativePrompt">负向提示词</param>
    /// <param name="keywordList">关键词列表</param>
    /// <param name="caseSensitive">是否区分大小写</param>
    /// <returns>标签生成结果</returns>
    public static TaggingResult ExtractKeywordsFromPrompts(
        string positivePrompt,
        string negativePrompt,
        List<string> keywordList,
        bool caseSensitive = false)
    {
        // 合并两个提示词
        var combinedPrompt = $"{positivePrompt ?? ""} {negativePrompt ?? ""}".Trim();
        return ExtractKeywords(combinedPrompt, keywordList, caseSensitive);
    }

    /// <summary>
    /// 批量提取关键词 (针对多条记录)
    /// </summary>
    /// <param name="records">记录列表，每条应包含 "正面提示词" 和 "负面提示词" 字段</param>
    /// <param name="keywordList">关键词列表</param>
    /// <returns>结果字典，键为原记录，值为标签结果</returns>
    public static Dictionary<Dictionary<string, string>, TaggingResult> ExtractKeywordsBatch(
        List<Dictionary<string, string>> records,
        List<string> keywordList)
    {
        var results = new Dictionary<Dictionary<string, string>, TaggingResult>();

        foreach (var record in records)
        {
            var positivePrompt = record.ContainsKey("正面提示词") ? record["正面提示词"] : "";
            var negativePrompt = record.ContainsKey("负面提示词") ? record["负面提示词"] : "";

            var taggingResult = ExtractKeywordsFromPrompts(
                positivePrompt,
                negativePrompt,
                keywordList,
                caseSensitive: false);

            results[record] = taggingResult;
        }

        return results;
    }

    /// <summary>
    /// 生成文件名后缀 (完整格式)
    /// </summary>
    /// <param name="positivePrompt">正向提示词</param>
    /// <param name="negativePrompt">负向提示词</param>
    /// <param name="keywordList">关键词列表</param>
    /// <param name="includeLeadingUnderscore">是否包含前导 ___</param>
    /// <returns>文件名后缀 (如 "___tag1___tag2___tag3" 或 "" 如果无匹配)</returns>
    public static string GenerateFilenameTagSuffix(
        string positivePrompt,
        string negativePrompt,
        List<string> keywordList,
        bool includeLeadingUnderscore = true)
    {
        var result = ExtractKeywordsFromPrompts(positivePrompt, negativePrompt, keywordList);

        if (!result.HasTags)
            return string.Empty;

        // TagSuffix 已经包含前导 ___，如果需要移除则去掉
        var suffix = result.TagSuffix;
        if (!includeLeadingUnderscore && suffix.StartsWith("___"))
        {
            suffix = suffix.Substring(3); // 移除前导 ___
        }

        return suffix;
    }

    /// <summary>
    /// 验证关键词列表
    /// </summary>
    /// <param name="keywordList">关键词列表</param>
    /// <returns>验证结果 (是否有效)</returns>
    public static (bool IsValid, List<string> ErrorMessages) ValidateKeywordList(List<string> keywordList)
    {
        var errors = new List<string>();

        if (keywordList == null || keywordList.Count == 0)
        {
            errors.Add("关键词列表不能为空");
            return (false, errors);
        }

        // 检查重复关键词
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();

        foreach (var keyword in keywordList)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                errors.Add("关键词不能为空或仅包含空白字符");
                continue;
            }

            if (seen.Contains(keyword))
            {
                duplicates.Add(keyword);
            }
            else
            {
                seen.Add(keyword);
            }
        }

        if (duplicates.Count > 0)
        {
            errors.Add($"发现重复关键词: {string.Join(", ", duplicates)}");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 获取默认关键词表 (来自 filename_tagger.py)
    /// </summary>
    /// <returns>默认关键词列表</returns>
    public static List<string> GetDefaultKeywordList()
    {
        return new List<string>(CustomKeywords.Keywords);
    }
}
