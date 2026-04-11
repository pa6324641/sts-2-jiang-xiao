using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using BaseLib.Patches.Hooks;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class QingMang : CustomCardModel
{
    // 仿照風車擊，宣告一個常數字串作為 Key
    private const string _mKey = "M";

    public QingMang() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) 
    {
    }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 【完全借鑒風車擊的乾淨寫法】直接在這裡初始化
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new(_mKey, 3m) 
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<ArtifactPower>()
    ];

    private int GetQualityRank()
    {
        if (IsCanonical || Owner == null) return 1;
        try 
        {
            var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            return relic != null ? (int)relic.GetRank() : 1;
        }
        catch { return 1; }
    }

    // 隨時計算並覆寫基礎值
    public void UpdateStatsBasedOnRank()
    {
        int rank = GetQualityRank();
        decimal baseDmg = IsUpgraded ? 9m : 6m;
        decimal baseM = IsUpgraded ? 4m : 3m;

        // 仿照風車擊，直接透過 DynamicVars 修改 BaseValue
        DynamicVars.Damage.BaseValue = baseDmg + (rank - 1) * 6m;
        DynamicVars[_mKey].BaseValue = baseM + (rank - 1) * 3m;
    }

    // 【利用引擎特性，偷渡刷新 UI 面板】
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 6 -> 9
        DynamicVars[_mKey].UpgradeValueBy(1m); // 3 -> 4
        UpdateStatsBasedOnRank();
    }

    private bool HasMeleePassive(Player player)
    {
        if (player?.Creature?.Powers == null) return false;
        string[] meleePowerIds = { "XiaFamilyBlade", "DaggerMastery", "UnarmedCombat", "CombatKnifeMastery", "HalberdMastery" };
        return player.Creature.Powers.Any(p => meleePowerIds.Contains(p.Id.Entry));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 這裡使用模式匹配：如果 Target 為空則退出；如果不為空，則賦值給本地變數 target
        // 這樣下方的代碼使用 target 時，編譯器會確定它絕對不是 null
        if (cardPlay.Target is not Creature target) return;

        // 打出的瞬間再次確保數值最新
        UpdateStatsBasedOnRank();

        // 直接取用 DynamicVars 的值
        decimal currentBaseDamage = DynamicVars.Damage.BaseValue;
        int strengthLoss = (int)DynamicVars[_mKey].BaseValue;

        if (HasMeleePassive(Owner))
        {
            currentBaseDamage *= 2m;
        }

        // 2. 這裡改用 target 判斷，且因為上方已經檢查過 target 必定不為 null，所以這裡不用再寫 target != null
        if (target.Powers.Any(p => p is ArtifactPower))
        {
            currentBaseDamage *= 5m;
        }

        // 3. 傳入 target，警告 CS8604 會消失
        await DamageCmd.Attack(currentBaseDamage)
            .FromCard(this)
            .Targeting(target) 
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<StrengthPower>(target, -strengthLoss, Owner.Creature, this);
        await PowerCmd.Apply<BeatBackPower>(target, strengthLoss, Owner.Creature, this);
    }
}