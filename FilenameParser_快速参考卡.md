# FilenameParser 快速参考卡

## 📝 核心概念

**三段式文件名**: `原名 + 后缀 + 格式`

```
photo_001___tag1___tag2@@@评分88.jpg
   └─ 原名      └─ 后缀         └─ 格式
```

## ⚡ 快速使用

### 基础解析
```csharp
using ImageInfo.Services;

var result = FilenameParser.ParseFilename("photo___tag1___tag2.jpg");
if (result.IsSuccess)
{
    var original = result.OriginalName;      // "photo"
    var ext = result.Extension;              // ".jpg"
    var suffix = result.Suffix;              // "___tag1___tag2"
}
```

### 快速提取
```csharp
// 一行代码提取原名
var name = FilenameParser.GetOriginalName("photo___tag.jpg");   // "photo"

// 提取扩展名
var ext = FilenameParser.GetExtension("photo___tag.jpg");       // ".jpg"

// 提取后缀
var suffix = FilenameParser.GetSuffix("photo___tag.jpg");       // "___tag"
```

### 路径解析
```csharp
var result = FilenameParser.ParseFilenamePath("C:\\Images\\photo___tag.jpg");
// result.OriginalName = "photo"
// result.Extension = ".jpg"
```

## 🔧 FilenameParseResult 属性

| 属性 | 说明 | 示例 |
|-----|------|------|
| `OriginalName` | 原始名称 | `photo` |
| `Extension` | 文件扩展名 | `.jpg` |
| `Suffix` | 完整后缀 | `___tag1___tag2@@@评分88` |
| `RawFilename` | 原始完整文件名 | `photo___tag1___tag2@@@评分88.jpg` |
| `IsSuccess` | 是否解析成功 | `true/false` |
| `ErrorMessage` | 错误信息 | `"文件名缺少扩展名"` |
| `RebuiltFilename` | 重建的文件名 | `photo___tag1___tag2@@@评分88.jpg` |

## 📋 支持的格式

| 格式 | 示例 | 原名 | 扩展名 | 后缀 |
|------|------|------|--------|------|
| 完整格式 | `photo___tag1___tag2@@@评分88.jpg` | `photo` | `.jpg` | `___tag1___tag2@@@评分88` |
| 仅标签 | `photo___tag1___tag2.jpg` | `photo` | `.jpg` | `___tag1___tag2` |
| 仅评分 | `photo@@@评分88.jpg` | `photo` | `.jpg` | `@@@评分88` |
| 简单 | `photo.jpg` | `photo` | `.jpg` | `` |

## 🎯 常见用途

### 重命名 (去除后缀)
```csharp
var result = FilenameParser.ParseFilename("photo___tag.jpg");
var newName = result.OriginalName + result.Extension;  // "photo.jpg"
```

### 提取元数据
```csharp
var result = FilenameParser.ParseFilename("photo___ai_generated___anime.jpg");
var originalName = result.OriginalName;  // 用于数据库存储
var suffix = result.Suffix;              // 保存标签信息
```

### 保留完整信息
```csharp
var result = FilenameParser.ParseFilename("photo___tag1___tag2.jpg");
var rebuilt = result.RebuiltFilename;    // 完全重建原文件名
```

### 文件格式转换
```csharp
var result = FilenameParser.ParseFilename("photo___tag.png");
var jpgName = result.OriginalName + result.Suffix + ".jpg";  // 保持后缀
// 或
var jpgName = result.OriginalName + ".jpg";  // 去除后缀
```

## ❌ 错误处理

```csharp
var result = FilenameParser.ParseFilename(filename);

if (!result.IsSuccess)
{
    switch (result.ErrorMessage)
    {
        case var msg when msg.Contains("缺少扩展名"):
            Console.WriteLine("请确保文件名包含扩展名");
            break;
        case var msg when msg.Contains("为空"):
            Console.WriteLine("原始名称不能为空");
            break;
        default:
            Console.WriteLine($"解析失败: {result.ErrorMessage}");
            break;
    }
}
```

## 📊 可用方法列表

| 方法 | 参数 | 返回值 | 说明 |
|-----|------|--------|------|
| `ParseFilename` | `string` | `FilenameParseResult` | 解析文件名 |
| `ParseFilenamePath` | `string` | `FilenameParseResult` | 从路径解析文件名 |
| `GetOriginalName` | `string` | `string?` | 快速提取原名 |
| `GetExtension` | `string` | `string?` | 快速提取扩展名 |
| `GetSuffix` | `string` | `string` | 快速提取后缀 |

## 🧪 测试验证

所有功能都通过了 **16 个单元测试**：
- ✅ 完整格式
- ✅ 单一后缀
- ✅ 错误情况
- ✅ 特殊字符
- ✅ 中文字符
- ✅ 文件路径
- ✅ 文件名重建

## 💡 性能提示

- 所有操作都是 **O(n)** 时间复杂度
- 适合批量处理数千个文件
- 无正则表达式，性能优异

## 📚 更多信息

- 详细文档: `FilenameParser_README.md`
- 使用示例: `FilenameParserExamples.cs`
- 集成模板: `FilenameParser_IntegrationTemplate.cs`
- 单元测试: `FilenameParserTests.cs`

---

**快速开始**: 只需 `using ImageInfo.Services;` 就可以使用！
