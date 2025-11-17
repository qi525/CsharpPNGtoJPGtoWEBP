using System;
using System.IO;
using ImageMagick;

namespace ImageInfo.Services
{
    /*
     File: ValidationService.cs
     Purpose: 提供用于单元测试与验证流程的简单检查函数。
     Functions:
      - FileExists: 检查文件存在性。
      - IsImageLoadable: 尝试使用 ImageSharp 加载图像以检测文件完整性。
      - ValidateConversion: 验证源与目标图片存在且尺寸一致。
    */

    /// <summary>
    /// 提供简单的验证函数：文件存在、可加载、以及转换后基础一致性校验（存在且可读取且尺寸一致）。
    /// 这些函数便于在自动化测试中断言转换与写入操作的正确性。
    /// </summary>
    public static class ValidationService
    {
        /// <summary>
        /// 检查指定路径的文件是否存在。
        /// </summary>
        /// <param name="path">要检查的文件路径</param>
        /// <returns>存在返回 true，否则返回 false</returns>
        public static bool FileExists(string path) => File.Exists(path);

        /// <summary>
        /// 判断指定路径的图片文件能否被 Magick.NET 成功加载。
        /// 常用于验证图片文件是否损坏或格式受支持。
        /// </summary>
        /// <param name="path">图片文件路径</param>
        /// <returns>可加载且宽高大于0返回 true，否则返回 false</returns>
        public static bool IsImageLoadable(string path)
        {
            try
            {
                using var img = new MagickImage(path);
                return img != null && img.Width > 0 && img.Height > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证图片格式转换的结果：
        /// 1. 目标文件存在；
        /// 2. 目标图片能被 ImageSharp 成功加载；
        /// 3. 源图片与目标图片的宽高完全一致。
        /// 常用于自动化测试，确保图片转换后未损坏且尺寸未变。
        /// </summary>
        /// <param name="sourcePath">原始图片路径</param>
        /// <param name="destPath">转换后图片路径</param>
        /// <returns>验证全部通过返回 true，否则返回 false</returns>
        public static bool ValidateConversion(string sourcePath, string destPath)
        {
            if (!FileExists(destPath))
                return false;

            if (!IsImageLoadable(destPath))
                return false;

            try
            {
                using var src = new MagickImage(sourcePath);
                using var dst = new MagickImage(destPath);
                return src.Width == dst.Width && src.Height == dst.Height;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证输出文件夹的层级结构是否与源文件夹的相对层级一致。
        /// 用于检查转换后的文件是否被正确地组织到了预期的目录结构中。
        /// 
        /// 示例：
        ///   源文件: C:\\test\\二层\\三层\\photo.png
        ///   源根目录: C:\\test
        ///   输出文件: C:\\PNG转JPG\\test\\二层\\三层\\photo.jpg
        ///   此函数验证输出文件相对于 "C:\\PNG转JPG\\test" 的路径是否为 "二层\\三层"
        /// </summary>
        /// <param name="sourceFilePath">源文件的完整路径</param>
        /// <param name="sourceRootFolder">源根目录路径（从此目录开始计算相对路径）</param>
        /// <param name="destFilePath">输出文件的完整路径</param>
        /// <param name="destRootFolder">输出根目录路径（从此目录开始计算相对路径）</param>
        /// <returns>如果相对层级结构匹配返回 true，否则返回 false</returns>
        public static bool VerifyOutputDirectoryStructure(string sourceFilePath, string sourceRootFolder, 
            string destFilePath, string destRootFolder)
        {
            try
            {
                // 标准化路径
                var srcAbsPath = Path.GetFullPath(sourceFilePath);
                var srcRootAbsPath = Path.GetFullPath(sourceRootFolder);
                var dstAbsPath = Path.GetFullPath(destFilePath);
                var dstRootAbsPath = Path.GetFullPath(destRootFolder);

                // 计算源文件相对于源根目录的相对路径（不含文件名，仅目录部分）
                var srcRelativeDir = GetRelativeDirectoryPath(srcRootAbsPath, srcAbsPath);
                
                // 计算输出文件相对于输出根目录的相对路径（不含文件名，仅目录部分）
                var dstRelativeDir = GetRelativeDirectoryPath(dstRootAbsPath, dstAbsPath);

                // 比较相对目录部分是否一致
                return string.Equals(srcRelativeDir, dstRelativeDir, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 计算文件相对于根目录的相对目录路径（不含文件名）。
        /// </summary>
        /// <param name="rootPath">根目录路径</param>
        /// <param name="filePath">文件完整路径</param>
        /// <returns>相对目录路径，如果文件不在根目录下则返回空字符串</returns>
        private static string GetRelativeDirectoryPath(string rootPath, string filePath)
        {
            var rootAbs = Path.GetFullPath(rootPath);
            var fileAbs = Path.GetFullPath(filePath);
            var fileDir = Path.GetDirectoryName(fileAbs) ?? string.Empty;

            // 检查文件目录是否在根目录下
            if (!fileDir.StartsWith(rootAbs, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            // 如果文件直接在根目录下，返回空字符串
            if (string.Equals(fileDir, rootAbs, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            // 计算相对路径
            var relativeDir = fileDir.Substring(rootAbs.Length).TrimStart(Path.DirectorySeparatorChar);
            return relativeDir;
        }
    }
}
