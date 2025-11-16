# 函数与难度系数（满分 100，按难度升序排列）

以下为项目中的主要函数/方法及其实现复杂度估算。分数越低越简单，越高越复杂。

| 函数/功能 | 难度系数 | 简要说明 | 使用的库 | 官方/第三方 | 两字概括 | 入侵性/危害性 | 隐患详细 | 优化空间 |
|---|---|---|---|---|---|---|---|---|
| FileScanner.GetImageFiles | 15 | 递归枚举图片文件，按扩展名过滤 | System.IO, System.Linq | 官方 | 扫描 | 低 | 可能遍历到敏感目录或列出不应公开的文件结构；符号链接/挂载点可能导致越界。 | 低 |
| MetadataService.SplitPossibleTagString | 20 | 字符串分割，拆分 tag | System, System.Linq | 官方 | 拆分 | 低 | 处理来自不可信元数据时可能产生超多标签或包含控制字符，影响展示或后续处理（内存/显示问题）。 | 中 |
| ValidationService.FileExists | 20 | 检查文件是否存在 | System.IO | 官方 | 存在 | 低 | 在不安全环境下用于探测文件存在性可能泄露信息（竞态条件、路径枚举）。 | 低 |
| ValidationService.IsImageLoadable | 20 | 检查图片能否被 ImageSharp 加载 | SixLabors.ImageSharp | 第三方 | 检验 | 低 | 加载恶意构造图片可能触发解析器缺陷或引起大量内存/CPU 消耗（拒绝服务风险）。 | 低 |
| 单元测试（ConvertersTests, ReadWriteValidateTests） | 20 | 生成临时图片、验证转换输出 | xUnit, SixLabors.ImageSharp | 第三方 | 测试 | 低 | 在共享或 CI 环境下产生临时文件需注意权限与清理；测试中使用真实文件可能暴露环境信息。 | 中 |
| ImageConverter.ConvertPngToJpeg | 30 | PNG 转 JPEG，处理透明背景 | SixLabors.ImageSharp | 第三方 | 转换 | 低 | 解码阶段可能触发解析器漏洞；写入阶段可能覆盖已有文件或引入路径遍历问题（如果输出路径不受控）。 | 中 |
| ValidationService.ValidateConversion | 35 | 验证转换后图片宽高一致 | SixLabors.ImageSharp | 第三方 | 检验 | 低 | 需要同时打开源与目标文件，存在加载漏洞或资源耗尽的风险；依赖不可信文件时需小心异常处理。 | 低 |
| MetadataService.ReadFileTimes | 10 | 读取文件创建/修改时间 | System.IO | 官方 | 时间 | 低 | 文件系统时间可能泄露文件创建/修改历史（时间线分析），在隐私敏感场景下属信息泄露风险。 | 低 |
| MetadataService.ExtractFilenameTokens | 15 | 从文件名提取 token | System.IO, System.Linq | 官方 | 文件名 | 低 | 文件名可能包含用户名、提示词或敏感信息；直接使用或导出会泄露这些信息。 | 中 |
| MetadataService.NormalizeAndDedup | 20 | 规范化和去重标签 | System, System.Linq | 官方 | 规范化 | 低 | 处理超长字符串或包含控制字符的字段可能导致内存或显示问题；去重逻辑需防止性能异常。 | 中 |
| MetadataService.CollectAllMetadataTags | 25 | 收集所有元数据来源 tag | MetadataExtractor | 第三方 | 收集 | 中 | 聚合 PNG/EXIF/XMP 等元数据可能收集到 GPS、作者、prompt、版权等敏感字段，若上报或存储会导致隐私泄露。 | 中 |
| MetadataService.ExtractTagsAndTimes | 25 | 【本项目自写】主入口，协调各子函数 | System.IO, System.Linq | 自写 | 协调 | 中 | 作为聚合入口会把多个来源的敏感信息汇总，若未经脱敏即导出（如 XLSX）会放大泄露风险；异常处理需严谨避免崩溃。 | 低 |
| PngMetadataExtractor.ReadAIMetadata | 32 | 从 PNG tEXt 块读取 AI 元数据 | MetadataExtractor.Formats.Png | 第三方 | PNG读 | 中 | PNG tEXt 字段常包含 prompt，若直接导出会泄露生成提示信息。 | 低 |
| PngMetadataExtractor.WriteAIMetadata | 38 | 将元数据写入 PNG tEXt 块 | 需第三方扩展 | 待实现 | PNG写 | 中 | PNG 字节级操作容易破坏文件结构；写入时需确保不覆盖关键块。 | 高 |
| PngMetadataExtractor.VerifyAIMetadata | 30 | 验证 PNG 元数据一致性 | MetadataExtractor | 第三方 | PNG验 | 低 | 验证逻辑应容忍小差异（如标准化问题）。 | 低 |
| JpegMetadataExtractor.ReadAIMetadata | 35 | 从 JPEG EXIF 段读取元数据 | MetadataExtractor.Formats.Exif | 第三方 | JPEG读 | 高 | EXIF 可能包含 GPS、设备序列号等敏感信息；prompt 存储在 ImageDescription 或 UserComment。 | 中 |
| JpegMetadataExtractor.WriteAIMetadata | 40 | 将元数据写入 JPEG EXIF | 需 ImageMagick 或 P/Invoke | 待实现 | JPEG写 | 中 | JPEG EXIF 写入复杂，需严格遵循规范避免破坏文件。 | 高 |
| JpegMetadataExtractor.VerifyAIMetadata | 30 | 验证 JPEG 元数据一致性 | MetadataExtractor | 第三方 | JPEG验 | 低 | 验证时应检查关键 EXIF 字段。 | 低 |
| WebPMetadataExtractor.ReadAIMetadata | 36 | 从 WebP 的 XMP/EXIF 读取元数据 | MetadataExtractor | 第三方 | WebP读 | 高 | WebP XMP 支持自定义命名空间，可能包含多种工具特定的元数据格式。 | 中 |
| WebPMetadataExtractor.WriteAIMetadata | 40 | 将元数据写入 WebP | 需 libwebp 扩展 | 待实现 | WebP写 | 中 | WebP 容器格式复杂，元数据写入需使用专门库。 | 高 |
| WebPMetadataExtractor.VerifyAIMetadata | 32 | 验证 WebP 元数据一致性 | MetadataExtractor | 第三方 | WebP验 | 低 | 验证时需同时检查 XMP 和 EXIF 字段。 | 低 |
| ImageConverter.ConvertJpegToWebP | 40 | JPEG 转 WebP | SkiaSharp | 第三方 | 转换 | 低 | 与其它转换类似，解码/编码过程中可能触发解析器漏洞或资源消耗；注意输出覆盖与路径安全。 | 低 |
| ImageConverter.ConvertPngToWebP | 40 | PNG 转 WebP | SkiaSharp | 第三方 | 转换 | 低 | 同上：处理不可信输入可能带来解析风险，写入需要防止覆盖与权限问题。 | 低 |

