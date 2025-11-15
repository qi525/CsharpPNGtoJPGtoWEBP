using System;
using System.IO;
using SixLabors.ImageSharp;

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
        /// 判断指定路径的图片文件能否被 ImageSharp 成功加载。
        /// 常用于验证图片文件是否损坏或格式受支持。
        /// </summary>
        /// <param name="path">图片文件路径</param>
        /// <returns>可加载且宽高大于0返回 true，否则返回 false</returns>
        public static bool IsImageLoadable(string path)
        {
            try
            {
                using var img = Image.Load(path);
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
                using var src = Image.Load(sourcePath);
                using var dst = Image.Load(destPath);
                return src.Width == dst.Width && src.Height == dst.Height;
            }
            catch
            {
                return false;
            }
        }
    }
}
