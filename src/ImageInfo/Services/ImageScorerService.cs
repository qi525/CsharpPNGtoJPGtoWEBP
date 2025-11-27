using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageInfo.Models;
using ImageInfo.Resources;

namespace ImageInfo.Services
{
    /// <summary>
    /// 图像个性化推荐评分系统 - 相似度匹配版本（简化）
    /// 
    /// 核心思路：
    /// 1. 从"超绝"等高质量文件夹提取理想特征
    /// 2. 用余弦相似度衡量其他图片与理想特征的接近程度
    /// 3. 相似度越高 = 评分越高
    /// 
    /// 优点：逻辑简单，内存占用少，符合"初步筛选"的目的
    /// </summary>
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearRegression;
    using System.Text.RegularExpressions;
    public class ImageScorerService
    {
        private readonly ImageScorerConfig _config;
        public ImageScorerService(ImageScorerConfig? config = null)
        {
            _config = config ?? new ImageScorerConfig();
        }

        /// <summary>
        /// 主入口：完全对齐Python方案的功能5评分流程
        /// </summary>
        // 新主入口，完全对齐Python方案，手动实现岭回归
        public async Task<bool> ScoreMetadataRecordsSupervisedAsync(List<MetadataRecord> records, string vocabColumnName = "TfidfKeywords")
        {
            try
            {
                // 1. 准备输入数据给 ScorerCliCs
                var scorerInput = records.Select(r => (
                    Path: r.FilePath, // Path用于从文件名中提取目标分数
                    Feature: GetPropertyValue(r, vocabColumnName) ?? string.Empty // Feature用于ML.NET模型训练和预测
                )).ToList();

                // 2. 调用ScorerCliCs进行评分
                var scoringResults = ScorerCliCs.RunFromItems(scorerInput);

                // 3. 将评分结果写回MetadataRecord
                for (int i = 0; i < records.Count; i++)
                {
                    records[i].TargetScore = scoringResults[i].TargetScore; // 保存目标分
                    records[i].PredictedScore = scoringResults[i].PredictedScore; // 保存预测分
                }
                
                // 4. 计算文件夹默认匹配分 (此逻辑保持不变，因为它独立于ML.NET预测)
                foreach (var record in records)
                {
                    record.FolderMatchScore = ExtractFolderScore(record.FilePath);
                }

                return true;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"⚠️  评分处理跳过: {ex.Message}");
                Console.WriteLine("  可能原因是训练样本不足，所有文件将获得默认分数。");
                // 确保即使跳过，PredictedScore也被设置为默认值，以避免报告中出现空值
                foreach (var record in records)
                {
                    record.PredictedScore = _config.DefaultNeutralScore;
                    record.FolderMatchScore = ExtractFolderScore(record.FilePath);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[功能5] C#原生评分异常：{ex.Message}");
                // 出现其他异常时，同样确保PredictedScore被设置为默认值
                foreach (var record in records)
                {
                    record.PredictedScore = _config.DefaultNeutralScore;
                    record.FolderMatchScore = ExtractFolderScore(record.FilePath);
                }
                return false;
            }
        }

        /// <summary>
        /// 提取人工分数（自定义标记优先，其次文件夹名，最后默认分）
        /// </summary>
        private double ExtractTargetScore(string filePath)
        {
            // 1. 自定义标记
            var prefix = _config.ScorePrefix ?? "@@@评分";
            var match = Regex.Match(filePath, Regex.Escape(prefix) + @"(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int score1))
                return Math.Clamp(score1, 0, 100);
            // 2. 文件夹名关键词
            foreach (var kv in _config.RatingMap)
                if (!string.IsNullOrEmpty(kv.Key) && filePath.Contains(kv.Key))
                    return kv.Value;
            // 3. 默认分
            return _config.DefaultNeutralScore;
        }

        /// <summary>
        /// 构建TF-IDF稀疏矩阵，返回(Matrix, 词表)
        /// </summary>
        private (Matrix<double>, List<string>) BuildTfidfMatrix(List<string> docs)
        {
            // 1. 分词
            var allTokens = new List<List<string>>();
            var vocabSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var doc in docs)
            {
                var tokens = Regex.Matches(doc, @"\w+").Select(m => m.Value.ToLowerInvariant()).ToList();
                allTokens.Add(tokens);
                foreach (var t in tokens) vocabSet.Add(t);
            }
            var vocab = vocabSet.OrderBy(s => s).ToList();
            var vocabIndex = vocab.Select((w, i) => (w, i)).ToDictionary(t => t.w, t => t.i, StringComparer.OrdinalIgnoreCase);
            // 2. 计算DF
            var df = new int[vocab.Count];
            foreach (var tokens in allTokens)
                foreach (var t in tokens.Distinct())
                    if (vocabIndex.TryGetValue(t, out int idx)) df[idx]++;
            // 3. 计算TF-IDF
            int N = docs.Count;
            var mat = Matrix<double>.Build.Dense(N, vocab.Count);
            for (int i = 0; i < N; i++)
            {
                var tokens = allTokens[i];
                if (tokens.Count == 0) continue;
                var tf = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in tokens)
                    tf[t] = tf.TryGetValue(t, out double v) ? v + 1 : 1;
                foreach (var t in tf.Keys.ToList())
                    tf[t] /= tokens.Count;
                foreach (var t in tf.Keys)
                {
                    if (!vocabIndex.TryGetValue(t, out int idx)) continue;
                    double idf = Math.Log((double)N / (1 + df[idx])) + 1.0;
                    mat[i, idx] = tf[t] * idf;
                }
            }
            return (mat, vocab);
        }

