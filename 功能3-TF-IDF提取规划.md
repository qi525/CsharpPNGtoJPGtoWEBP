# 功能3：TF-IDF 区分度关键词提取 - 详细规划【只读模式】

## ⚠️ 重要说明
**本功能完全为只读操作**，仅对图片文件进行扫描和分析，不涉及任何文件修改、移动或重命名操作。安全无风险。

## 1. 需求概述

### 1.1 功能定义
从清洗后的正向词（功能2的 `CorePositivePrompt`）中，使用 TF-IDF 算法计算词语的区分度，提取TOP 10个最具代表性的关键词。

### 1.2 前置条件
- ✅ 功能1：基础元数据提取（FilePath, Prompt等）
- ✅ 功能2：正向词清洗（CorePositivePrompt字段已生成）
- 数据约束：133,509 个图片文件 × 平均100-500词/文件

### 1.3 输出形式
**Excel新增列：** `TF-IDF区分度关键词(Top 10)`
- 格式：`关键词1(0.82)|关键词2(0.76)|...|关键词10(0.45)`
- 包含：Top10词语 + 其TF-IDF分数
- 文件完全只读，不修改原始数据

---

## 2. 技术方案设计

### 2.1 核心算法
**TF-IDF = TF × IDF**

```
TF (词频) = 词在文档中出现次数 / 文档总词数
IDF (逆文档频率) = log(总文档数 / 包含该词的文档数)
TF-IDF(词,文档) = TF × IDF
```

### 2.2 实现方案选择

| 方案 | 优点 | 缺点 | 推荐度 |
|------|------|------|--------|
| **手写实现** | 无依赖，轻量，易维护 | 需自行优化性能 | ⭐⭐⭐⭐⭐ |
| Accord.NET库 | 功能完整，经过验证 | 增加依赖，需学习API | ⭐⭐⭐ |
| ML.NET库 | 微软官方支持 | 过度设计，用大炮打蚊子 | ⭐⭐ |

**最终选择：** 手写实现（轻量、高效、易调试）

### 2.3 性能预期
- 单文件处理：< 1ms（Top10提取）
- 全量数据（133,509文件）：< 30秒
- 内存占用：< 500MB（文档库+词汇表）

---

## 3. 实现流程详细设计

### 3.1 前置功能：文本预处理模块

**函数：** `PreprocessText(string text) → string[]`

**步骤数：** 6步
```
原始文本 
  ↓ [1] 转小写
  ↓ [2] 移除特殊符号（保留中文、英文、数字）
  ↓ [3] 分词（空格、逗号、句号分割）
  ↓ [4] 去重（HashSet）
  ↓ [5] 过滤停用词（长度<2，常见词）
  ↓ [6] 返回词数组
结果：string[]
```

**伪代码：**
```csharp
private static string[] PreprocessText(string text)
{
    // [1] 小写化
    text = text.ToLowerInvariant();
    
    // [2] 正则移除非中文非英文非数字
    text = Regex.Replace(text, @"[^\u4e00-\u9fff\w\s]", " ");
    
    // [3] 分词 (空格/逗号/句号/中文逗号)
    var words = Regex.Split(text, @"[\s,，。、]+")
        .Where(w => !string.IsNullOrWhiteSpace(w))
        .ToArray();
    
    // [4] 去重 + [5] 过滤停用词
    var filtered = new HashSet<string>();
    foreach(var word in words)
    {
        if(word.Length >= 2 && !StopWords.Contains(word))
            filtered.Add(word);
    }
    return filtered.ToArray();
}
```

**复杂度分析：**
- 时间：O(n)，n=词总数
- 空间：O(m)，m=去重后词数
- 调用：每个文件调用1次

---

### 3.2 后置功能：TF-IDF核心计算

#### 第一步：构建文档库 (BuildDocumentLibrary)

**输入：** `List<MetadataRecord> allRecords`

**过程：**
```
遍历所有133,509个文件（只读）
  ↓
提取 CorePositivePrompt 字段
  ↓
调用 PreprocessText() 分词
  ↓
存储到 Document 对象：
  {
    DocId: int,
    Words: string[],
    WordCounts: Dictionary<string, int>
  }
  ↓
存储到全局 documents 列表
```

