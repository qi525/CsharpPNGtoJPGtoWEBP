import pandas as pd
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import Ridge
import sys
import os
import re

def extract_score(file_path, rating_map, score_prefix, default_score):
    pattern = re.compile(rf'{re.escape(score_prefix)}(\d+)', re.IGNORECASE)
    match = pattern.search(file_path)
    if match:
        try:
            score = int(match.group(1))
            return float(np.clip(score, 0, 100))
        except ValueError:
            pass
    for keyword, score in rating_map.items():
        if keyword in file_path:
            return float(score)
    return default_score

def main():
    if len(sys.argv) < 3:
        print("用法: python scorer_cli.py 输入csv路径 输出csv路径", file=sys.stderr)
        sys.exit(1)
    input_csv = sys.argv[1]
    output_csv = sys.argv[2]

    # 配置参数
    rating_map = {
        "特殊：100分": 100,
        "特殊：98分": 98,
        "超绝": 95,
        "特殊画风": 90,
        "超级精选": 85,
        "精选": 80
    }
    score_prefix = "@@@评分"
    default_score = 50.0
    predicted_col = "个性化推荐预估评分"
    target_col = "偏好定标分"

    df = pd.read_csv(input_csv)
    if df.shape[0] == 0:
        print("输入CSV无数据", file=sys.stderr)
        sys.exit(2)
    # 假定A列为路径，L列为特征
    a_col = df.columns[0]
    l_col = df.columns[11] if len(df.columns) > 11 else df.columns[-1]
    df[a_col] = df[a_col].fillna('').astype(str)
    df[l_col] = df[l_col].fillna('').astype(str)
    # 目标分
    df[target_col] = df[a_col].apply(lambda x: extract_score(x, rating_map, score_prefix, default_score))
    Y_all = df[target_col].values
    corpus = df[l_col].tolist()
    vectorizer = TfidfVectorizer(token_pattern=r'(?u)\b\w+\b', stop_words=None)
    X_all = vectorizer.fit_transform(corpus)
    train_indices = np.where(Y_all != default_score)[0]
    if len(train_indices) == 0:
        print("无有效训练样本", file=sys.stderr)
        sys.exit(3)
    X_train = X_all[train_indices]
    Y_train = Y_all[train_indices]
    model = Ridge(alpha=1.0)
    model.fit(X_train, Y_train)
    predicted_scores = model.predict(X_all)
    final_scores = np.clip(predicted_scores, 0.0, 100.0).round().astype(int)
    df[predicted_col] = final_scores
    df.to_csv(output_csv, index=False, encoding='utf-8-sig')
    print(f"评分完成，结果已写入: {output_csv}")

if __name__ == "__main__":
    main()
