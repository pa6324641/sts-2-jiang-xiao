using BaseLib.Abstracts;
using Godot;

namespace JiangXiaoMod.Code.Character;
// 藥水池定義
public class JiangXiaoPotionPool : CustomPotionPoolModel
{
	public override string EnergyColorName => JiangXiao.CharacterId;
	public override Color LabOutlineColor => JiangXiao.Color;
}
