using System.Collections.Generic;

namespace ImageInfo.Data;

/// <summary>
/// 自定义关键词表
/// 来源：filename_tagger.py
/// 用途：从提示词中提取匹配的关键词，自动添加文件名后缀（便于搜索分类）
/// 格式：匹配到的关键词用 ___ 连接，添加到文件名末尾
/// </summary>
public static class CustomKeywords
{
    /// <summary>
    /// 自定义关键词列表
    /// 按顺序匹配，每个关键词对应一个文件名后缀标签
    /// </summary>
    public static readonly List<string> Keywords = new()
    {
        // 游戏/动画人物和作品
        "shorekeeper",
        "noshiro",
        "rio_(blue_archive)",
        "taihou",
        "azur_lane",
        "blue_archive",
        "fgo",
        "pokemon",
        "fate",
        "touhou",
        "idolmaster",
        "love_live",
        "bleach",
        "gundam",
        "umamusume",
        "honkai",
        "hololive",
        "one_piece",
        "final_fantasy",
        "persona",
        "zelda",
        "chainsaw_man",
        "nikke",
        "xenoblade",
        "kantai_collection",
        "genshin_impact",
        "naruto",
        "overwatch",
        
        // 身体特征和装饰
        "genderswap",
        "futanari",
        "skeleton",
        "green_hair",
        "splatoon",
        "boku_no_hero_academia",
        "midoriya_izuku",
        "ashido_mina",
        "band-aid",
        "covered_nipples",
        
        // 服装和脱衣
        "undressing",        // 脱衣
        "removing_bra",      // 脱下胸罩
        "tail_around_neck",  // 尾巴绕在脖子上
        
        // 危险/不当行为
        "asphyxiation",      // 窒息
        "strangling",        // 绞杀
        
        // 服装配置 (乳贴/胸贴类)
        "pasties",           // 乳贴/胸贴
        "cross_pasties",     // 十字乳贴
        "tape",              // 胶带
        "tape_on_nipples",   // 胶带贴在乳头上
        "open_clothes",      // 敞开的衣服
        "open_jacket",       // 敞开的夹克
        "no_pants"           // 没穿裤子
    };
}
