# 功能5 API 参考文档

## 📚 完整API参考

### ImageScorerConfig 配置类

位置: `src/ImageInfo/Models/ImageScorerConfig.cs`

#### 属性列表

##### 1. RatingMap
```csharp
public Dictionary<string, int> RatingMap { get; set; }
```
**说明**: 文件夹名称关键词到评分的映射  
**类型**: 字典 (关键词 → 分数)  
**默认值**:
```csharp
{
    { "特殊：100分", 100 },
    { "特殊：98分", 98 },
    { "超绝", 95 },
    { "特殊画风", 90 },
    { "超级精选", 85 },
    { "精选", 80 }
}
```
**示例使用**:
```csharp
var config = new ImageScorerConfig();
// 添加新规则
config.RatingMap.Add("我的最爱", 95);
// 修改现有规则
config.RatingMap["精选"] = 75;
```

---

##### 2. ScorePrefix
```csharp
public string ScorePrefix { get; set; }
```
**说明**: 自定义评分标记的前缀  
**默认值**: `"@@@评分"`  
**用途**: 识别文件名中的自定义分数标记  
**示例**:
```
文件名: image@@@评分75.jpg
匹配: @@@评分75 → 提取分数75
```

---

##### 3. DefaultNeutralScore
```csharp
public double DefaultNeutralScore { get; set; }
```
**说明**: 未被任何规则标注的图片的默认分数  
**类型**: double  
**默认值**: `50.0`  
**范围**: 0-100  
**用途**: 
- 作为基础分（文件夹默认匹配分）
- 作为模型的中性参考点
**示例**:
```csharp
config.DefaultNeutralScore = 60.0; // 改为60分
```

---

##### 4. LColumnIndex
```csharp
public int LColumnIndex { get; set; }
```
**说明**: 核心词汇列的索引（0-based）  
**类型**: int  
**默认值**: `11` (对应Excel的L列)  
**说明**: 第一列（A列）是索引0，第12列（L列）是索引11  
**示例**:
```csharp
config.LColumnIndex = 11;  // L列
config.LColumnIndex = 10;  // K列
config.LColumnIndex = 12;  // M列
```

---

##### 5. FolderMatchScoreColumn
```csharp
public string FolderMatchScoreColumn { get; set; }
```
**说明**: 新增"文件夹默认匹配分"列的列名  
**类型**: string  
**默认值**: `"文件夹默认匹配分"`  
**示例**:
```csharp
config.FolderMatchScoreColumn = "规则匹配分";
```

---

##### 6. PredictedScoreColumn
```csharp
public string PredictedScoreColumn { get; set; }
```
**说明**: 新增"个性化推荐预估评分"列的列名  
**类型**: string  
**默认值**: `"个性化推荐预估评分"`  
**示例**:
```csharp
config.PredictedScoreColumn = "ML预测分";
```

---

##### 7. TargetScoreColumn
```csharp
public string TargetScoreColumn { get; set; }
```
**说明**: 用于存储目标评分（内部使用）  
**类型**: string  
**默认值**: `"偏好定标分"`  
**备注**: 通常不需要修改

---

##### 8. RidgeAlpha
```csharp
public double RidgeAlpha { get; set; }
```
**说明**: Ridge回归的正则化参数  
**类型**: double  
**默认值**: `1.0`  
**范围**: 0.1 - 10.0 (推荐)  
**含义**:
- alpha越小 (0.1-0.5): 模型更灵活，易拟合特殊情况
- alpha = 1.0: 平衡的默认值
- alpha越大 (2.0-10.0): 模型更稳定，预测更平滑
**示例**:
```csharp
config.RidgeAlpha = 0.5;   // 更灵活
config.RidgeAlpha = 2.0;   // 更稳定
```

---

##### 9. EnableStopWordFilter
```csharp
public bool EnableStopWordFilter { get; set; }
```
**说明**: 是否过滤常用词（停用词）  
**类型**: bool  
**默认值**: `false`  
**说明**: 推荐保持false以保留所有特征信息  
**示例**:
```csharp
config.EnableStopWordFilter = true;  // 启用过滤
```

