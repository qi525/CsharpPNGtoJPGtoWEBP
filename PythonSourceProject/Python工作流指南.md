# Python 源码工作流程与实现指南

## 📋 目录
1. [项目总体流程](#项目总体流程)
2. [模块说明](#模块说明)
3. [函数准则](#函数准则)
4. [C# 缺失功能对比](#c-缺失功能对比)

---

## 项目总体流程

```
用户输入文件夹路径
    ↓
选择转换格式 (JPG/WebP)
    ↓
选择输出目录模式 (Mode 1/2)
    ↓
扫描文件夹 → 获取所有图片 (PNG/JPG/WebP)
    ↓
预提取元数据和时间戳 (并行读取)
    ↓
多线程转换任务
    ├─ 读取源文件 (Read)
    ├─ 提取AI元数据 (格式特化处理)
    ├─ 提取文件时间 (mtime/ctime)
    ├─ 转换图片格式
    ├─ 写入元数据到新文件
    ├─ 复制文件时间戳
    └─ 验证 (Verify)
    ↓
收集转换结果
    ↓
生成 XLSX 报告 (中文表头)
    ├─ 截断超长字段 (避免32767字符限制)
    ├─ 添加每行时间戳
    └─ 记录转换统计
    ↓
附加日志摘要到转换日志
    ↓
自动打开报告文件
```

---

## 模块说明

### 1. `file_timestamp_tools.py` - 时间戳工具库

**功能**: 读取、修改、验证文件的 mtime (修改时间) 和 ctime (创建时间)

#### 核心函数

##### `parse_time_from_filename(filename, time_format="%Y%m%d_%H%M%S") → Optional[float]`
- **输入**: 文件名, 时间格式字符串
- **输出**: Unix 时间戳 (成功) 或 None (失败)
- **工作原理**:
  1. 用正则匹配文件名中的时间字符串 (如 20250101_123456)
  2. strptime 解析为 datetime 对象
  3. 转换为 Unix 时间戳
- **用途**: 从命名规范的文件名提取时间信息
- **示例**: `"photo_20250115_143025.png"` → `1736901625.0`

##### `_unix_time_to_filetime(unix_time) → FILETIME`
- **输入**: Unix 时间戳
- **输出**: Windows FILETIME 结构体
- **工作原理**:
  1. 时间戳乘以 10,000,000 (100纳秒精度)
  2. 加上 1601-1970 间隔常数 (116444736000000000)
  3. 拆分为 32 位 DWORD 对 (Low/High)
- **用途**: 支持 Windows API 修改创建时间
- **平台**: Windows 仅

##### `modify_file_timestamps(file_path, new_timestamp, set_mtime=True, set_ctime=False) → bool`
- **输入**: 文件路径, 目标时间戳, 是否设置 mtime/ctime
- **输出**: 成功 True, 失败 False
- **工作流**:
  1. **验证**: 时间戳有效性
  2. **修改 mtime/atime** (跨平台, os.utime): 设置文件的修改时间和访问时间
  3. **修改 ctime** (Windows only, ctypes API):
     - 打开文件句柄 (CreateFileW)
     - 调用 SetFileTime (仅设置 CreationTime 参数)
     - 关闭句柄 (CloseHandle)
  4. **验证**: 读取文件状态, 比对 mtime (容差 <1秒)
- **返回**:
  - 若仅设置 mtime: 返回验证结果
  - 若设置 ctime: 返回 True (ctime 验证在外部进行)
- **备注**:
  - mtime 最可靠 (跨平台)
  - ctime 设置仅 Windows 有效
  - 允许 ctime 设置失败, 不影响整体结果

---

### 2. `exif_metadata_debugger.py` - EXIF 元数据调试工具

**功能**: 从 JPEG 图片中提取 SD (Stable Diffusion) 生成参数

#### 核心函数

##### `extract_sd_params_from_user_comment(raw_bytes: bytes) → Tuple[str, str]`
- **输入**: EXIF UserComment 标签的原始字节
- **输出**: (原始解码文本, 清洗后文本)
- **工作流**:
  1. **移除 UNICODE 头部**: 检查字节是否以 `b"UNICODE\x00"` 开头
  2. **UTF-16LE 解码**: 这是 SD WebUI 的标准编码方式
  3. **清洗**: 移除所有空字符 `\x00`
  4. **返回** 解码结果和清洗结果
- **为什么清洗**: SD WebUI 写入时会填充空字节, 导致截断/乱码
- **关键点**: UTF-16LE + 空字符移除是提取 SD 参数的核心策略

##### `decode_exif_bytes(tag_name: str, raw_bytes: bytes) → Dict[str, str]`
- **输入**: 标签名, EXIF 字节
- **输出**: 多种解码方案结果字典
- **工作流** (枚举解码):
  1. **EXIF 标准** (UTF-16LE + UNICODE 头移除)
  2. **通用 UTF-8**
  3. **兼容 Latin-1** (单字节)
  4. **中文 GBK** (区域性)
- **用途**: 应对不同编码格式, 定位正确的 SD 参数提取方式

##### `analyze_exif_metadata(image_path: str) → None`
- **输入**: JPEG 图片路径
- **输出**: 控制台和日志文件输出
- **工作流**:
  1. 打开图片, 加载 EXIF 字典
  2. 遍历 UserComment 和 ImageDescription 标签
  3. 调用 decode_exif_bytes 进行多策略解码
  4. 在日志中打印所有解码结果和分析结论
- **日志**:
  - 控制台: INFO 级别
  - 文件: exif_debugger_*.log (DEBUG + 更多详情)
- **备注**: 这是调试工具, 生产环境用 JpegMetadataExtractor 等

---

### 3. `image_processor_and_converter.py` - 主转换管道

**功能**: 批量扫描、转换、提取元数据、生成报告

#### 核心数据结构

```python
ConversionReportRow = {
    "所在文件夹": str,
    "图片的绝对路径": str,
    "图片超链接": str,           # Excel 内链
    "原文件的绝对路径": str,      # 转换任务
    "原文件的pnginfo信息": str,    # AI 元数据
    "生成的JPG/WEBP文件的绝对路径": str,
    "生成的JPG/WEBP文件的pnginfo信息": str,
    "原文件和生成文件的pnginfo信息是否一致": str,  # Read-Write-Verify
    "原文件修改时间(mtime)": str,  # YYYY-MM-DD HH:MM:SS
    "新文件修改时间(mtime)": str,
    "Mtime移植是否成功": str,     # Yes/No
    "原文件创建时间(ctime)": str,  # Windows only
    "新文件创建时间(ctime)": str,
    "Ctime移植是否成功(Win Only)": str,
    "任务执行状态": str,          # 成功/失败
    "报告时间戳": str,            # ISO 8601 UTC
}
```

#### 核心函数

##### `get_png_files(folder_path: str) → List[str]`
- **输入**: 根文件夹
- **输出**: 所有图片文件的绝对路径列表
- **工作流**:
  1. `os.walk()` 递归遍历
  2. 跳过 `.bf` 文件夹 (黑名单)
  3. 过滤支持的扩展名 (.png, .jpg, .jpeg, .gif, .bmp, .webp)
  4. 返回完整路径列表

##### `extract_metadata_from_png(file_path: str) → str`
- **输入**: PNG 文件路径
- **输出**: 原始元数据字符串 (或空字符串)
- **工作流**:
  1. 打开 PNG 文件
  2. 读取 `info` 属性的 "parameters" 字段
  3. 返回 SD 提示词 (如果存在)
  4. 异常时记录错误, 返回 ""

##### `process_single_image(absolute_path: str) → Dict[str, Any] | None`
- **输入**: 图片文件绝对路径
- **输出**: 结构化元数据字典 (或 None)
- **提取的字段**:
  - 文件夹路径、图片路径、超链接
  - 完整 SD 生成信息、去换行版本
  - 正面/负面提示词、其他设置
  - 正面提示词字数、提取的核心词
  - 模型名称、创建日期目录
- **异常处理**: try-catch, 记录到 logger

##### `_get_output_sub_dir(input_path, output_dir_base, root_folder, output_dir_type) → str | None`
- **输入**: 输入文件路径, 输出目录前缀, 根文件夹, 模式
- **输出**: 目标输出子目录路径
- **两种模式**:
  - **模式 1**: 兄弟目录 + 复刻结构
    ```
    D:/Source/Photos/subfolder/image.png
    → D:/PNG转JPG/Photos/subfolder/image.jpg
    ```
  - **模式 2**: 本地子目录
    ```
    D:/Source/Photos/subfolder/image.png
    → D:/Source/Photos/subfolder/PNG转JPG/image.jpg
    ```
- **工作流**:
  1. 确保路径为绝对路径
  2. 计算相对目录
  3. 根据模式拼接输出路径

##### `generate_exif_bytes(raw_metadata: str) → bytes | None`
- **输入**: 原始 SD 元数据字符串
- **输出**: EXIF 字节 (或 None)
- **工作流** (混合优化方案):
  1. **UserComment 标签**: EXIF 标准编码
     - 使用 piexif.helper.UserComment.dump
     - encoding="unicode" (UNICODE\x00 + UTF-16LE)
  2. **ImageDescription 标签**: UTF-8 编码
     - 纯 UTF-8 字节 (最高兼容性)
  3. 构造 piexif 字典并 dump
- **异常**: 元数据过长导致失败时记录详细警告
- **备注**: 这是生产方案, 结合标准和兼容性

##### `convert_and_write_metadata(...) → str | None`
- **输入**: 源PNG, 元数据, 输出格式, 时间戳等
- **输出**: 转换后文件路径 (或 None)
- **Read-Write-Verify 流程**:
  1. **Read**: 打开源文件, 读取图像
  2. **Write**: 
     - 转换格式 (PNG→JPG/WebP)
     - 生成 EXIF 字节
     - 保存到目标路径
  3. **Verify**: (由 process_conversion_task 完成)
     - 重新扫描新文件
     - 比对元数据字符串
     - 验证时间戳
- **异常处理**: 捕获读取/保存失败, 记录到 logger

##### `process_conversion_task(...) → Dict[str, Any]`
- **输入**: 单个图片任务参数
- **输出**: 转换结果字典 (包含成功/失败状态)
- **多线程工作单元**:
  1. 调用 convert_and_write_metadata (Read-Write)
  2. 成功路径:
     - 扫描新文件获取元数据
     - 比对原文件和新文件的 metadata
     - 验证时间戳 (mtime/ctime)
     - 返回完整结果行
  3. 失败路径:
     - 复制源文件到目标目录 (恢复机制)
     - 返回失败状态和原始文件时间信息

##### `main_conversion_process(root_folder, choice, choice_dir) → None`
- **输入**: 根文件夹, 格式选择 (1=JPG/2=WebP), 目录模式
- **输出**: XLSX 报告 + 转换日志
- **主工作流**:
  1. **预处理**: 绝对路径化, 定义输出格式
  2. **扫描**: get_png_files 获取所有图片
  3. **预提取**: 并行读取元数据和时间戳, 构建任务队列
  4. **多线程转换**:
     - ThreadPoolExecutor (MAX_WORKERS = CPU核心数)
     - 遍历任务, 提交给线程池
     - 用 as_completed + tqdm 显示进度条
  5. **结果收集**: 统计成功/失败数
  6. **报告生成**:
     - ClosedXML 创建 XLSX
     - 中文表头, 字段截断, 每行时间戳
     - 成功率、失败列表
  7. **日志总结**: 附加到 conversion-log.txt
  8. **自动打开**: 用 os.startfile (Windows)

##### `TruncateIfNeeded(text: str, max_length: int) → str`
- **输入**: 文本, 最大长度
- **输出**: 截断文本 (超长时加 "...")
- **工作流**:
  1. 检查长度
  2. 超长则截断并添加省略号
- **用途**: 避免 ClosedXML 的 32,767 字符限制
- **应用字段**:
  - AIPrompt: 1000 字符
  - AINegativePrompt: 1000 字符
  - AIMetadata: 500 字符

#### 重要常量

```python
EXIF_USER_COMMENT_TAG = 37510  # 0x9286
EXIF_IMAGE_DESCRIPTION_TAG = 270  # 0x010E
MAX_WORKERS = os.cpu_count() or 4
POSITIVE_PROMPT_STOP_WORDS = [...]  # 核心词提取用
```

#### 错误处理策略

1. **读取失败**: 记录错误到 logger, 返回 None
2. **转换失败**: 复制源文件作为备份, 标记为失败
3. **元数据提取失败**: 使用默认值 "未扫描到生成信息"
4. **文件时间操作失败**: 允许失败, 继续处理
5. **XLSX 生成失败**: 字段截断, 重试保存

---

## 函数准则

### 命名规范
- **参数**: snake_case (file_path, output_format)
- **函数**: snake_case (extract_metadata_from_png)
- **类/常量**: UPPER_SNAKE_CASE (EXIF_USER_COMMENT_TAG)

### 返回值
- **成功时**: 返回结果对象或 True
- **失败时**: 返回 None, False 或空容器, 并记录 logger.error
- **可选类型**: 使用 `Optional[T]` 或 `T | None`

### 文档注释
```python
def function_name(param1: Type1, param2: Type2) -> ReturnType:
    """
    一行简要说明
    
    详细工作流:
    1. 第一步
    2. 第二步
    
    Args:
        param1: 参数说明
        param2: 参数说明
    
    Returns:
        返回值说明
    
    Raises:
        ExceptionType: 异常情况说明
    """
```

### 异常处理
- 优先捕获具体异常类型
- 记录异常到 logger, 包含文件路径等上下文
- 不要悄无声息地吞掉异常 (可能导致难以调试)

### Read-Write-Verify 模式
所有数据操作遵循:
1. **Read**: 读取原始数据, 验证有效性
2. **Write**: 写入新数据或修改现有数据
3. **Verify**: 读取验证, 确保写入成功

---

## C# 缺失功能对比

### ✅ C# 已实现
- [x] 图片格式转换 (PNG→JPG/WebP)
- [x] 文件时间读取 (mtime/ctime)
- [x] XLSX 报告生成 (中文表头)
- [x] 自动打开报告
- [x] 多线程处理
- [x] 格式特化的元数据提取 (PNG/JPEG/WebP)

### ❌ C# 缺失功能

#### 1. **从文件名解析时间** 
- **Python**: `parse_time_from_filename()`
- **功能**: 从命名规范的文件名 (如 photo_20250115_143025.jpg) 提取时间戳
- **用途**: 可用于恢复丢失的原始时间信息
- **C# 实现建议**: 用 Regex 和 DateTime.ParseExact

#### 2. **Windows 创建时间设置** 
- **Python**: `_unix_time_to_filetime()` + Windows API SetFileTime
- **功能**: 修改文件的创建时间 (ctime)
- **用途**: 完整保留原文件的所有时间属性
- **C# 实现建议**: 
  ```csharp
  var info = new FileInfo(path);
  info.CreationTimeUtc = targetTime;
  ```
  或使用 P/Invoke 调用 SetFileTime (更精确)

#### 3. **EXIF 元数据调试工具** 
- **Python**: `exif_metadata_debugger.py` 完整模块
- **功能**: 枚举多种编码方案解码 EXIF, 帮助诊断元数据格式问题
- **用途**: 生产环境调试, 定位不兼容的元数据格式
- **C# 实现建议**: 
  - 使用 MetadataExtractor 库
  - 实现多种编码尝试 (UTF-16LE, UTF-8, GBK 等)
  - 输出详细日志用于诊断

#### 4. **从文件名提取时间并应用**
- **Python**: 组合 parse_time_from_filename + modify_file_timestamps
- **功能**: 根据文件名推断源文件时间, 并应用到转换后的文件
- **用途**: 恢复丢失的元数据时的应急方案
- **C# 实现建议**: 在 FileTimeService 中添加相关方法

#### 5. **完整的 Log 收集和分析**
- **Python**: 多处使用 loguru, 结构化日志摘要
- **功能**: 
  - 每次转换的详细日志
  - 最终统计摘要 (成功率、失败列表、性能指标)
  - 可选的日志分级 (INFO/DEBUG/ERROR)
- **C# 实现建议**: 
  - 集成 Serilog 或 NLog
  - 记录每个任务的开始/结束时间
  - 生成运行统计报告

#### 6. **XLSX 字段截断保护**
- **Python**: TruncateIfNeeded 对每个超长字段自动截断
- **功能**: 避免 ClosedXML 的 32,767 字符限制抛出异常
- **用途**: 安全处理超长 AI 提示词
- **C# 实现建议**: 在 ReportService.AddConversionRow 中添加截断逻辑

#### 7. **自动打开报告** (跨平台版)
- **Python**: os.startfile (Windows) + subprocess (Linux/Mac)
- **功能**: 任务完成后自动打开 XLSX 报告文件
- **C# 实现建议**: 
  ```csharp
  System.Diagnostics.Process.Start(reportPath);
  // 或 ProcessStartInfo + OS 检测
  ```

#### 8. **转换失败恢复机制**
- **Python**: convert_and_write_metadata 失败时调用 shutil.copy 复制源文件
- **功能**: 如果转换失败, 在目标目录放置原始文件作为备份
- **用途**: 防止数据丢失
- **C# 实现建议**: 在 ConversionService 的异常处理中添加 File.Copy 逻辑

#### 9. **多模式输出目录结构**
- **Python**: `_get_output_sub_dir()` 支持两种模式
- **功能**:
  - 模式1: 兄弟目录 + 复刻源目录结构
  - 模式2: 本地子目录
- **用途**: 灵活适应不同的输出需求
- **C# 现状**: 只实现了模式1, 缺模式2
- **建议**: 添加 OutputDirectoryMode 枚举和条件逻辑

#### 10. **报告时间戳追溯**
- **Python**: 每行记录 ReportTimestamp (ISO 8601 UTC)
- **功能**: 快速定位哪次运行产生的转换结果
- **用途**: 批量转换时的审计追踪
- **C# 现状**: 已有 ReportTimestamp 字段, 但未在所有行一致填充
- **建议**: 确保 ReportTimestamp 在所有结果行一致设置

---

## 改进建议

### 短期 (立即可做)
1. 在 FileTimeService 中添加 `ParseTimeFromFilename` 方法
2. 在 ReportService 中添加字段截断保护
3. 实现 ctime 修改 (P/Invoke SetFileTime)
4. 添加 OutputDirectoryMode 枚举支持模式2

### 中期 (优先考虑)
5. 集成 Serilog, 完善日志系统
6. 实现 EXIF 多编码尝试诊断工具
7. 添加转换失败时的源文件备份逻辑
8. 实现报告自动打开 (跨平台)

### 长期 (进阶)
9. 性能指标收集 (转换速度、成功率趋势)
10. 批量任务管理和调度
11. Web 界面展示转换历史

