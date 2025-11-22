using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageInfo.Services
{
    /*
     File: FileScanner.cs
     Purpose: 提供递归扫描目录获取图片文件路径的简单工具。
     Notes:
     - 返回的是文件的完整路径字符串集合。
     - 仅按文件扩展名过滤常见图片格式。
    */

    /// <summary>
    /// 文件扫描工具，递归查找指定目录下所有常见图片文件。
    /// 用途：被上层逻辑（如 Program 或测试）调用以获取待处理图片的列表。
    /// </summary>
    public static class FileScanner
    {
        /// <summary>
        /// 支持的图片扩展名列表（小写）。可以根据需要扩展。
        /// </summary>
        private static readonly string[] ImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tiff" };

        /// <summary>
        /// 需要跳过的文件夹名称列表（不区分大小写）。
        /// 例如：.bf, .preview, .thumbnails, __pycache__ 等
        /// 如果文件路径的任何部分匹配这些名称，该文件会被过滤掉。
        /// </summary>
        private static readonly string[] ExcludedFolderNames = new[] 
        { 
            ".bf", 
            ".preview", 
            ".thumbnails",
            ".cache",
            "__pycache__",
            ".git",
            ".svn",
            "node_modules"
        };

        /// <summary>
        /// 检查文件路径是否包含需要跳过的文件夹。
        /// 例如：C:\stable-diffusion-webui\outputs\txt2img-images\.bf\.preview\ff\image.png
        /// 如果路径中包含 .bf 或 .preview，返回 true（表示应该跳过）
        /// </summary>
        /// <param name="filePath">完整文件路径</param>
        /// <returns>如果应该跳过返回 true，否则返回 false</returns>
        private static bool ShouldSkipFile(string filePath)
        {
            // 将路径分解为各个部分
            var parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // 检查是否有任何部分匹配排除的文件夹名称
            foreach (var part in parts)
            {
                if (ExcludedFolderNames.Contains(part, System.StringComparer.OrdinalIgnoreCase))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// 递归扫描 <paramref name="root"/> 目录下所有图片文件，跳过指定的文件夹。
        /// 返回值为文件完整路径集合；如果目录不存在则返回空集合。
        /// </summary>
        /// <param name="root">要扫描的根目录</param>
        /// <returns>所有符合条件的图片文件的完整路径集合</returns>
        public static IEnumerable<string> GetImageFiles(string root)
        {
            if (!Directory.Exists(root))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => !ShouldSkipFile(f))  // 先过滤掉包含排除文件夹的文件
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        }

        /// <summary>
        /// 获取扫描时被跳过的文件夹名称列表。
        /// 用于配置和日志输出。
        /// </summary>
        /// <returns>被跳过的文件夹名称列表</returns>
        public static IEnumerable<string> GetExcludedFolders()
        {
            return ExcludedFolderNames.ToList();
        }
    }
}