---

##### 10. MinTokenLength
```csharp
public int MinTokenLength { get; set; }
```
**说明**: 分词时的最小词长  
**类型**: int  
**默认值**: `1`  
**用途**: 过滤掉过短的词  
**示例**:
```csharp
config.MinTokenLength = 2;  // 只保留长度≥2的词
```

---

##### 11. MaxTokenLength
```csharp
public int MaxTokenLength { get; set; }
```
**说明**: 分词时的最大词长  
**类型**: int  
**默认值**: `100`  
**用途**: 过滤掉过长的词  
**示例**:
```csharp
config.MaxTokenLength = 30;  // 只保留长度≤30的词
```

---

### ImageScorerService 服务类

位置: `src/ImageInfo/Services/ImageScorerService.cs`

#### 构造函数

##### ImageScorerService(ImageScorerConfig config = null)
```csharp
public ImageScorerService(ImageScorerConfig config = null)
```
**参数**:
- `config`: 可选的配置对象，为null时使用默认配置

**示例**:
```csharp
// 使用默认配置
var scorer = new ImageScorerService();

// 使用自定义配置
var config = new ImageScorerConfig { RidgeAlpha = 0.5 };
var scorer = new ImageScorerService(config);
```

---

#### 公开方法

##### ScoreFromExcelAsync(string excelPath)
```csharp
public async Task<bool> ScoreFromExcelAsync(string excelPath)
```
**说明**: 主工作流程，读取Excel → 计算评分 → 保存结果  
**参数**:
- `excelPath` (string): Excel文件的完整路径

**返回值**:
- `true`: 处理成功
- `false`: 处理失败

**抛出异常**: 无（内部处理所有异常）

**示例**:
```csharp
var config = new ImageScorerConfig();
var scorer = new ImageScorerService(config);
bool success = await scorer.ScoreFromExcelAsync(@"C:\data\report.xlsx");

if (success)
{
    Console.WriteLine("✅ 评分完成!");
}
else
{
    Console.WriteLine("❌ 评分失败!");
}
```

---

##### ScoreDataTableAsync(DataTable dataTable)
```csharp
public async Task<bool> ScoreDataTableAsync(DataTable dataTable)
```
**说明**: 核心评分逻辑，直接处理DataTable  
**参数**:
- `dataTable` (DataTable): 包含文件路径和词汇的数据表

**返回值**:
- `true`: 处理成功
- `false`: 处理失败

**说明**: 此方法会修改输入的DataTable，添加新列：
- `FolderMatchScoreColumn` (文件夹默认匹配分)
- `PredictedScoreColumn` (个性化推荐预估评分)
- `TargetScoreColumn` (内部使用)

**示例**:
```csharp
var dataTable = ReadExcelFile(@"C:\data\report.xlsx");
var scorer = new ImageScorerService();
bool success = await scorer.ScoreDataTableAsync(dataTable);

// dataTable现在包含新的评分列
```

---

#### 私有方法（供参考）

##### ExtractFolderScore(string filePath)
```csharp
private double ExtractFolderScore(string filePath)
```
**说明**: 【难度0】从文件路径提取文件夹默认匹配分  
**参数**:
- `filePath` (string): 文件完整路径

**返回值**:
- `double`: 0-100范围内的评分

**逻辑流程**:
1. 检查自定义标记 (@@@评分75)
2. 检查RATING_MAP关键词匹配
3. 返回默认中性分

---

##### BuildVocabularyAndIDF(DataTable dataTable, string vocabColumn)
```csharp
private void BuildVocabularyAndIDF(DataTable dataTable, string vocabColumn)
```
**说明**: 【步骤A】构建词汇表并计算IDF值  
**参数**:
- `dataTable` (DataTable): 输入数据
- `vocabColumn` (string): 包含词汇的列名

**副作用**:
- 修改内部成员: `_vocabulary`, `_vocabularySize`

**计算公式**:
```
IDF(词) = log(总文档数 / 包含该词的文档数)
```

---

