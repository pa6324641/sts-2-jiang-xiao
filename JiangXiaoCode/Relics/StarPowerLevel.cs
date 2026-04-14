using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarPowerLevel : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    private const string VarLevel = "level";
    private const string VarEnergy = "energy";

    protected override string IconBaseName => "star_power_level";
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// [STS2_API] 正確的能量修正勾子
    /// </summary>
    /// <param name="player">當前玩家實例</param>
    /// <param name="amount">修正鏈中目前的能量上限值</param>
    /// <returns>修正後的能量上限</returns>
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        // 基礎邏輯：等級 1+3=4, 2+3=5... 
        // 這裡我們只回傳「加成值」，讓系統自動與基礎值累加
        int bonus = GetLevel();
        return amount + (decimal)bonus;
    }

    public int GetLevel()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        var player = runState?.Players.FirstOrDefault() ?? Owner;
        if (player == null) return 1;

        var mainRelic = player.Relics.OfType<InnerStarMap>().FirstOrDefault();
        int points = mainRelic?.JiangXiaoMod_SkillPoints ?? 0;

        if (points < 5000) return 1;
        if (points < 20000) return 2;
        if (points < 30000) return 3;
        if (points < 40000) return 4;
        if (points < 45000) return 5;
        return 6;
    }

    // public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    // {
    //     var owner = this.Owner;
    //     if (owner == null || side != owner.Creature.Side) return;

    //     // 每回合開始時，根據當前等級獲得能量 (補充到新的上限或額外獲得)
    //     this.Flash();
    //     int energyGain = GetLevel();
    //     await PlayerCmd.GainEnergy(energyGain, owner);
        
    //     await base.AfterSideTurnStart(side, combatState);
    // }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            int currentLevel = GetLevel();
            yield return new DynamicVar(VarLevel, (decimal)currentLevel);
            yield return new DynamicVar(VarEnergy, (decimal)currentLevel); 
        }
    }

    public override Task BeforeCombatStart()
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    public override bool ShouldReceiveCombatHooks => true;
}