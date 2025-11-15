using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageInfo.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;

namespace ImageInfo.Services
{
    /*
     File: MetadataService.cs
     Purpose: 从图片文件读取元数据并提取 AI 风格的标签集合，同时读取文件的创建/修改时间。
     Responsibilities:
      - 提供一个公开入口 ExtractTagsAndTimes 返回包含路径、时间、标签的 ImageInfoModel。
      - 将具体解析逻辑拆分为多个私有职责函数（PNG text、EXIF、XMP、文件名 token、规范化）。
     Third-party: 使用 MetadataExtractor 解析 PNG/EXIF/XMP 数据。
    */

    /// <summary>
    /// 图片元数据读取与标签提取服务。
    /// 支持从 PNG tEXt、EXIF、XMP 及文件名中提取 AI 风格标签。
    /// </summary>
    public static class MetadataService
    {
        /// <summary>
        /// 主入口：读取图片文件的路径、UTC 创建/修改时间，以及从多种元数据来源提取的标签集合。
        /// 将调用内部的 ReadFileTimes、CollectAllMetadataTags、NormalizeAndDedup 等方法。
        /// 返回的标签已去重且按字符串方式规范化（去除空白）。
        /// </summary>
        /// <param name="filePath">图片文件路径</param>
        /// <returns>包含路径、时间、标签的 ImageInfoModel</returns>
        public static ImageInfoModel ExtractTagsAndTimes(string filePath)
        {
            var model = new ImageInfoModel
            {
                FilePath = filePath,
            };

            ReadFileTimes(filePath, model);

            try
            {
                var rawTags = CollectAllMetadataTags(filePath);
                var normalizedTags = NormalizeAndDedup(rawTags);
                var filenameTokens = ExtractFilenameTokens(filePath);
                model.Tags.AddRange(normalizedTags);
                model.Tags.AddRange(filenameTokens);
                model.Tags = model.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch
            {
                // 元数据读取失败时返回仅含时间的信息
            }

            return model;
        }

        /// <summary>
        /// 读取文件的创建和修改时间（UTC）并写入目标模型。
        /// </summary>
        private static void ReadFileTimes(string filePath, ImageInfoModel model)
        {
            model.CreatedUtc = File.GetCreationTimeUtc(filePath);
            model.ModifiedUtc = File.GetLastWriteTimeUtc(filePath);
        }

        /// <summary>
        /// 从所有可用目录中收集原始标签字符串（包括 PNG tEXt、EXIF 和 XMP）。
        /// 返回未经规范化的字符串集合，由上层负责拆分/去重。
        /// </summary>
        private static IEnumerable<string> CollectAllMetadataTags(string filePath)
        {
            var tags = new List<string>();
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                tags.AddRange(ParsePngText(directories));
                tags.AddRange(ParseExifTags(directories));
                tags.AddRange(ParseXmpTags(directories));
            }
            catch
            {
                // 元数据读取失败时返回空列表
            }
            return tags;
        }

        /// <summary>
        /// 规范化并去重：将可能包含多个标签的字符串拆分、去空格并按不区分大小写去重。
        /// </summary>
        private static IEnumerable<string> NormalizeAndDedup(IEnumerable<string> rawTags)
        {
            return rawTags
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .SelectMany(SplitPossibleTagString)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 从文件名（不含扩展）按 -, _, 空格 拆分 token，作为辅助标签返回。
        /// 仅返回长度 >=2 的 token，避免噪声。
        /// </summary>
        private static IEnumerable<string> ExtractFilenameTokens(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath)
                .Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length >= 2)
                .ToList();
        }

        /// <summary>
        /// 解析 PNG tEXt 条目并返回描述字符串集合。
        /// </summary>
        private static IEnumerable<string> ParsePngText(IEnumerable<MetadataExtractor.Directory> directories)
        {
            var tags = new List<string>();
            foreach (var pngDir in directories.OfType<PngDirectory>())
            {
                foreach (var tag in pngDir.Tags)
                {
                    var desc = tag?.Description;
                    if (!string.IsNullOrWhiteSpace(desc))
                        tags.Add(desc);
                }
            }
            return tags;
        }

        /// <summary>
        /// 遍历非 PNG/XMP 的目录（如 EXIF 子目录），收集 tag 描述文本。
        /// </summary>
        private static IEnumerable<string> ParseExifTags(IEnumerable<MetadataExtractor.Directory> directories)
        {
            var tags = new List<string>();
            foreach (var dir in directories)
            {
                if (dir is PngDirectory || dir is XmpDirectory) continue;
                foreach (var tag in dir.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag.Description))
                        tags.Add(tag.Description);
                }
            }
            return tags;
        }

        /// <summary>
        /// 从 XMP 目录中尝试获取常见字段（示例获取 dc:subject）。
        /// 注意：XMP 解析复杂，此处为简要示例，可根据需要扩展。
        /// </summary>
        private static IEnumerable<string> ParseXmpTags(IEnumerable<MetadataExtractor.Directory> directories)
        {
            var tags = new List<string>();
            foreach (var xmpDir in directories.OfType<XmpDirectory>())
            {
                var xmp = xmpDir.XmpMeta;
                if (xmp != null)
                {
                    var subject = xmp.GetPropertyString("http://purl.org/dc/elements/1.1/", "subject");
                    if (!string.IsNullOrWhiteSpace(subject))
                        tags.Add(subject);
                }
            }
            return tags;
        }

        /// <summary>
        /// 拆分可能包含多个标签的字符串（逗号、分号、竖线、换行等），返回单个 tag。
        /// </summary>
        private static IEnumerable<string> SplitPossibleTagString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Array.Empty<string>();

            var parts = s.Split(new[] { ',', ';', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim());

            return parts;
        }
    }
}
