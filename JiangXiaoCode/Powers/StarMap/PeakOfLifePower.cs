using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace JiangXiaoMod.Code.Powers.StarMaps;

// --- 第八星圖：生命巔峰 ---
public sealed class PeakOfLifePower : StarMapPowerModel
{
    public override string CustomPackedIconPath => "Test.png".PowerImagePath();
    public const string VarX = "X";
    public const string VarM = "M";

    private bool _hasTriggered = false;
    private decimal _bonusHpAdded = 0m;

    public PeakOfLifePower() : base() 
    { 
        _hasTriggered = false;
        _bonusHpAdded = 0m;
    }

    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield return new DynamicVar(VarX, 270m);
        yield return new DynamicVar(VarM, 27m);
    }

    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        // 預留空間
    }

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        _hasTriggered = false; 
        _bonusHpAdded = 0m;
    }

    /// <summary>
    /// [STS2_API] 每回合開始時觸發
    /// 負責將再生重置回 270 層
    /// </summary>
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartLate(choiceContext, player);
        
        if (player?.Creature != null && player.Creature == base.Owner)
        {
            int targetRegen = (int)DynamicVars[VarX].BaseValue;
            int currentRegen = player.Creature.GetPowerAmount<RegenPower>();
            
            if (currentRegen < targetRegen)
            {
                int amountToAdd = targetRegen - currentRegen;
                await PowerCmd.Apply<RegenPower>(player.Creature, amountToAdd, base.Owner, null);
            }
        }
    }

    /// <summary>
    /// [STS2_API] 能力被賦予時觸發
    /// </summary>
    public override async Task AfterApplied(Creature? targetCreature, CardModel? cardSource)
    {
        // 只要還沒觸發過就給予大屬性包
        if (!_hasTriggered && targetCreature != null)
        {
            _hasTriggered = true; 
            this.Flash();         

            _bonusHpAdded = DynamicVars[VarX].BaseValue;
            int regenAmount = (int)DynamicVars[VarX].BaseValue;
            int statAmount = (int)DynamicVars[VarM].BaseValue;

            // 1. 獲得臨時最大生命值
            decimal newMaxHp = targetCreature.MaxHp + _bonusHpAdded;
            await CreatureCmd.SetMaxHp(targetCreature, newMaxHp);
            await CreatureCmd.Heal(targetCreature, _bonusHpAdded); 

            // 2. 獲得 再生、力量、靈敏
            await PowerCmd.Apply<RegenPower>(targetCreature, regenAmount, base.Owner, null);
            await PowerCmd.Apply<StrengthPower>(targetCreature, statAmount, base.Owner, null);
            await PowerCmd.Apply<DexterityPower>(targetCreature, statAmount, base.Owner, null);
        }
        
        await base.AfterApplied(targetCreature, cardSource);
    }

    /// <summary>
    /// 獨立出恢復所有屬性的邏輯 (血量、力量、靈敏、再生)
    /// </summary>
    private async Task RestorePeakState(Creature creature)
    {
        if (_hasTriggered && creature != null && _bonusHpAdded > 0m)
        {
            // 1. 還原最大血量
            decimal restoredMaxHp = creature.MaxHp - _bonusHpAdded;
            if (restoredMaxHp < 1m) restoredMaxHp = 1m;
            await CreatureCmd.SetMaxHp(creature, restoredMaxHp);
            
            // 2. 扣除賦予的屬性 (透過給予負數層數來扣除)
            int regenAmount = (int)DynamicVars[VarX].BaseValue;
            int statAmount = (int)DynamicVars[VarM].BaseValue;
            
            await PowerCmd.Apply<RegenPower>(creature, -regenAmount, base.Owner, null);
            await PowerCmd.Apply<StrengthPower>(creature, -statAmount, base.Owner, null);
            await PowerCmd.Apply<DexterityPower>(creature, -statAmount, base.Owner, null);
            
            // 清空紀錄
            _hasTriggered = false;
            _bonusHpAdded = 0m;
        }
    }

    /// <summary>
    /// [STS2_API] 處理戰鬥中被主動移除的情況 (例如舊星圖被覆蓋、被驅散)
    /// </summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        await RestorePeakState(oldOwner);
        await base.AfterRemoved(oldOwner);
    }

    /// <summary>
    /// [STS2_API] 戰鬥結束時的清理邏輯
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RestorePeakState(base.Owner);
        await base.AfterCombatEnd(room);
    }
}