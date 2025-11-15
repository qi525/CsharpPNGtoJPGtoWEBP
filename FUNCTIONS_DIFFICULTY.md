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
| MetadataService.ParsePngText | 35 | 提取 PNG tEXt 文本 | MetadataExtractor.Formats.Png.PngDirectory | 第三方 | PNG | 中 | PNG 的 tEXt 字段常包含描述或 prompt，可能泄露生成提示、注释或版权信息；字段任意性高。 | 低 |
| MetadataService.ParseExifTags | 35 | 提取 EXIF 及其它目录 tag | MetadataExtractor.Formats.Exif.ExifSubIfdDirectory | 第三方 | EXIF | 高 | EXIF 可能包含 GPS 坐标、设备序列号、拍摄时间等敏感信息，若不加处理直接导出会造成严重隐私泄露或安全问题。 | 中 |
| MetadataService.ParseXmpTags | 40 | 提取 XMP 目录标签 | MetadataExtractor.Formats.Xmp.XmpDirectory | 第三方 | XMP | 高 | XMP 可承载丰富结构化元数据与嵌入资源（关键词、作者、版权、嵌入数据）；解析复杂、且字段可能包含敏感 prompt 或机密，风险最高。 | 高 |
| ImageConverter.ConvertJpegToWebP | 40 | JPEG 转 WebP | SkiaSharp | 第三方 | 转换 | 低 | 与其它转换类似，解码/编码过程中可能触发解析器漏洞或资源消耗；注意输出覆盖与路径安全。 | 低 |
| ImageConverter.ConvertPngToWebP | 40 | PNG 转 WebP | SkiaSharp | 第三方 | 转换 | 低 | 同上：处理不可信输入可能带来解析风险，写入需要防止覆盖与权限问题。 | 低 |

实现建议：如遇到复杂逻辑（如元数据深度解析），可拆分为更小的函数：

- `ParsePngText(PngDirectory)`
- `ParseExifTags(ExifSubIfdDirectory)`
- `ParseXmp(XmpDirectory)`

这样每个函数职责单一、易于测试与维护。
实现建议：如果某个元数据解析步骤显得复杂（例如 XMP 深度解析），可以把复杂逻辑拆成更小的解析函数：

- `ParsePngText(PngDirectory)`
- `ParseExifTags(ExifSubIfdDirectory)`
- `ParseXmp(XmpDirectory)`

这样每个函数职责单一、易于测试与维护。
