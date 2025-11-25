# FilenameParser 文件名解析工具使用指南

## 概述

`FilenameParser` 是一个专门用于解析文件名的工具类，支持识别文件名中的**原始名称**、**后缀部分**和**文件扩展名**。

### 文件名格式

支持的文件名格式为：**原名 + 后缀 + 格式**

```
原名___tag1___tag2___tag3@@@评分88.jpg
```

- **原名**：核心内容，例如 `00000-2365214977`
- **后缀**：算法生成的部分，例如 `___blue_archive___whip___mari...@@@评分88`（可选）
- **格式**：文件扩展名，例如 `.jpg` / `.png` / `.webp`

## 核心方法

### 1. `ParseFilename(string filename)` - 主解析方法

**功能**：解析文件名，返回结构化的解析结果。

**输入**：完整的文件名（包含扩展名）

**返回**：`FilenameParseResult` 对象

**示例**：

```csharp
var filename = "00000-2365214977___blue_archive___whip___dominatrix@@@评分88.jpg";
var result = FilenameParser.ParseFilename(filename);

if (result.IsSuccess)
{
    Console.WriteLine($"原名: {result.OriginalName}");        // 00000-2365214977
    Console.WriteLine($"扩展名: {result.Extension}");          // .jpg
    Console.WriteLine($"后缀: {result.Suffix}");              // ___blue_archive___whip___dominatrix@@@评分88
    Console.WriteLine($"重建文件名: {result.RebuiltFilename}"); // 00000-2365214977___blue_archive___whip___dominatrix@@@评分88.jpg
}
else
{
    Console.WriteLine($"解析失败: {result.ErrorMessage}");
}
```

### 2. `ParseFilenamePath(string filePath)` - 路径解析方法

**功能**：从完整文件路径解析文件名。

**输入**：完整的文件路径

**返回**：`FilenameParseResult` 对象

**示例**：

```csharp
var filePath = "C:\\Users\\test\\Documents\\image___tag1___tag2.jpg";
var result = FilenameParser.ParseFilenamePath(filePath);

if (result.IsSuccess)
{
    Console.WriteLine($"原名: {result.OriginalName}");  // image
    Console.WriteLine($"扩展名: {result.Extension}");    // .jpg
}
```

## 便利方法

### 3. `GetOriginalName(string filename)` - 快速提取原名

```csharp
string originalName = FilenameParser.GetOriginalName("photo___tag1___tag2.jpg");
// 返回: "photo"
```

### 4. `GetExtension(string filename)` - 快速提取扩展名

```csharp
string extension = FilenameParser.GetExtension("image___tag1.png");
// 返回: ".png"
```

### 5. `GetSuffix(string filename)` - 快速提取后缀

```csharp
string suffix = FilenameParser.GetSuffix("photo___tag1___tag2@@@评分72.jpg");
// 返回: "___tag1___tag2@@@评分72"
```

## FilenameParseResult 结构

```csharp
public class FilenameParseResult
{
    /// <summary>原始文件名 (不包含任何后缀和扩展名)</summary>
    public string OriginalName { get; set; }

    /// <summary>文件扩展名 (包括点号)</summary>
    public string Extension { get; set; }

    /// <summary>完整后缀部分 (所有 ___ 和 @@@ 之间的内容)</summary>
    public string Suffix { get; set; }

    /// <summary>原始完整文件名</summary>
    public string RawFilename { get; set; }

    /// <summary>是否成功解析</summary>
    public bool IsSuccess { get; set; }

    /// <summary>错误信息 (解析失败时)</summary>
    public string ErrorMessage { get; set; }

    /// <summary>重建的文件名 (原名 + 后缀 + 扩展名)</summary>
    public string RebuiltFilename { get; set; }
}
```

## 使用场景

### 场景 1：提取原始文件名用于数据库存储

```csharp
var filename = "artwork_2025___anime___girl___cute@@@评分88.jpg";
var originalName = FilenameParser.GetOriginalName(filename);

// 存储到数据库
database.Insert(new ImageRecord { OriginalFilename = originalName });
```

### 场景 2：重命名文件去除标签

