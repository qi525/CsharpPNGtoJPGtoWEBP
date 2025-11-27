using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ImageInfo.Resources
{
    public class CsvScoreRow
    {
        public string Path { get; set; } = string.Empty;
        public string Feature { get; set; } = string.Empty;
        public float TargetScore { get; set; }
        public float PredictedScore { get; set; }
    }

    public class FeatureData
    {
        public string Feature { get; set; } = string.Empty;
        public float Label { get; set; }
    }

    public class Prediction
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }

    public static class ScorerCliCs
    {
        static readonly Dictionary<string, float> RatingMap = new()
        {
            ["特殊：100分"] = 100,
            ["特殊：98分"] = 98,
            ["超绝"] = 95,
            ["特殊画风"] = 90,
            ["超级精选"] = 85,
            ["精选"] = 80
        };
        const string ScorePrefix = "@@@评分";
        const float DefaultScore = 50.0f;
        const string PredictedCol = "个性化推荐预估评分";
        const string TargetCol = "偏好定标分";

        /// <summary>
        /// 纯C#内存评分主入口：输入为 (Path, Feature) 列表，输出为 (Path, TargetScore, PredictedScore) 列表
        /// </summary>
        public static List<(string Path, float TargetScore, float PredictedScore)> RunFromItems(IEnumerable<(string Path, string Feature)> items)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var rows = items.Select(i => new CsvScoreRow { Path = i.Path ?? string.Empty, Feature = i.Feature ?? string.Empty }).ToList();
            if (rows.Count == 0) throw new ArgumentException("输入数据为空");

            // 目标分提取
            foreach (var row in rows)
                row.TargetScore = ExtractScore(row.Path);
            Console.WriteLine($"目标分提取耗时: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            // ML.NET 数据准备
            var mlContext = new MLContext();
            var data = rows.Select(r => new FeatureData { Feature = r.Feature, Label = r.TargetScore }).ToList();

            // 训练集筛选
            var trainData = data.Where(d => d.Label != DefaultScore).ToList();
            if (trainData.Count == 0)
                throw new InvalidOperationException("无有效训练样本");

            var trainView = mlContext.Data.LoadFromEnumerable(trainData);
            Console.WriteLine($"ML.NET数据准备耗时: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            // 构建并训练模型
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(FeatureData.Feature))
                .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features"));
            var model = pipeline.Fit(trainView);
            Console.WriteLine($"模型训练耗时: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            // 预测
            var allDataView = mlContext.Data.LoadFromEnumerable(data);
            var predictions = model.Transform(allDataView);
            var scores = mlContext.Data.CreateEnumerable<Prediction>(predictions, reuseRowObject: false).ToList();
            for (int i = 0; i < rows.Count; i++)
                rows[i].PredictedScore = Math.Clamp((float)Math.Round(scores[i].Score), 0, 100);
            Console.WriteLine($"预测耗时: {sw.ElapsedMilliseconds} ms");

            return rows.Select(r => (r.Path, r.TargetScore, r.PredictedScore)).ToList();
        }

        static float ExtractScore(string path)
        {
            var match = Regex.Match(path, $"{Regex.Escape(ScorePrefix)}(\\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int score))
                return Math.Clamp(score, 0, 100);
            foreach (var kv in RatingMap)
                if (path.Contains(kv.Key))
                    return kv.Value;
            return DefaultScore;
        }
    }
}
