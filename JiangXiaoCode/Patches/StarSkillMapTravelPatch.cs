using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using BaseLib.Utils; 
using JiangXiaoMod.Code.Extensions; 

namespace JiangXiaoMod.Code.Patches;

[HarmonyPatch(typeof(NMapScreen), "RecalculateTravelability")]
public static class StarSkillMapTravelPatch
{
    private const string SpaceGaptId = "JIANGXIAOMOD-SPACE_GAP";

    public static void Postfix(NMapScreen __instance)
    {
        try
        {
            // 透過反射獲取 NMapScreen 的私有資料
            FieldInfo? runStateField = typeof(NMapScreen).GetField("_runState", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo? mapPointDictField = typeof(NMapScreen).GetField("_mapPointDictionary", BindingFlags.NonPublic | BindingFlags.Instance);

            if (runStateField == null || mapPointDictField == null) return;

            RunState? runState = runStateField.GetValue(__instance) as RunState;
            Dictionary<MapCoord, NMapPoint>? mapPointDictionary = mapPointDictField.GetValue(__instance) as Dictionary<MapCoord, NMapPoint>;

            if (runState == null || mapPointDictionary == null) return;

            // 基本防呆
            if (runState.Players == null || !runState.VisitedMapCoords.Any()) return;

            // 💡【團隊共享核心修正】
            // 每台電腦都會掃描「整個隊伍」的所有玩家。
            // 只要隊伍中有人帶有「時空之隙」，全隊就能一起飛！飛行距離取全隊中的【最高等級】。
            bool teamHasSpaceGap = false;
            int maxFlightDistance = 1;

            foreach (Player? p in runState.Players)
            {
                if (p == null || p.Deck?.Cards == null) continue;

                // 檢查該名隊友是否有「時空之隙」
                bool pHasGap = p.Deck.Cards.Any(c => c.Id.Entry == SpaceGaptId);
                if (pHasGap)
                {
                    teamHasSpaceGap = true;
                    // 獲取該隊友的星技品質等級
                    int pRank = (int)JiangXiaoUtils.GetSkillRank(p);
                    if (pRank > maxFlightDistance)
                    {
                        maxFlightDistance = pRank; // 紀錄全隊最高的飛行距離
                    }
                }
            }

            // 如果整隊（包含自己與隊友）都沒有人滿足條件，直接退出維持原狀
            if (!teamHasSpaceGap) return;

            // 安全保底：確保距離至少為 1
            if (maxFlightDistance < 1) maxFlightDistance = 1;

            // 3. 獲取隊伍當前所在的層數 (row)
            MapCoord currentCoord = runState.VisitedMapCoords.Last();
            int currentRow = currentCoord.row;

            // 4. 遍歷全圖節點，為所有隊友的螢幕「同步解鎖」相同的格子
            foreach (var pair in mapPointDictionary)
            {
                MapCoord coord = pair.Key;
                NMapPoint nMapPoint = pair.Value;

                // 已經走過的格子保持原樣，不重複覆蓋
                if (nMapPoint.State == MapPointState.Traveled) continue;

                // 計算目標層數與當前層數的絕對值垂直距離
                int layerDifference = Math.Abs(coord.row - currentRow);

                // 只要在允許的最高層數範圍內，強制點亮為可前往狀態 (Travelable)
                if (layerDifference <= maxFlightDistance)
                {
                    nMapPoint.State = MapPointState.Travelable;
                }
            }
        }
        catch (Exception ex)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[JiangXiaoMod] Travel Team Sync Patch Error: {ex.Message}");
        }
    }
}