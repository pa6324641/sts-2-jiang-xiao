using BaseLib.Abstracts;
using Godot;

namespace JiangXiaoMod.Code.Character;
// 遺物池定義
public class JiangXiaoRelicPool : CustomRelicPoolModel
{
	public override string EnergyColorName => JiangXiao.CharacterId;
	public override Color LabOutlineColor => JiangXiao.Color;
}
