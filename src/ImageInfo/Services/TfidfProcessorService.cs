using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageInfo.Services
{
    /// <summary>
    /// TF-IDF处理服务：用于从文本中提取关键词
    /// 功能3：完全只读模式，不修改任何文件
    /// </summary>
    public class TfidfProcessorService
    {
        // ==================== 数据结构 ====================
        
        /// <summary>
        /// 代表一个文档（行）的TF-IDF数据
        /// </summary>
        public class Document
        {
            public int DocId { get; set; }
            public string[]? Words { get; set; }
            public Dictionary<string, int>? WordCounts { get; set; }
            public int TotalWords { get; set; }
        }

        /// <summary>
        /// TF-IDF处理结果
        /// </summary>
        public class TfidfResult
        {
            public int DocId { get; set; }
            public string? ExcelString { get; set; }      // 包含分数的格式化字符串，用于Excel
            public List<string>? TopKeywords { get; set; } // 仅关键词列表，用于文件名
        }

        // ==================== 类成员变量 ====================
        
        private Dictionary<string, int> _vocabularyDf;    // 词汇表的文档频率
        private Dictionary<string, double> _idfTable;     // IDF查询表
        private List<Document> _documents;
        private int _topN = 10;                           // 提取Top N个关键词
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
            "in", "to", "for", "of", "with", "by", "from", "as", "be", "have", "are", "was", "were"
        };
        private bool _enableStopWordFilter = false;

        /// <summary>
        /// 【可选优化2】词长限制 - 只保留指定长度范围的词
        /// 使用场景：过滤过短或过长的词
        /// </summary>
        private int _minWordLength = 2;
        private int _maxWordLength = 50;
        private bool _enableWordLengthFilter = false;

        /// <summary>
        /// 【可选优化3】性能统计 - 跟踪各阶段耗时
        /// 使用场景：性能优化、瓶颈识别
        /// </summary>
        private Dictionary<string, long> _performanceStats = new Dictionary<string, long>();
        private bool _enablePerformanceTracking = false;

        // ==================== 公共方法 ====================

        /// <summary>
        /// 初始化服务
        /// </summary>
        public TfidfProcessorService(int topN = 10)
        {
            _topN = topN;
            _vocabularyDf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _idfTable = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            _documents = new List<Document>();
        }

        /// <summary>
        /// 框架方法1：预处理文本 - 6步清洗流程
        /// 步骤：1.小写 2.分隔符替换 3.分词 4.去除空词 5.计数 6.返回
        /// 支持可选的StopWords和词长过滤
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

            // 步骤1：转小写
            string text = rawText.ToLower();

            // 步骤2：替换特殊分隔符为空格
            text = Regex.Replace(text, @"[\n,:_()\[\]\-]+", " ");
            
            // 步骤3：分词 - 使用正则表达式提取所有单词
            var matches = _tokenRegex.Matches(text);
            var words = new List<string>();
            foreach (Match match in matches)
            {
                string word = match.Value;
                
                // 应用可选过滤器
                if (!ShouldKeepWord(word)) continue;              // 【优化2】StopWords过滤
                if (!IsValidWordLength(word)) continue;           // 【优化3】词长限制
                
                words.Add(word);
            }

            // 步骤4：计数 - 统计每个词出现的次数
            var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var word in words)
            {
                if (wordCounts.ContainsKey(word))
                    wordCounts[word]++;
                else
                    wordCounts[word] = 1;
            }

            // 步骤5：创建Document对象
            var doc = new Document
            {
                DocId = docId,
                Words = words.ToArray(),
                WordCounts = wordCounts,
                TotalWords = words.Count
            };

            return doc;
        }

        /// <summary>
        /// 框架方法2：构建文档库 - 遍历所有文本
        /// 时间复杂度：O(N*M)，N=文档数，M=平均词数
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

            // 计算词的文档频率（为后续IDF计算准备数据）
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
        /// 框架方法3：构建IDF表
        /// 步骤：1.遍历所有文档 2.统计词的文档频率 3.计算IDF值 4.存储到表中
        /// IDF = log(N / df)，N=总文档数，df=包含该词的文档数
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
                
                // IDF = log(N / df)
                double idf = Math.Log((double)totalDocs / df);
                _idfTable[word] = idf;
            }
        }

        /// <summary>
        /// 框架方法4：计算单个文档的TF-IDF分数
        /// 时间复杂度：O(W*log W)，W=文档中的不同词数
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

            // 计算每个词的TF-IDF分数
            var wordScores = new List<(string word, double score)>();

            foreach (var kvp in doc.WordCounts)
            {
                string word = kvp.Key;
                int count = kvp.Value;

                // TF = 词在文档中出现次数 / 文档总词数
                double tf = (double)count / doc.TotalWords;

                // IDF从表中获取（如果词不在表中，给个默认值）
                double idf = _idfTable.ContainsKey(word) ? _idfTable[word] : Math.Log(_documents.Count + 1);

                // TF-IDF = TF * IDF
                double tfidf = tf * idf;

                wordScores.Add((word, tfidf));
            }

            // 按TF-IDF分数降序排列，取Top N
            var topWords = wordScores
                .OrderByDescending(x => x.score)
                .Take(_topN)
                .ToList();

            // 格式化为Excel字符串和关键词列表
            var excelString = FormatExcelString(topWords);
            var topKeywords = topWords.Select(x => x.word).ToList();

            return new TfidfResult
            {
                DocId = doc.DocId,
                ExcelString = excelString,
                TopKeywords = topKeywords
            };
        }

        /// <summary>
        /// 框架方法5：批量并行提取TF-IDF特征
        /// 使用Parallel.ForEach进行多线程处理
        /// 时间复杂度：O(N*W*log W / P)，P=线程数
        /// </summary>
        public List<TfidfResult> ExtractTfidfFeaturesParallel(int maxDegreeOfParallelism = 8)
        {
            var results = new List<TfidfResult>();
            var lockObj = new object();

            var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            Parallel.ForEach(_documents, options, doc =>
            {
                var result = CalculateTfIdfScores(doc);
                lock (lockObj)
                {
                    results.Add(result);
                }
            });

            // 按DocId排序，确保顺序一致
            return results.OrderBy(r => r.DocId).ToList();
        }

        /// <summary>
        /// 框架方法6：处理一行数据的完整流程
        /// 输入：原始文本 → 输出：TF-IDF结果
        /// </summary>
        public TfidfResult ProcessSingleRow(int docId, string rawText)
        {
            // 临时构建单个文档的库
            var texts = new List<string> { rawText };
            BuildDocumentLibrary(texts);
            BuildIdfTable();
            
            if (_documents.Count > 0)
            {
                return CalculateTfIdfScores(_documents[0]);
            }

            return new TfidfResult
            {
                DocId = docId,
                ExcelString = "",
                TopKeywords = new List<string>()
            };
        }

        /// <summary>
        /// 框架方法7：主流程 - 从文本列表生成TF-IDF特征
        /// </summary>
        public List<TfidfResult> ProcessAll(List<string> texts)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[框架] ProcessAll - 开始处理 {texts.Count} 条文本");

            // 步骤1：预处理所有文本
            Console.WriteLine("[步骤1] 文本预处理中...");
            BuildDocumentLibrary(texts);

            // 步骤2：构建文档库
            Console.WriteLine("[步骤2] 文档库已构建");

            // 步骤3：构建IDF表
            Console.WriteLine("[步骤3] 构建IDF表中...");
            BuildIdfTable();

            // 步骤4：并行提取特征
            Console.WriteLine("[步骤4] 并行提取特征中...");
            var results = ExtractTfidfFeaturesParallel();

            sw.Stop();
            Console.WriteLine($"[框架] ProcessAll - 完成，耗时 {sw.ElapsedMilliseconds}ms");
            return results;
        }

        /// <summary>
        /// 私有方法1：文本分词 - 使用正则表达式
        /// </summary>
        private string[] Tokenize(string text)
        {
            var matches = _tokenRegex.Matches(text);
            var words = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                words[i] = matches[i].Value;
            }
            return words;
        }

        /// <summary>
        /// 私有方法2：规范化单个词汇 - 小写、去除特殊字符等
        /// </summary>
        private string NormalizeWord(string word)
        {
            return word?.ToLower().Trim() ?? "";
        }

        /// <summary>
        /// 私有方法3：计算单词的TF值
        /// TF = 词在文档中出现次数 / 文档总词数
        /// </summary>
        private double CalculateTf(int wordCount, int totalWords)
        {
            if (totalWords == 0) return 0;
            return (double)wordCount / totalWords;
        }

        /// <summary>
        /// 私有方法4：格式化结果为Excel字符串
        /// 格式：word1(0.82)|word2(0.76)|...
        /// </summary>
        private string FormatExcelString(List<(string word, double score)> topWords)
        {
            if (topWords == null || topWords.Count == 0)
                return "";

            var formatted = topWords.Select(w => $"{w.word}({w.score:F4})");
            return string.Join("|", formatted);
        }

        /// <summary>
        /// 私有方法5：从文本中提取特殊符号和连接符
        /// </summary>
        private string PreprocessSpecialChars(string text)
        {
            // 替换常见的分隔符为空格
            return Regex.Replace(text, @"[\n,:_()\[\]\-]+", " ");
        }

        // ==================== 测试/调试方法 ====================

        /// <summary>
        /// 输出当前的IDF表（用于调试）
        /// </summary>
        public void PrintIdfTable()
        {
            Console.WriteLine("\n========== IDF表信息 ==========");
            Console.WriteLine($"总词汇量: {_idfTable.Count}");
            var topWords = _idfTable.OrderByDescending(kv => kv.Value).Take(10);
            foreach (var kv in topWords)
            {
                Console.WriteLine($"  {kv.Key}: {kv.Value:F4}");
            }
            Console.WriteLine("==============================\n");
        }

        /// <summary>
        /// 输出单个文档的处理结果
        /// </summary>
        public void PrintDocumentResult(TfidfResult result)
        {
            Console.WriteLine($"\n[文档 {result.DocId} 结果]");
            Console.WriteLine($"  Excel字符串: {result.ExcelString}");
            Console.WriteLine($"  关键词: {string.Join(", ", result.TopKeywords ?? new List<string>())}");
        }

        // ==================== 优化函数集 (可插拔) ====================

        /// <summary>
        /// 【优化函数1】启用/禁用各项过滤和统计
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
        /// 【优化函数2】应用StopWords过滤
        /// 难度：1/10 - 简单的集合查询
        /// </summary>
        private bool ShouldKeepWord(string word)
        {
            if (!_enableStopWordFilter) return true;
            return !_stopWords.Contains(word);
        }

        /// <summary>
        /// 【优化函数3】应用词长限制
        /// 难度：1/10 - 简单的长度检查
        /// </summary>
        private bool IsValidWordLength(string word)
        {
            if (!_enableWordLengthFilter) return true;
            return word.Length >= _minWordLength && word.Length <= _maxWordLength;
        }

        /// <summary>
        /// 【优化函数4】计算文档统计信息
        /// 难度：3/10 - 遍历和统计
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
        /// 【优化函数5】过滤低频词
        /// 难度：3/10 - 字典过滤
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
        }

        /// <summary>
        /// 【优化函数6】获取词的IDF排名
        /// 难度：4/10 - 排序和选择
        /// 使用场景：分析词汇重要性、验证IDF计算
        /// </summary>
        public List<(string Word, double IDF, int DF)> GetTopIdfWords(int count = 20)
        {
            return _idfTable
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .Select(kv => (kv.Key, kv.Value, _vocabularyDf.ContainsKey(kv.Key) ? _vocabularyDf[kv.Key] : 0))
                .ToList();
        }

        /// <summary>
        /// 【优化函数7】TF-IDF向量化
        /// 难度：6/10 - 矩阵运算
        /// 使用场景：机器学习特征提取、相似度计算
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
        /// 【优化函数8】计算文档相似度 (余弦相似度)
        /// 难度：7/10 - 向量运算
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
    }
}
