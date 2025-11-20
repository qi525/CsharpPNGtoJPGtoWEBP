using System;
using System.IO;
using System.Threading;

namespace ImageInfo.Services
{
    /// <summary>
    /// 文件时间戳处理服务（读→写→验证流程，容差 ±2 秒）。
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
        /// 将时间戳写入目标文件（仅 LastWriteTimeUtc，创建时间受平台限制）。
        /// </summary>
        public static void WriteFileTimes(string destPath, DateTime createdUtc, DateTime modifiedUtc)
        {
            try
            {
                if (!File.Exists(destPath))
                    return;
                File.SetLastWriteTimeUtc(destPath, modifiedUtc);
                Thread.Sleep(100);
                _ = createdUtc;
            }
            catch { }
        }

        /// <summary>
        /// 验证时间戳是否成功写入（容差 ±2 秒）。
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


    }
}
