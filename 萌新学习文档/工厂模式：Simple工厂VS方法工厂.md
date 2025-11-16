# Simple Factory vs Factory Method：详细对比

## 🎯 核心区别（一句话）

| 模式 | 核心特点 |
|------|---------|
| **Simple Factory** | 一个工厂类 + 一个 switch 语句，所有逻辑在一处 |
| **Factory Method** | 一个抽象工厂 + 多个具体工厂类，每个格式一个类 |

---

## 📊 完整对比表

| 维度 | Simple Factory | Factory Method |
|-----|----------------|-----------------|
| **设计思想** | 程序式（Procedural） | 面向对象（OOP） |
| **代码文件数** | 1 个 | 6+ 个 |
| **代码行数** | ~10 行 | ~30+ 行 |
| **学习难度** | ⭐ 极简单 | ⭐⭐⭐ 中等偏难 |
| **理解时间** | 5 分钟 | 30-60 分钟 |
| **新人接受度** | 95% | 40% |
| **维护难度** | ⭐ 容易 | ⭐⭐⭐ 需要 OOP 基础 |
| **添加新格式** | 改 1 个文件 | 新建 1 个类 + 改 1 个文件 |
| **代码复用性** | ⭐⭐ 低 | ⭐⭐⭐⭐ 高 |
| **可测试性** | ⭐⭐ 中 | ⭐⭐⭐⭐ 高 |
| **符合 SOLID** | 部分 | 全部 |
| **性能** | 最快 | 稍慢（虚方法调用） |
| **适用场景** | 小型、格式固定 | 中型、需要扩展 |

---

## 💻 代码对比

### Simple Factory 完整实现

```csharp
/// <summary>
/// 简单工厂 - 所有逻辑在一个类中
/// </summary>
public static class MetadataFactory
{
    /// <summary>
    /// 统一入口：根据格式选择处理器
    /// </summary>
    public static AIMetadata GetImageInfo(string imagePath)
    {
        // 第一步：识别格式
        var format = ImageTypeDetector.DetectImageFormat(imagePath);
        
        // 第二步：根据格式调用对应的处理器
        return ImageTypeDetector.FormatToString(format) switch
        {
            "PNG" => PngMetadataExtractor.ReadAIMetadata(imagePath),
            "JPEG" => JpegMetadataExtractor.ReadAIMetadata(imagePath),
            "WEBP" => WebPMetadataExtractor.ReadAIMetadata(imagePath),
            _ => new AIMetadata()  // 默认返回空对象
        };
    }
}

// ✅ 全部代码就这么简单！一个方法搞定。
```

**特点：**
- ✅ 一个文件
- ✅ 一个方法
- ✅ 直接 switch 分派
- ✅ 没有继承，没有虚方法
- ✅ 所有逻辑一目了然

---

### Factory Method 完整实现

```csharp
// ======================== 第 1 步：定义抽象基类 ========================

/// <summary>
/// 工厂方法 - 抽象提取器基类
/// </summary>
public abstract class MetadataExtractor
{
    /// <summary>
    /// 虚方法：由子类实现
    /// </summary>
    public abstract AIMetadata Read(string imagePath);
    
    /// <summary>
    /// 工厂方法：根据格式返回对应的具体实现类
    /// </summary>
    public static MetadataExtractor Create(string imagePath)
    {
        var format = ImageTypeDetector.DetectImageFormat(imagePath);
        
        return ImageTypeDetector.FormatToString(format) switch
        {
            "PNG" => new PngExtractor(),      // 返回 PNG 提取器实例
            "JPEG" => new JpegExtractor(),    // 返回 JPEG 提取器实例
            "WEBP" => new WebPExtractor(),    // 返回 WebP 提取器实例
            _ => new NullExtractor()          // 返回空处理器
        };
    }
}

// ======================== 第 2 步：实现具体的 PNG 提取器 ========================

/// <summary>
/// PNG 专用提取器 - 继承自抽象基类
/// </summary>
public class PngExtractor : MetadataExtractor
{
    /// <summary>
    /// 重写虚方法 - PNG 格式的具体实现
    /// </summary>
    public override AIMetadata Read(string imagePath)
    {
        return PngMetadataExtractor.ReadAIMetadata(imagePath);
    }
}

// ======================== 第 3 步：实现具体的 JPEG 提取器 ========================

/// <summary>
/// JPEG 专用提取器
/// </summary>
public class JpegExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath)
    {
        return JpegMetadataExtractor.ReadAIMetadata(imagePath);
    }
}

// ======================== 第 4 步：实现具体的 WebP 提取器 ========================

/// <summary>
/// WebP 专用提取器
/// </summary>
public class WebPExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath)
    {
        return WebPMetadataExtractor.ReadAIMetadata(imagePath);
    }
}

// ======================== 第 5 步：实现 Null Object 模式 ========================

/// <summary>
/// 空处理器 - 处理不支持的格式
/// </summary>
public class NullExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath)
    {
        Console.WriteLine($"Unsupported format for {imagePath}");
        return new AIMetadata();
    }
}

// ======================== 使用方式 ========================

// 旧方式（Simple Factory）
// var metadata = MetadataFactory.GetImageInfo("photo.png");

// 新方式（Factory Method）
var extractor = MetadataExtractor.Create("photo.png");    // 获得具体提取器对象
var metadata = extractor.Read("photo.png");                // 调用虚方法

// 或者合并成一行
var metadata = MetadataExtractor.Create("photo.png").Read("photo.png");
```

