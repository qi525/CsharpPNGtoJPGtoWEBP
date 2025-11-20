using System;
using System.IO;

namespace ImageInfo.Services
{
    /// <summary>
    /// 文件备份服务。
    /// 转换失败时，将源文件复制到目标目录作为备份，防止数据丢失。
    /// </summary>
    public static class FileBackupService
    {
        /// <summary>
        /// 将源文件复制到目标目录作为备份。失败返回 null，不中断流程。
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destDirectoryPath">目标目录路径</param>
        /// <param name="destFilename">目标文件名（可选，默认为源文件名）</param>
        /// <returns>备份文件的完整路径，失败时返回 null</returns>
        public static string? CreateBackupFile(string sourceFilePath, string destDirectoryPath, string? destFilename = null)
        {
            try
            {
                // 验证源文件存在
                if (!File.Exists(sourceFilePath))
                {
                    Console.WriteLine($"Source file not found for backup: {sourceFilePath}");
                    return null;
                }

                // 确保目标目录存在
                if (!Directory.Exists(destDirectoryPath))
                {
                    Directory.CreateDirectory(destDirectoryPath);
                }

                // 确定目标文件名
                string backupFilename = destFilename ?? Path.GetFileName(sourceFilePath);
                string backupFilePath = Path.Combine(destDirectoryPath, backupFilename);

                // 如果备份文件已存在，生成新名称
                if (File.Exists(backupFilePath))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(backupFilename);
                    string fileExt = Path.GetExtension(backupFilename);
                    string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    backupFilename = $"{fileNameWithoutExt}_backup_{timestamp}{fileExt}";
                    backupFilePath = Path.Combine(destDirectoryPath, backupFilename);
                }

                // 复制源文件
                File.Copy(sourceFilePath, backupFilePath, overwrite: false);

                Console.WriteLine($"Backup created: {backupFilePath}");
                return backupFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create backup for {sourceFilePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 验证备份文件是否成功复制（检查存在性和大小）。
        /// </summary>
        /// <param name="sourceFilePath">原始源文件路径</param>
        /// <param name="backupFilePath">备份文件路径</param>
        /// <returns>备份有效返回 true，否则 false</returns>
        public static bool VerifyBackupFile(string sourceFilePath, string backupFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                    return false;

                if (!File.Exists(backupFilePath))
                    return false;

                // 比较文件大小
                var sourceInfo = new FileInfo(sourceFilePath);
                var backupInfo = new FileInfo(backupFilePath);

                return sourceInfo.Length == backupInfo.Length;
            }
            catch
            {
                return false;
            }
        }
    }
}
