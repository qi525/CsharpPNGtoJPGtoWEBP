using ImageInfo.Services;

namespace ImageInfo.Templates;

/// <summary>
/// FilenameParser 集成模板
/// 
/// 这个模板展示如何在实际项目中集成 FilenameParser 功能
/// </summary>
public class FilenameParserIntegrationTemplate
{
    /// <summary>
    /// 场景 1: 在图像处理服务中使用
    /// </summary>
    public class ImageProcessingWithFilenameParser
    {
        /// <summary>
        /// 处理单个图像文件
        /// </summary>
        public void ProcessImageFile(string imagePath)
        {
            // 第1步: 解析文件名
            var parseResult = FilenameParser.ParseFilenamePath(imagePath);
            
            if (!parseResult.IsSuccess)
            {
                Console.WriteLine($"❌ 文件名解析失败: {parseResult.ErrorMessage}");
                return;
            }

            // 第2步: 提取关键信息
            var originalName = parseResult.OriginalName;
            var fileExtension = parseResult.Extension;
            var suffix = parseResult.Suffix;

            Console.WriteLine($"✓ 成功解析文件");
            Console.WriteLine($"  原始名称: {originalName}");
            Console.WriteLine($"  文件扩展名: {fileExtension}");
            Console.WriteLine($"  附加后缀: {(string.IsNullOrEmpty(suffix) ? "(无)" : suffix)}");

            // 第3步: 处理图像
            // TODO: 这里添加实际的图像处理逻辑
            // ProcessImage(imagePath, originalName, fileExtension);
        }
    }

    /// <summary>
    /// 场景 2: 在批量文件处理中使用
    /// </summary>
    public class BatchFileProcessing
    {
        /// <summary>
        /// 批量处理文件夹中的图像
        /// </summary>
        public void ProcessImageFolder(string folderPath)
        {
            var successCount = 0;
            var failureCount = 0;
            var processedFiles = new List<(string original, string extension, string fullPath)>();

            // 遍历文件夹中的所有图像文件
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var filePath in imageFiles)
            {
                var filename = Path.GetFileName(filePath);
                var parseResult = FilenameParser.ParseFilename(filename);

                if (parseResult.IsSuccess)
                {
                    successCount++;
                    processedFiles.Add((parseResult.OriginalName, parseResult.Extension, filePath));
                    Console.WriteLine($"✓ {parseResult.OriginalName}{parseResult.Extension}");
                }
                else
                {
                    failureCount++;
                    Console.WriteLine($"✗ {filename} - {parseResult.ErrorMessage}");
                }
            }

            // 输出统计信息
            Console.WriteLine($"\n📊 处理完成: 成功 {successCount}, 失败 {failureCount}");
            Console.WriteLine($"已处理文件列表:");
            foreach (var (original, ext, path) in processedFiles)
            {
                Console.WriteLine($"  - {original}{ext}");
            }
        }
    }

    /// <summary>
    /// 场景 3: 在数据库操作中使用
    /// </summary>
    public class DatabaseOperations
    {
        /// <summary>
        /// 将文件信息保存到数据库
        /// </summary>
        public void SaveImageToDatabaseByFilepath(string imagePath)
        {
            var parseResult = FilenameParser.ParseFilenamePath(imagePath);

            if (!parseResult.IsSuccess)
            {
                throw new ArgumentException($"无效的文件名: {parseResult.ErrorMessage}");
            }

            // 构建数据库记录
            var imageRecord = new
            {
                OriginalFilename = parseResult.OriginalName,
                FileExtension = parseResult.Extension,
                Suffix = parseResult.Suffix,
                FullPath = imagePath,
                RawFilename = parseResult.RawFilename,
                ProcessedAt = DateTime.Now
            };

            // TODO: 将 imageRecord 保存到数据库
            // database.Images.Insert(imageRecord);

            Console.WriteLine($"✓ 数据库记录已创建: {imageRecord.OriginalFilename}");
        }
    }

    /// <summary>
    /// 场景 4: 在文件转换中使用
    /// </summary>
    public class FileConversion
    {
        /// <summary>
        /// 转换文件并保持原始名称信息
        /// </summary>
        public string ConvertImageFile(string sourceFile, string targetFormat)
        {
            var parseResult = FilenameParser.ParseFilenamePath(sourceFile);

            if (!parseResult.IsSuccess)
            {
                throw new ArgumentException($"无法解析源文件名: {parseResult.ErrorMessage}");
            }

            // 构建目标文件名
            var newFileName = parseResult.OriginalName + parseResult.Suffix + $".{targetFormat}";
            var targetPath = Path.Combine(
                Path.GetDirectoryName(sourceFile) ?? "",
                newFileName
            );

            // TODO: 执行文件转换
            // ConvertFile(sourceFile, targetPath, targetFormat);

            Console.WriteLine($"✓ 文件转换完成");
            Console.WriteLine($"  源文件: {Path.GetFileName(sourceFile)}");
            Console.WriteLine($"  目标文件: {newFileName}");

            return targetPath;
        }

        /// <summary>
        /// 转换并去除后缀
        /// </summary>
        public string ConvertImageFileWithoutSuffix(string sourceFile, string targetFormat)
        {
            var parseResult = FilenameParser.ParseFilenamePath(sourceFile);

            if (!parseResult.IsSuccess)
            {
                throw new ArgumentException($"无法解析源文件名: {parseResult.ErrorMessage}");
            }

            // 仅保留原始名称，去除所有后缀
            var newFileName = parseResult.OriginalName + $".{targetFormat}";
            var targetPath = Path.Combine(
                Path.GetDirectoryName(sourceFile) ?? "",
                newFileName
            );

            // TODO: 执行文件转换
            // ConvertFile(sourceFile, targetPath, targetFormat);

            Console.WriteLine($"✓ 文件转换完成 (已去除后缀)");
            Console.WriteLine($"  源文件: {Path.GetFileName(sourceFile)}");
            Console.WriteLine($"  目标文件: {newFileName}");

            return targetPath;
        }
    }

    /// <summary>
    /// 场景 5: 在文件验证中使用
    /// </summary>
    public class FileValidation
    {
        /// <summary>
        /// 验证文件名格式
        /// </summary>
        public bool ValidateFilename(string filename)
        {
            var parseResult = FilenameParser.ParseFilename(filename);
            return parseResult.IsSuccess;
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        public string? GetValidationError(string filename)
        {
            var parseResult = FilenameParser.ParseFilename(filename);
            return parseResult.IsSuccess ? null : parseResult.ErrorMessage;
        }

        /// <summary>
        /// 批量验证文件名列表
        /// </summary>
        public void ValidateFilenameList(IEnumerable<string> filenames)
        {
            var valid = new List<string>();
            var invalid = new List<(string name, string error)>();

            foreach (var filename in filenames)
            {
                var parseResult = FilenameParser.ParseFilename(filename);

                if (parseResult.IsSuccess)
                {
                    valid.Add(filename);
                }
                else
                {
                    invalid.Add((filename, parseResult.ErrorMessage));
                }
            }

            Console.WriteLine($"✓ 有效文件名: {valid.Count}");
            foreach (var name in valid)
            {
                Console.WriteLine($"  - {name}");
            }

            Console.WriteLine($"\n✗ 无效文件名: {invalid.Count}");
            foreach (var (name, error) in invalid)
            {
                Console.WriteLine($"  - {name}: {error}");
            }
        }
    }
}
