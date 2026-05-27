using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace JiangXiaoMod.Code.Cards.Token;

// --- 第一星圖 Token ---
public sealed class BeiDouNineStarsToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BeiDouNineStarsPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<BeiDouNineStarsPower>(player, 1, player, this);
    }
}

// --- 第二星圖 Token ---
public sealed class FlowerBladeToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<FlowerBladePower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<FlowerBladePower>(player, 1, player, this);
    }
}

// --- 第3星圖 Token ---
public sealed class WitherBowToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WitherBowPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<WitherBowPower>(player, 1, player, this);
    }
}

// --- 第4星圖 Token ---
public sealed class SoulOfDevouringSeaToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<SoulOfDevouringSeaPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<SoulOfDevouringSeaPower>(player, 1, player, this);
    }
}

// --- 第5星圖 Token ---
public sealed class StarMartialRecordsToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<StarMartialRecordsPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<StarMartialRecordsPower>(player, 1, player, this);
    }
}

// --- 第6星圖 Token ---
public sealed class InkFlowerToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<InkFlowerPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<InkFlowerPower>(player, 1, player, this);
    }
}

// --- 第7星圖 Token ---
public sealed class HolyCrossToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HolyCrossPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<HolyCrossPower>(player, 1, player, this);
    }
}

// --- 第8星圖 Token ---
public sealed class PeakOfLifeToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<PeakOfLifePower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<PeakOfLifePower>(player, 1, player, this);
    }
}

// --- 第9星圖 Token ---
public sealed class EarthStarMapToken : BaseStarMapToken
{
    public override string PortraitPath => "Temporarily.png".CardImagePath();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<EarthStarMapPower>()
    ];

    protected override async Task ApplyNewStarMap(PlayerChoiceContext context, Creature player)
    {
        await PowerCmd.Apply<EarthStarMapPower>(player, 1, player, this);
    }
}