using System;
using System.Runtime.InteropServices;
using System.IO;

namespace ImageInfo.Services
{
    /// <summary>
    /// 文件创建时间服务。
    /// 在 Windows 上使用 P/Invoke 调用 SetFileTime API 修改文件创建时间（ctime）。
    /// </summary>
    public static class CreationTimeService
    {
        /// <summary>
        /// 设置文件的创建时间。
        /// 
        /// 实现方式：
        /// - Windows: 使用 P/Invoke 调用 SetFileTime() 修改 ctime
        /// - 其他平台: 使用标准 File.SetCreationTime()（可能无效）
        /// 
        /// 工作流：
        /// 1. 验证文件存在
        /// 2. 调用 Windows SetFileTime API 或标准方法
        /// 3. 成功返回 true，异常返回 false
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
        /// 验证文件创建时间是否已正确设置。
        /// 
        /// 允许误差：5秒（文件系统精度问题）
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

        // ======================== 不再使用 P/Invoke，改用 C# 原生 File.SetCreationTime ========================
        // 原生方法更可靠，避免文件锁定问题

        /// <summary>
        /// Windows 平台专用：使用 SetFileTime API 修改文件创建时间。
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
