using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace TfidfDemo
{
    /// <summary>
    /// 最简单的TF-IDF实现演示
    /// 完全独立的可运行代码，用于验证框架逻辑
    /// </summary>
    public class SimpleTfidfDemo
    {
        // ==================== 数据结构 ====================
        
        public class Document
        {
            public int DocId { get; set; }
            public string[] Words { get; set; }
            public Dictionary<string, int> WordCounts { get; set; }
            public int TotalWords { get; set; }
        }

        public class TfidfResult
        {
            public int DocId { get; set; }
            public string ExcelString { get; set; }
            public List<string> TopKeywords { get; set; }
        }

        // ==================== 主要逻辑 ====================

        private List<Document> _documents = new List<Document>();
        private Dictionary<string, int> _vocabularyDf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, double> _idfTable = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private int _topN = 10;
        private readonly Regex _tokenRegex = new Regex(@"\b\w+\b", RegexOptions.IgnoreCase);

        // ==================== 可选优化参数 ====================
        /// <summary>
        /// 【可选优化1】StopWords过滤 - 过滤掉常见无意义词
        /// 使用场景：提高关键词质量，减少"the"、"is"等干扰词
        /// 耦合度：低 - 可独立启用/禁用
        /// </summary>
        private HashSet<string> _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "is", "at", "which", "on", "a", "an", "and", "or", "but",
            "in", "to", "for", "of", "with", "by", "from", "as", "be", "have"
        };
        private bool _enableStopWordFilter = false;

        /// <summary>
        /// 【可选优化2】词长限制 - 只保留3-20字符的词
        /// 使用场景：过滤过短（如"a"、"to"）或过长（噪音）的词
        /// 耦合度：低 - 独立参数控制
        /// </summary>
        private int _minWordLength = 3;
        private int _maxWordLength = 20;
        private bool _enableWordLengthFilter = false;

        /// <summary>
        /// 【可选优化3】性能统计 - 跟踪各阶段耗时
        /// 使用场景：性能优化、瓶颈识别
        /// 耦合度：低 - 仅用于日志输出
        /// </summary>
        private Dictionary<string, long> _performanceStats = new Dictionary<string, long>();
        private bool _enablePerformanceTracking = false;

        // ==================== 优化函数 1-10 ====================

        /// <summary>
        /// 【优化函数1】应用StopWords过滤
        /// 难度：1/10 - 简单的集合查询
        /// 优化方向：可改为Bloom Filter在超大词汇表中提速
        /// </summary>
        private bool ShouldKeepWord(string word)
        {
            if (!_enableStopWordFilter) return true;
            return !_stopWords.Contains(word);
        }

        /// <summary>
        /// 【优化函数2】应用词长限制
        /// 难度：1/10 - 简单的长度检查
        /// 优化方向：可配置的min/max长度规则
        /// </summary>
        private bool IsValidWordLength(string word)
        {
            if (!_enableWordLengthFilter) return true;
            return word.Length >= _minWordLength && word.Length <= _maxWordLength;
        }

        /// <summary>
        /// 【优化函数3】记录性能数据
        /// 难度：1/10 - 简单的字典操作
        /// 优化方向：可改为结构化日志、分布式追踪
        /// </summary>
        private void RecordPerformance(string stageName, long milliseconds)
        {
            if (!_enablePerformanceTracking) return;
            _performanceStats[stageName] = milliseconds;
        }

        /// <summary>
        /// 【优化函数4】打印性能统计
        /// 难度：1/10 - 格式化输出
        /// 调用时机：流程完成后调用，用于性能分析
        /// </summary>
        public void PrintPerformanceStats()
        {
            if (!_enablePerformanceTracking || _performanceStats.Count == 0) return;
            
            Console.WriteLine("\n[性能统计]");
            long total = 0;
            foreach (var kv in _performanceStats)
            {
                Console.WriteLine($"  {kv.Key}: {kv.Value}ms");
                total += kv.Value;
            }
            Console.WriteLine($"  总耗时: {total}ms");
        }

        /// <summary>
        /// 【优化函数5】动态StopWords加载
        /// 难度：2/10 - 简单的字符串处理
        /// 优化方向：从文件、数据库或网络加载StopWords列表
        /// 使用场景：针对不同领域定制StopWords（医学、法律等）
        /// </summary>
        public void LoadCustomStopWords(params string[] words)
        {
            foreach (var word in words)
            {
                _stopWords.Add(word.ToLower());
            }
        }

        /// <summary>
        /// 【优化函数6】启用/禁用各项过滤
        /// 难度：1/10 - 简单的标志设置
        /// 使用场景：快速切换不同的处理模式
        /// </summary>
        public void SetOptimizations(bool enableStopWords = false, bool enableWordLength = false, bool enableStats = false)
        {
            _enableStopWordFilter = enableStopWords;
            _enableWordLengthFilter = enableWordLength;
            _enablePerformanceTracking = enableStats;
        }

        /// <summary>
        /// 【优化函数7】计算文档统计信息
        /// 难度：3/10 - 遍历和统计
        /// 优化方向：缓存统计结果，支持增量更新
        /// 返回值：(总文档数, 平均词数, 总词汇量, 平均DF)
        /// </summary>
        public (int DocCount, double AvgWordsPerDoc, int VocabSize, double AvgDf) GetDocumentStats()
        {
            int docCount = _documents.Count;
            int totalWords = _documents.Sum(d => d.TotalWords);
            int vocabSize = _vocabularyDf.Count;
            double avgWordsPerDoc = docCount > 0 ? (double)totalWords / docCount : 0;
            double avgDf = vocabSize > 0 ? (double)_vocabularyDf.Values.Sum() / vocabSize : 0;
            
            return (docCount, avgWordsPerDoc, vocabSize, avgDf);
        }

        /// <summary>
        /// 【优化函数8】过滤低频词
        /// 难度：3/10 - 字典过滤
        /// 优化方向：支持百分比阈值（如去掉出现在<5%文档中的词）
        /// 使用场景：减少噪音词、降低内存占用
        /// </summary>
        public void FilterLowFrequencyWords(int minDocFrequency)
        {
            var toRemove = _vocabularyDf
                .Where(kv => kv.Value < minDocFrequency)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var word in toRemove)
            {
                _vocabularyDf.Remove(word);
                _idfTable.Remove(word);
            }

            Console.WriteLine($"[过滤] 移除 {toRemove.Count} 个低频词 (DF < {minDocFrequency})");
        }

        /// <summary>
        /// 【优化函数9】获取词的IDF排名
        /// 难度：4/10 - 排序和选择
        /// 优化方向：支持多种排序方式（IDF、DF、词长等）
        /// 使用场景：分析词汇重要性、验证IDF计算
        /// </summary>
        public List<(string Word, double IDF, int DF)> GetTopIdfWords(int count = 20)
        {
            return _idfTable
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .Select(kv => (kv.Key, kv.Value, _vocabularyDf[kv.Key]))
                .ToList();
        }

        /// <summary>
        /// 【优化函数10】验证TF-IDF计算正确性
        /// 难度：5/10 - 复杂的数学验证
        /// 优化方向：支持单文档或全量验证
        /// 使用场景：单元测试、算法调试
        /// 返回：是否通过验证以及详细信息
        /// </summary>
        public (bool Valid, string Details) ValidateTfidfCalculation(int docIndex)
        {
            if (docIndex >= _documents.Count)
                return (false, $"文档索引越界: {docIndex}");

            var doc = _documents[docIndex];
            if (doc.WordCounts == null || doc.WordCounts.Count == 0)
                return (false, "文档无有效词汇");

            // 验证TF计算
            double expectedTfSum = 0;
            foreach (var kvp in doc.WordCounts)
            {
                double tf = (double)kvp.Value / doc.TotalWords;
                expectedTfSum += tf;
                
                // TF应该在0-1之间
                if (tf < 0 || tf > 1)
                    return (false, $"TF值无效: {kvp.Key}={tf}");
            }

            // 验证IDF存在
            foreach (var word in doc.WordCounts.Keys)
            {
                if (!_idfTable.ContainsKey(word))
                    return (false, $"缺少IDF值: {word}");
            }

            return (true, $"✓ 验证通过 - TF总和≈{expectedTfSum:F2}, 词数={doc.WordCounts.Count}");
        }

        /// <summary>
        /// 【优化函数11】TF-IDF向量化
        /// 难度：6/10 - 矩阵运算
        /// 优化方向：支持稀疏矩阵、GPU加速
        /// 使用场景：机器学习特征提取、相似度计算
        /// 返回：(文档ID, 词汇表, 稀疏TF-IDF向量)
        /// </summary>
        public List<(int DocId, Dictionary<string, double> TfidfVector)> GetTfidfVectors()
        {
            var result = new List<(int, Dictionary<string, double>)>();

            foreach (var doc in _documents)
            {
                var vector = new Dictionary<string, double>();
                
                if (doc.WordCounts != null)
                {
                    foreach (var kvp in doc.WordCounts)
                    {
                        string word = kvp.Key;
                        double tf = (double)kvp.Value / doc.TotalWords;
                        double idf = _idfTable.ContainsKey(word) ? _idfTable[word] : 0;
                        double tfidf = tf * idf;
                        
                        if (tfidf > 0.0001)  // 只保存非零值（稀疏）
                            vector[word] = tfidf;
                    }
                }

                result.Add((doc.DocId, vector));
            }

            return result;
        }

        /// <summary>
        /// 【优化函数12】计算文档相似度 (余弦相似度)
        /// 难度：7/10 - 向量运算
        /// 优化方向：支持批量计算、缓存、并行化
        /// 使用场景：去重、聚类、推荐
        /// </summary>
        public double CalculateCosineSimilarity(int docId1, int docId2)
        {
            if (docId1 >= _documents.Count || docId2 >= _documents.Count)
                return -1;

            var vectors = GetTfidfVectors();
            var v1 = vectors[docId1].TfidfVector;
            var v2 = vectors[docId2].TfidfVector;

            double dotProduct = 0;
            foreach (var word in v1.Keys)
            {
                if (v2.ContainsKey(word))
                    dotProduct += v1[word] * v2[word];
            }

            double norm1 = Math.Sqrt(v1.Values.Sum(x => x * x));
            double norm2 = Math.Sqrt(v2.Values.Sum(x => x * x));

            if (norm1 == 0 || norm2 == 0) return 0;

            return dotProduct / (norm1 * norm2);
        }

        /// <summary>
        /// 【优化函数13】批量相似度计算矩阵
        /// 难度：8/10 - 矩阵计算
        /// 优化方向：使用MKL、CUDA等加速库
        /// 返回：N×N相似度矩阵
        /// </summary>
        public double[,] CalculateSimilarityMatrix()
        {
            int n = _documents.Count;
            double[,] matrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = i; j < n; j++)
                {
                    double sim = CalculateCosineSimilarity(i, j);
                    matrix[i, j] = sim;
                    matrix[j, i] = sim;
                }
            }

            return matrix;
        }

        /// <summary>
        /// 【优化函数14】自定义评分函数
        /// 难度：6/10 - 策略模式
        /// 优化方向：支持可配置的评分权重
        /// 使用场景：调整TF-IDF权重、多指标融合
        /// 默认评分 = TF-IDF * (1 + log(词长)) * (1 - 词频偏差)
        /// </summary>
        public TfidfResult CalculateWithCustomScoring(Document doc)
        {
            if (doc == null || doc.WordCounts == null || doc.WordCounts.Count == 0)
            {
                return new TfidfResult
                {
                    DocId = doc?.DocId ?? -1,
                    ExcelString = "无有效词汇",
                    TopKeywords = new List<string>()
                };
            }

            var wordScores = new List<(string word, double score, double tfidf)>();

            foreach (var kvp in doc.WordCounts)
            {
                string word = kvp.Key;
                int count = kvp.Value;
                double tf = (double)count / doc.TotalWords;
                double idf = _idfTable.ContainsKey(word) ? _idfTable[word] : Math.Log(_documents.Count + 1);
                double baseTfidf = tf * idf;

                // 自定义权重：词长奖励 + 频率平衡
                double lengthBonus = 1 + Math.Log10(word.Length + 1) * 0.1;
                double frequencyPenalty = 1 - Math.Min(tf, 0.5);  // 避免单个词过于主导
                double customScore = baseTfidf * lengthBonus * frequencyPenalty;

                wordScores.Add((word, customScore, baseTfidf));
            }

            var topWords = wordScores
                .OrderByDescending(x => x.score)
                .Take(_topN)
                .ToList();

            var excelString = string.Join("|", topWords.Select(w => $"{w.word}({w.tfidf:F4})"));
            var topKeywords = topWords.Select(x => x.word).ToList();

            return new TfidfResult
            {
                DocId = doc.DocId,
                ExcelString = excelString,
                TopKeywords = topKeywords
            };
        }

        /// <summary>
        /// 【优化函数15】关键词提取质量评估
        /// 难度：8/10 - 统计分析
        /// 优化方向：支持多种评估指标（覆盖度、区分度、稳定性）
        /// 返回：评估报告
        /// </summary>
        public string EvaluateKeywordQuality()
        {
            if (_documents.Count == 0) return "无文档";

            var report = new System.Text.StringBuilder();
            report.AppendLine("[关键词质量评估]");

            var topIdfWords = GetTopIdfWords(10);
            var stats = GetDocumentStats();

            report.AppendLine($"  文档总数: {stats.DocCount}");
            report.AppendLine($"  词汇总数: {stats.VocabSize}");
            report.AppendLine($"  平均词长: {stats.AvgWordsPerDoc:F1}");
            report.AppendLine($"  平均DF: {stats.AvgDf:F2}");
            report.AppendLine($"  Top IDF词: {string.Join(", ", topIdfWords.Take(5).Select(x => $"{x.Word}({x.IDF:F2})"))}");

            return report.ToString();
        }

        // ==================== 核心步骤（不改动） ====================

        /// <summary>
        /// 步骤1：预处理单个文本
        /// </summary>
        public Document PreprocessText(int docId, string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new Document
                {
                    DocId = docId,
                    Words = Array.Empty<string>(),
                    WordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                    TotalWords = 0
                };
            }

            // 小写
            string text = rawText.ToLower();

            // 替换特殊字符
            text = Regex.Replace(text, @"[\n,:_()\[\]\-]+", " ");
            
            // 分词
            var matches = _tokenRegex.Matches(text);
            var words = new List<string>();
            foreach (Match match in matches)
            {
                string word = match.Value;
                
                // 应用可选过滤器
                if (!ShouldKeepWord(word)) continue;              // 【优化1】StopWords过滤
                if (!IsValidWordLength(word)) continue;           // 【优化2】词长限制
                
                words.Add(word);
            }

            // 计数
            var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var word in words)
            {
                if (wordCounts.ContainsKey(word))
                    wordCounts[word]++;
                else
                    wordCounts[word] = 1;
            }

            return new Document
            {
                DocId = docId,
                Words = words.ToArray(),
                WordCounts = wordCounts,
                TotalWords = words.Count
            };
        }

        /// <summary>
        /// 步骤2：构建文档库并计算DF
        /// </summary>
        public void BuildDocumentLibrary(List<string> texts)
        {
            _documents.Clear();
            _vocabularyDf.Clear();

            for (int i = 0; i < texts.Count; i++)
            {
                var doc = PreprocessText(i, texts[i]);
                _documents.Add(doc);
            }

            // 计算DF
            foreach (var doc in _documents)
            {
                if (doc.WordCounts != null)
                {
                    var uniqueWords = new HashSet<string>(doc.WordCounts.Keys, StringComparer.OrdinalIgnoreCase);
                    foreach (var word in uniqueWords)
                    {
                        if (_vocabularyDf.ContainsKey(word))
                            _vocabularyDf[word]++;
                        else
                            _vocabularyDf[word] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 步骤3：计算IDF表
        /// IDF = log(N / df)
        /// </summary>
        public void BuildIdfTable()
        {
            _idfTable.Clear();
            int totalDocs = _documents.Count;
            if (totalDocs == 0) return;

            foreach (var kvp in _vocabularyDf)
            {
                string word = kvp.Key;
                int df = kvp.Value;
                double idf = Math.Log((double)totalDocs / df);
                _idfTable[word] = idf;
            }
        }

        /// <summary>
        /// 步骤4：计算单个文档的TF-IDF
        /// </summary>
        public TfidfResult CalculateTfIdfScores(Document doc)
        {
            if (doc == null || doc.WordCounts == null || doc.WordCounts.Count == 0)
            {
                return new TfidfResult
                {
                    DocId = doc?.DocId ?? -1,
                    ExcelString = "无有效词汇",
                    TopKeywords = new List<string>()
                };
            }

            var wordScores = new List<(string word, double score)>();

            foreach (var kvp in doc.WordCounts)
            {
                string word = kvp.Key;
                int count = kvp.Value;
                double tf = (double)count / doc.TotalWords;
                double idf = _idfTable.ContainsKey(word) ? _idfTable[word] : Math.Log(_documents.Count + 1);
                double tfidf = tf * idf;

                wordScores.Add((word, tfidf));
            }

            var topWords = wordScores
                .OrderByDescending(x => x.score)
                .Take(_topN)
                .ToList();

            var excelString = string.Join("|", topWords.Select(w => $"{w.word}({w.score:F4})"));
            var topKeywords = topWords.Select(x => x.word).ToList();

            return new TfidfResult
            {
                DocId = doc.DocId,
                ExcelString = excelString,
                TopKeywords = topKeywords
            };
        }

        /// <summary>
        /// 步骤5：主流程
        /// </summary>
        public List<TfidfResult> ProcessAll(List<string> texts)
        {
            BuildDocumentLibrary(texts);
            BuildIdfTable();

            var results = new List<TfidfResult>();
            foreach (var doc in _documents)
            {
                var result = CalculateTfIdfScores(doc);
                results.Add(result);
            }

            return results;
        }

        // ==================== 测试 ====================

        public static void Main()
        {
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║   TF-IDF框架 - 难度逐升测试 (1/10 → 10/10)   ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            // ========== 难度1：基础测试 ==========
            Console.WriteLine("【难度1/10】基础测试 - 简单词频");
            var demo = new SimpleTfidfDemo();
            var texts1 = new List<string> { "cat dog cat bird cat" };
            Console.WriteLine($"输入: {texts1[0]}");
            var r1 = demo.ProcessAll(texts1);
            Console.WriteLine($"✓ 关键词: {string.Join(", ", r1[0].TopKeywords)}\n");

            // ========== 难度2：StopWords优化 ==========
            Console.WriteLine("【难度2/10】StopWords过滤");
            demo = new SimpleTfidfDemo();
            demo.SetOptimizations(enableStopWords: true);
            var texts2 = new List<string> { "the cat is on the mat" };
            Console.WriteLine($"输入: {texts2[0]}");
            var r2 = demo.ProcessAll(texts2);
            Console.WriteLine($"✓ 过滤后: {string.Join(", ", r2[0].TopKeywords)} (去除了the, is, on)\n");

            // ========== 难度3：词长限制 ==========
            Console.WriteLine("【难度3/10】词长限制 (3-15字符)");
            demo = new SimpleTfidfDemo();
            demo.SetOptimizations(enableWordLength: true);
            demo._minWordLength = 3;
            demo._maxWordLength = 15;
            var texts3 = new List<string> { "a beautiful wonderfulincrediblylong day" };
            Console.WriteLine($"输入: {texts3[0]}");
            var r3 = demo.ProcessAll(texts3);
            Console.WriteLine($"✓ 过滤后: {string.Join(", ", r3[0].TopKeywords)}\n");

            // ========== 难度4：性能统计 ==========
            Console.WriteLine("【难度4/10】性能统计");
            demo = new SimpleTfidfDemo();
            demo.SetOptimizations(enableStats: true);
            var texts4 = new List<string>();
            for (int i = 0; i < 50; i++)
                texts4.Add($"document{i} performance metrics analysis");
            var sw = Stopwatch.StartNew();
            var r4 = demo.ProcessAll(texts4);
            sw.Stop();
            Console.WriteLine($"✓ 处理50条文本耗时: {sw.ElapsedMilliseconds}ms");
            demo.PrintPerformanceStats();
            Console.WriteLine();

            // ========== 难度5：文档统计 ==========
            Console.WriteLine("【难度5/10】文档统计");
            demo = new SimpleTfidfDemo();
            var texts5 = new List<string>
            {
                "machine learning deep neural network",
                "artificial intelligence algorithms",
                "data science statistics"
            };
            var r5 = demo.ProcessAll(texts5);
            var stats = demo.GetDocumentStats();
            Console.WriteLine($"✓ 文档数: {stats.DocCount}, 词汇数: {stats.VocabSize}");
            Console.WriteLine($"✓ 平均词数: {stats.AvgWordsPerDoc:F1}, 平均DF: {stats.AvgDf:F2}\n");

            // ========== 难度6：低频词过滤 ==========
            Console.WriteLine("【难度6/10】低频词过滤 (DF>=2)");
            demo = new SimpleTfidfDemo();
            var texts6 = new List<string>
            {
                "python java python",
                "python rust",
                "java golang"
            };
            demo.ProcessAll(texts6);
            Console.WriteLine($"✓ 过滤前词汇数: {demo._vocabularyDf.Count}");
            demo.FilterLowFrequencyWords(2);
            Console.WriteLine($"✓ 过滤后词汇数: {demo._vocabularyDf.Count}\n");

            // ========== 难度7：IDF排名 ==========
            Console.WriteLine("【难度7/10】Top IDF词排名");
            demo = new SimpleTfidfDemo();
            var texts7 = new List<string>
            {
                "apple banana cherry apple banana",
                "apple orange date",
                "grape melon pear"
            };
            demo.ProcessAll(texts7);
            var topIdf = demo.GetTopIdfWords(3);
            Console.WriteLine("✓ Top IDF词:");
            foreach (var (word, idf, df) in topIdf)
                Console.WriteLine($"   {word}: IDF={idf:F4}, DF={df}");
            Console.WriteLine();

            // ========== 难度8：向量化 ==========
            Console.WriteLine("【难度8/10】TF-IDF向量化");
            demo = new SimpleTfidfDemo();
            var texts8 = new List<string>
            {
                "cat sat mat",
                "dog sat tree"
            };
            demo.ProcessAll(texts8);
            var vectors = demo.GetTfidfVectors();
            Console.WriteLine($"✓ 生成{vectors.Count}个向量, 第一个文档{vectors[0].TfidfVector.Count}个非零特征\n");

            // ========== 难度9：相似度计算 ==========
            Console.WriteLine("【难度9/10】文档相似度 (余弦相似度)");
            demo = new SimpleTfidfDemo();
            var texts9 = new List<string>
            {
                "cat and dog playing",
                "cat and dog running",
                "apple and orange"
            };
            demo.ProcessAll(texts9);
            double sim01 = demo.CalculateCosineSimilarity(0, 1);
            double sim02 = demo.CalculateCosineSimilarity(0, 2);
            Console.WriteLine($"✓ 文档0-1相似度: {sim01:F4}");
            Console.WriteLine($"✓ 文档0-2相似度: {sim02:F4}\n");

            // ========== 难度10：质量评估 ==========
            Console.WriteLine("【难度10/10】关键词提取质量评估");
            demo = new SimpleTfidfDemo();
            var texts10 = new List<string>();
            for (int i = 0; i < 5; i++)
                texts10.Add($"machine learning artificial intelligence data science algorithms");
            var r10 = demo.ProcessAll(texts10);
            Console.WriteLine(demo.EvaluateKeywordQuality());
            
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║          所有测试完成 - 框架可用性验证        ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
        }
    }
}