**特点：**
- ✅ 多个文件（基类 + 4 个具体类）
- ✅ 使用继承和虚方法
- ✅ 每个格式有独立的类
- ✅ 符合 SOLID 原则
- ✅ 便于单元测试

---

## 🔍 详细对比

### 1️⃣ 代码量

#### Simple Factory
```csharp
// 总共约 10 行代码
public static AIMetadata GetImageInfo(string imagePath)
{
    var format = ImageTypeDetector.DetectImageFormat(imagePath);
    return ImageTypeDetector.FormatToString(format) switch
    {
        "PNG" => PngMetadataExtractor.ReadAIMetadata(imagePath),
        "JPEG" => JpegMetadataExtractor.ReadAIMetadata(imagePath),
        "WEBP" => WebPMetadataExtractor.ReadAIMetadata(imagePath),
        _ => new AIMetadata()
    };
}
```

#### Factory Method
```csharp
// 总共约 50+ 行代码

// 1. 抽象基类（~10 行）
public abstract class MetadataExtractor
{
    public abstract AIMetadata Read(string imagePath);
    public static MetadataExtractor Create(string imagePath) { ... }
}

// 2. PNG 提取器（~6 行）
public class PngExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath) { ... }
}

// 3. JPEG 提取器（~6 行）
public class JpegExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath) { ... }
}

// 4. WebP 提取器（~6 行）
public class WebPExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath) { ... }
}

// 5. Null 提取器（~6 行）
public class NullExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath) { ... }
}
```

**结论：Simple Factory 代码量是 Factory Method 的 1/5**

---

### 2️⃣ 文件结构

#### Simple Factory
```
src/ImageInfo/Services/
├── MetadataExtractorFactory.cs ← 一个文件搞定
├── PngMetadataExtractor.cs
├── JpegMetadataExtractor.cs
└── WebPMetadataExtractor.cs
```

#### Factory Method
```
src/ImageInfo/Services/
├── MetadataExtractor.cs        ← 抽象基类
├── PngExtractor.cs             ← PNG 具体实现
├── JpegExtractor.cs            ← JPEG 具体实现
├── WebPExtractor.cs            ← WebP 具体实现
├── NullExtractor.cs            ← Null Object 模式
├── PngMetadataExtractor.cs
├── JpegMetadataExtractor.cs
└── WebPMetadataExtractor.cs
```

**结论：Factory Method 需要多 4 个新文件**

---

### 3️⃣ 学习难度

#### Simple Factory 需要理解
```
1. switch 语句 ← 初级
2. 方法调用 ← 初级
完成！
```

#### Factory Method 需要理解
```
1. abstract 关键字 ← 中级
2. 继承 ← 中级
3. 虚方法 override ← 中级
4. 多态 ← 中级
5. 工厂方法模式 ← 高级
6. 为什么要这样设计 ← 哲学问题
```

**结论：Factory Method 需要的前置知识是 Simple Factory 的 5 倍**

---

### 4️⃣ 添加新格式

#### Simple Factory - 添加 AVIF 支持