        // ...existing code...

        // ...已重构主入口，保留唯一主入口ScoreMetadataRecordsSupervisedAsync...

        /// <summary>
        /// 从指定样本中提取理想特征
        /// 方法：计算这些样本中所有关键词的平均TF-IDF分数
        /// </summary>
        private Dictionary<string, double> BuildIdealFeaturesFromSamples(List<MetadataRecord> records, string vocabColumnName, List<int> sampleIndices)
        {
            var idealFeatures = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var wordScores = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

            // 收集所有关键词的分数
            foreach (int idx in sampleIndices)
            {
                string? vocabText = GetPropertyValue(records[idx], vocabColumnName);
                if (string.IsNullOrWhiteSpace(vocabText))
                    continue;

                // 解析 "word(score)|word(score)" 格式
                var pairs = vocabText.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var match = Regex.Match(pair.Trim(), @"^(\w+)\(([\d.]+)\)$");
                    if (match.Success)
                    {
                        string word = match.Groups[1].Value.ToLowerInvariant();
                        if (double.TryParse(match.Groups[2].Value, out double score))
                        {
                            if (!wordScores.ContainsKey(word))
                                wordScores[word] = new List<double>();
                            wordScores[word].Add(score);
                        }
                    }
                }
            }

            // 计算每个词的平均分数
            foreach (var kvp in wordScores)
            {
                idealFeatures[kvp.Key] = kvp.Value.Average();
            }

            return idealFeatures;
        }

        /// <summary>
        /// 纯TF-IDF评分方法
        /// 直接用TF-IDF词的分数作为评分
        /// Score = 所有词的平均TF-IDF分数
        /// 逻辑：高质量词自动得高分，低质量词得低分
        /// </summary>
        private void ComputeTFIDFScores(List<MetadataRecord> records, string vocabColumnName)
        {
            int zeroCount = 0;
            int validCount = 0;
            double minScore = double.MaxValue;
            double maxScore = double.MinValue;

            for (int i = 0; i < records.Count; i++)
            {
                string? vocabText = GetPropertyValue(records[i], vocabColumnName);
                if (string.IsNullOrWhiteSpace(vocabText))
                {
                    records[i].PredictedScore = _config.DefaultNeutralScore;
                    zeroCount++;
                    if (i < 5) Console.WriteLine($"[调试] 记录{i}: TfidfKeywords为空或空白");
                    continue;
                }

                // 调试：打印前几条的原始内容
                if (i < 3)
                {
                    Console.WriteLine($"[调试] 记录{i}: vocabText长度={vocabText.Length}");
                    Console.WriteLine($"[调试]     内容预览: {vocabText.Substring(0, Math.Min(100, vocabText.Length))}");
                }

                // 解析所有关键词的分数
                var scores = new List<double>();
                var pairs = vocabText.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                
                Console.WriteLine($"[调试] 记录{i}: 分割后有 {pairs.Length} 个词对");

                foreach (var pair in pairs)
                {
                    var match = Regex.Match(pair.Trim(), @"^(\w+)\(([\d.]+)\)$");
                    if (match.Success && double.TryParse(match.Groups[2].Value, out double score))
                    {
                        scores.Add(score);
                    }
                    else if (i < 3)
                    {
                        Console.WriteLine($"[调试]     无法解析: '{pair.Trim()}'");
                    }
                }

                Console.WriteLine($"[调试] 记录{i}: 成功解析 {scores.Count} 个词的分数");

                // 用所有词的平均分数作为预估评分（TF-IDF范围0-1，乘以100转换为0-100分）
                if (scores.Count > 0)
                {
                    double avgScore = scores.Average();
                    records[i].PredictedScore = Math.Max(0, Math.Min(100, Math.Round(avgScore * 100, 1)));
                    validCount++;
                    minScore = Math.Min(minScore, avgScore);
                    maxScore = Math.Max(maxScore, avgScore);
                    if (i < 5) Console.WriteLine($"[调试] 记录{i}: 平均TF-IDF={avgScore:F4}, 最终分={records[i].PredictedScore}");
                }
                else
                {
                    records[i].PredictedScore = _config.DefaultNeutralScore;
                    zeroCount++;
                    if (i < 5) Console.WriteLine($"[调试] 记录{i}: 没有解析出任何词");
                }

                // 只打印前10条调试信息
                if (i >= 10) break;
            }

            Console.WriteLine($"\n[评分统计] 总记录数={records.Count}, 有效记录={validCount}, 零分记录={zeroCount}");
            if (validCount > 0)
            {
                Console.WriteLine($"[评分统计] 分数范围: {minScore:F2} - {maxScore:F2}");
            }
        }

        /// <summary>
        /// 提取文件夹默认匹配分（精确匹配文件夹名）
        /// </summary>
        private double ExtractFolderScore(string filePath)
        {
            string? directFolderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directFolderPath))
                return _config.DefaultNeutralScore;

            string folderNameOnly = Path.GetFileName(directFolderPath);

            foreach (var kvp in _config.RatingMap)
            {
                if (folderNameOnly.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return _config.DefaultNeutralScore;
        }

        /// <summary>
        /// 从MetadataRecord的属性中获取值
        /// </summary>
        private string? GetPropertyValue(MetadataRecord record, string propertyName)
        {
            return propertyName.ToLower() switch
            {
                "tfidfkeywords" => record.TfidfKeywords,
                "customkeywords" => record.CustomKeywords,
                "prompt" => record.Prompt,
                _ => record.TfidfKeywords
            };
        }
    }
}
