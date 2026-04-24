using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Cards;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class WitheringArrow : JiangXiaoCardModel
{
    // 常量定義，方便後續維護
    private const decimal BaseDmgValue = 3m;
    private const decimal BasePoison = 3m;
    private const decimal BaseDoom = 3m;

    public WitheringArrow() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        // 使用基類工具方法初始化變量
        JJDamage(BaseDmgValue);
        JJCustomVar("M", BasePoison); // 中毒
        JJCustomVar("D", BaseDoom);   // 災厄
        
        // 註冊關鍵字與提示
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBOW);
    }

    /// <summary>
    /// 根據弓箭等級縮放：傷害 +3/Rank, 中毒 +5/Rank, 災厄 +5/Rank
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取弓箭特定等級
        int bowRank = JiangXiaoUtils.GetBowRank(player);
        
        // 獲取升級狀態帶來的基礎值偏移 (如果有在 OnUpgrade 調整)
        decimal upM = DynamicVars["M"].WasJustUpgraded ? 2m : 0m;
        decimal upD = DynamicVars["D"].WasJustUpgraded ? 2m : 0m;

        // [邏輯] 計算最終數值
        // 注意：Damage.BaseValue 會影響 UI 顏色顯示，建議直接操作 BaseValue
        DynamicVars.Damage.BaseValue = BaseDmgValue + (bowRank * 3m);
        DynamicVars["M"].BaseValue = BasePoison + upM + (bowRank * 6m);
        DynamicVars["D"].BaseValue = BaseDoom + upD + (bowRank * 6m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // JiangXiaoCardModel 已在必要勾子中處理 UpdateStatsBasedOnRank，
        // 但在 OnPlay 開頭再次確認可確保數值絕對即時。
        UpdateStatsBasedOnRank();

        if (cardPlay.Target == null) return;
        var player = Owner;
        if (player?.Creature == null) return;

        // 1. 執行攻擊 (使用更新後的傷害值)
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") 
            .Execute(choiceContext);

        // 2. 獲取當前動態數值
        int poisonAmt = (int)DynamicVars["M"].BaseValue;
        int doomAmt = (int)DynamicVars["D"].BaseValue;

        // 3. 施加狀態
        if (poisonAmt > 0)
        {
            await PowerCmd.Apply<PoisonPower>(cardPlay.Target, poisonAmt, player.Creature, this);
        }

        if (doomAmt > 0)
        {
            // 提醒：需確保 DoomPower 已在項目中定義
            await PowerCmd.Apply<DoomPower>(cardPlay.Target, doomAmt, player.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 標記變量為升級狀態，具體數值增加在 ApplyRankLogic 中統一計算        
        // 刷新當前卡牌實例的數值顯示
        UpdateStatsBasedOnRank();
    }
}