```csharp
// 只需改一个地方：
public static AIMetadata GetImageInfo(string imagePath)
{
    var format = ImageTypeDetector.DetectImageFormat(imagePath);
    return ImageTypeDetector.FormatToString(format) switch
    {
        "PNG" => PngMetadataExtractor.ReadAIMetadata(imagePath),
        "JPEG" => JpegMetadataExtractor.ReadAIMetadata(imagePath),
        "WEBP" => WebPMetadataExtractor.ReadAIMetadata(imagePath),
        "AVIF" => AvifMetadataExtractor.ReadAIMetadata(imagePath),  // ← 新增这一行
        _ => new AIMetadata()
    };
}

// 总工作量：
// - 修改文件：1 个
// - 新增代码：1 行
// - 时间：2 分钟
```

#### Factory Method - 添加 AVIF 支持

```csharp
// 步骤 1：创建新的提取器类（新文件 AvifExtractor.cs）
public class AvifExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath)
    {
        return AvifMetadataExtractor.ReadAIMetadata(imagePath);
    }
}

// 步骤 2：修改 Create() 方法
public static MetadataExtractor Create(string imagePath)
{
    var format = ImageTypeDetector.DetectImageFormat(imagePath);
    return ImageTypeDetector.FormatToString(format) switch
    {
        "PNG" => new PngExtractor(),
        "JPEG" => new JpegExtractor(),
        "WEBP" => new WebPExtractor(),
        "AVIF" => new AvifExtractor(),  // ← 新增这一行
        _ => new NullExtractor()
    };
}

// 总工作量：
// - 新建文件：1 个（AvifExtractor.cs）
// - 修改文件：1 个（MetadataExtractor.cs）
// - 新增代码：7 行（整个类）
// - 时间：10 分钟
```

**结论：添加新格式时，Simple Factory 比 Factory Method 快 5 倍**

---

### 5️⃣ 维护和调试

#### Simple Factory 调试
```
调试流程：
1. 在 GetImageInfo 打断点
2. F10 步进
3. 跟着 switch 语句看分支
4. 完成

你看到的是：
GetImageInfo()
 ├─ DetectFormat() → "PNG"
 ├─ PngMetadataExtractor.ReadAIMetadata()
 └─ 返回 AIMetadata

代码路径清晰明了！
```

#### Factory Method 调试
```
调试流程：
1. 在 Create 打断点
2. F10 步进进入虚方法调用
3. 等等，虚方法调用？跳进去了吗？
4. 需要理解多态调用机制
5. 在 PngExtractor.Read 再打一个断点
6. 然后...一堆虚方法栈帧

你看到的是：
MetadataExtractor.Create()
 ├─ new PngExtractor() ← 返回基类引用
 └─ PngExtractor.Read()  ← 虚方法调用
      └─ PngMetadataExtractor.ReadAIMetadata()

多层栈帧，不容易理解
```

**结论：Simple Factory 更容易调试**

---

### 6️⃣ 单元测试

#### Simple Factory 测试
```csharp
[TestMethod]
public void TestGetImageInfo_PNG()
{
    // 测试 PNG 格式
    var metadata = MetadataFactory.GetImageInfo("photo.png");
    Assert.IsNotNull(metadata);
}

// 问题：难以 Mock 具体的提取器
// 因为工厂直接调用 PngMetadataExtractor.ReadAIMetadata
```

#### Factory Method 测试
```csharp
[TestMethod]
public void TestCreate_PNG()
{
    // 获取 PNG 提取器
    var extractor = MetadataExtractor.Create("photo.png");
    
    // 可以检查实际类型
    Assert.IsInstanceOfType(extractor, typeof(PngExtractor));
}

[TestMethod]
public void TestPngExtractor()
{
    // 直接测试 PNG 提取器
    var extractor = new PngExtractor();
    var metadata = extractor.Read("photo.png");
    Assert.IsNotNull(metadata);
}

// 优点：可以单独测试每个提取器
// 可以 Mock 基类进行测试
```

**结论：Factory Method 更容易进行单元测试**

---

### 7️⃣ 扩展性

#### Simple Factory 的限制

