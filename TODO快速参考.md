# ⚡ TODO 快速参考卡

**更新时间**：2025-11-16 15:45 (UTC)

---

## 🚀 开发流程（5 步）

```
1️⃣ 查看进度
   → 打开 TODO_PROGRESS.md
   → 选择最高优先级的 not-started 项

2️⃣ 标记为进行中
   → manage_todo_list write
   → 改状态为 in-progress

3️⃣ 编码与测试
   → 在代码中嵌入 TODO ID（XML 注释）
   → 编写单元测试，对照验收标准逐一检查

4️⃣ 本地验证
   → dotnet build
   → dotnet test

5️⃣ 标记为完成
   → manage_todo_list write
   → 改状态为 completed
   → 提交 PR
```

---

## 📋 10 个 TODO 一览

| # | 标题 | 优先级 | 耗时 | 依赖 |
|---|------|--------|------|------|
| **1** | ✅ 验证 PNG 实现 | 🔴 P0 | 2h | - |
| **2** | 📝 开发模式测试 | 🔴 P0 | 1h | #1 |
| **3** | 📊 Excel 新列验证 | 🟠 P1 | 1h | #2 |
| **4** | 🖼️ JPEG EXIF 写入 | 🟠 P1 | 2.5h | - |
| **5** | 🌐 WebP XMP 写入 | 🟠 P1 | 2.5h | - |
| **6** | 🔗 集成测试 | 🟠 P1 | 3h | #4,5 |
| **7** | ⚡ 性能优化 | 🟡 P2 | 4h | #6 |
| **8** | 🤖 提示词精化 | 🟡 P2 | 3h | - |
| **9** | 📚 文档完善 | 🟠 P1 | 2h | #2 |
| **10** | 🚀 版本发布 | 🔴 P0 | 1h | #1-6 |

**图例**：🔴 P0=阻塞 | 🟠 P1=重要 | 🟡 P2=可选

---

## 🎯 关键路径（7h）

```
┌─────────────┐
│ TODO #1 (2h) │ 验证 PNG
└──────┬──────┘
       ↓
┌─────────────┐
│ TODO #2 (1h) │ 开发测试
└──────┬──────┘
       ↓
┌─────────────┐
│TODO #10 (1h) │ 发布
└─────────────┘
```

**建议**：
- 串行：#1 → #2 → #10（必须）
- 并行：#3,4,5,9 可同时进行
- 延后：#7,8 为 P2，完成核心后再做

---

## 💻 代码中的 TODO 模板

### 1. XML 文档注释

```csharp
/// <summary>
/// PNG 二进制 tEXt 块写入
/// </summary>
/// <remarks>
/// TODO #1: 验证 MetadataWriter PNG 实现
/// 验收标准：
///   ✓ tEXt 块长度正确（大端序）
///   ✓ CRC 计算无误
///   ✓ 块插入到 IEND 前
/// </remarks>
private static byte[] BuildPngTextChunk(string keyword, string text)
{
    // TODO #1: PNG tEXt 块格式
    // [4字节] 长度 + [4字节] "tEXt" + [数据] + [4字节] CRC
}
```

### 2. 单元测试

```csharp
[Fact(DisplayName = "TODO #1: tEXt 块长度正确")]
public void WritePngTextChunk_LengthCorrect()
{
    // 验收标准：tEXt 块长度 = keyword + null + text
    Assert.Equal(expectedLength, chunk.BlockLength);
}
```

### 3. Git Commit

```bash
git commit -m "feat: TODO #1 - 验证 MetadataWriter PNG 实现

验收标准检查：
  ✓ tEXt 块长度正确
  ✓ CRC 计算无误
  ✓ 块插入到 IEND 前
  ✓ 读回内容可识别
  ✓ PNG 文件可打开

Closes TODO #1"
```

---

## 📊 进度更新命令

```powershell
# 查看当前进度
manage_todo_list read

# 更新 TODO 状态（开始一个任务）
manage_todo_list write
# 改成：
# "status": "in-progress"

# 完成一个任务
manage_todo_list write
# 改成：
# "status": "completed"
```

---

## ⏱️ 日常时间表

```
上午（8:00-12:00）：
  ✓ 查看 TODO 列表，选择任务
  ✓ 编码 + 单元测试（2-3h）
  ✓ 本地验证（dotnet build/test）

下午（13:00-17:00）：
  ✓ 完成当前 TODO + 提交
  ✓ 开始下一个 TODO
  ✓ 代码审查和反馈

周五（收尾）：
  ✓ 检查周进度
  ✓ 更新 TODO_PROGRESS.md
  ✓ 调整下周优先级
```

---

## ⚠️ 常见陷阱

```
❌ 不要：
  - TODO 标记为 in-progress 但一周没动
  - 代码中没有 TODO ID 注释
  - 跳过单元测试，直接提交
  - 长时间占用一个 TODO，没有进度同步

✅ 要：
  - 每天至少 30 分钟检查和更新进度
  - 在 XML 注释和测试中嵌入 TODO ID
  - 每个验收标准都有对应的测试
  - 卡顿超过 1 天就同步，寻求帮助
```

---

## 📞 快速帮助

**问：TODO #1 是什么？**
→ 验证 PNG tEXt 块写入的二进制逻辑，需运行单元测试

**问：优先级怎么理解？**
→ 🔴 P0 阻塞（必须做）| 🟠 P1 重要（本周做）| 🟡 P2 可选（下周做）

**问：如何关联代码和 TODO？**
→ XML 注释中写 "TODO #X: 描述"，单元测试名称带 DisplayName

**问：卡顿怎么办？**
→ 更新 TODO_PROGRESS.md 中的说明，记录遇到的问题，可寻求帮助

**问：什么时候更新 CHANGELOG？**
→ TODO 全部 completed 时，在发布 v1.1.0 前更新

---

## 📍 关键文件路径

| 文件 | 用途 |
|-----|------|
| `TODO_PROGRESS.md` | 详细进度和验收标准 |
| `DEVELOPMENT_SUMMARY.md` | 本文档的完整版 |
| `项目章程.md` | TODO 管理规范 + 完整开发指南 |
| `src/ImageInfo/Services/MetadataWriter.cs` | TODO #1-#5 的实现 |
| `tests/ImageInfo.Tests/` | 单元测试和集成测试 |
| `CHANGELOG.md` | 版本发布记录 |

---

**更新频率**：每完成一个 TODO 就更新本卡片  
**最后编辑**：2025-11-16 15:45 (UTC)

