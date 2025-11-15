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
        /// 递归扫描 <paramref name="root"/> 目录下所有图片文件。
        /// 返回值为文件完整路径集合；如果目录不存在则返回空集合。
        /// </summary>
        /// <param name="root">要扫描的根目录</param>
        /// <returns>所有图片文件的完整路径集合</returns>
        public static IEnumerable<string> GetImageFiles(string root)
        {
            if (!Directory.Exists(root))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        }
    }
}
