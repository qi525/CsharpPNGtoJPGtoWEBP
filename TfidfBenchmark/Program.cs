using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TfidfBenchmark;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"C:\Users\SNOW\AppData\Local\Temp\metadata_scan_Mode3_Tagger_2025-11-25_20-59-53.xlsx";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 文件不存在: {filePath}");
            return;
        }

        try
        {
            var sw = Stopwatch.StartNew();
            ExtractTfidfFeaturesMultiThreaded(filePath);
            sw.Stop();
            Console.WriteLine($"\n⏱️  总耗时: {sw.ElapsedMilliseconds}ms ({sw.Elapsed.TotalSeconds:F2}秒)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}\n{ex.StackTrace}");
        }
    }

    static void ExtractTfidfFeaturesMultiThreaded(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);

        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            // O列是第15列
            int columnIndex = 15;

            // 1. 读取并预处理文本语料库
            Console.WriteLine("📖 正在读取O列数据...");
            var corpus = ReadAndPreprocessCorpus(worksheet, columnIndex, out List<int> validRows);
            
            Console.WriteLine($"✅ 读取完成: {corpus.Count} 条有效文本");

            // 2. 构建词汇表和计算TF-IDF
            Console.WriteLine("\n🧮 计算TF-IDF矩阵...");
            var tfidfMatrix = CalculateTfidfMatrix(corpus, out Dictionary<string, int> vocabulary);
            
            Console.WriteLine($"✅ TF-IDF计算完成");
            Console.WriteLine($"   词汇总数: {vocabulary.Count}");
            Console.WriteLine($"   矩阵大小: {tfidfMatrix.Count} x {vocabulary.Count}");

            // 3. 为每行提取TOP 5特征词 (使用并行处理)
            Console.WriteLine("\n🔍 提取TOP 5特征词 (多线程模式)...");
            var results = ExtractTopFeaturesPerRowParallel(tfidfMatrix, vocabulary, topN: 5);

            // 4. 生成输出报告
            GenerateReport(results, filePath, worksheet.Dimension.Rows);
        }
    }

    /// <summary>
    /// 读取并预处理语料库
    /// </summary>
    static List<string> ReadAndPreprocessCorpus(ExcelWorksheet worksheet, int columnIndex, out List<int> validRows)
    {
        var corpus = new List<string>();
        validRows = new List<int>();

        int rowCount = worksheet.Dimension?.Rows ?? 0;

        for (int row = 2; row <= rowCount; row++)
        {
            var cellValue = worksheet.Cells[row, columnIndex].Value;
            
            if (cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                continue;
            }

            string text = cellValue.ToString()!.ToLower();
            
            // 预处理: 替换分隔符为空格
            text = Regex.Replace(text, @"[\n,:_()\[\]\-;|]+", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (!string.IsNullOrEmpty(text))
            {
                corpus.Add(text);
                validRows.Add(row);
            }
        }

        return corpus;
    }

    /// <summary>
    /// 计算TF-IDF矩阵 (稀疏表示)
    /// </summary>
    static List<Dictionary<int, double>> CalculateTfidfMatrix(
        List<string> corpus, 
        out Dictionary<string, int> vocabulary)
    {
        vocabulary = new Dictionary<string, int>();
        var documentFrequency = new Dictionary<int, int>();
        var documents = new List<Dictionary<int, int>>();

        // 第一遍: 构建词汇表和文档词频矩阵
        foreach (var doc in corpus)
        {
            var tokens = doc.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var docTermFreq = new Dictionary<int, int>();

            foreach (var token in tokens)
            {
                if (token.Length < 2) continue;

                if (!vocabulary.ContainsKey(token))
                {
                    vocabulary[token] = vocabulary.Count;
                }

                int termId = vocabulary[token];

                if (docTermFreq.ContainsKey(termId))
                    docTermFreq[termId]++;
                else
                    docTermFreq[termId] = 1;

                if (!documentFrequency.ContainsKey(termId))
                    documentFrequency[termId] = 0;
            }

            foreach (var termId in docTermFreq.Keys)
            {
                documentFrequency[termId]++;
            }

            documents.Add(docTermFreq);
        }

        // 第二遍: 计算TF-IDF分数 (使用并行处理)
        var tfidfMatrix = new List<Dictionary<int, double>>();
        var tfidfLock = new object();
        double totalDocs = corpus.Count;

        Parallel.ForEach(documents, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
            (docTermFreq, state, index) =>
        {
            var docTfidf = new Dictionary<int, double>();

            foreach (var kvp in docTermFreq)
            {
                int termId = kvp.Key;
                int termFreq = kvp.Value;

                double tf = termFreq;
                double idf = Math.Log(totalDocs / documentFrequency[termId]);
                double tfidfScore = tf * idf;

                if (tfidfScore > 0)
                {
                    docTfidf[termId] = tfidfScore;
                }
            }

            lock (tfidfLock)
            {
                tfidfMatrix.Add(docTfidf);
            }
        });

        // 排序以保持顺序
        var sortedTfidf = tfidfMatrix.OrderBy(x => documents.IndexOf(documents.First())).ToList();

        return tfidfMatrix;
    }

    /// <summary>
    /// 为每行文档提取TOP N特征词 (使用并行处理)
    /// </summary>
    static List<List<(string term, double score)>> ExtractTopFeaturesPerRowParallel(
        List<Dictionary<int, double>> tfidfMatrix,
        Dictionary<string, int> vocabulary,
        int topN = 5)
    {
        var reverseVocab = vocabulary.ToDictionary(kv => kv.Value, kv => kv.Key);
        var results = new List<List<(string, double)>>();
        var resultsLock = new object();

        Parallel.ForEach(tfidfMatrix, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            docTfidf =>
        {
            var topFeatures = docTfidf
                .OrderByDescending(kv => kv.Value)
                .Take(topN)
                .Select(kv => (reverseVocab[kv.Key], kv.Value))
                .ToList();

            lock (resultsLock)
            {
                results.Add(topFeatures);
            }
        });

        return results;
    }

    /// <summary>
    /// 生成报告并导出
    /// </summary>
    static void GenerateReport(
        List<List<(string term, double score)>> results,
        string excelPath,
        int totalRows)
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║   TF-IDF 特征词提取报告（C# 多线程）   ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"📄 源文件: {Path.GetFileName(excelPath)}");
        Console.WriteLine($"📊 总行数: {totalRows}");
        Console.WriteLine($"✅ 有效文本: {results.Count}");
        Console.WriteLine($"🔧 处理器核心数: {Environment.ProcessorCount}");
        Console.WriteLine();
        
        // 显示前5行的样本
        Console.WriteLine("📋 样本输出 (前5行的TOP 5特征词):");
        Console.WriteLine("─────────────────────────────────────────────────────");

        for (int i = 0; i < Math.Min(5, results.Count); i++)
        {
            var features = results[i];
            Console.WriteLine($"\n行 {i + 2}:");
            
            if (features.Count == 0)
            {
                Console.WriteLine("  (无特征词)");
                continue;
            }

            int rank = 1;
            foreach (var (term, score) in features)
            {
                Console.WriteLine($"  {rank}. {term,-20} (TF-IDF: {score:F4})");
                rank++;
            }
        }

        // 生成格式化输出
        Console.WriteLine("\n═════════════════════════════════════════════════════════");
        Console.WriteLine("📋 全部结果格式化输出 (___term1___term2___...):");
        Console.WriteLine("═════════════════════════════════════════════════════════");
        Console.WriteLine();

        var csvLines = new List<string>();
        csvLines.Add("行号,TOP_1,TOP_2,TOP_3,TOP_4,TOP_5,格式化输出");

        for (int i = 0; i < results.Count; i++)
        {
            var features = results[i];
            var termsOnly = features.Select(f => f.term).ToList();
            var formattedOutput = "___" + string.Join("___", termsOnly);

            // CSV行
            var csvLine = new StringBuilder();
            csvLine.Append(i + 2);
            csvLine.Append(",");
            csvLine.Append(string.Join(",", termsOnly.Select(t => $"\"{t}\"")));
            csvLine.Append(",\"");
            csvLine.Append(formattedOutput);
            csvLine.Append("\"");
            csvLines.Add(csvLine.ToString());

            // 显示部分行
            if (i < 3)
            {
                Console.WriteLine($"行{i + 2}: {formattedOutput}");
            }
        }

        if (results.Count > 3)
        {
            Console.WriteLine($"...(共 {results.Count} 行)");
        }

        // 保存CSV
        string csvPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "TfidfBenchmark_Features_MultiThread.csv");

        try
        {
            File.WriteAllLines(csvPath, csvLines, new UTF8Encoding(false));
            Console.WriteLine();
            Console.WriteLine($"✅ CSV文件已保存: {csvPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CSV保存失败: {ex.Message}");
        }
    }
}
