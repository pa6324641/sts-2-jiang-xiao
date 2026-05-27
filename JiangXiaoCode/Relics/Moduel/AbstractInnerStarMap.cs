using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Powers.StarMaps;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Rewards;

namespace JiangXiaoMod.Code.Relics;

/// <summary>
/// 內視星圖的抽象基底類別：完美的將所有共用邏輯（含卡牌獎勵替代選項、點數設定等）封裝於此
/// </summary>
public abstract class AbstractInnerStarMap : CustomRelicModel, IInnerStarMap
{
    // 共用常數定義
    private const string VarPoints = "points";
    private const string VarGain = "gain"; 

    private int _skillPoints = 0;

    // 核心點數屬性（基礎版與升級版共用相同的邊界限制與 UI 刷新通知邏輯）
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int JiangXiaoMod_SkillPoints 
    { 
        get => _skillPoints; 
        set 
        {
            int clampedValue = Math.Min(value, 99999);
            if (_skillPoints == clampedValue) return;
            _skillPoints = clampedValue;

            if (base.IsMutable && Owner != null)
            {
                var player = Owner;
                player.Relics.OfType<StarSkillQuality>().FirstOrDefault()?.RefreshDisplay();
                player.Relics.OfType<StarPowerLevel>().FirstOrDefault()?.RefreshDisplay();
                player.Relics.OfType<BasicArts>().FirstOrDefault()?.RefreshDisplay();
            }
            RefreshDynamicText();
        }
    }

    // 提取基準點數收益為虛擬屬性，供本體遺物敘述（Tooltip）的 DynamicVar 使用
    protected virtual int SkillPointsGain => 1000;

    // 新增：由子類別各自實現唯一的替代選項 Key（例如基礎版回傳 "CONVERT_SKILL_POINTS_BASE"）
    // 藉此繞過 STS2 介面無法動態讀取變量的限制
    protected abstract string CardRewardAlternativeKey { get; }

    // 由子類別各自實現的抽象屬性（區分基礎版與升級版的點數收益差別）
    protected abstract int CombatVictoryBaseGain { get; }
    protected abstract int CombatVictoryEliteGain { get; }
    protected abstract int CombatVictoryBossGain { get; }
    protected abstract int RoomEnteredGain { get; }

    // 圖標路徑會根據子類別實際的類別名稱（Id）動態映射，因此可完全共用
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    public override bool ShouldReceiveCombatHooks => true;

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public void RefreshDynamicText()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    // 全域靜態輔助函式：供其他模組快速查詢當前星圖點數
    public static int GetSkillPoints(IRunState? runState)
    {
        var player = runState?.Players.FirstOrDefault();
        var starMapRelic = player?.Relics.FirstOrDefault(r => r is IInnerStarMap) as IInnerStarMap;
        return starMapRelic?.JiangXiaoMod_SkillPoints ?? 0;
    }

    /// <summary>
    /// 共用鉤子：根據子類別提供的專屬 Key 完美整合「轉換技能點」的替代按鈕
    /// </summary>
    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        if (base.Owner != player) return false;
        
        // 使用各子類別自定義的靜態 Key，防止 UI 渲染時發生變量遺失錯誤
        alternatives.Add(new CardRewardAlternative(CardRewardAlternativeKey, OnConvertSkillPointsSynchronized, PostAlternateCardRewardAction.DismissScreenAndRemoveReward));
        return true;
    }

    private async Task OnConvertSkillPointsSynchronized()
    {
        await OnConvertSkillPoints();
    }

    private async Task OnConvertSkillPoints()
    {
        Flash(); 
        this.JiangXiaoMod_SkillPoints += SkillPointsGain; 
        await Task.CompletedTask;
    }

    // 共用戰鬥勝利點數結算邏輯
    public override Task AfterCombatVictory(CombatRoom room)
    {
        int gain = CombatVictoryBaseGain;
        if (room != null)
        {
            if (room.RoomType == RoomType.Boss) gain = CombatVictoryBossGain;
            else if (room.RoomType == RoomType.Elite) gain = CombatVictoryEliteGain;
        }

        if (Owner?.Creature?.CombatState != null)
        {
            CombatManager.Instance.History.StarsModified(Owner.Creature.CombatState, gain, Owner);
        }

        JiangXiaoMod_SkillPoints += gain;
        Flash(); 
        return Task.CompletedTask;
    }

    // 共用進入非戰鬥房間點數結算邏輯
    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (room.RoomType == RoomType.Event || room.RoomType == RoomType.Shop || room.RoomType == RoomType.RestSite || room.RoomType == RoomType.Treasure)
        {
            JiangXiaoMod_SkillPoints += RoomEnteredGain;
            Flash(); 
        }
        return Task.CompletedTask;
    }

    // 遺物本體的敘述（Tooltip）依然可以完美支援動態變量解析
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar(VarPoints, (decimal)JiangXiaoMod_SkillPoints);
            yield return new DynamicVar(VarGain, (decimal)SkillPointsGain); 
        }
    }

    // 共用北斗九星能力偵測與施加
    public override async Task BeforeCombatStart()
    {
        if (!Owner.HasPower<BeiDouNineStarsPower>())
        {
            GD.Print("江曉成功啟動了北斗九星！");
            await PowerCmd.Apply<BeiDouNineStarsPower>(Owner.Creature, 1, null, null);
            Flash();
        }
        await base.BeforeCombatStart();
    }
}