using System;
using System.Linq;
using System.Collections.Generic;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Extensions;

/// <summary>
/// 技藝類型定義，防止字串拼寫錯誤
/// </summary>
public enum BasicArtType
{
    Unarmed, Blade, Bow, Dagger, Halberd, Knife
}

public static class JiangXiaoUtils
{
    /// <summary>
    /// 獲取內視星圖實例（自動兼容基礎版與升級版）
    /// </summary>
    public static IInnerStarMap? GetStarMap(Player? player)
    {
        // STS2 推薦做法：使用 OfType 過濾實作了介面的所有遺物
        return player?.Relics.OfType<IInnerStarMap>().FirstOrDefault();
    }

    /// <summary>
    /// 快速獲取當前星點數
    /// </summary>
    public static int GetTotalSkillPoints(Player? player)
    {
        var starMap = GetStarMap(player);
        return starMap?.JiangXiaoMod_SkillPoints ?? 0;
    }

    // --- 核心遺物獲取 ---

    /// <summary>
    /// 獲取星技品質等級 (1-7)
    /// </summary>
    public static int GetSkillRank(Player? player)
    {
        if (player == null) return 1;
        var relic = player.Relics.OfType<StarSkillQuality>().FirstOrDefault();
        return relic?.SkillRank ?? 1;
    }


    public static int GetPowerLevel(Player? player)
    {
        if (player == null) return 1;
        var relic = player.Relics.OfType<StarPowerLevel>().FirstOrDefault();
        return relic?.PowerLevel ?? 1;
    }


    /// <summary>
    /// 獲取基礎技藝遺物實例
    /// </summary>
    public static BasicArts? GetBasicArtsRelic(Player? player)
    {
        // 使用 OfType 替代 as 轉型，代碼更簡潔
        return player?.Relics.OfType<BasicArts>().FirstOrDefault();
    }

    // --- 技藝等級獲取 ---
	//徒手格鬥
    public static int GetUnarmedRank(Player? player) => GetArtRank(player, BasicArtType.Unarmed);
	//夏家刀法
    public static int GetBladeRank(Player? player)   => GetArtRank(player, BasicArtType.Blade);
	//弓箭
    public static int GetBowRank(Player? player)     => GetArtRank(player, BasicArtType.Bow);
	//匕首
    public static int GetDaggerRank(Player? player)  => GetArtRank(player, BasicArtType.Dagger);
	//天方戟
    public static int GetHalberdRank(Player? player) => GetArtRank(player, BasicArtType.Halberd);
	//格鬥刀
    public static int GetCombatKnifeRank(Player? player) => GetArtRank(player, BasicArtType.Knife);

    /// <summary>
    /// 統一的等級獲取內核
    /// </summary>
    public static int GetArtRank(Player? player, BasicArtType type)
    {
        var relic = GetBasicArtsRelic(player);
        if (relic == null) return 1;

        int pts = GetArtPoints(player, type);
        // 調用 BasicArts 遺物模型中的等級計算邏輯
        return relic.GetRank(pts);
    }

    /// <summary>
    /// 獲取所有基礎技藝 Rank 的總和（用於某些特殊星技的加成計算）
    /// </summary>
    public static int GetTotalBasicArtRankSum(Player? player)
    {
        if (player == null) return 0;
        
        // 修正點：直接調用本類別的靜態方法 GetArtRank
        return Enum.GetValues<BasicArtType>()
                   .Sum(type => GetArtRank(player, type));
    }

    /// <summary>
    /// 萬用數值讀取器
    /// </summary>
    public static int GetArtPoints(Player? player, BasicArtType type)
    {
        var relic = GetBasicArtsRelic(player);
        if (relic == null) return 0;

        return type switch
        {
            BasicArtType.Unarmed => relic.UnarmedPts,
            BasicArtType.Blade   => relic.BladePts,
            BasicArtType.Bow     => relic.BowPts,
            BasicArtType.Dagger  => relic.DaggerPts,
            BasicArtType.Halberd => relic.HalberdPts,
            BasicArtType.Knife   => relic.CombatKnifePts,
            _ => 0
        };
    }
    
    /// <summary>
    /// 字串版本兼容 (主要用於從本地化或外部配置讀取時)
    /// </summary>
    public static int GetArtPoints(Player? player, string artType)
    {
        if (Enum.TryParse<BasicArtType>(artType, true, out var type))
        {
            return GetArtPoints(player, type);
        }
        return 0;
    }
}