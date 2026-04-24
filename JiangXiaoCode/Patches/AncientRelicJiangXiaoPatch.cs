using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using JiangXiaoMod.Code.Cards.Ancient;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace JiangXiaoMod.Code.Patches;

[HarmonyPatch]
public static class AncientRelicJiangXiaoPatch
{
    private const string JiangXiaoStrikeId = "JIANGXIAOMOD-STRIKE_JIANG_XIAO";

    // --- ArchaicTooth (古代牙齒) ---

    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
    [HarmonyPrefix]
    public static bool ArchaicToothAfterObtainedPrefix(ArchaicTooth __instance, ref Task __result)
    {
        var owner = __instance.Owner as Player;
        if (owner == null) return true;

        // 查找牌組中是否有江曉的初始打擊
        var starterInDeck = owner.Deck.Cards.FirstOrDefault(c => c.Id.Entry == JiangXiaoStrikeId);
        
        if (starterInDeck == null) return true;

        MainFile.Logger.Info($"[AncientRelicJiangXiaoPatch] ArchaicTooth 觸發：檢測到 {JiangXiaoStrikeId}，執行江曉專屬轉換。");
        
        // 攔截原版邏輯，執行自定義異步轉換
        __result = HandleArchaicToothTransform(owner, starterInDeck);
        return false;
    }

    // --- DustyTome (塵封典籍) ---

    [HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
    [HarmonyPrefix]
    public static bool DustyTomeSetupForPlayerPrefix(DustyTome __instance, Player player)
    {
        // 判定是否為江曉玩家
        if (!IsJiangXiaoPlayer(player)) return true;

        MainFile.Logger.Info("[AncientRelicJiangXiaoPatch] DustyTome 初始化：為江曉篩選專屬古代卡池。");

        // 獲取當前角色的完整卡池卡牌
        var poolCards = GetCardPoolCards(player).ToList();
        
        // 篩選符合條件的古代卡
        var candidates = poolCards.Where(IsDustyTomeCandidate).ToList();

        if (candidates.Count == 0)
        {
            MainFile.Logger.Warn("[AncientRelicJiangXiaoPatch] DustyTome：江曉卡池中未找到符合條件的古代卡候選者。");
            return true;
        }

        // 使用玩家的獎勵隨機序列挑選卡牌
        var selected = player.PlayerRng.Rewards.NextItem(candidates);
        __instance.AncientCard = selected.Id;

        MainFile.Logger.Info($"[AncientRelicJiangXiaoPatch] DustyTome：已選定古代卡 {selected.Id.Entry}。");
        return false;
    }

    // --- 輔助邏輯 ---

    /// <summary>
    /// 執行古代牙齒的卡牌轉換
    /// </summary>
    private static async Task HandleArchaicToothTransform(Player owner, CardModel starterInDeck)
    {
        // 創建目標古代卡：神:技藝打擊
        var transformedCard = owner.RunState.CreateCard<GodSkillStrike>(owner);
        
        // 如果原卡已升級，目標卡也自動升級
        if (starterInDeck.IsUpgraded)
        {
            CardCmd.Upgrade(transformedCard);
        }

        // 執行 STS2 標準卡牌轉換指令
        await CardCmd.Transform(starterInDeck, transformedCard);
        MainFile.Logger.Info("[AncientRelicJiangXiaoPatch] ArchaicTooth 轉換完成：打擊 -> 星辰大同。");
    }

    /// <summary>
    /// 判定是否為江曉玩家（基於角色 ID 或初始卡牌）
    /// </summary>
    private static bool IsJiangXiaoPlayer(Player? player)
    {
        if (player == null) return false;

        // 判定角色 ID
        if (player.Character?.Id.Entry == JiangXiao.CharacterId) return true;

        // 備援判定：牌組內持有江曉的初始打擊
        return player.Deck.Cards.Any(c => c.Id.Entry == JiangXiaoStrikeId);
    }

    /// <summary>
    /// 獲取角色卡池中的所有卡牌實例
    /// </summary>
    private static IEnumerable<CardModel> GetCardPoolCards(Player player)
    {
        // 嘗試透過反射獲取 AllCards 屬性（這是 BaseLib 擴展卡池的常見結構）
        var pool = player.Character.CardPool;
        var allCardsProp = AccessTools.Property(pool.GetType(), "AllCards");
        
        if (allCardsProp?.GetValue(pool) is IEnumerable<CardModel> allCards)
        {
            return allCards;
        }

        // 若反射失敗，則回退至 STS2 標準 API：獲取當前已解鎖的卡牌
        return pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }

    /// <summary>
    /// 篩選塵封典籍可產生的卡牌
    /// </summary>
    private static bool IsDustyTomeCandidate(CardModel card)
    {
        // 1. 必須是古代稀有度
        if (card.Rarity != CardRarity.Ancient) return false;

        // 2. 排除原版古代牙齒已經包含的轉換目標
        if (ArchaicTooth.TranscendenceCards.Any(c => c.Id.Entry == card.Id.Entry)) return false;

        // 3. 排除江曉古代牙齒的轉換目標（星辰大同），避免兩件古代遺物拿到重複的卡
        if (card is GodSkillStrike || card.Id.Entry == GodSkillStrike.CardId)
        {
            return false;
        }

        return true;
    }
}