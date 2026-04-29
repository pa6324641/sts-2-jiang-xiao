using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using SmartFormat.Extensions.PersistentVariables;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class UnarmedStrike : JiangXiaoCardModel
{
    // 定義常量，方便維護
    private const decimal BaseDmgValue = 3m;
    private const decimal DmgPerRank = 1m;
    private const decimal UpgradeDmgBonus = 3m;
    // private const string Var = "M";

    public UnarmedStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModUNARMED);
        JJDamage(BaseDmgValue, ValueProp.Move);
        JJCustomVar("M", 1m);
    }

    /// <summary>
    /// 核心邏輯：根據等級動態調整數值。
    /// 由基類 JiangXiaoCardModel 自動在狀態變更時調用。
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取「徒手格鬥」專用等級
        int unarmedRank = JiangXiaoUtils.GetUnarmedRank(player);
        
        // 傷害 = 基礎(3) + 等級加成(rank * 1) + (若升級則 +3)
        decimal finalBaseDmg = BaseDmgValue + (unarmedRank * DmgPerRank);
        if (IsUpgraded)
        {
            finalBaseDmg += UpgradeDmgBonus;
        }
        
        DynamicVars.Damage.BaseValue = finalBaseDmg;
        
        // 抽牌邏輯：1 + (Rank / 3)
        DynamicVars["M"].BaseValue = 1m + ((unarmedRank-1) / 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 注意：基類已處理 UpdateStatsBasedOnRank，此處不需手動調用
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 1. 執行攻擊動作
        // 使用 PreviewValue 以確保包含力量 (Strength) 等戰鬥內加成
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 執行抽牌動作
        int drawAmount = (int)DynamicVars["M"].BaseValue;
        if (drawAmount > 0)
        {
            await CardPileCmd.Draw(choiceContext, drawAmount, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // STS2 建議：在 OnUpgrade 中處理能量降低
        EnergyCost.UpgradeBy(-1);
        
        // 數值的實際更新會交給 ApplyRankLogic 處理
        // 我們手動觸發一次更新以確保 UI 立即反應
        UpdateStatsBasedOnRank();
    }
}