实现建议：

**元数据提取的格式特化原则**：

项目已将原来的通用 `MetadataService` 拆分为三个专化服务，以避免不同格式的实现相互干扰：

- `PngMetadataExtractor`：仅处理 PNG tEXt 块（通常包含 Stable Diffusion WebUI 格式的 prompt）
- `JpegMetadataExtractor`：仅处理 JPEG EXIF 段（ImageDescription 或 UserComment 字段）
- `WebPMetadataExtractor`：仅处理 WebP 容器的 XMP 和 EXIF（通常 ComfyUI 等工具使用 XMP）

**为什么要分离**：

1. **格式差异大**：三种格式的元数据存储方式完全不同，混在一起会导致代码充斥着格式判断逻辑
2. **难度不同**：PNG 读写相对简单，JPEG EXIF 复杂，WebP XMP 最复杂，分离便于按难度优先级实现
3. **维护性**：修改某个格式的处理方式不会影响其他格式
4. **可测试性**：每个格式的提取器可独立单元测试，不需要处理多格式混合情况
5. **可扩展性**：支持新格式（如 TIFF、GIF）时只需添加新的提取器，不需要修改现有代码

**优化空间**：

- `WriteAIMetadata` 方法目前为占位符，需要使用第三方库实现实际的元数据写入
- `VerifyAIMetadata` 验证时允许小差异，应明确定义容差范围
- 可添加日志记录以追踪元数据提取失败的原因
实现建议：如果某个元数据解析步骤显得复杂（例如 XMP 深度解析），可以把复杂逻辑拆成更小的解析函数：

- `ParsePngText(PngDirectory)`
- `ParseExifTags(ExifSubIfdDirectory)`
- `ParseXmp(XmpDirectory)`

这样每个函数职责单一、易于测试与维护。
