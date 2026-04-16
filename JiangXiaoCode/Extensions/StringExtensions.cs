namespace JiangXiaoMod.Code.Extensions;

public static class StringExtensions
{
	// 指向江曉遺物資料夾
	public static string RelicImagePath(this string name) => $"res://JiangXiao/images/relics/{name}";
	public static string BigRelicImagePath(this string name)
    {
        return $"res://JiangXiao/images/relics/big/{name}";
    }

	// 指向江曉卡牌資料夾
	public static string CardImagePath(this string name) => $"res://JiangXiao/images/card_portraits/{name}";

	// 指向江曉能力圖示資料夾 (通常是 32x32 或 64x64 的 png)
	public static string PowerImagePath(this string name) => $"res://JiangXiao/images/powers/{name}";
	public static string CharacterUiPath(this string path)
	{
		return Path.Join(MainFile.ResPath, "images", "JiangXiao", path);
	}
}