##### BuildTFIDFMatrix(DataTable dataTable, string vocabColumn)
```csharp
private double[][] BuildTFIDFMatrix(DataTable dataTable, string vocabColumn)
```
**说明**: 【步骤B】构建TF-IDF特征矩阵  
**参数**:
- `dataTable` (DataTable): 输入数据
- `vocabColumn` (string): 包含词汇的列名

**返回值**:
- `double[][]`: TF-IDF矩阵 (行=样本数, 列=词汇数)

**矩阵含义**:
- 每行代表一张图片
- 每列代表一个词汇
- 矩阵值 = TF(词频) × IDF(逆文档频率)

---

##### TrainRidgeRegression(double[][] tfidfMatrix, DataTable dataTable, List<int> trainingIndices)
```csharp
private void TrainRidgeRegression(double[][] tfidfMatrix, DataTable dataTable, List<int> trainingIndices)
```
**说明**: 【步骤C】训练Ridge回归模型学习权重  
**参数**:
- `tfidfMatrix` (double[][]): TF-IDF矩阵
- `dataTable` (DataTable): 包含目标分数的数据
- `trainingIndices` (List<int>): 训练样本的索引列表

**副作用**:
- 修改内部成员: `_modelWeights`, `_modelMeanScore`
- 输出Top 10权重词汇到控制台

---

##### PredictAllScores(DataTable dataTable, double[][] tfidfMatrix)
```csharp
private void PredictAllScores(DataTable dataTable, double[][] tfidfMatrix)
```
**说明**: 【步骤D】对所有图片进行个性化评分预测  
**参数**:
- `dataTable` (DataTable): 输出数据表
- `tfidfMatrix` (double[][]): TF-IDF矩阵

**副作用**:
- 在dataTable中填充`PredictedScoreColumn`列

**预测公式**:
```
分数 = 均值 + Σ(TF-IDF向量 × 学到的权重)
      (限制在0-100范围内)
```

---

### DevelopmentModeService 开发服务

位置: `src/ImageInfo/Services/DevelopmentModeService.cs`

#### 公开方法

##### RunScanMode5(string folder)
```csharp
public static void RunScanMode5(string folder)
```
**说明**: 功能5的入口点  
**参数**:
- `folder` (string): 根文件夹路径（此参数在功能5中未使用）

**作用**:
1. 显示功能说明
2. 提示用户输入Excel文件路径
3. 调用`RunImageScorerAsync`执行评分

**示例**:
```csharp
DevelopmentModeService.RunScanMode5(@"C:\images");
```

---

## 🔌 使用模式

### 模式1：快速启动（命令行）

```powershell
dotnet run -- --5
```

---

### 模式2：编程调用

```csharp
// 方式A：使用默认配置
var scorer = new ImageScorerService();
bool success = await scorer.ScoreFromExcelAsync(@"C:\data.xlsx");

// 方式B：使用自定义配置
var config = new ImageScorerConfig
{
    RatingMap = new Dictionary<string, int> { {"精选", 80} },
    RidgeAlpha = 0.5
};
var scorer = new ImageScorerService(config);
bool success = await scorer.ScoreFromExcelAsync(@"C:\data.xlsx");

// 方式C：处理DataTable
var dataTable = new DataTable();
// ... 填充dataTable ...
bool success = await scorer.ScoreDataTableAsync(dataTable);
```

---

### 模式3：集成到其他功能

```csharp
public static void ProcessAndScore(string excelPath)
{
    // 步骤1：执行其他处理（如功能4）
    // ...
    
    // 步骤2：执行评分
    var config = new ImageScorerConfig();
    var scorer = new ImageScorerService(config);
    await scorer.ScoreFromExcelAsync(excelPath);
    
    // 步骤3：继续后续处理
    // ...
}
```

---

## ⚙️ 配置示例

### 示例1：默认配置
```csharp
var scorer = new ImageScorerService();
// 使用所有默认值
```

### 示例2：自定义RATING_MAP
```csharp
var config = new ImageScorerConfig
{
    RatingMap = new Dictionary<string, int>
    {
        { "S级", 100 },
        { "A级", 80 },
        { "B级", 60 }
    }
};
var scorer = new ImageScorerService(config);
```

