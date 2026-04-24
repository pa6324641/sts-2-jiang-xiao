using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.ValueProps;
using HarmonyLib;

namespace JiangXiaoMod.Code.Cards.Ancient;

[Pool(typeof(JiangXiaoCardPool))]
public class GodSkillStrike : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-GOD_SKILL_STRIKE";
    private const string MVarKey = "M"; // 與 Localization 中的 {M} 對齊

    public GodSkillStrike() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
        // 【核心修正 1】在構造函數中初始化動態變數 M
        // 這會將變數註冊到基類的 _customVars 中，確保 UI 能找到它
        JJCustomVar(MVarKey, 1m);

        // 添加力量提示，方便玩家查看
        JJPowerTip<StrengthPower>();
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBASICARTS);
    }

    /// <summary>
    /// 當 Rank 變動時，STS2 會調用此處更新數值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null) return;

        // 1. 獲取所有基礎技藝的總 Rank (6種武器)
        // 參考 JiangXiaoUtils 與 QingMang 的做法
        int totalRank = Enum.GetValues<BasicArtType>().Sum(t => JiangXiaoUtils.GetArtRank(player, t));
        
        // 2. 計算力量值：(總Rank - 5) 太強了
        decimal calculatedAmount = Math.Max(1, (totalRank - 5)/2);
        // 【核心修正 2】直接更新基類維護的 DynamicVars
        // 這樣 STS2 的渲染引擎才能正確抓到數值並顯示在卡面上
        if (DynamicVars.ContainsKey(MVarKey))
        {
            DynamicVars[MVarKey].BaseValue = calculatedAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 從動態變數中抓取當前數值，確保打出效果與卡面描述完全一致
        int strengthAmount = (int)DynamicVars[MVarKey].BaseValue;

        // 獲取所有符合關鍵字的「技藝」卡牌
        var cardsToPlay = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(IsJiangXiaoArtCard)
            .ToList();

        foreach (var card in cardsToPlay)
        {
            // 自動打出
            await CardCmd.AutoPlay(choiceContext, card, ResolveTargetFor(card));

            // 增加力量
            if (strengthAmount > 0)
            {
                await PowerCmd.Apply<StrengthPower>(Owner.Creature, strengthAmount, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        // 如果升級有額外數值變動，可以在此調用 UpdateStatsBasedOnRank();
        AddKeyword(CardKeyword.Innate);
    }

    private bool IsJiangXiaoArtCard(CardModel card)
    {
        return card.IsJiangXiaoModUNARMED() || 
               card.IsJiangXiaoModBLADE() || 
               card.IsJiangXiaoModBOW() || 
               card.IsJiangXiaoModDAGGER() || 
               card.IsJiangXiaoModHALBERD() || 
               card.IsJiangXiaoModCOMBATKNIFE();
    }

    private Creature? ResolveTargetFor(CardModel card)
    {
        if (card.TargetType != TargetType.AnyEnemy || CombatState == null)
            return null;

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return null;

        return Owner?.RunState?.Rng?.CombatTargets?.NextItem(enemies);
    }
    
    // 【核心修正 3】刪除原有的 CanonicalVars 重寫！
    // 讓它使用 JiangXiaoCardModel 基類的預設實作（回傳 _customVars），
    // 這樣你在 constructor 呼叫的 JJCustomVar 才會生效。
}