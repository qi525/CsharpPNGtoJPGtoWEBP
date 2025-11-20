using System;
using System.IO;
using ImageMagick;

namespace ImageInfo.Services
{
    /// <summary>
    /// 验证工具：文件存在性、图片可加载性、转换一致性检查。
    /// </summary>
    public static class ValidationService
    {
        /// <summary>检查文件是否存在。</summary>
        public static bool FileExists(string path) => File.Exists(path);

        /// <summary>判断图片是否可被 Magick.NET 成功加载（宽高 > 0）。</summary>
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

        /// <summary>验证转换结果：目标存在、可加载、尺寸一致。</summary>
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

        /// <summary>验证输出文件目录层级与源文件层级是否一致。</summary>
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

        /// <summary>计算文件相对于根目录的相对路径（不含文件名）。</summary>
        private static string GetRelativeDirectoryPath(string rootPath, string filePath)
        {
            var rootAbs = Path.GetFullPath(rootPath);
            var fileAbs = Path.GetFullPath(filePath);
            var fileDir = Path.GetDirectoryName(fileAbs) ?? string.Empty;
            if (!fileDir.StartsWith(rootAbs, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            if (string.Equals(fileDir, rootAbs, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return fileDir.Substring(rootAbs.Length).TrimStart(Path.DirectorySeparatorChar);
        }
    }
}
