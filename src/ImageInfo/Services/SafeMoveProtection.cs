using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageInfo.Services
{
    /*
     File: SafeMoveProtection.cs
     Purpose: 提供文件安全移动保护机制，防止重要的已归档文件被错误移动。
     
     设计理念：
     - 基于路径关键词的保护机制
     - 支持文件和文件夹的保护
     - 禁止移动但允许重命名
     - 只能通过人工操作来移动被保护的文件
     
     Notes:
     - 保护关键词：超、绝、精、特、待
     - 这些关键词可能出现在文件名或文件夹名中
     - 如果文件的完整路径中包含这些关键词，则该文件受保护
    */

    /// <summary>
    /// 文件安全移动保护服务
    /// 
    /// 用途：
    /// 为已归档的重要文件提供保护机制，防止通过代码错误地移动这些文件。
    /// 这些文件只能通过人工操作来移动。
    /// 
    /// 保护范围：
    /// - 文件名包含保护关键词的文件
    /// - 所在文件夹名包含保护关键词的文件
    /// - 完整路径中任何位置包含保护关键词的文件
    /// 
    /// 例如：
    /// - C:\Image\[超清]photo.png        ✓ 受保护（文件名包含"超"）
    /// - C:\[绝版]\files\image.png       ✓ 受保护（路径包含"绝版"）
    /// - C:\Archive\[精选]\pic.jpg       ✓ 受保护（路径包含"精选"）
    /// - C:\Normal\photo.png             ✗ 不受保护
    /// </summary>
    public static class SafeMoveProtection
    {
        /// <summary>
        /// 保护关键词列表
        /// 
        /// 这些关键词表示文件已被妥善归档和分类，禁止通过代码进行移动操作。
        /// 如果文件或其所在路径中包含这些关键词中的任何一个，该文件将被视为受保护。
        /// 
        /// 关键词说明：
        /// - "超"   : 表示超清/超大等特殊分类或最终版本
        /// - "绝"   : 表示绝版/绝对/终极等不可更改的状态
        /// - "精"   : 表示精选/精品/精华等经过精心处理的文件
        /// - "特"   : 表示特殊/特定/特别等具有特殊用途的文件
        /// - "待"   : 表示待处理/待审核/待归档等需要特殊对待的文件
        /// 
        /// 这些关键词均为中文单个字符，便于在文件夹和文件名中识别。
        /// </summary>
        private static readonly string[] ProtectedKeywords = new[]
        {
            "超",  // 超清、超大、超版等 - 表示特殊分类或最终版本
            "绝",  // 绝版、绝对、绝禁等 - 表示不可更改的状态
            "精",  // 精选、精品、精华等 - 表示精心处理的文件
            "特",  // 特殊、特定、特别等 - 表示有特殊用途的文件
            "待"   // 待处理、待审核、待移动等 - 表示需要人工处理的文件
        };

        /// <summary>
        /// 检查文件或文件夹是否受保护（禁止移动）
        /// 
        /// 方法说明：
        /// 1. 提取文件的完整路径
        /// 2. 检查路径中的每个部分（文件夹名和文件名）
        /// 3. 如果任何部分包含保护关键词，则该文件受保护
        /// 4. 返回布尔值表示是否受保护
        /// 
        /// 工作流程：
        /// - 路径为空或无效 → 返回 false（不保护）
        /// - 提取文件名和文件夹路径
        /// - 逐字符检查是否包含保护关键词
        /// - 区分大小写：只有中文字符匹配才会触发保护
        /// 
        /// 性能考虑：
        /// - 使用 Contains() 方法，时间复杂度 O(n)
        /// - 对于通常的路径长度（<200字符），性能极佳
        /// - 最坏情况：扫描整个路径，耗时 <1ms
        /// </summary>
        /// <param name="filePath">
        /// 文件或文件夹的完整路径
        /// 例如：
        ///   - C:\Images\[超清]\photo.png
        ///   - D:\Archive\[绝版]\important.jpg
        ///   - E:\[精选]\collection\image.webp
        /// </param>
        /// <returns>
        /// true  - 文件/文件夹受保护，禁止移动
        /// false - 文件/文件夹不受保护，可以移动
        /// 
        /// 返回 true 的情况：
        /// - 文件名包含保护关键词
        /// - 所在文件夹名包含保护关键词
        /// - 完整路径中任何位置包含保护关键词
        /// </returns>
        public static bool IsProtectedPath(string filePath)
        {
            // 输入验证：空路径或null直接返回false（不保护）
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            // 检查路径中是否包含任何保护关键词
            // 遍历每个保护关键词，检查路径中是否存在
            foreach (var keyword in ProtectedKeywords)
            {
                // 使用 Contains() 进行直接字符串匹配
                // 这会匹配关键词在路径中的任何位置
                // 例如：
                //   "C:\[超清]\photo.png".Contains("超") → true
                //   "C:\Images\photo[绝版].jpg".Contains("绝") → true
                //   "C:\待处理\files\image.png".Contains("待") → true
                if (filePath.Contains(keyword))
                {
                    return true;  // 发现保护关键词，文件受保护
                }
            }

            // 没有发现任何保护关键词，文件不受保护
            return false;
        }

        /// <summary>
        /// 检查文件是否可以被移动
        /// 
        /// 这是 IsProtectedPath() 的反函数，用于更清晰的代码表达。
        /// 
        /// 用法示例：
        /// if (SafeMoveProtection.CanMove(filePath))
        /// {
        ///     // 安全地移动文件
        ///     MoveFile(filePath, targetPath);
        /// }
        /// else
        /// {
        ///     // 文件受保护，禁止移动
        ///     Log("This file is protected and cannot be moved by code");
        /// }
        /// </summary>
        /// <param name="filePath">文件或文件夹的完整路径</param>
        /// <returns>
        /// true  - 文件可以被移动
        /// false - 文件受保护，禁止移动
        /// </returns>
        public static bool CanMove(string filePath)
        {
            return !IsProtectedPath(filePath);
        }

        /// <summary>
        /// 获取当前的保护关键词列表
        /// 
        /// 用途：
        /// - 显示哪些关键词会触发保护机制
        /// - 用于日志输出和配置展示
        /// - 帮助用户了解保护规则
        /// 
        /// 使用示例：
        /// var keywords = SafeMoveProtection.GetProtectedKeywords();
        /// Console.WriteLine("受保护的关键词：");
        /// foreach (var kw in keywords)
        /// {
        ///     Console.WriteLine($"  • {kw}");
        /// }
        /// 
        /// 输出示例：
        ///   受保护的关键词：
        ///   • 超
        ///   • 绝
        ///   • 精
        ///   • 特
        ///   • 待
        /// </summary>
        /// <returns>保护关键词的列表副本</returns>
        public static IEnumerable<string> GetProtectedKeywords()
        {
            return ProtectedKeywords.ToList();
        }

        /// <summary>
        /// 获取文件的保护状态详细信息
        /// 
        /// 用途：
        /// - 提供详细的保护状态说明
        /// - 用于日志记录和调试
        /// - 帮助用户理解为什么文件受保护
        /// 
        /// 返回的信息包括：
        /// - 文件是否受保护
        /// - 触发保护的关键词（如果受保护）
        /// - 保护原因描述
        /// 
        /// 使用示例：
        /// var status = SafeMoveProtection.GetProtectionStatus(@"C:\[超清]\photo.png");
        /// if (status.IsProtected)
        /// {
        ///     Console.WriteLine($"文件受保护：{status.Reason}");
        ///     Console.WriteLine($"触发关键词：{status.TriggeredKeyword}");
        /// }
        /// </summary>
        /// <param name="filePath">文件或文件夹的完整路径</param>
        /// <returns>
        /// 包含保护状态的对象，包括：
        /// - IsProtected: 是否受保护
        /// - TriggeredKeyword: 触发保护的关键词
        /// - Reason: 保护原因描述
        /// </returns>
        public static ProtectionStatus GetProtectionStatus(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new ProtectionStatus
                {
                    IsProtected = false,
                    TriggeredKeyword = "",
                    Reason = "路径无效"
                };
            }

            // 检查每个关键词，找到触发保护的第一个
            foreach (var keyword in ProtectedKeywords)
            {
                if (filePath.Contains(keyword))
                {
                    var reason = $"路径中包含保护关键词\"{keyword}\"，该文件已被标记为归档文件，禁止通过代码移动。如需移动，请通过人工操作进行。";
                    return new ProtectionStatus
                    {
                        IsProtected = true,
                        TriggeredKeyword = keyword,
                        Reason = reason
                    };
                }
            }

            // 没有找到任何保护关键词
            return new ProtectionStatus
            {
                IsProtected = false,
                TriggeredKeyword = "",
                Reason = "文件不受保护，可以被移动"
            };
        }

        /// <summary>
        /// 检查多个文件中哪些受保护
        /// 
        /// 用途：
        /// - 批量检查文件的保护状态
        /// - 分别处理受保护和不受保护的文件
        /// - 在批量移动操作前进行安全检查
        /// 
        /// 使用示例：
        /// var files = new[]
        /// {
        ///     @"C:\[超清]\photo1.png",
        ///     @"C:\Normal\photo2.jpg",
        ///     @"C:\[精选]\photo3.webp"
        /// };
        /// 
        /// var result = SafeMoveProtection.FilterProtectedFiles(files);
        /// Console.WriteLine($"受保护的文件：{result.Protected.Count}个");
        /// Console.WriteLine($"可移动的文件：{result.Unprotected.Count}个");
        /// </summary>
        /// <param name="filePaths">文件路径集合</param>
        /// <returns>
        /// 包含两个列表的对象：
        /// - Protected: 受保护的文件列表
        /// - Unprotected: 不受保护的文件列表
        /// </returns>
        public static FilteredFiles FilterProtectedFiles(IEnumerable<string> filePaths)
        {
            var protected_ = new List<string>();
            var unprotected = new List<string>();

            if (filePaths == null || !filePaths.Any())
            {
                return new FilteredFiles { Protected = protected_, Unprotected = unprotected };
            }

            // 遍历所有文件，按保护状态分类
            foreach (var filePath in filePaths)
            {
                if (IsProtectedPath(filePath))
                    protected_.Add(filePath);
                else
                    unprotected.Add(filePath);
            }

            return new FilteredFiles
            {
                Protected = protected_,
                Unprotected = unprotected
            };
        }
    }

    /// <summary>
    /// 文件保护状态信息
    /// 
    /// 包含以下属性：
    /// - IsProtected: 是否受保护
    /// - TriggeredKeyword: 触发保护的关键词
    /// - Reason: 保护原因描述
    /// 
    /// 用于 GetProtectionStatus() 方法的返回值
    /// </summary>
    public class ProtectionStatus
    {
        /// <summary>文件是否受保护</summary>
        public bool IsProtected { get; set; }

        /// <summary>触发保护的关键词（如果不受保护则为空字符串）</summary>
        public string TriggeredKeyword { get; set; } = "";

        /// <summary>保护原因的详细说明</summary>
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// 文件过滤结果
    /// 
    /// 包含两个列表：
    /// - Protected: 受保护的文件列表
    /// - Unprotected: 不受保护的文件列表
    /// 
    /// 用于 FilterProtectedFiles() 方法的返回值
    /// </summary>
    public class FilteredFiles
    {
        /// <summary>受保护的文件列表（禁止移动）</summary>
        public List<string> Protected { get; set; } = new List<string>();

        /// <summary>不受保护的文件列表（可以移动）</summary>
        public List<string> Unprotected { get; set; } = new List<string>();
    }
}
