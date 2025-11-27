using System;
using System.Collections.Generic;

namespace ImageInfo.Models
{
    /// <summary>
    /// 个性化推荐评分系统的配置参数。
    /// 难度: 1 (简单配置类)
    /// 
    /// 核心概念：
    /// - RATING_MAP：人工标注规则，基于文件夹名称关键词匹配（硬编码）
    /// - 未被标注的图片使用默认中性分数
    /// - TF-IDF向量化和Ridge回归用于学习模型权重
    /// </summary>
    public class ImageScorerConfig
    {
        /// <summary>
        /// 基准评分映射（基于文件夹名称精确匹配）。
        /// 这是模型学习的"正面"信号 - 告诉模型哪些图片是高质量的。
        /// 
        /// 使用场景：
        /// - 如果文件所在文件夹名称为"精选"，该图片被标记为80分（人工认可）
        /// - 这些有明确标注的图片用于训练模型
        /// - 模型学习这些高分图片的词汇特征，预测其他未标注图片的分数
        /// 
        /// 注意：每个分类应该有至少50-100张样本，样本太少会导致权重不稳定
        /// </summary>
        public Dictionary<string, double> RatingMap { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "特殊：100分", 100 },
            { "特殊：98分", 98 },
            { "超绝", 95 },
            { "特殊画风", 90 },
            // { "超级精选", 85 },  // 注释掉：样本数过少(46张)，会导致权重不稳定，建议加入更多数据后再启用
            { "精选", 80 }
        };

        /// <summary>
        /// 自定义评分标记的前缀（如@@@评分）
        /// </summary>
        public string ScorePrefix { get; set; } = "@@@评分";

        /// <summary>
        /// 未被人工标记的图片的默认中性分数。
        /// 作用：作为模型训练的"背景"信号，不参与模型学习。
        /// </summary>
        public double DefaultNeutralScore { get; set; } = 50.0;

        /// <summary>
        /// 核心词汇列的索引（对应Excel的L列，即第12列，0-based索引为11）。
        /// 这一列包含TF-IDF提取的关键词，是模型的输入特征。
        /// </summary>
        public int LColumnIndex { get; set; } = 11;

        /// <summary>
        /// 最终预测评分的列名。
        /// 新增的列用于存储算法预测的个性化评分。
        /// </summary>
        public string PredictedScoreColumn { get; set; } = "个性化推荐预估评分";

        /// <summary>
        /// 文件夹默认匹配分的列名。
        /// 新增的列用于存储基于RATING_MAP的硬编码规则结果。
        /// </summary>
        public string FolderMatchScoreColumn { get; set; } = "文件夹默认匹配分";

        /// <summary>
        /// 训练目标分数的列名（内部使用）。
        /// 用于存储从文件夹名称提取的基准评分（基于RATING_MAP的精确匹配）。
        /// </summary>
        public string TargetScoreColumn { get; set; } = "偏好定标分";

        /// <summary>
        /// Ridge回归的正则化参数。
        /// alpha越大，正则化强度越强，模型越简单，权重越接近0。
        /// 推荐值：1000.0 (在没有特征归一化的情况下，需要更强的约束)
        /// </summary>
        public double RidgeAlpha { get; set; } = 1000.0;

        /// <summary>
        /// TF-IDF分词时是否启用StopWords过滤。
        /// 推荐值：false（保留所有词汇以提高表达能力）
        /// </summary>
        public bool EnableStopWordFilter { get; set; } = false;

        /// <summary>
        /// TF-IDF分词时的最小词长。
        /// </summary>
        public int MinTokenLength { get; set; } = 1;

        /// <summary>
        /// TF-IDF分词时的最大词长。
        /// </summary>
        public int MaxTokenLength { get; set; } = 100;
    }
}
