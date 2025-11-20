using System;
using System.Runtime.InteropServices;
using System.IO;

namespace ImageInfo.Services
{
    /// <summary>
    /// 文件创建时间服务（跨平台支持）。
    /// </summary>
    public static class CreationTimeService
    {
        /// <summary>
        /// 设置文件的创建时间（Windows 优化，其他平台使用标准方法）。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="creationTime">要设置的创建时间</param>
        /// <returns>设置成功返回 true，失败返回 false</returns>
        public static bool SetCreationTime(string filePath, DateTime creationTime)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return false;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return SetCreationTimeWindows(filePath, creationTime);
                }
                else
                {
                    // Linux/macOS：使用标准方法（通常效果有限）
                    File.SetCreationTime(filePath, creationTime);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set creation time for {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取文件的创建时间。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件的创建时间，获取失败返回 DateTime.MinValue</returns>
        public static DateTime GetCreationTime(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return DateTime.MinValue;

                return File.GetCreationTime(filePath);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 验证文件创建时间是否正确设置（容差 ±5 秒）。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expectedTime">期望的创建时间</param>
        /// <param name="toleranceSeconds">允许的时间偏差（秒）</param>
        /// <returns>创建时间正确返回 true，否则 false</returns>
        public static bool VerifyCreationTime(string filePath, DateTime expectedTime, int toleranceSeconds = 5)
        {
            try
            {
                DateTime actualTime = GetCreationTime(filePath);
                if (actualTime == DateTime.MinValue)
                    return false;

                TimeSpan difference = Math.Abs((actualTime - expectedTime).TotalSeconds) < toleranceSeconds
                    ? TimeSpan.FromSeconds(0)
                    : actualTime - expectedTime;

                return Math.Abs((actualTime - expectedTime).TotalSeconds) < toleranceSeconds;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Windows 平台专用实现（使用原生 File.SetCreationTime）。
        /// </summary>
        private static bool SetCreationTimeWindows(string filePath, DateTime creationTime)
        {
            try
            {
                // 直接使用 C# 原生方法替代 P/Invoke
                // 这样更可靠，不会出现文件锁定问题
                File.SetCreationTime(filePath, creationTime);
                Console.WriteLine($"Creation time set to {creationTime:yyyy-MM-dd HH:mm:ss} for {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SetCreationTimeWindows: {ex.Message}");
                return false;
            }
        }
    }
}
