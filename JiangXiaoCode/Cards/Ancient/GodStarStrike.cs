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

/// <summary>
/// 神性系列卡牌：神星一擊 (GodStarStrike)
/// 修改重點：現在會打出所有帶有「Star」關鍵字的卡牌
/// </summary>
[Pool(typeof(JiangXiaoCardPool))]
public class GodStarStrike : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-GOD_STAR_STRIKE";
    private const string MVarKey = "M";

    public GodStarStrike() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
        JJCustomVar(MVarKey, 1m);
        JJPowerTip<StrengthPower>();
        
        // 設定本卡的關鍵字與提示
        // JJKeywordAndTip(JiangXiaoModKeywords.Star);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null) return;

        // 維持原有的數值成長邏輯 (根據技藝總等級)
        int totalRank = Enum.GetValues<BasicArtType>().Sum(t => JiangXiaoUtils.GetArtRank(player, t));
        decimal calculatedAmount = Math.Max(1, (totalRank - 5) / 2m);

        if (DynamicVars.ContainsKey(MVarKey))
        {
            DynamicVars[MVarKey].BaseValue = calculatedAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        int strengthAmount = (int)DynamicVars[MVarKey].BaseValue;

        // 【修改點】搜尋所有「Star」類卡牌，並排除這張牌本身防止無限迴圈
        var cardsToPlay = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(c => c != this && IsStarCard(c)) 
            .ToList();

        foreach (var card in cardsToPlay)
        {
            // 執行自動打出
            await CardCmd.AutoPlay(choiceContext, card, ResolveTargetFor(card));

            // 每打出一張，增加力量
            if (strengthAmount > 0)
            {
                await PowerCmd.Apply<StrengthPower>(Owner.Creature, strengthAmount, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
    }

    /// <summary>
    /// 【修改點】判定是否為帶有 Star 標籤的卡牌
    /// </summary>
    private bool IsStarCard(CardModel card)
    {
        // 檢查卡牌的關鍵字清單中是否包含 Star
        return card.Keywords.Contains(JiangXiaoModKeywords.Star);
    }

    private Creature? ResolveTargetFor(CardModel card)
    {
        if (card.TargetType != TargetType.AnyEnemy || CombatState == null)
            return null;

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return null;

        return Owner?.RunState?.Rng?.CombatTargets?.NextItem(enemies);
    }
}