**数据结构：**
```csharp
public class Document
{
    public int DocId { get; set; }
    public string[] Words { get; set; }  // 去重后的词数组
    public Dictionary<string, int> WordCounts { get; set; }  // 词频统计
    public int TotalWords { get; set; }  // 词总数
}

// 全局词汇表
public static Dictionary<string, int> VocabularyDF { get; set; }  
// key: 词语, value: 包含该词的文档数(DF)
```

**复杂度：**
- 步骤数：1步（遍历）
- 函数数：1个 `BuildDocumentLibrary()`
- 时间复杂度：O(N × M)
  - N = 文件总数 (133,509)
  - M = 平均每文件词数 (200-300)
  - 预期：~20秒

---

#### 第二步：计算IDF全局表 (BuildIdfTable)

**输入：** `List<Document> documents`

**过程：**
```
统计每个词的文档频率 DF
  ↓
对每个词计算 IDF = log(总文档数 / DF)
  ↓
存储到全局 idfTable
```

**伪代码：**
```csharp
public static Dictionary<string, double> BuildIdfTable(List<Document> documents)
{
    int totalDocs = documents.Count;
    var idfTable = new Dictionary<string, double>();
    
    // 遍历每个词
    foreach(var word in VocabularyDF.Keys)
    {
        int df = VocabularyDF[word];  // 包含该词的文档数
        double idf = Math.Log10((double)totalDocs / df);  // log10(N/DF)
        idfTable[word] = idf;
    }
    
    return idfTable;
}
```

**复杂度：**
- 步骤数：2步（统计DF + 计算IDF）
- 函数数：1个 `BuildIdfTable()`
- 时间复杂度：O(V)
  - V = 总词汇量 (~50,000-100,000个词)
  - 预期：< 100ms

---

#### 第三步：为每文件计算TF-IDF (CalculateTfIdfScores)

**输入：** `Document doc, Dictionary<string, double> idfTable`

**过程：**
```
对文档中每个词计算 TF
  ↓
查询 IDF表获取该词的IDF值
  ↓
计算 TF-IDF = TF × IDF
  ↓
按TF-IDF分数降序排序
  ↓
取TOP 10
```

**伪代码：**
```csharp
public static List<(string Word, double Score)> CalculateTfIdfScores(
    Document doc, 
    Dictionary<string, double> idfTable)
{
    var scores = new List<(string, double)>();
    
    foreach(var word in doc.Words)
    {
        // 计算 TF
        double tf = (double)doc.WordCounts[word] / doc.TotalWords;
        
        // 查询 IDF
        if(idfTable.TryGetValue(word, out double idf))
        {
            double tfidf = tf * idf;
            scores.Add((word, tfidf));
        }
    }
    
    // 降序排序 + 取TOP 10
    return scores
        .OrderByDescending(x => x.Score)
        .Take(10)
        .ToList();
}
```

**复杂度：**
- 步骤数：4步（TF计算 + IDF查询 + TF-IDF计算 + 排序）
- 函数数：1个 `CalculateTfIdfScores()`
- 时间复杂度（单文件）：O(W log W)
  - W = 文件内不重词数 (~50-300)
  - 预期单文件：< 1ms

---

#### 第四步：并行批量提取 (ExtractTfidfFeaturesParallel)

**输入：** `List<MetadataRecord> allRecords, List<Document> documents`

**过程：**
```
使用 Parallel.ForEach 并行处理每个文件
  ↓
为每个 MetadataRecord 调用 CalculateTfIdfScores()
  ↓
格式化输出为字符串：
  "词1(0.82)|词2(0.76)|...|词10(0.45)"
  ↓
存储到 MetadataRecord.TfidfFeatures 字段（内存中，不写回源文件）
```

**伪代码：**
```csharp
public static void ExtractTfidfFeaturesParallel(
    List<MetadataRecord> records,
    List<Document> documents,
    Dictionary<string, double> idfTable,
    IProgress<int> progress = null)
{
    var docDict = documents.ToDictionary(d => d.DocId);
    
    Parallel.ForEach(records, new ParallelOptions { MaxDegreeOfParallelism = 8 }, 
        (record, state, index) =>
    {
        if(docDict.TryGetValue((int)index, out var doc))
        {
            var scores = CalculateTfIdfScores(doc, idfTable);
            record.TfidfFeatures = FormatScoresToString(scores);
        }
        
        // 报告进度
        progress?.Report((int)index);
    });
}

private static string FormatScoresToString(List<(string Word, double Score)> scores)
{
    var parts = scores.Select(s => $"{s.Word}({s.Score:F2})");
    return string.Join("|", parts);
}
```

