
# imageInfo

## 依赖说明：官方API与第三方库

本项目既用到了 .NET 官方标准库 API，也用到了第三方库。下表列出各模块/函数涉及的库：

| 模块/函数 | 官方API | 第三方库 |
|---|---|---|
| FileScanner | System.IO, System.Linq | 无 |
| MetadataService | System, System.IO, System.Linq | MetadataExtractor |
| ImageConverter | System, System.IO | SixLabors.ImageSharp, Magick.NET-Q8-AnyCPU |
| ValidationService | System, System.IO | SixLabors.ImageSharp |
| 单元测试 | System, System.IO | xUnit, SixLabors.ImageSharp |

**第三方库简介：**

- [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet)：图片元数据读取（EXIF、XMP、PNG tEXt等），广泛用于图片分析。
- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)：纯 C# 跨平台图片处理库，支持读取/写入/转换多种格式。
- [Magick.NET-Q8-AnyCPU](https://github.com/dlemstra/Magick.NET)：ImageMagick 的 .NET 封装，支持 WebP 等格式的高质量读写。
- [xUnit](https://xunit.net/)：主流 .NET 单元测试框架。

**官方API** 指 .NET 自带的命名空间（如 System.IO、System.Linq、System、System.Collections.Generic 等），无需额外安装。

**第三方库** 需通过 NuGet 安装，已在 csproj 文件中声明。


这是一个面向学习的简洁 C# 控制台程序，目标：

- 扫描指定目录下的图片文件（支持常见格式），
- 从图片元数据（PNG tEXt、EXIF、XMP）和文件名提取可能的 AI 标签（tags），
- 获取文件的创建时间与修改时间（UTC），
- 提供简单的图片格式转换：`png -> jpg`、`png -> webp`、`jpg -> webp`。

这个仓库为学习目的做了模块化设计，每个模块职责单一，便于阅读和扩展。

**快速运行**

- 在 PowerShell 中恢复依赖并运行测试：

```powershell
dotnet restore .\src\ImageInfo\ImageInfo.csproj
dotnet restore .\tests\ImageInfo.Tests\ImageInfo.Tests.csproj
dotnet test .\tests\ImageInfo.Tests\ImageInfo.Tests.csproj
```

- 运行程序示例（扫描本地目录并输出每张图片的时间与标签）：

```powershell
dotnet run --project .\src\ImageInfo\ImageInfo.csproj -- "C:\\Users\\10374\\Desktop\\test"
```


---

**项目流程（高层）**

1. 扫描目录：`FileScanner.GetImageFiles(root)` -> 返回图片完整路径列表。
2. 读取元数据：`MetadataService.ExtractTagsAndTimes(filePath)` -> 返回 `ImageInfoModel`（包含 `FilePath`、`CreatedUtc`、`ModifiedUtc`、`Tags`）。
3. 可选转换：`ImageConverter.ConvertPngToJpeg`, `ImageConverter.ConvertPngToWebP`, `ImageConverter.ConvertJpegToWebP`。
4. 单元测试覆盖转换功能，测试在运行时动态生成小图片并验证输出文件存在。

**模块与核心函数（学习索引）**

- `src\ImageInfo\Services\FileScanner.cs`
	- `FileScanner.GetImageFiles(string root)` : 枚举并过滤常见图片扩展名（.png, .jpg, .jpeg, .webp, .gif, .bmp, .tiff）。

- `src\ImageInfo\Services\MetadataService.cs`
	- `MetadataService.ExtractTagsAndTimes(string filePath)` :
		- 读取文件系统时间（`File.GetCreationTimeUtc` / `File.GetLastWriteTimeUtc`）。
		- 使用 `MetadataExtractor.ImageMetadataReader.ReadMetadata(filePath)` 读取所有目录的 tags（PNG/EXIF/XMP 等），汇总后按常见分隔符（`, ; | \n`）拆分为候选 tags，并将文件名 token 追加为补充提示。
	- `MetadataService.SplitPossibleTagString(string s)` : 将可能的一行 tags 拆分为单个 tag（便于解析 AI 风格的 tag 列表）。

- `src\ImageInfo\Services\ImageConverter.cs`
	- `ImageConverter.ConvertPngToJpeg(string pngPath, string? outPath = null, int quality = 85)` : 使用 `SixLabors.ImageSharp` 将 PNG 转为 JPEG（处理透明背景为白色），返回输出路径。
	- `ImageConverter.ConvertPngToWebP(string pngPath, string? outPath = null, int quality = 80)` : 使用 `Magick.NET` 将 PNG 写为 WebP（通过 `MagickImage.Format = MagickFormat.WebP` 与 `Quality` 设置）。
	- `ImageConverter.ConvertJpegToWebP(string jpgPath, string? outPath = null, int quality = 80)` : JPEG -> WebP 同上。

**测试位置**

- `tests\ImageInfo.Tests\ConvertersTests.cs` :
	- 在临时目录生成小的 PNG（`SixLabors.ImageSharp`）并调用转换函数，断言输出文件存在。

**函数复杂度参考**

请参阅仓库根的 `FUNCTIONS_DIFFICULTY.md`，我已经为每个主要函数给出 0-100 的难度评分（100 最难），该文件适合学习时决定先学哪个模块。

**关于依赖与安全性**

- 当前依赖：
	- `MetadataExtractor`：读取图片元数据（EXIF、XMP、PNG tEXt）。
	- `SixLabors.ImageSharp`：用于 PNG->JPEG（处理透明背景）。
	- `Magick.NET-Q8-AnyCPU`：用于生成 WebP（注意：NuGet 解析到 9.0.0，构建时会显示若干安全性警告）。

- 建议：如果你只是为了学习/本地使用，可以继续当前依赖；如果要在生产环境中使用，建议替换 `Magick.NET` 为更安全的替代（例如 `SkiaSharp` 或 ImageSharp 的 WebP 扩展）。我可以帮你替换并更新测试。

**扩展与练习建议（学习路线）**

- 练习 1：修改 `MetadataService.SplitPossibleTagString`，增加对中英文分号、斜杠等分隔符的处理，并为 tag 添加小写归一化与停用词过滤。
- 练习 2：把 `ImageConverter` 中的 WebP 实现替换为 `SkiaSharp`，验证大小与质量差异。
- 练习 3：为 `MetadataService` 增加更细粒度的 XMP 解析（例如解析 `dc:subject` 或 `photoshop:Keywords`）。

**项目文件与结构索引**

- `src\ImageInfo\ImageInfo.csproj` — 主项目（入口 `Program.cs`）。
- `src\ImageInfo\Program.cs` — 示例 CLI，调用 `FileScanner` 与 `MetadataService` 并打印结果。
- `src\ImageInfo\Models\ImageInfoModel.cs` — 简单数据模型（`FilePath`, `CreatedUtc`, `ModifiedUtc`, `Tags`）。
- `src\ImageInfo\Services\` — 模块实现目录（`FileScanner`, `MetadataService`, `ImageConverter`）。
- `tests\ImageInfo.Tests\` — 单元测试项目，包含 `ConvertersTests`。

如果你希望我把 README 进一步拆成 `docs/` 中的多页指南（例如“快速入门”、“API 参考”、“进阶练习”），我可以帮你生成带目录的文档。或者我可以现在替换 `Magick.NET` 为 `SkiaSharp`，将 WebP 转换也改为 `SkiaSharp` 实现，你更倾向哪个方向？
