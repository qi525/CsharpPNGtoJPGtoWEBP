using System;
using ImageInfo.Models;

namespace ImageInfo.Services
{
    /// <summary>
    /// 元数据提取工厂 (Factory Pattern)。
    /// 
    /// 在大型项目中，这种写法称为 "Factory Pattern（工厂模式)"：
    /// - 将对象创建逻辑集中到一个地方
    /// - 根据输入参数（图片类型）返回相应的实现
    /// - 隐藏具体实现细节，暴露统一接口
    /// 
    /// 优势：
    /// 1. 易于扩展：添加新图片类型时，只需在工厂中添加分支
    /// 2. 解耦合：调用者无需知道具体实现类
    /// 3. 代码复用：多处都可使用同一工厂
    /// 4. 便于测试：可以注入 Mock 对象
    /// 
    /// 常见场景：
    /// - 数据库驱动工厂（SQL Server、MySQL、PostgreSQL）
    /// - 日志器工厂（Console、File、Cloud）
    /// - 支付网关工厂（Stripe、PayPal、Alipay）
    /// </summary>
    public static class MetadataExtractorFactory
    {
        /// <summary>
        /// 【核心方法】统一的元数据读取入口。
        /// 
        /// 工作流：
        /// 1. 检测图片类型（使用 ImageTypeDetector）
        /// 2. 根据类型分派到相应的提取器
        /// 3. 返回提取结果
        /// 
        /// 这就是 Factory Pattern 中的 "单一责任原则"：
        /// 工厂只负责根据类型选择实现，不关心具体业务逻辑。
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <returns>提取的 AI 元数据</returns>
        public static AIMetadata GetImageInfo(string imagePath)
        {
            // 第一步：判断图片类型
            var imageFormat = ImageTypeDetector.DetectImageFormat(imagePath);

            // 第二步：根据类型选择相应的提取器（分派逻辑）
            return imageFormat switch
            {
                ImageTypeDetector.ImageFormat.PNG => GetPngInfo(imagePath),
                ImageTypeDetector.ImageFormat.JPEG => GetJpegInfo(imagePath),
                ImageTypeDetector.ImageFormat.WebP => GetWebPInfo(imagePath),
                _ => HandleUnknownFormat(imagePath)
            };
        }

        /// <summary>
        /// 【具体实现 1】PNG 元数据读取。
        /// 分派到 PngMetadataExtractor。
        /// </summary>
        private static AIMetadata GetPngInfo(string imagePath)
        {
            try
            {
                var metadata = PngMetadataExtractor.ReadAIMetadata(imagePath);
                Console.WriteLine($"PNG metadata extracted from: {imagePath}");
                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract PNG metadata: {ex.Message}");
                return new AIMetadata();
            }
        }

        /// <summary>
        /// 【具体实现 2】JPEG 元数据读取。
        /// 分派到 JpegMetadataExtractor。
        /// </summary>
        private static AIMetadata GetJpegInfo(string imagePath)
        {
            try
            {
                var metadata = JpegMetadataExtractor.ReadAIMetadata(imagePath);
                Console.WriteLine($"JPEG metadata extracted from: {imagePath}");
                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract JPEG metadata: {ex.Message}");
                return new AIMetadata();
            }
        }

        /// <summary>
        /// 【具体实现 3】WebP 元数据读取。
        /// 分派到 WebPMetadataExtractor。
        /// </summary>
        private static AIMetadata GetWebPInfo(string imagePath)
        {
            try
            {
                var metadata = WebPMetadataExtractor.ReadAIMetadata(imagePath);
                Console.WriteLine($"WebP metadata extracted from: {imagePath}");
                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract WebP metadata: {ex.Message}");
                return new AIMetadata();
            }
        }

        /// <summary>
        /// 【错误处理】未知格式的处理。
        /// </summary>
        private static AIMetadata HandleUnknownFormat(string imagePath)
        {
            Console.WriteLine($"Unknown or unsupported image format: {imagePath}");
            return new AIMetadata();
        }

        // ==================== 【扩展方法】如需添加新格式，遵循此模板 ====================

        /// <summary>
        /// 示例：如果要支持新格式（如 AVIF），遵循此步骤：
        /// 
        /// 步骤 1: 创建新的提取器类
        ///   public static class AvifMetadataExtractor { ... }
        /// 
        /// 步骤 2: 在 ImageTypeDetector 中添加新的 Magic Number 检测
        ///   if (buffer matches AVIF signature) return ImageFormat.AVIF;
        /// 
        /// 步骤 3: 在此工厂中添加新的 case 分支
        ///   ImageTypeDetector.ImageFormat.AVIF => GetAvifInfo(imagePath),
        /// 
        /// 步骤 4: 实现对应的 GetXxxInfo 方法
        ///   private static AIMetadata GetAvifInfo(string imagePath) { ... }
        /// 
        /// 这就是 Factory Pattern 的强大之处：
        /// - 新格式支持只需在这一个文件中添加代码
        /// - 不影响其他部分
        /// - 符合"开放-闭合原则"（Open/Closed Principle）
        /// </summary>

        // ==================== 【替代方案】Dictionary 映射工厂 ====================

        /// <summary>
        /// 高级方案：使用 Dictionary 和反射实现更灵活的工厂。
        /// 
        /// 优点：
        /// - 可运行时注册新格式
        /// - 代码更简洁（减少 switch 分支）
        /// - 支持插件式架构
        /// 
        /// 示例（未实现）：
        /// private static Dictionary&lt;ImageTypeDetector.ImageFormat, Func&lt;string, AIMetadata&gt;&gt; extractors =
        ///     new Dictionary&lt;ImageTypeDetector.ImageFormat, Func&lt;string, AIMetadata&gt;&gt;
        ///     {
        ///         { ImageTypeDetector.ImageFormat.PNG, PngMetadataExtractor.ReadAIMetadata },
        ///         { ImageTypeDetector.ImageFormat.JPEG, JpegMetadataExtractor.ReadAIMetadata },
        ///         { ImageTypeDetector.ImageFormat.WebP, WebPMetadataExtractor.ReadAIMetadata }
        ///     };
        /// 
        /// 使用方式：
        /// public static AIMetadata GetImageInfo(string imagePath)
        /// {
        ///     var format = ImageTypeDetector.DetectImageFormat(imagePath);
        ///     return extractors.TryGetValue(format, out var extractor) 
        ///         ? extractor(imagePath)
        ///         : new AIMetadata();
        /// }
        /// </summary>
    }
}