### 示例3：调整模型参数
```csharp
var config = new ImageScorerConfig
{
    RidgeAlpha = 0.5,           // 更灵活的模型
    MinTokenLength = 2,         // 过滤长度<2的词
    MaxTokenLength = 30         // 过滤长度>30的词
};
var scorer = new ImageScorerService(config);
```

### 示例4：完整自定义
```csharp
var config = new ImageScorerConfig
{
    RatingMap = new Dictionary<string, int>
    {
        { "favorite", 100 },
        { "good", 75 },
        { "ok", 50 }
    },
    ScorePrefix = "@score",
    DefaultNeutralScore = 45.0,
    LColumnIndex = 10,          // 改为K列
    FolderMatchScoreColumn = "RuleScore",
    PredictedScoreColumn = "MLScore",
    RidgeAlpha = 0.8,
    MinTokenLength = 2,
    MaxTokenLength = 25
};
var scorer = new ImageScorerService(config);
await scorer.ScoreFromExcelAsync(@"C:\data.xlsx");
```

---

## 📊 返回值说明

### ScoreFromExcelAsync 返回值

| 返回值 | 含义 | 说明 |
|-------|------|------|
| `true` | ✅ 成功 | Excel已更新，包含新的两列评分 |
| `false` | ❌ 失败 | 检查控制台输出查看错误信息 |

### ScoreDataTableAsync 返回值

| 返回值 | 含义 | 说明 |
|-------|------|------|
| `true` | ✅ 成功 | DataTable已修改，新增评分列 |
| `false` | ❌ 失败 | 检查日志了解具体原因 |

---

## 🔍 调试信息

程序会在控制台输出详细的处理信息，例如：

```
[功能5] 开始处理Excel文件: report.xlsx
[功能5] 读取成功，共 1000 行数据
[功能5] 识别的列: 路径列='文件路径', 词汇列='核心词汇'
[功能5] 开始计算文件夹默认匹配分 (难度0)...
  [自定义标记] '@@@评分75' → 75分
  [关键词匹配] '精选' → 80分
[功能5] 文件夹匹配分计算完成，找到 150 个训练样本
[功能5] 开始计算个性化推荐预估评分 (难度3)...
[功能5-A] 步骤A：构建词汇表和IDF值...
[功能5-A] 词汇表大小: 425
[功能5-B] 步骤B：构建TF-IDF矩阵...
[功能5-C] 步骤C：训练Ridge回归模型 (训练集大小: 150)...
[功能5-C] 学到的Top 10高权重词汇:
  少女: 0.8520 ↑ (正向)
  精致: 0.7234 ↑ (正向)
  ...
[功能5-D] 步骤D：预测所有图片的个性化评分...
[功能5] 个性化推荐预估评分计算完成

✅ 评分处理完成！
```

---

## ❌ 错误处理

所有异常都在内部捕获并记录，用户会看到清晰的错误消息：

```
[错误] Excel文件为空或无法读取
[错误] DataFrame中没有任何列
[错误] Excel列数不足，无法找到L列
[警告] 未找到训练样本，使用默认评分
```

---

## 性能指标

| 操作 | 耗时 | 说明 |
|-----|------|------|
| 读取Excel (1000行) | <0.5秒 | 取决于文件大小 |
| 构建词汇表 | <0.2秒 | 取决于词汇量 |
| 构建TF-IDF矩阵 | 0.2-0.5秒 | O(样本数×词汇数) |
| 训练模型 | <0.2秒 | 取决于训练样本数 |
| 预测所有 | <0.3秒 | O(样本数×词汇数) |
| 写入Excel | <0.5秒 | 取决于行数 |
| **总计** | **1-2秒** | 对于1000行数据 |

---

## 兼容性

| 框架 | 版本 | 状态 |
|-----|------|------|
| .NET | 10.0 | ✅ 支持 |
| ClosedXML | 0.105.0 | ✅ 已集成 |
| C# | 12.0+ | ✅ 支持 |

---

**完整的API参考文档完成！** 🎉