```csharp
var filename = "photo___tag1___tag2@@@评分85.jpg";
var result = FilenameParser.ParseFilename(filename);

if (result.IsSuccess)
{
    var newFilename = result.OriginalName + result.Extension;
    File.Move(oldPath, newPath + newFilename);
    // 将 "photo___tag1___tag2@@@评分85.jpg" 改名为 "photo.jpg"
}
```

### 场景 3：保留原始信息进行完整复制

```csharp
var filename = "image___tag1___tag2.png";
var result = FilenameParser.ParseFilename(filename);

// 完整复制：保留原名和后缀，修改扩展名
var jpgName = result.OriginalName + result.Suffix + ".jpg";
```

### 场景 4：批量处理文件

```csharp
foreach (var file in Directory.GetFiles(@"C:\Images"))
{
    var filename = Path.GetFileName(file);
    var result = FilenameParser.ParseFilename(filename);
    
    if (result.IsSuccess)
    {
        // 根据原名分类
        var category = result.OriginalName.Split('-')[0];
        var destDir = @$"C:\Sorted\{category}";
        Directory.CreateDirectory(destDir);
        File.Move(file, Path.Combine(destDir, filename));
    }
}
```

## 错误处理

### 异常情况

| 情况 | 错误信息 |
|------|---------|
| 文件名为空 | `文件名不能为空` |
| 缺少扩展名 | `文件名缺少扩展名` |
| 没有有效的原始名称 | `解析后原始名称为空` |
| 其他异常 | `解析异常: [异常信息]` |

### 推荐的错误处理方式

```csharp
var result = FilenameParser.ParseFilename(filename);

if (!result.IsSuccess)
{
    logger.Error($"文件名解析失败: {result.RawFilename}");
    logger.Error($"错误: {result.ErrorMessage}");
    // 采取相应的处理措施，如跳过或使用默认值
    return;
}

// 继续处理
var originalName = result.OriginalName;
```

## 测试覆盖

该类已通过 **16 个单元测试**，覆盖场景包括：

- ✅ 完整格式（原名 + 多个标签 + 评分 + 扩展名）
- ✅ 仅有原名和扩展名
- ✅ 仅有标签后缀
- ✅ 仅有评分后缀
- ✅ 特殊字符支持
- ✅ 中文字符支持
- ✅ 错误情况（缺少扩展名等）
- ✅ 文件路径解析
- ✅ 文件名重建

## 集成建议

### 在 ImageService 中使用

```csharp
public class ImageService
{
    public void ProcessImage(string imagePath)
    {
        var result = FilenameParser.ParseFilenamePath(imagePath);
        
        if (!result.IsSuccess)
        {
            logger.Error($"无法解析文件名: {imagePath}");
            return;
        }
        
        var originalName = result.OriginalName;
        var metadata = ExtractMetadata(imagePath, originalName);
        
        SaveToDatabase(metadata);
    }
}
```

### 在报告生成中使用

```csharp
var conversionReport = new List<ConversionReportRow>();

foreach (var sourceFile in sourceFiles)
{
    var parseResult = FilenameParser.ParseFilenamePath(sourceFile);
    
    var row = new ConversionReportRow
    {
        SourceFilename = parseResult.OriginalName,  // 只保存原名
        SourceFullFilename = parseResult.RawFilename, // 保存完整名
        // ... 其他字段
    };
    
    conversionReport.Add(row);
}
```

## 常见问题

**Q1: 如果后缀中包含多个 `@@@` 会怎样？**

A: 只会识别第一个 `@@@` 之前的部分作为原名，之后的全部作为后缀。

**Q2: 后缀中可以包含 `.` 吗？**

A: 可以。系统通过最后一个 `.` 来识别扩展名，所以后缀中的 `.` 不会被误识别。

**Q3: 如果原名为空会怎样？**

A: 解析会失败，返回 `IsSuccess = false` 和相应的错误信息。

**Q4: 支持哪些扩展名？**

A: 理论上支持所有扩展名。系统只关注最后一个 `.` 之后的部分，与具体扩展名无关。

## 版本信息

- **创建日期**: 2025-11-25
- **最后更新**: 2025-11-25
- **单元测试**: 16/16 通过 ✅
