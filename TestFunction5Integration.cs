using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageInfo.Models;
using ImageInfo.Services;

namespace ImageInfoTests
{
    /// <summary>
    /// 功能5集成测试
    /// 验证评分系统是否正确集成到功能4流程中
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("功能5 (个性化评分) 集成测试");
            Console.WriteLine("═══════════════════════════════════════════════════\n");

            // 创建测试数据
            var testRecords = CreateTestMetadataRecords();
            
            Console.WriteLine($"📦 创建测试数据: {testRecords.Count} 条记录\n");
            PrintTestData(testRecords);

            // 测试评分服务
            Console.WriteLine("\n\n🚀 启动评分服务...\n");
            var config = new ImageScorerConfig();
            var scorer = new ImageScorerService(config);

            bool success = await scorer.ScoreMetadataRecordsAsync(testRecords, "TfidfKeywords");

            if (!success)
            {
                Console.WriteLine("❌ 评分失败");
                return;
            }

            Console.WriteLine("\n\n📊 评分结果：\n");
            PrintScoredResults(testRecords);

            Console.WriteLine("\n✅ 测试完成");
        }

        /// <summary>
        /// 创建测试MetadataRecord列表
        /// </summary>
        static List<MetadataRecord> CreateTestMetadataRecords()
        {
            return new List<MetadataRecord>
            {
                new MetadataRecord
                {
                    FilePath = "D:\\MyPhotos\\精选\\portrait.jpg",
                    Filename = "portrait.jpg",
                    Prompt = "portrait photo beautiful lighting",
                    TfidfKeywords = "portrait:0.85, photo:0.72, beautiful:0.68",
                    TargetScore = 85.0  // 标记为训练样本
                },
                new MetadataRecord
                {
                    FilePath = "D:\\MyPhotos\\日常\\normal.jpg",
                    Filename = "normal.jpg",
                    Prompt = "casual daily photo ordinary",
                    TfidfKeywords = "casual:0.45, photo:0.38, ordinary:0.25",
                    TargetScore = 50.0  // 标记为训练样本
                },
                new MetadataRecord
                {
                    FilePath = "D:\\MyPhotos\\超绝\\masterpiece.jpg",
                    Filename = "masterpiece.jpg",
                    Prompt = "masterpiece art excellent quality",
                    TfidfKeywords = "masterpiece:0.92, art:0.88, excellent:0.85",
                    TargetScore = 95.0  // 标记为训练样本
                },
                new MetadataRecord
                {
                    FilePath = "D:\\MyPhotos\\other\\unknown.jpg",
                    Filename = "unknown.jpg",
                    Prompt = "photo with some interesting elements",
                    TfidfKeywords = "photo:0.55, interesting:0.48, elements:0.35"
                    // 不设置TargetScore，将被预测
                },
                new MetadataRecord
                {
                    FilePath = "D:\\MyPhotos\\特别\\special.jpg",
                    Filename = "special.jpg",
                    Prompt = "special category image",
                    TfidfKeywords = "special:0.75, category:0.52, image:0.40"
                    // 不设置TargetScore，将被预测
                }
            };
        }

        /// <summary>
        /// 打印测试数据
        /// </summary>
        static void PrintTestData(List<MetadataRecord> records)
        {
            Console.WriteLine("文件路径".PadRight(40) + "| Prompt".PadRight(40) + "| Target");
            Console.WriteLine(new string('-', 95));

            foreach (var record in records)
            {
                string target = record.TargetScore > 0 ? $"{record.TargetScore:F0}" : "预测";
                Console.WriteLine(
                    record.FilePath.PadRight(40) + 
                    "| " + (record.Prompt?.Substring(0, Math.Min(38, record.Prompt.Length)) ?? "").PadRight(38) +
                    "| " + target.PadRight(4)
                );
            }
        }

        /// <summary>
        /// 打印评分结果
        /// </summary>
        static void PrintScoredResults(List<MetadataRecord> records)
        {
            Console.WriteLine("文件名".PadRight(25) + "| 文件夹默认分".PadRight(14) + "| 推荐预估分".PadRight(14) + "| 原始Target");
            Console.WriteLine(new string('-', 75));

            foreach (var record in records)
            {
                string target = record.TargetScore > 0 ? $"{record.TargetScore:F1}" : "-";
                Console.WriteLine(
                    record.Filename.PadRight(25) +
                    "| " + $"{record.FolderMatchScore:F1}".PadRight(12) +
                    "| " + $"{record.PredictedScore:F1}".PadRight(12) +
                    "| " + target
                );
            }
        }
    }
}
