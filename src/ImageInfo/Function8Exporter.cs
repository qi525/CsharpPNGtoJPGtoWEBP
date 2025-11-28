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
            // 每张图片一行，仅核心正向词（顺序已由全局控制）
            var lines = metaList.Select(m => (m.CorePositivePrompt ?? string.Empty).Trim()).ToList();
            File.WriteAllLines(outputTxtPath, lines);
            Console.WriteLine($"已导出 {lines.Count} 条图片核心正向词到：{outputTxtPath}");
        }
    }
}
