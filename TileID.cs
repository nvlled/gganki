
namespace gganki_love;

public class TileID
{
	public const int player = 3869;
	static Random rand = new Random();

	public static int[] monsters = new int[]{
		3976, 4047, 3982, 3981, 3913, 3921, 3825, 4018, 4016, 3949, 4094, 4222, 4282, 4278,
		4272, 4329, 4325, 4189, 4182, 4178, 4176, 4179, 4110, 4113, 4105, 4170, 4359, 4358,
		4357, 4227, 4228, 4225, 3969, 3904, 3784, 3788, 3925, 3927, 3937, 3938, 3877, 3887,
		4075, 4072, 4006, 4070, 4198, 4201, 3829, 3775, 3903, 3966, 3965, 3963, 4026, 3957,
		3956, 4095, 4341, 4337, 4401, 4400
	};

	public static int RandomMonsterID()
	{
		return monsters[rand.Next(monsters.Length)];
	}

	public static HashSet<int> mirroredIDs = new HashSet<int>
	{
	};
}