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
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarSkillQuality : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public enum QualityRank { Bronze = 1, Silver = 2, Gold = 3, Platinum = 4, Diamond = 5, CandleMoon = 6, ScorchingSun = 7 }

    private const string VarRank = "rank"; 
    
    // 定義觸發特殊效果的卡牌 ID
    private const string SpaceGaptId = "JIANGXIAOMOD-SPACE_GAP"; 

    // 用於強制刷新 UI 文字的反射字段
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public int SkillRank => (int)GetRank();

    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();

    /// <summary>
    /// 獲取技能點數
    /// </summary>
    public int GetPoints()
    {
        // 💡【圖鑑防崩潰核心修復】
        // 在 STS2 中，如果遺物處於圖鑑原型 (Canonical) 狀態，直接調用 Owner 會觸發 AssertMutable 崩潰。
        // 我們透過 base.IsCanonical 或者是直接判斷 Owner 欄位是否為空來進行防呆。
        
        // 如果您的基底類別有支援 IsCanonical 屬性，可以直接用：
        // if (base.IsCanonical || Owner == null) return 0;

        // 如果不確定是否有 IsCanonical，用最安全的 try-catch 配合 null 檢查攔截：
        try
        {
            if (Owner == null)
            {
                return 0; // 圖鑑或商店預覽時，沒有持有者，直接回傳 0 點（顯示青銅）
            }

            var mainRelic = JiangXiaoUtils.GetStarMap(Owner);
            return mainRelic?.JiangXiaoMod_SkillPoints ?? 0;
        }
        catch (MegaCrit.Sts2.Core.Models.Exceptions.CanonicalModelException)
        {
            // 如果不幸在 Owner 讀取時就踩到原生的 Assert 崩潰，直接在這裡捕獲並安全回傳 0
            return 0;
        }
    }

    public QualityRank GetRank()
    {
        int points = GetPoints();
        
        if (points < 5000) return QualityRank.Bronze;
        if (points < 10000) return QualityRank.Silver;
        if (points < 20000) return QualityRank.Gold;
        if (points < 30000) return QualityRank.Platinum;
        if (points < 40000) return QualityRank.Diamond;
        if (points < 50000) return QualityRank.CandleMoon;
        return QualityRank.ScorchingSun;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar(VarRank, (decimal)SkillRank);
        }
    }

    // ==========================================
    // 地圖自由移動（飛行）
    // ==========================================
    public override bool ShouldAllowFreeTravel()
    {
        if (Owner?.Deck?.Cards == null) return false;
        
        // 只要有卡片，就允許原生飛行（作為基礎權限放行）
        return Owner.Deck.Cards.Any(c => c.Id.Entry == SpaceGaptId);
    }

    /// <summary>
    /// 進入新房間時刷新 UI。
    /// 這能確保玩家在戰鬥中獲得/移除該卡牌，或是星技點數變動時，遺物文字能保持最新。
    /// </summary>
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        RefreshDisplay();
        return Task.CompletedTask;
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

    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>
    /// 強制清除動態變量緩存，促使遊戲重新讀取 CanonicalVars 並更新 UI 文本。
    /// </summary>
    public void RefreshDisplay()
    {
        try 
        {
            DynamicVarsField?.SetValue(this, null);
        }
        catch (Exception)
        {
            // 靜默處理反射異常
        }
    }
}