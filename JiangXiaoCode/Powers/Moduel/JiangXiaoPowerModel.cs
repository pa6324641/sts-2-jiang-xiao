using BaseLib.Abstracts;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Powers;

/// <summary>
/// 江曉模組的能力基類，自動處理圖示路徑與基礎初始化
/// </summary>
public abstract class JiangXiaoPowerModel : CustomPowerModel
{
    // [STS2_API] 自動根據 PowerId 獲取圖示路徑
    // 檔案應置於：res://JiangXiao/images/powers/[id].png
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => CustomPackedIconPath;

    protected JiangXiaoPowerModel() : base()
    {
        // 可以在此處加入通用的初始化邏輯
    }
}