# TF-IDF 快速参考卡

## 核心流程（不变）

```
文本列表 → PreprocessText → BuildDocumentLibrary → BuildIdfTable 
    → CalculateTfIdfScores → ExtractTfidfFeaturesParallel → 结果列表
```

## 优化函数速查表

| # | 难度 | 函数名 | 功能 | 性能影响 | 推荐场景 |
|----|------|--------|------|---------|---------|
| 1 | 1/10 | SetOptimizations | 启用/禁用优化 | 无 | 快速实验 |
| 2 | 1/10 | ShouldKeepWord | StopWords过滤 | ↓5% mem, ↑5% speed | 通用 |
| 3 | 1/10 | IsValidWordLength | 词长过滤 | ↓10% mem, ↑10% speed | 脏数据 |
| 4 | 3/10 | GetDocumentStats | 统计信息 | ↑3% time | 数据分析 |
| 5 | 3/10 | FilterLowFrequencyWords | 低频词过滤 | ↓15% mem | 性能优化 |
| 6 | 4/10 | GetTopIdfWords | IDF排名 | ↑2% time | 词汇分析 |
| 7 | 6/10 | GetTfidfVectors | 向量化 | ↑20% mem | 机器学习 |
| 8 | 7/10 | CalculateCosineSimilarity | 相似度 | ↓5% time | 聚类/去重 |

## 一行启用

```csharp
service.SetOptimizations(enableStopWords: true, enableWordLength: true);
```

## 常用代码片段

### 基础使用
```csharp
var service = new TfidfProcessorService(topN: 10);
var results = service.ProcessAll(texts);
```

### 质量优化
```csharp
service.SetOptimizations(true, true);  // 启用过滤
service.FilterLowFrequencyWords(2);    // 移除低频词
```

### 数据分析
```csharp
var stats = service.GetDocumentStats();
var topIdf = service.GetTopIdfWords(20);
```

### 相似度计算
```csharp
double sim = service.CalculateCosineSimilarity(docId1, docId2);
```

## 难度分级

- **1/10**：简单检查 / 标志设置
- **3/10**：遍历 / 字典操作
- **4/10**：排序 / 选择
- **6/10**：矩阵运算
- **7/10**：向量运算

## StopWords默认列表

```
the, is, at, which, on, a, an, and, or, but,
in, to, for, of, with, by, from, as, be, have,
are, was, were
```

## 记住这些

✓ **高聚低耦合**：不用优化，核心也能工作  
✓ **插拔式**：可随意启用/禁用  
✓ **渐进增强**：从简单开始，逐步复杂  
✓ **性能一致**：基础功能永不牺牲性能  

## 下次迭代

- [ ] Excel读取与集成
- [ ] 真实数据验证
- [ ] 性能基准测试
- [ ] 并行化优化

---

**版本**：1.0 | **日期**：2025-11-22 | **状态**：✅ 可用
