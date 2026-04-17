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
        // 直接使用介面進行過濾，OfType 會同時抓到實作了該介面的所有遺物
        return player?.Relics.OfType<IInnerStarMap>().FirstOrDefault();
    }
    /// 快速獲取當前星點數
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
        // [STS2_Optimization] 使用 OfType 直接過濾類型
        var relic = player.Relics.OfType<StarSkillQuality>().FirstOrDefault();
        return relic?.SkillRank ?? 1;
    }

    /// <summary>
    /// 獲取基礎技藝遺物實例
    /// </summary>
    public static BasicArts? GetBasicArtsRelic(Player? player)
    {
        return player?.Relics.OfType<BasicArts>().FirstOrDefault();
    }

    // --- 技藝等級獲取 (簡化版) ---

    public static int GetUnarmedRank(Player? player) => GetArtRank(player, BasicArtType.Unarmed);
    public static int GetBladeRank(Player? player)   => GetArtRank(player, BasicArtType.Blade);
    public static int GetBowRank(Player? player)     => GetArtRank(player, BasicArtType.Bow);
    public static int GetDaggerRank(Player? player)  => GetArtRank(player, BasicArtType.Dagger);
    public static int GetHalberdRank(Player? player) => GetArtRank(player, BasicArtType.Halberd);
    public static int GetCombatKnifeRank(Player? player) => GetArtRank(player, BasicArtType.Knife);

    /// <summary>
    /// 統一的等級獲取內核
    /// </summary>
    public static int GetArtRank(Player? player, BasicArtType type)
    {
        var relic = GetBasicArtsRelic(player);
        if (relic == null) return 1;

        int pts = GetArtPoints(player, type);
        return relic.GetRank(pts);
    }

    /// <summary>
    /// 萬用數值讀取器 (使用 Enum 確保安全)
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
    
    // 為了相容性，保留原有的字串版本但標記為過時，或內部轉向 Enum
    public static int GetArtPoints(Player? player, string artType)
    {
        if (Enum.TryParse<BasicArtType>(artType, true, out var type))
        {
            return GetArtPoints(player, type);
        }
        return 0;
    }
}