
namespace gganki_love;

public class TileID
{
    public const int player = 3869;
    static Random rand = new Random();

    public static int[] players = new int[]{
        3869,
        3890,
        3919,
        4757,
        4233,
        3777,
        3850,
        3795,
        3801,
        3804,
        3864,
        4915,
        4909,
        4893,
    };

    public static int[] monsters = new int[]{
        3976, 4047, 3982, 3981, 3913, 3921, 3825, 4018, 4016, 3949, 4094, 4222, 4282, 4278,
        4272, 4329, 4325, 4189, 4182, 4178, 4176, 4179, 4110, 4113, 4105, 4170, 4359, 4358,
        4357, 4227, 4228, 4225, 3969, 3904, 3784, 3788, 3925, 3927, 3937, 3938, 3877, 3887,
        4075, 4072, 4006, 4070, 4198, 4201, 3829, 3775, 3903, 3966, 3965, 3963, 4026, 3957,
        3956, 4095, 4341, 4337, 4401, 4400,
        4955, 5025, 5024, 4975, 5039, 5043, 5050, 4985, 4921, 4858, 4794, 4728, 4732, 4733, 4796, 4792, 4990, 4784, 4719, 4721, 4723, 4724, 4968, 4903, 4904, 4898, 4896, 5012, 4877, 5001, 4870, 4996, 5057, 4364, 4247, 4309, 4178, 4177, 4315, 4327, 4197, 4193, 4196, 4209,

        4032, 4033, 3975, 3912, 3791, 4047, 4052, 4053, 4055, 4056, 4177, 4176, 4120, 4119, 4184, 4245, 4125, 4126, 4060, 4006, 3944, 3945, 3945, 3946, 4073, 4076, 3948, 3821, 3820, 3819, 4145, 4207, 4143, 4142, 4021, 3960, 3963, 3835, 3837, 4157, 4216, 4218, 4277, 4149, 4148, 4030, 4159, 4222, 3839, 4351, 4281, 4733, 5025
    };
    public static int[] foods = new int[]{
        2561, 2562, 2564, 2566, 2569, 2572, 2570, 2573, 2576, 2579, 2583, 2588, 2587, 2591, 2597, 2598,
    };

    public static int[] weaponsTR = new int[]{
        3008, 2944, 2945, 2946, 3138, 3016, 3080, 2880,

        3017,2822,3085,3086,3087,3149,3091,3102,3103,3109,3126,3005,3006,3037,3108
    };

    public static int RandomMonsterID()
    {
        return monsters[rand.Next(monsters.Length)];
    }

    public static HashSet<int> mirroredIDs = new HashSet<int>
    {
    };
}