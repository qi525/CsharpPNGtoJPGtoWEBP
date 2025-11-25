using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageInfo.Services;

/// <summary>
/// 文件名解析服务
/// 
/// 简化设计：文件名格式为 原名 + 后缀 (___tag___ 等) + 格式(.jpg/.png/.webp)
/// 由于后缀部分(___tag___ 或 @@@评分xx)是算法生成的，重点是提取原名和格式
/// 
/// 示例:
///   输入: "00000-2365214977___blue_archive___whip___mari___track___commentaries___kit___aid___archive___milkshakework___highleg___dominatrix@@@评分88.jpg"
///   返回:
///     - OriginalName: "00000-2365214977"
///     - Extension: ".jpg"
///     - Suffix: "___blue_archive___whip___...___dominatrix@@@评分88" (完整后缀部分，包括所有标签和评分)
/// </summary>
public class FilenameParser
{
    /// <summary>
    /// 文件名解析结果数据结构
    /// </summary>
    public class FilenameParseResult
    {
        /// <summary>
        /// 原始文件名 (核心内容，不包含任何后缀和扩展名)
        /// </summary>
        public string OriginalName { get; set; } = string.Empty;

        /// <summary>
        /// 文件扩展名 (包括点号，例如 .jpg, .png, .webp)
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// 完整后缀部分 (所有 ___ 和 @@@ 之间的内容)
        /// 例如: "___blue_archive___whip___mari@@@评分88"
        /// </summary>
        public string Suffix { get; set; } = string.Empty;

        /// <summary>
        /// 原始完整文件名 (用于追踪)
        /// </summary>
        public string RawFilename { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功解析
        /// </summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>
        /// 错误信息 (解析失败时)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 重建文件名 (原名 + 后缀 + 扩展名)
        /// </summary>
        public string RebuiltFilename
        {
            get
            {
                if (!IsSuccess)
                    return RawFilename;

                return OriginalName + Suffix + Extension;
            }
        }

        public override string ToString()
        {
            if (!IsSuccess)
                return $"[解析失败] {ErrorMessage}";

            return $"原名: {OriginalName}, 扩展名: {Extension}, 后缀: {Suffix}";
        }
    }

    /// <summary>
    /// 解析文件名
    /// </summary>
    /// <param name="filename">完整的文件名 (包含扩展名)</param>
    /// <returns>解析结果</returns>
    public static FilenameParseResult ParseFilename(string filename)
    {
        var result = new FilenameParseResult { RawFilename = filename };

        try
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                result.ErrorMessage = "文件名不能为空";
                return result;
            }

            // 1. 提取扩展名 (.jpg, .png, .webp 等)
            var lastDotIndex = filename.LastIndexOf('.');
            if (lastDotIndex < 0 || lastDotIndex == 0)
            {
                result.ErrorMessage = "文件名缺少扩展名";
                return result;
            }

            result.Extension = filename.Substring(lastDotIndex);
            var filenameWithoutExt = filename.Substring(0, lastDotIndex);

            // 2. 识别后缀部分开始的位置
            // 后缀通常以 ___ 或 @@@ 开头
            // 查找第一次出现 ___ 或 @@@ 的位置
            int suffixStart = -1;
            
            int underscorePos = filenameWithoutExt.IndexOf("___");
            int scorePos = filenameWithoutExt.IndexOf("@@@");

            if (underscorePos >= 0 && scorePos >= 0)
            {
                // 两个都找到，取较小的位置
                suffixStart = Math.Min(underscorePos, scorePos);
            }
            else if (underscorePos >= 0)
            {
                // 只找到 ___
                suffixStart = underscorePos;
            }
            else if (scorePos >= 0)
            {
                // 只找到 @@@
                suffixStart = scorePos;
            }
            // 如果都没找到，suffixStart 保持 -1

            // 3. 分割原名和后缀
            if (suffixStart > 0)
            {
                result.OriginalName = filenameWithoutExt.Substring(0, suffixStart);
                result.Suffix = filenameWithoutExt.Substring(suffixStart);
            }
            else if (suffixStart == 0)
            {
                // 后缀从开头开始，这是异常情况
                result.ErrorMessage = "文件名格式异常：找不到有效的原始名称";
                return result;
            }
            else
            {
                // 没有后缀，整个就是原始名称
                result.OriginalName = filenameWithoutExt;
                result.Suffix = string.Empty;
            }

            // 4. 验证解析结果
            if (string.IsNullOrWhiteSpace(result.OriginalName))
            {
                result.ErrorMessage = "解析后原始名称为空";
                return result;
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"解析异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 从文件路径解析文件名
    /// </summary>
    /// <param name="filePath">完整的文件路径</param>
    /// <returns>解析结果</returns>
    public static FilenameParseResult ParseFilenamePath(string filePath)
    {
        try
        {
            var filename = System.IO.Path.GetFileName(filePath);
            return ParseFilename(filename);
        }
        catch (Exception ex)
        {
            return new FilenameParseResult
            {
                RawFilename = filePath,
                ErrorMessage = $"路径处理异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 提取原始名称 (便利方法)
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>原始名称，如果解析失败则返回 null</returns>
    public static string? GetOriginalName(string filename)
    {
        var result = ParseFilename(filename);
        return result.IsSuccess ? result.OriginalName : null;
    }

    /// <summary>
    /// 提取扩展名 (便利方法)
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>扩展名，如果解析失败则返回 null</returns>
    public static string? GetExtension(string filename)
    {
        var result = ParseFilename(filename);
        return result.IsSuccess ? result.Extension : null;
    }

    /// <summary>
    /// 提取后缀部分 (便利方法)
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>后缀部分，如果不存在或解析失败则返回空字符串</returns>
    public static string GetSuffix(string filename)
    {
        var result = ParseFilename(filename);
        return result.IsSuccess ? result.Suffix : string.Empty;
    }
}
