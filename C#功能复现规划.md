# ImageInfo C# 项目功能复现规划

## 📋 项目概述

本规划文档描述了从 Python 代码向 C# 重构的完整功能实现计划，涉及四个只读扫描功能的渐进式开发。

**核心原则：**
- ✅ **渐进式迭代**：一个功能一列，逐步扩展
- ✅ **只读操作**：暂不实现文件移动/重命名（高风险）
- ✅ **充分测试**：每个功能都需大量测试验证
- ✅ **代码复用**：基于已验证的 Python 实现进行复现和优化

---

## 🎯 功能映射表

| 功能号 | 功能名称 | 核心特性 | Excel 新增列 | 前置条件 | 状态 |
|--------|---------|---------|-------------|---------|------|
| 1 | 基础扫描（未清洗） | 提取元数据 | 无 | 无 | ✅ 完成 |
| 2 | 清洗扫描 | 清洗正向词 | `正向词核心词提取` | 功能1 | ✅ 完成 |
| 3 | 图片评分转换 | 评分+重命名 | （属于处理功能，暂不实现） | 功能2 | ⏸️ 暂停 |
| 4 | 文件分类 | 文件分类移动 | （属于处理功能，暂不实现） | 功能2 | ⏸️ 暂停 |
| **5** | **TF-IDF 提取** | TF-IDF Top10 | `TF-IDF区分度关键词(Top 10)` | 功能2 | 🔄 **进行中** |
| **6** | **个性化评分** | 模型预测评分 | `偏好定标分`、`个性化推荐预估评分` | 功能5 | 📋 **待实现** |

---

## 🏗️ 总体架构设计

```
┌─────────────────────────────────────────────────────┐
│                    Program.cs                       │
│   快速启动菜单：--1, --2, --5, --6                 │
└────────────────┬────────────────────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
   ┌────▼────┐      ┌────▼────┐
   │功能1-2  │      │功能5-6  │
   │基础扫描 │      │高级分析 │
   └────┬────┘      └────┬────┘
        │                │
   ┌────▼────────────────▼────┐
   │  DevelopmentModeService   │
   │                           │
   │ RunScanMode()      - 功能1│
   │ RunScanMode2()     - 功能2│
   │ RunScanMode5()     - 功能5│ (新增)
   │ RunScanMode6()     - 功能6│ (新增)
   └────┬────────────────────┘
        │
   ┌────▼──────────────────────┐
   │    核心服务层               │
   │                            │
   │ MetadataExtractors      │
   │ PromptCleanerService    │
   │ TfidfProcessorService   │ (新增)
   │ ImageScorerService      │ (新增)
   └────────────────────────────┘
        │
   ┌────▼──────────────────────┐
   │   Excel 报告生成            │
   │                            │
   │ GenerateExcelReport()    │
   │ (动态列头，按功能灵活调整) │
   └────────────────────────────┘
```

---

## 📝 详细实现计划

### 第一阶段：功能5（TF-IDF Top10 提取）

#### 1.1 需求分析

**Python 实现参考：**
- `tfidf_processor.py` 的 `calculate_and_extract_tfidf()` 函数
- 输入：清洗后的正向词（来自功能2的 `CorePositivePrompt` 列）
- 输出：Top10 关键词列表（含TF-IDF分数）

**C# 实现目标：**
- 支持 TF-IDF 向量化计算
- 并行提取每个文档的 Top10 关键词
- 生成 Excel 友好的格式（关键词+分数）

#### 1.2 核心类设计

**新文件：`TfidfProcessorService.cs`**

```csharp
public static class TfidfProcessorService
{
    /// <summary>
    /// TF-IDF 配置常量
    /// </summary>
    public const int TOP_N_FEATURES = 10;
    public const string TFIDF_COLUMN_NAME = "TF-IDF区分度关键词(Top 10)";
    
    /// <summary>
    /// 计算 TF-IDF 并提取 Top N 关键词
    /// 
    /// 算法流程：
    /// 1. 数据预处理：将标签文本标准化（分词、去特殊符号）
    /// 2. TF-IDF 向量化：使用 Accord.NET 或手动实现
    /// 3. 并行提取：为每个文档提取 Top10 关键词
    /// 4. 格式化输出：关键词 + TF-IDF分数
    /// </summary>
    public static List<string> ExtractTfidfFeatures(
        List<MetadataRecord> records, 
        int topN = TOP_N_FEATURES)
    {
        // TODO: 实现 TF-IDF 计算逻辑
        // 返回：[关键词1 (0.5678)\n关键词2 (0.4567)\n...]
    }
}
```

**扩展 MetadataRecord：**

```csharp
public class MetadataRecord
{
    // ... 现有字段 ...
    
    // [新增] 功能5 字段
    public string TfidfFeatures { get; set; } = string.Empty;
}
```

#### 1.3 实现步骤

