using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Creatures;
using JiangXiaoMod.Code.Extensions;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using JiangXiaoMod.Code.Cards.CardModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class QingMang : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-QING_MANG";
    private const string MVarKey = "M";

    public QingMang() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // 初始化基礎數值
        JJDamage(6m, ValueProp.Move);
        JJCustomVar(MVarKey, 3m);
        
        // 添加關鍵字與提示
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<StrengthPower>();
        JJPowerTip<ArtifactPower>();
        
        // 添加標籤
        JJTag(CardTag.Strike);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 定義成長係數
        decimal stepDmg = 3m;
        decimal stepM = 3m;

        decimal baseDmg = IsUpgraded ? 9m : 6m;
        decimal baseM = IsUpgraded ? 4m : 3m;

        // 2. 獲取近戰類技藝加成
        decimal artsBonusDmg = GetMeleeArtsBonus(player);

        // 3. 更新基礎值：基礎 + (等級成長) + 技藝加成
        DynamicVars.Damage.BaseValue = baseDmg + (skillRank - 1) * stepDmg + artsBonusDmg;
        DynamicVars[MVarKey].BaseValue = baseM + (skillRank - 1) * stepM;
    }

    protected override void OnUpgrade() => UpdateStatsBasedOnRank();

    private decimal GetMeleeArtsBonus(Player? player)
    {
        if (player == null) return 0m;

        int totalMeleeRank = (JiangXiaoUtils.GetUnarmedRank(player) - 1) +
                             (JiangXiaoUtils.GetBladeRank(player) - 1) +
                             (JiangXiaoUtils.GetDaggerRank(player) - 1) +
                             (JiangXiaoUtils.GetHalberdRank(player) - 1) +
                             (JiangXiaoUtils.GetCombatKnifeRank(player) - 1);

        return (decimal)totalMeleeRank;
    }

    /// <summary>
    /// [STS2 特有勾子] 處理傷害加成邏輯
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        decimal finalAmount = base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);

        // [STS2_API] 判定目標是否有人工製品，若是則傷害乘 5
        // 確保 cardSource == this，避免其他來源的計算意外觸發此邏輯
        if (target != null && cardSource == this && target.Powers.Any(p => p is ArtifactPower))
        {
            finalAmount *= 5m;
        }

        return finalAmount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not Creature target) return;

        // 【核心修正】
        // 在 STS2 中，DamageCmd.Attack() 應傳入「尚未經過此卡片修改」的基礎傷害。
        // 當使用 .FromCard(this) 時，管線會自動調用卡片內部的 ModifyDamageMultiplicative。
        // 原本傳入 PreviewValue (140) 會導致管線內再次 *5 (140 * 5 = 700)。
        // 改為傳入 BaseValue (28) 後，管線執行時 28 * 5 = 140，與預覽一致。
        decimal damageBase = DynamicVars.Damage.BaseValue;

        int strengthLoss = (int)DynamicVars[MVarKey].BaseValue;

        // 1. 執行攻擊
        await DamageCmd.Attack(damageBase)
            .FromCard(this) // 此處會自動應用包括力量、易傷以及卡片自定義的 5 倍邏輯
            .Targeting(target) 
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 施加負面狀態
        // 減少力量
        await PowerCmd.Apply<StrengthPower>(target, -strengthLoss, Owner.Creature, this);
        // 施加擊退
        await PowerCmd.Apply<BeatBackPower>(target, strengthLoss, Owner.Creature, this);
    }
}