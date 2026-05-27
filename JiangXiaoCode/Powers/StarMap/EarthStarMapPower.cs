using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace JiangXiaoMod.Code.Powers.StarMaps;

// --- 第九星圖：大地星圖 ---
public sealed class EarthStarMapPower : StarMapPowerModel
{
    public override string CustomPackedIconPath => "Test.png".PowerImagePath();
    private bool _hasTriggered = false;
    private decimal _originalHp = 0m;

    // 整合自 InterceptPower 範本：用於記錄受到保護/分擔傷害的隊友
    private class Data
    {
        public readonly List<Creature> coveredCreatures = new List<Creature>();
    }

    public EarthStarMapPower() : base() 
    { 
        _hasTriggered = false;
        _originalHp = 0m;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }

    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        // 預留空間
    }

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        _hasTriggered = false; 
        _originalHp = 0m;
    }

    /// <summary>
    /// 註冊需要分擔傷害的隊友
    /// </summary>
    public void AddCoveredCreature(Creature c)
    {
        List<Creature> coveredCreatures = GetInternalData<Data>().coveredCreatures;
        if (!coveredCreatures.Contains(c))
        {
            coveredCreatures.Add(c);
        }
    }

    /// <summary>
    /// [STS2_API] 能力被賦予時觸發
    /// </summary>
    public override async Task AfterApplied(Creature? targetCreature, CardModel? cardSource)
    {
        if (!_hasTriggered && targetCreature != null)
        {
            _hasTriggered = true; 
            this.Flash();         

            // 1. 備份原本真實血量
            _originalHp = targetCreature.MaxHp;

            // 2. 將血量設定為近十億並顯示無限標籤
            await CreatureCmd.SetMaxAndCurrentHp(targetCreature, 999999999m);
            targetCreature.ShowsInfiniteHp = true;

            // [STS2_API] 透過 RunManager 獲取當前戰鬥房間，並遍歷 Allies 以加入隊友
            if (targetCreature?.CombatState != null)
            {
                foreach (var teammate in targetCreature.CombatState.Allies)
                {
                    // 確保保護名單不包含本體
                    if (teammate != targetCreature)
                    {
                        AddCoveredCreature(teammate);
                    }
                }
            }
        }
        
        await base.AfterApplied(targetCreature, cardSource);
    }

    /// <summary>
    /// [STS2_API] 每回合開始時觸發，確保回復至滿血
    /// </summary>
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartLate(choiceContext, player);
        
        // [修正] 必須確認當前開始回合的玩家，正是持有此能力的主人 (Owner)
        if (player?.Creature != null && player.Creature == base.Owner && _hasTriggered)
        {
            decimal missingHp = player.Creature.MaxHp - player.Creature.CurrentHp;
            if (missingHp > 0)
            {
                await CreatureCmd.SetCurrentHp(player.Creature, 999999999m);
            }
        }
    }

    /// <summary>
    /// [STS2_API] 攔截隊友受到的傷害，實現 50% 減免分擔
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var coveredCreatures = GetInternalData<Data>().coveredCreatures;

        // 如果受擊者是隊友 (在保護名單中，且不是本體)
        if (target != null && target != base.Owner && coveredCreatures.Contains(target))
        {
            if (props.IsPoweredAttack())
            {
                // 讓隊友只承受 50% 的傷害。
                // 由於本體已有無限血量，無需再將 50% 傷害轉移到本體扣除。
                return 0.5m; 
            }
        }

        return 1m;
    }

    /// <summary>
    /// 獨立出恢復原始狀態的邏輯
    /// </summary>
    private async Task RestoreOriginalState(Creature creature)
    {
        if (_hasTriggered && creature != null && _originalHp > 0m)
        {
            // 1. 關閉無限血量 UI 標籤
            creature.ShowsInfiniteHp = false;

            // 2. 還原最大血量與當前血量 (避免切換星圖後直接死亡)
            await CreatureCmd.SetMaxAndCurrentHp(creature, _originalHp);
            
            // 3. 清空隊友分擔名單
            GetInternalData<Data>().coveredCreatures.Clear();

            // 4. 重置內部紀錄
            _hasTriggered = false;
            _originalHp = 0m;
        }
    }

    /// <summary>
    /// [STS2_API] 處理戰鬥中被主動移除的情況 (例如舊星圖被覆蓋)
    /// </summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        await RestoreOriginalState(oldOwner);
        oldOwner.ShowsInfiniteHp = false;
        await base.AfterRemoved(oldOwner);
    }

    /// <summary>
    /// [STS2_API] 戰鬥結束時的清理邏輯
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RestoreOriginalState(base.Owner);
        await base.AfterCombatEnd(room);
    }
}