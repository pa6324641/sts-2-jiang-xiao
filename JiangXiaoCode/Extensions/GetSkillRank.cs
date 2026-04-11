using System.Linq;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players; // 必須新增此命名空間

namespace JiangXiaoMod.Code.Extensions;

public static class JiangXiaoUtils
{
    // [STS2_Optimization] 統一獲取星技等級
    // 修正點：將 PlayerModel 改為 Player
    public static int GetSkillRank(Player? player)
    {
        if (player == null) return 1;

        // 根據 JiangXiaoMod 源碼，玩家對象直接擁有 Relics 屬性 
        var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.SkillRank ?? 1; // 沒找到遺物則預設為 1 級
    }

    // --- 新增：讀取「基礎技藝」遺物實例 ---
    public static BasicArts? GetBasicArtsRelic(Player? player)
    {
        return player?.Relics.FirstOrDefault(r => r is BasicArts) as BasicArts;
    }

    // --- 新增：讀取特定技藝等級 (Rank) ---
    // 範例：獲取徒手格鬥等級
    public static int GetUnarmedRank(Player? player)
    {
        var relic = GetBasicArtsRelic(player);
        return relic != null ? relic.GetRank(relic.UnarmedPts) : 1;
    }
    public static int GetBladeRank(Player? player)
    {
        var relic = GetBasicArtsRelic(player);
        return relic != null ? relic.GetRank(relic.BladePts) : 1;
    }
    public static int GetBowRank(Player? player)
    {
        var relic = GetBasicArtsRelic(player);
        return relic != null ? relic.GetRank(relic.BowPts) : 1;
    }
    public static int GetDaggerRank(Player? player)
    {
        var relic = GetBasicArtsRelic(player);
        return relic != null ? relic.GetRank(relic.DaggerPts) : 1;
    }
    public static int GetHalberdRank(Player? player)
    {
        var relic = GetBasicArtsRelic(player);
        return relic != null ? relic.GetRank(relic.HalberdPts) : 1;
    }
    public static int GetCombatKnifeRank(Player? player)
    {
        var relic = GetBasicArtsRelic(player);
        return relic != null ? relic.GetRank(relic.CombatKnifePts) : 1;
    }


    // --- 新增：萬用數值讀取器 (如果你想根據類型讀取 Pts) ---
    public static int GetArtPoints(Player? player, string artType)
    {
        var relic = GetBasicArtsRelic(player);
        if (relic == null) return 0;

        return artType.ToUpper() switch
        {
            "UNARMED" => relic.UnarmedPts,
            "BLADE"   => relic.BladePts,
            "BOW"     => relic.BowPts,
            "DAGGER"  => relic.DaggerPts,
            "HALBERD" => relic.HalberdPts,
            "KNIFE"   => relic.CombatKnifePts,
            _         => 0
        };
    }
}