**复杂度：**
- 步骤数：3步（并行循环 + 格式化 + 进度报告）
- 函数数：2个（主函数 + 格式化函数）
- 时间复杂度：O(N × W log W / P)
  - P = 并行度（通常 8-16）
  - 预期全量：5-10秒

---

## 4. 集成点设计

### 4.1 新增字段：MetadataRecord.TfidfFeatures

**修改位置：** `DevelopmentModeService.cs` → `MetadataRecord` 类

```csharp
public class MetadataRecord
{
    // 现有字段...
    public string CorePositivePrompt { get; set; } = string.Empty;
    
    // 新增字段
    public string TfidfFeatures { get; set; } = string.Empty;  // "词1(分数1)|词2(分数2)|..."
}
```

### 4.2 新增入口方法：RunScanMode3

**修改位置：** `DevelopmentModeService.cs`

```csharp
public static void RunScanMode3(string folder)
{
    Console.WriteLine($"🔄 功能3：TF-IDF区分度关键词提取【只读】");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // 第一步：构建文档库（需要调用功能2的数据）
    var documents = new List<Document>();
    var records = ScanAndExtractMetadata(folder);  // 复用功能2的扫描
    
    // 第二步：构建IDF表
    var idfTable = TfidfProcessorService.BuildIdfTable(documents);
    
    // 第三步：并行提取TF-IDF
    var progress = new Progress<int>(count => 
    {
        Console.WriteLine($"已处理: {count}/{records.Count}");
    });
    TfidfProcessorService.ExtractTfidfFeaturesParallel(records, documents, idfTable, progress);
    
    // 第四步：生成Excel报告
    ReportService.GenerateExcelReport(records, folder, scanMode: 3);
    
    stopwatch.Stop();
    Console.WriteLine($"✅ 功能3完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
}
```

### 4.3 修改Excel生成逻辑

**修改位置：** `ReportService.cs` → `GenerateExcelReport()`

新增列头处理：
```csharp
if(scanMode == 3 || scanMode == 4)  // 功能3和4都需要此列
{
    worksheet.Cell(1, columnIndex).Value = "TF-IDF区分度关键词(Top 10)";
    columnIndex++;
}
```

---

## 5. 新增文件清单

### 5.1 TfidfProcessorService.cs

**位置：** `src/ImageInfo/Services/TfidfProcessorService.cs`

**核心类和方法：**

```csharp
public static class TfidfProcessorService
{
    // ===== 公开方法 =====
    public static List<Document> BuildDocumentLibrary(List<MetadataRecord> records);
    public static Dictionary<string, double> BuildIdfTable(List<Document> documents);
    public static void ExtractTfidfFeaturesParallel(
        List<MetadataRecord> records,
        List<Document> documents,
        Dictionary<string, double> idfTable,
        IProgress<int> progress = null);
    
    // ===== 私有方法 =====
    private static string[] PreprocessText(string text);
    private static List<(string Word, double Score)> CalculateTfIdfScores(
        Document doc, 
        Dictionary<string, double> idfTable);
    private static string FormatScoresToString(List<(string Word, double Score)> scores);
    
    // ===== 常量和配置 =====
    public const int TOP_N_FEATURES = 10;
    private static readonly HashSet<string> StopWords = new();
}

public class Document
{
    public int DocId { get; set; }
    public string[] Words { get; set; }
    public Dictionary<string, int> WordCounts { get; set; }
    public int TotalWords { get; set; }
}
```

**代码行数估计：** 300-400行

---

## 6. 测试计划

### 6.1 单元测试 (xUnit)

**测试文件：** `tests/ImageInfo.Tests/TfidfProcessorTests.cs`