| 步骤 | 任务 | 技术方案 | 测试验证 |
|------|------|---------|---------|
| 1 | 集成 TF-IDF 库 | Accord.NET / SharpNLP | 单元测试 |
| 2 | 实现文本预处理 | 正则表达式标准化 | 集成测试 |
| 3 | 实现向量化计算 | IDF 权重计算 | 单元测试 |
| 4 | 实现并行提取 | Parallel.ForEach | 性能测试 |
| 5 | 集成到 RunScanMode5 | 功能5 入口 | 端到端测试 |
| 6 | Excel 列动态调整 | 列头和数据填充 | 报告验证 |

#### 1.4 集成点

```csharp
// Program.cs 新增入口
if (devMode?.ToLowerInvariant() == "5")
{
    Console.WriteLine("开发功能5： [只读模式-TF-IDF分析] 添加TF-IDF Top10");
    DevelopmentModeService.RunScanMode5(folder);
    return 0;
}

// DevelopmentModeService.cs 新增方法
public static void RunScanMode5(string folder)
{
    // 1. 调用 RunScanInternal 获取功能2的扫描结果
    // 2. 调用 TfidfProcessorService.ExtractTfidfFeatures()
    // 3. 添加 TfidfFeatures 到 MetadataRecord
    // 4. 生成 Excel（新增 TF-IDF 列）
}
```

---

### 第二阶段：功能6（个性化评分预测）

#### 2.1 需求分析

**Python 实现参考：**
- `image_scorer_supervised.py` 的 `ImageScorer` 和 `Ridge` 回归模型
- 输入：TF-IDF 向量 + 基准评分（文件夹名称或文件名标记）
- 输出：`偏好定标分` 和 `个性化推荐预估评分` 两列

**C# 实现目标：**
- 支持特征向量学习（Ridge 回归）
- 基于文件夹名称提取评分标签
- 模型训练与预测

#### 2.2 核心类设计

**新文件：`ImageScorerService.cs`**

```csharp
public class ScorerConfig
{
    /// <summary>
    /// 文件夹名称到评分的映射（学习信号）
    /// </summary>
    public static readonly Dictionary<string, int> RATING_MAP = new()
    {
        { "特殊：98分", 98 },
        { "超绝", 95 },
        { "特殊画风", 90 },
        { "超级精选", 85 },
        { "精选", 80 }
    };
    
    public const string SCORE_PREFIX = "@@@评分";
    public const float DEFAULT_NEUTRAL_SCORE = 50.0f;
    
    public const string TARGET_SCORE_COLUMN = "偏好定标分";
    public const string PREDICTED_SCORE_COLUMN = "个性化推荐预估评分";
}

public class ImageScorer
{
    /// <summary>
    /// 核心评分逻辑：
    /// 1. 从文件路径提取基准评分
    /// 2. TF-IDF 特征向量化
    /// 3. Ridge 回归模型训练
    /// 4. 全集预测评分
    /// </summary>
    public ScoreResult Score(List<MetadataRecord> records)
    {
        // TODO: 实现评分预测
    }
}
```

#### 2.3 实现步骤

| 步骤 | 任务 | 技术方案 | 测试验证 |
|------|------|---------|---------|
| 1 | 集成 ML.NET | Ridge 回归模型 | 单元测试 |
| 2 | 实现评分提取 | 正则匹配文件夹名 | 集成测试 |
| 3 | 实现特征向量化 | TF-IDF 向量化 | 单元测试 |
| 4 | 实现模型训练 | Ridge 回归 API | 性能测试 |
| 5 | 实现预测逻辑 | 向量预测 | 单元测试 |
| 6 | 集成到 RunScanMode6 | 功能6 入口 | 端到端测试 |

#### 2.4 集成点

```csharp
// Program.cs 新增入口
if (devMode?.ToLowerInvariant() == "6")
{
    Console.WriteLine("开发功能6： [只读模式-个性化评分] 添加评分预测列");
    DevelopmentModeService.RunScanMode6(folder);
    return 0;
}

// DevelopmentModeService.cs 新增方法
public static void RunScanMode6(string folder)
{
    // 1. 调用 RunScanInternal 获取功能5的扫描结果
    // 2. 调用 ImageScorerService.ScoreRecords()
    // 3. 添加评分列到 MetadataRecord
    // 4. 生成 Excel（新增两列评分）
}
```

---

## 🧪 测试计划

### 测试框架
- **单元测试**：xUnit + Moq
- **集成测试**：真实文件测试集
- **性能测试**：大数据集（133,509 文件）

### 功能5 测试用例

```csharp
[TestClass]
public class TfidfProcessorServiceTests
{
    [TestMethod]
    public void ExtractTfidfFeatures_WithValidInput_ReturnsTopNFeatures()
    {
        // Arrange
        var records = new List<MetadataRecord>
        {
            new() { CorePositivePrompt = "beautiful girl with long hair" },
            new() { CorePositivePrompt = "girl with beautiful face" }
        };
        
        // Act
        var result = TfidfProcessorService.ExtractTfidfFeatures(records, topN: 3);
        
        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].Contains("("));  // 包含分数
    }
    
    [TestMethod]
    public void ExtractTfidfFeatures_WithEmptyInput_ReturnsEmptyList()
    {
        // Arrange / Act / Assert
    }
    
    [TestMethod]
    [Timeout(60000)]  // 60秒超时
    public void ExtractTfidfFeatures_PerformanceTest_ProcessLargeDataset()
    {
        // 测试 133,509 文件的性能
    }
}
```