```csharp
// 假设将来要添加"验证"功能
// Simple Factory 怎么做？

public static bool VerifyImageInfo(string imagePath)
{
    var format = ImageTypeDetector.DetectImageFormat(imagePath);
    return format switch
    {
        "PNG" => PngMetadataExtractor.VerifyAIMetadata(imagePath),
        "JPEG" => JpegMetadataExtractor.VerifyAIMetadata(imagePath),
        "WEBP" => WebPMetadataExtractor.VerifyAIMetadata(imagePath),
        _ => false
    };
}

// 问题：代码重复！
// Read 和 Verify 都需要同样的 switch 逻辑
// 如果再加 Write、Delete 等方法，会有多个 switch

// ❌ 不符合 DRY（Don't Repeat Yourself）原则
```

#### Factory Method 的优势

```csharp
// 扩展基类，添加更多方法
public abstract class MetadataExtractor
{
    public abstract AIMetadata Read(string imagePath);
    public abstract void Write(string imagePath, AIMetadata data);
    public abstract bool Verify(string imagePath, AIMetadata data);
    
    // Create 方法保持不变！
    public static MetadataExtractor Create(string imagePath) { ... }
}

// 具体实现类只需实现新方法
public class PngExtractor : MetadataExtractor
{
    public override AIMetadata Read(string imagePath) { ... }
    public override void Write(string imagePath, AIMetadata data) { ... }
    public override bool Verify(string imagePath, AIMetadata data) { ... }
}

// ✅ 不需要重复代码
// ✅ 所有操作都在一个方法中完成
```

**结论：Factory Method 更容易扩展功能**

---

## 🎯 选择建议

### 选择 Simple Factory 如果：

```csharp
✅ 格式少且稳定（3-5 个）
✅ 项目规模小（< 1000 行代码）
✅ 团队新手多，技术水平参差不齐
✅ 快速原型开发
✅ 功能稳定，不会频繁改需求

// 典型场景：创业公司、学习项目、小工具
```

### 选择 Factory Method 如果：

```csharp
✅ 格式可能增加到 10+ 个
✅ 项目规模中等（1000+ 行代码）
✅ 需要为每个格式添加特殊逻辑
✅ Read/Write/Verify 等多种操作
✅ 需要好的可测试性
✅ 团队重视代码规范

// 典型场景：中型互联网项目、企业应用
```

---

## 📈 你的项目建议

### 当前状态：3 个格式

**推荐：Simple Factory**

```
理由：
1. 格式数少
2. 代码简单
3. 新人容易理解
4. 当前不需要额外功能

代码：
public static AIMetadata GetImageInfo(string imagePath)
{
    var format = ImageTypeDetector.DetectImageFormat(imagePath);
    return format switch
    {
        "PNG" => PngMetadataExtractor.ReadAIMetadata(imagePath),
        "JPEG" => JpegMetadataExtractor.ReadAIMetadata(imagePath),
        "WEBP" => WebPMetadataExtractor.ReadAIMetadata(imagePath),
        _ => new AIMetadata()
    };
}
```

### 未来规划：可能支持 AVIF、HEIC 等

**考虑升级：Factory Method**

```
升级时机：
1. 格式达到 6-8 个时
2. 需要为不同格式添加特殊处理时
3. 代码出现大量重复 switch 时

升级步骤：
1. 创建 MetadataExtractor 抽象基类
2. 为每个现有格式创建具体类
3. 逐步迁移现有代码
4. 全部迁移完后删除旧的工厂方法
```

---

## 🎓 总结表格

| 对比项 | Simple Factory | Factory Method |
|--------|----------------|-----------------|
| **实现复杂度** | ⭐ | ⭐⭐⭐⭐ |
| **代码量** | 少 | 多 |
| **学习难度** | 易 | 难 |
| **维护难度** | 易 | 中等 |
| **添加新格式** | 快 | 慢 |
| **可扩展性** | 差 | 优 |
| **可测试性** | 中 | 优 |
| **符合 SOLID** | 部分 | 全部 |
| **适用格式数** | 1-5 | 5+ |
| **最佳场景** | 小型稳定 | 中型变化 |

---

## 💡 一句话结论

```
Simple Factory：    快速上手，但成长有天花板
Factory Method：    前期投入大，后期收益高

选择 Simple Factory 如果你想"立即开发"
选择 Factory Method 如果你想"长期维护"
```

对于你的项目（3 个格式，预期未来增长）：
**现在用 Simple Factory，当格式达到 6-8 个时升级到 Factory Method。**
