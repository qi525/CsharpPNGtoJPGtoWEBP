using System;
using System.IO;
using System.Threading;

namespace ImageInfo.Services
{
    /// <summary>
    /// 处理文件时间戳的服务。实现读→写→验证三步流程。
    /// 用于将源文件的创建/修改时间复刻到转换后的图片。
    /// 
    /// 注意：
    /// - .NET Framework 只提供 SetLastWriteTimeUtc，不提供原生 SetCreationTimeUtc
    /// - 创建时间在 Windows 上需要 P/Invoke；在 Linux/macOS 上无法设置
    /// - 验证步骤允许 ±2 秒的时间差异以应对操作系统延迟
    /// </summary>
    public static class FileTimeService
    {
        /// <summary>
        /// 【步骤 1：读】读取源文件的创建和修改时间。
        /// </summary>
        public static (DateTime CreatedUtc, DateTime ModifiedUtc) ReadFileTimes(string sourcePath)
        {
            try
            {
                var createdUtc = File.GetCreationTimeUtc(sourcePath);
                var modifiedUtc = File.GetLastWriteTimeUtc(sourcePath);
                return (createdUtc, modifiedUtc);
            }
            catch
            {
                // 读取失败时返回当前时间
                return (DateTime.UtcNow, DateTime.UtcNow);
            }
        }

        /// <summary>
        /// 【步骤 2：写】将时间戳写入到目标文件。
        /// 仅支持修改时间（LastWriteTimeUtc），创建时间设置受平台限制。
        /// </summary>
        public static void WriteFileTimes(string destPath, DateTime createdUtc, DateTime modifiedUtc)
        {
            try
            {
                if (!File.Exists(destPath))
                    return;

                // 设置修改时间（LastWriteTimeUtc）
                File.SetLastWriteTimeUtc(destPath, modifiedUtc);
                
                // 小延迟确保操作系统完全应用更改
                Thread.Sleep(100);
                
                // 注：创建时间（CreatedUtc）在 .NET 中无原生支持
                // Windows: 需要 P/Invoke（System.Runtime.InteropServices）
                // Linux/macOS: 文件系统不支持创建时间概念（只有访问/修改时间）
                // 这里作为占位符，可在未来扩展
                _ = createdUtc; // 避免编译器警告
            }
            catch
            {
                // 写入失败时继续，不中断流程
                // 此处可添加日志记录以便诊断
            }
        }

        /// <summary>
        /// 【步骤 3：验证】验证时间戳是否成功写入。
        /// 检查实际修改时间是否与预期值在容差范围内（±2秒）。
        /// </summary>
        public static bool VerifyFileTimes(string destPath, DateTime expectedModifiedUtc)
        {
            try
            {
                if (!File.Exists(destPath))
                    return false;

                var actualModifiedUtc = File.GetLastWriteTimeUtc(destPath);
                
                // 允许小的时间差异（±2 秒）以应对操作系统延迟和精度限制
                var diff = Math.Abs((actualModifiedUtc - expectedModifiedUtc).TotalSeconds);
                return diff < 2;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试通过 P/Invoke 设置文件创建时间（仅 Windows 支持）。
        /// 此方法为可选功能，失败时不中断主流程。
        /// </summary>
        public static void TrySetCreationTime(string filePath, DateTime createdUtc)
        {
            // 此功能需要平台检测和 P/Invoke，留作未来扩展
            // 实现参考：
            // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            // {
            //     // Windows P/Invoke 调用 SetFileTime
            // }
        }
    }
}