```csharp
public class TfidfProcessorTests
{
    // [1] 测试文本预处理
    [Fact]
    public void PreprocessText_RemoveSpecialChars_Success()
    {
        // Arrange
        string input = "beautiful, 美丽的 @#$% girl!!!";
        
        // Act
        var result = TfidfProcessorService.PreprocessText(input);
        
        // Assert
        Assert.Contains("beautiful", result);
        Assert.Contains("美丽的", result);
        Assert.DoesNotContain("@#$%", result);
    }
    
    // [2] 测试单文件TF-IDF计算
    [Fact]
    public void CalculateTfIdf_SingleDocument_Top10Extracted()
    {
        // 创建模拟Document
        var doc = new Document { /* ... */ };
        var idfTable = new Dictionary<string, double> { /* ... */ };
        
        // 调用方法
        var scores = TfidfProcessorService.CalculateTfIdfScores(doc, idfTable);
        
        // 验证：返回≤10个结果，按降序排列
        Assert.True(scores.Count <= 10);
        for(int i = 1; i < scores.Count; i++)
            Assert.True(scores[i-1].Score >= scores[i].Score);
    }
    
    // [3] 测试空文档处理
    [Fact]
    public void CalculateTfIdf_EmptyDocument_ReturnEmpty()
    {
        var emptyDoc = new Document { Words = new string[0], TotalWords = 0 };
        var idfTable = new Dictionary<string, double>();
        
        var result = TfidfProcessorService.CalculateTfIdfScores(emptyDoc, idfTable);
        
        Assert.Empty(result);
    }
    
    // [4] 性能测试：单文件处理时间
    [Fact]
    public void CalculateTfIdf_Performance_SingleFile_UnderOneMs()
    {
        var doc = GenerateLargeDocument(500);  // 500个词
        var idfTable = GenerateIdfTable(10000);  // 10000个词汇
        
        var sw = Stopwatch.StartNew();
        var result = TfidfProcessorService.CalculateTfIdfScores(doc, idfTable);
        sw.Stop();
        
        Assert.True(sw.ElapsedMilliseconds < 1);
    }
    
    // [5] 集成测试：全流程
    [Fact]
    public void ExtractTfidf_Integration_AllSteps_Success()
    {
        // 构造133k条模拟元数据
        var records = GenerateTestRecords(1000);  // 本地测试用1000条
        
        // 第1步：构建文档库
        var documents = TfidfProcessorService.BuildDocumentLibrary(records);
        Assert.NotEmpty(documents);
        
        // 第2步：构建IDF表
        var idfTable = TfidfProcessorService.BuildIdfTable(documents);
        Assert.NotEmpty(idfTable);
        
        // 第3步：提取TF-IDF
        TfidfProcessorService.ExtractTfidfFeaturesParallel(records, documents, idfTable);
        
        // 验证结果
        Assert.All(records, r => Assert.NotEmpty(r.TfidfFeatures));
    }
}
```

**测试用例数：** 5个
**覆盖率目标：** 85%+

---

## 7. 流程总结

### 7.1 实现步骤清单

| 序号 | 步骤 | 函数数 | 复杂度 | 预期耗时 |
|-----|------|--------|--------|---------|
| 1 | 文本预处理（分词、去重、过滤） | 1 | O(n) | - |
| 2 | 构建文档库 | 1 | O(N×M) | ~20秒 |
| 3 | 构建IDF全局表 | 1 | O(V) | ~100ms |
| 4 | TF-IDF单文件计算 | 1 | O(W log W) | < 1ms/文件 |
| 5 | 并行批量提取 | 1 | O(N×W log W/P) | 5-10秒 |
| 6 | 格式化输出 | 1 | O(N×10) | ~1秒 |
| 7 | 生成Excel报告 | 1 | O(N) | ~2秒 |

**总步骤数：** 7步
**总函数数：** 6个公开 + 3个私有 = 9个
**总复杂度：** O(N×M + V + N×W log W) ≈ O(N×M)
**预期全量耗时：** 28-33秒

### 7.2 前置功能 vs 后置功能

| 阶段 | 功能 | 依赖 | 关键操作 |
|------|------|------|---------|
| **前置** | 文本预处理 | 无 | 分词、去重、过滤 |
| **后置** | 文档库构建 | 前置 | 遍历所有文件（只读） |
| **后置** | IDF表计算 | 文档库 | 全局统计 |
| **后置** | TF-IDF提取 | IDF表 | 单文件计算 + 排序 |

---

## 8. 验收标准

- ✅ 所有7个函数实现完成
- ✅ 单元测试通过率 100%
- ✅ 代码覆盖率 ≥ 85%
- ✅ 133,509文件处理时间 < 35秒
- ✅ Excel报告生成成功，TF-IDF列正常显示
- ✅ Top10关键词按分数降序排列
- ✅ **无任何文件修改、只读操作**
- ✅ 无内存泄漏，无并发异常