### 功能6 测试用例

```csharp
[TestClass]
public class ImageScorerServiceTests
{
    [TestMethod]
    public void ScoreRecords_WithLabeledData_TrainsModelSuccessfully()
    {
        // 测试模型训练
    }
    
    [TestMethod]
    public void ScoreRecords_PredictAccuracy_WithinReasonableRange()
    {
        // 测试预测精度（预测值与实际值的偏差）
    }
}
```

---

## 📊 Excel 报告结构演变

### 功能1 Excel 列结构
```
1. 文件名
2. 文件绝对路径
3. 文件所在文件夹路径
4. 格式
5. Prompt
6. NegativePrompt
7. Model
8. ModelHash
9. Seed
10. Sampler
11. 其他信息
12. 完整信息
13. 提取方法
```

### 功能2 Excel 列结构（新增1列）
```
... [功能1所有列] ...
14. 正向词核心词提取
```

### 功能5 Excel 列结构（新增1列）
```
... [功能2所有列] ...
15. TF-IDF区分度关键词(Top 10)
```

### 功能6 Excel 列结构（新增2列）
```
... [功能5所有列] ...
16. 偏好定标分
17. 个性化推荐预估评分
```

---

## ⚠️ 风险管理

### 高风险操作（暂不实现）
- ❌ 文件重命名（功能3）
- ❌ 文件移动/分类（功能4）
- ❌ 文件删除

**风险原因：**
- 无法完全恢复（数据丢失风险）
- 需要大量测试和备份验证
- 一次失败影响整个数据集

### 低风险操作（已实现）
- ✅ 只读扫描
- ✅ 元数据提取
- ✅ Excel 报告生成
- ✅ 内存数据处理

---

## 🔄 开发迭代流程

```
功能1-2 (已完成)
    ↓
功能5 分析设计
    ↓
功能5 实现编码
    ↓
功能5 单元测试
    ↓
功能5 集成测试 ← [132,509 文件真实测试]
    ↓
功能5 性能优化
    ↓
功能5 上线
    ↓
功能6 分析设计
    ↓
...（同上）
    ↓
功能6 上线
```

---

## 📦 依赖库清单

### 现有（已集成）
- ClosedXML (Excel 操作)
- MetadataExtractor (图片元数据)
- System.Text.RegularExpressions (文本处理)

### 新增需求
| 库名 | 功能 | 用途 | 替代方案 |
|-----|------|------|---------|
| Accord.NET | TF-IDF | 功能5 向量化 | 手动实现 TF-IDF |
| ML.NET | Ridge 回归 | 功能6 模型训练 | scikit-learn (跨进程) |
| MathNet.Numerics | 数值计算 | 矩阵操作 | Accord.NET 包含 |

---

## 📅 项目时间表

| 阶段 | 功能 | 预计耗时 | 开始日期 | 完成日期 |
|------|------|---------|---------|---------|
| Phase 1 | 功能5 设计 | 1-2 天 | 2025-11-22 | 2025-11-23 |
| Phase 2 | 功能5 编码 | 3-5 天 | 2025-11-24 | 2025-11-28 |
| Phase 3 | 功能5 测试 | 2-3 天 | 2025-11-29 | 2025-12-01 |
| Phase 4 | 功能6 设计 | 1-2 天 | 2025-12-02 | 2025-12-03 |
| Phase 5 | 功能6 编码 | 3-5 天 | 2025-12-04 | 2025-12-08 |
| Phase 6 | 功能6 测试 | 2-3 天 | 2025-12-09 | 2025-12-11 |

---

## ✅ 交付清单

### 功能5 完成标志
- [ ] `TfidfProcessorService.cs` 实现完整
- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试通过（真实数据集）
- [ ] 性能测试通过（< 30 秒处理 133,509 文件）
- [ ] Excel 报告结构正确
- [ ] 文档更新完整

### 功能6 完成标志
- [ ] `ImageScorerService.cs` 实现完整
- [ ] 模型训练与预测逻辑验证
- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试通过（真实数据集）
- [ ] 评分预测精度验证
- [ ] Excel 报告结构正确
- [ ] 文档更新完整

---

## 📖 参考文档

- Python 源码：`getIMGINFOandClassify.py`、`tfidf_processor.py`、`image_scorer_supervised.py`
- C# 现有代码：`DevelopmentModeService.cs`、`PromptCleanerService.cs`
- 技术文档：[TF-IDF 原理](https://en.wikipedia.org/wiki/Tf%E2%80%93idf)、[Ridge 回归](https://scikit-learn.org/stable/modules/generated/sklearn.linear_model.Ridge.html)

---

**最后更新：2025-11-21**  
**作者：AI Assistant**
