using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Dawn : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-DAWN";
    private const string VarM = "M";

    public Dawn() : base(0, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
    {
        // [STS2_Correction] 使用 ValueProp.None 或 Move 均可，BlockVar 本身是強類型
        JJVar(new BlockVar(8m, ValueProp.Move));
        JJCustomVar(VarM, 1m);

        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<DawnPower>();
        JJStaticTip(StaticHoverTip.Block);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        decimal calculatedM = skillRank switch
        {
            <= 2 => 1m,
            <= 4 => 2m,
            _ => 3m
        };

        if (IsUpgraded)
        {
            calculatedM += 1m;
        }

        // [Fix_CS1061] 直接透過索引器更新數值，這是 STS2 最穩定的做法
        // 只要構造函數有定義過 VarM，此處就不會報錯
        DynamicVars[VarM].BaseValue = calculatedM;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null) return;

        UpdateStatsBasedOnRank();

        var targets = combat.Allies;
        decimal mValue = DynamicVars[VarM].BaseValue;

        // [Fix_CS1503] 傳遞整個 BlockVar 對象 (DynamicVars.Block)
        // 這樣 STS2 的渲染引擎才能追蹤到這格擋是由這張卡牌產生的
        foreach (var target in targets)
        {
            await CreatureCmd.GainBlock(target, DynamicVars.Block, cardPlay);
        }

        // 施加能力
        await PowerCmd.Apply<DawnPower>(targets, mValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}