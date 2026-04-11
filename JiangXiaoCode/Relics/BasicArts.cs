using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs; // 存檔標籤所在的命名空間

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class BasicArts : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override string IconBaseName => "basic_arts";

    // --- 存檔字段定義 ---
    
    private int _unarmedPts = 0;
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int UnarmedPts { get => _unarmedPts; set { _unarmedPts = value; RefreshDisplay(); } } // 徒手格鬥

    private int _bladePts = 0;
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int BladePts { get => _bladePts; set { _bladePts = value; RefreshDisplay(); } } // 夏家刀法

    private int _bowPts = 0;
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int BowPts { get => _bowPts; set { _bowPts = value; RefreshDisplay(); } } // 弓箭精通

    private int _daggerPts = 0;
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int DaggerPts { get => _daggerPts; set { _daggerPts = value; RefreshDisplay(); } } // 匕首精通

    private int _halberdPts = 0;
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int HalberdPts { get => _halberdPts; set { _halberdPts = value; RefreshDisplay(); } } // 天方戟精通

    private int _combatKnifePts = 0;
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int CombatKnifePts { get => _combatKnifePts; set { _combatKnifePts = value; RefreshDisplay(); } } // 格鬥刀精通

    // --- 反射工具與 Hook 配置 ---

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);
    public override bool ShouldReceiveCombatHooks => true;

    public BasicArts() : base() { }

    /// <summary>
    /// 每 10 點提升 1 Rank，起始 1 Rank，封頂 7 Rank (60點為 7 Rank)
    /// </summary>
    public int GetRank(int points) => Math.Min(7, (points / 10) + 1);

    /// <summary>
    /// 強制清空 STS2 的動態變量快取，觸發 UI 更新
    /// </summary>
    public void RefreshDisplay()
    {
        DynamicVarsField?.SetValue(this, null);
        // 透過觸發 Status 的賦值來通知引擎視覺刷新
        this.Status = this.Status; 
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("UnarmedR", (decimal)GetRank(UnarmedPts));
            yield return new DynamicVar("UnarmedP", (decimal)UnarmedPts);
            yield return new DynamicVar("BladeR", (decimal)GetRank(BladePts));
            yield return new DynamicVar("BladeP", (decimal)BladePts);
            yield return new DynamicVar("BowR", (decimal)GetRank(BowPts));
            yield return new DynamicVar("BowP", (decimal)BowPts);
            yield return new DynamicVar("DaggerR", (decimal)GetRank(DaggerPts));
            yield return new DynamicVar("DaggerP", (decimal)DaggerPts);
            yield return new DynamicVar("HalberdR", (decimal)GetRank(HalberdPts));
            yield return new DynamicVar("HalberdP", (decimal)HalberdPts);
            yield return new DynamicVar("CombatKnifeR", (decimal)GetRank(CombatKnifePts));
            yield return new DynamicVar("CombatKnifeP", (decimal)CombatKnifePts);
        }
    }

    /// <summary>
    /// 卡片打出後的點數成長邏輯
    /// </summary>
    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        bool changed = false;

        // 使用你的自定義擴展方法檢測關鍵字
        if (card.IsJiangXiaoModUNARMED()) { UnarmedPts++; changed = true; }
        if (card.IsJiangXiaoModBLADE()) { BladePts++; changed = true; }
        if (card.IsJiangXiaoModBOW()) { BowPts++; changed = true; }
        if (card.IsJiangXiaoModDAGGER()) { DaggerPts++; changed = true; }
        if (card.IsJiangXiaoModHALBERD()) { HalberdPts++; changed = true; }
        if (card.IsJiangXiaoModCOMBATKNIFE()) { CombatKnifePts++; changed = true; }

        if (changed)
        {
            this.Flash();
            // setter 已包含 RefreshDisplay()，此處不需額外調用
        }

        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }
}