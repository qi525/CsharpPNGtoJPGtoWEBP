using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace ImageInfo.Services
{
    /// <summary>
    /// 功能7：分析词频，统计所有图片的核心正向词出现频率并生成表格
    /// </summary>
    public static class Mode7WordFrequencyAnalyzer
    {
        /// <summary>
        /// 运行词频分析，直接调用功能2的扫描逻辑，统计“核心正向词”词频并生成统计表
        /// </summary>
        /// <param name="folder">图片根目录</param>
        public static void RunWordFrequencyAnalysis(string folder)
        {
            Console.WriteLine("[功能7] 分析词频：直接调用功能2扫描逻辑，统计所有图片的核心正向词出现频率\n");
            var metadataList = DevelopmentModeService.GetScan2MetadataList(folder);
            if (metadataList == null || metadataList.Count == 0)
            {
                Console.WriteLine("未找到任何图片元数据，无法分析词频。");
                return;
            }

            var freqDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in metadataList)
            {
                if (string.IsNullOrWhiteSpace(record.CorePositivePrompt)) continue;
                var words = record.CorePositivePrompt
                    .Split(new[] { ',', '，', ';', '；', ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim())
                    .Where(w => !string.IsNullOrWhiteSpace(w));
                foreach (var word in words)
                {
                    if (freqDict.ContainsKey(word)) freqDict[word]++;
                    else freqDict[word] = 1;
                }
            }

            if (freqDict.Count == 0)
            {
                Console.WriteLine("未提取到任何核心正向词，无法生成词频表。");
                return;
            }

            // 按出现次数降序排序
            var sorted = freqDict.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).ToList();

            // 生成词频统计表
            string freqPath = Path.Combine(folder, "词频统计表.xlsx");
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("词频统计");
                ws.Cell(1, 1).Value = "词语";
                ws.Cell(1, 2).Value = "出现次数";
                int row = 2;
                foreach (var kv in sorted)
                {
                    ws.Cell(row, 1).Value = kv.Key;
                    ws.Cell(row, 2).Value = kv.Value;
                    row++;
                }
                ws.Columns().AdjustToContents();
                wb.SaveAs(freqPath);
            }
            Console.WriteLine($"✓ 词频统计表已生成: {freqPath}\n");
            // 自动打开报告
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = freqPath, UseShellExecute = true }); } catch { }
        }
    }
}
