using System;
using System.IO;
using System.Linq;
using ImageInfo.Services;

namespace ImageInfo
{
    public static class Function8Exporter
    {
        /// <summary>
        /// 扫描目录，按创建时间降序导出所有图片的文件名、核心正向词、创建时间到txt，不查重。
        /// </summary>
        public static void ExportCorePositivePrompts(string folder, string outputTxtPath)
        {
            var metaList = DevelopmentModeService.GetScan2MetadataList(folder);
            // 按创建时间降序排列（最近的在前）
            var sorted = metaList.OrderByDescending(m => DateTime.TryParse(m.CreationTime, out var dt) ? dt : DateTime.MinValue).ToList();
            // 每张图片一行，仅核心正向词
            var lines = sorted.Select(m => (m.CorePositivePrompt ?? string.Empty).Trim()).ToList();
            File.WriteAllLines(outputTxtPath, lines);
            Console.WriteLine($"已导出 {lines.Count} 条图片核心正向词到：{outputTxtPath}");
        }
    }
}
