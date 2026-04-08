using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players; // 修正引用
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands.Builders;

namespace JiangXiaoMod.Code.Powers;

public class YearningHaloPower : CustomPowerModel
{
    public const string PowerId = "YearningHaloPower";
    private const string VarM = "M";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public YearningHaloPower() : base()
    {
    }

    // 依據您提供的邏輯獲取當前百分比數值
    public int GetLifestealValue()
    {
        int rankLevel = 1;

        // 【STS2 獨特處理】：優先從 Owner 拿，拿不到則從全域獲取
        // 注意：Creature 沒 Player 屬性，這裡我們透過類型轉換或全域實例獲取
        var player = Owner?.Player ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();

        if (player?.Relics != null)
        {
            var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            if (relic != null)
            {
                rankLevel = (int)relic.GetRank(); 
            }
        }

        // 依據您的要求：基礎 10%，每一級 + 5%
        // 等級 1 -> 10 + (1-1)*5 = 10
        // 等級 2 -> 10 + (2-1)*5 = 15
        return 10 + (rankLevel - 1) * 5;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將計算好的 M 值拋給 STS2 的 SmartFormat 系統渲染
            yield return new DynamicVar(VarM, (decimal)GetLifestealValue());
        }
    }

    public override async Task AfterAttack(AttackCommand command)
    {
        // 判定攻擊者是擁有者
        if (command.Attacker == Owner && command.Results != null)
        {
            // 獲取實際造成的穿透傷害
            int totalDamage = command.Results.Sum(r => r.UnblockedDamage);
            
            if (totalDamage > 0)
            {
                // 獲取當前精確的吸血比例（例如 10 或 15）
                int currentPercentValue = GetLifestealValue();
                
                // 轉化為小數（10 -> 0.10, 15 -> 0.15）
                decimal healPercent = currentPercentValue / 100m;
                
                // 使用 Ceiling 確保 6 點傷害回 1 點
                int healAmount = (int)Math.Ceiling(totalDamage * healPercent);

                if (healAmount > 0)
                {
                    await CreatureCmd.Heal(Owner, (uint)healAmount);
                }
            }
        }
        await base.AfterAttack(command);
    }
}