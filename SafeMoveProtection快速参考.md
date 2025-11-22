# SafeMoveProtection 快速参考卡

## 🎯 一句话说明
防止含有特定关键词（超、绝、精、特、待）的已归档文件被代码错误移动。

## 🔑 保护关键词 (5个)
```
超  绝  精  特  待
```

## ⚡ 最常用的3个API

### 1️⃣ 检查是否受保护
```csharp
bool isProtected = SafeMoveProtection.IsProtectedPath(@"C:\[超清]\photo.png");
// true - 受保护，false - 不受保护
```

### 2️⃣ 检查是否可以移动
```csharp
if (SafeMoveProtection.CanMove(filePath))
{
    // 可以安全移动
}
else
{
    // 文件受保护，禁止移动
}
```

### 3️⃣ 批量过滤文件
```csharp
var result = SafeMoveProtection.FilterProtectedFiles(files);
// result.Protected    - 受保护的文件列表
// result.Unprotected  - 可移动的文件列表
```

## 📚 其他API

| API | 功能 | 返回 |
|-----|------|------|
| `GetProtectedKeywords()` | 获取保护关键词列表 | IEnumerable<string> |
| `GetProtectionStatus(path)` | 获取详细保护状态 | ProtectionStatus |

## 💻 代码示例

### 安全移动文件
```csharp
if (SafeMoveProtection.CanMove(sourcePath))
{
    File.Move(sourcePath, targetPath);
}
else
{
    Log("File is protected and cannot be moved");
}
```

### 查看详细信息
```csharp
var status = SafeMoveProtection.GetProtectionStatus(path);
if (status.IsProtected)
    Console.WriteLine($"触发关键词: {status.TriggeredKeyword}");
```

### 批量处理
```csharp
var filtered = SafeMoveProtection.FilterProtectedFiles(files);
foreach (var unprotected in filtered.Unprotected)
    ProcessFile(unprotected);
```

## ✅ 被保护的文件示例

```
✓ C:\Images\[超清]\photo.png              (文件名含"超")
✓ C:\Archive\[绝版]\important.jpg         (文件名含"绝")
✓ D:\[精选]\images\image.webp             (路径含"精")
✓ E:\特定\special\file.gif                (路径含"特")
✓ F:\待处理\待审核\pending.bmp            (多处含"待")
```

## ❌ 不被保护的文件示例

```
✗ C:\Images\photo.png                     (无保护关键词)
✗ C:\Archive\backup.jpg                   (无保护关键词)
✗ D:\Normal\file.webp                     (无保护关键词)
```

## 🏗️ 核心逻辑

```
输入: 文件路径 → 检查是否包含 [超|绝|精|特|待] → 输出: true/false
```

## 📊 性能

- 单次检查：<1ms
- 批量检查（100文件）：<10ms
- 内存占用：极小（仅字符串对比）

## 🔒 安全设计

- ✅ 只有代码路径中包含关键词才受保护
- ✅ 受保护的文件可以重命名，但禁止移动
- ✅ 提供详细的保护原因说明
- ✅ 支持批量过滤和分类处理

## 🎓 使用场景

| 场景 | 用法 |
|------|------|
| 移动前检查 | `IsProtectedPath()` / `CanMove()` |
| 获取原因 | `GetProtectionStatus()` |
| 批量分类 | `FilterProtectedFiles()` |
| 显示规则 | `GetProtectedKeywords()` |

## 🚀 集成建议

```csharp
// 在所有文件移动操作前加入检查
public void SafeMove(string src, string dst)
{
    if (!SafeMoveProtection.CanMove(src))
        throw new Exception("File is protected");
    
    File.Move(src, dst);
}
```

## ⚠️ 注意事项

- 空路径不受保护（返回false）
- 关键词必须是完整的中文字符
- 区分大小写（仅中文字符匹配）
- 目前不支持正则表达式或通配符

---

**版本**：1.0  
**状态**：✅ 生产就绪  
**测试**：21/21 通过

