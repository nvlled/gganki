
namespace gganki_love;

using Love;
using Co = AwaitableCoroutine.Coroutine;
public static class IEnumerableXt
{
    public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> items)
    {
        int i = 0;
        foreach (var x in items)
        {
            yield return (x, i++);
        }
    }
}

public static class CoroutineXt
{
    public static void TryCancel(this Co coroutine)
    {
        if (!coroutine.IsCompleted)
        {
            coroutine.Cancel();
        }
    }
}
public static class CollectionXt
{
    public static T? GetRandom<T>(this IList<T> list)
    {
        if (list.Count() == 0) return default(T);
        for (var n = 0; n < list.Count() / 2; n++)
        {
            var i = Random.Shared.Next(0, list.Count());
            var item = list[i];
            if (item != null)
            {
                return item;
            }
        }
        return default(T);
    }
}

public static class ListXt
{
    public static T? GetRandom<T>(this List<T> list, Predicate<T> predicate)
    {
        if (list.Count() == 0) return default(T);
        for (var n = 0; n < list.Count() / 2; n++)
        {
            var i = Random.Shared.Next(0, list.Count());
            var item = list[i];
            if (item != null && predicate(item))
            {
                return item;
            }
        }
        return default(T);
    }
    /*
	public static T? GetRandom<T>(this List<T> list)
	{
		if (list.Count() == 0) return default(T);
		var i = Random.Shared.Next(0, list.Count());
		return list[i];
	}
	*/
}

public static class StringXt
{
    public static (int, int) FindSubstringIndex(this string text, string sub)
    {
        var end = sub.Length;
        while (end > 0)
        {
            if (text == sub)
            {
                return (-1, -1);
            }

            var mid = sub[0..end];
            var index = text.IndexOf(mid);
            if (index < 0)
            {
                end--;
                continue;
            }
            var duplicate = text.IndexOf(mid, index + 1) >= index;
            if (duplicate)
            {
                end--;
                continue;
            }


            return (index, index + mid.Length - 1);
        }
        return (-1, -1);
    }

    public static (string, string, string) DivideBy(this string text, string sub)
    {
        var end = sub.Length;
        while (end > 0)
        {
            if (text == sub)
            {
                return ("", text, "");
            }

            var mid = sub[0..end];
            var index = text.IndexOf(mid);
            if (index < 0)
            {
                end--;
                continue;
            }
            var duplicate = text.IndexOf(mid, index + 1) >= index;
            if (duplicate)
            {
                end--;
                continue;
            }

            var pre = text[0..index];
            var post = text[(index + mid.Length)..];

            return (pre, mid, post);
        }
        return ("", text, "");
    }
}

public static class Xt
{
    public static class MathF
    {
        public static int RandomSign()
        {
            return Random.Shared.Next(0, 2) == 1 ? 1 : -1;
        }

        public static float Clamp(float x, int v1, int v2)
        {
            if (x < v1)
                return v1;
            if (x > v2)
                return v2;
            return x;
        }
    }
    public static class String
    {
        public static (string, string, string) Split(string text, string sub)
        {
            if (text == sub)
            {
                return ("", sub, "");
            }
            var index = text.IndexOf(sub);
            if (index < 0)
            {
                return ("", text, "");
            }

            var pre = text[0..index];
            var post = text[(index + sub.Length)..];

            return (pre, sub, post);
        }
    }

    public static class Graphics
    {
        public static Love.Vector2 NullVector = new Love.Vector2(float.PositiveInfinity, float.NegativeInfinity);
        public static Love.Vector2 PrintPos { get; set; } = new Love.Vector2(0, 0);
        public static float PrintMarginY { get; set; } = 10;
        public static float PrintWidth { get; set; } = Love.Graphics.GetWidth();

        public static void Println(string text, Font font)
        {
            var pos = PrintPos;
            var width = font.GetWidth(text);
            Love.Graphics.SetFont(font);
            Love.Graphics.Printf(text, pos.X - width / 2, pos.Y, width, AlignMode.Center);

            PrintPos = PrintPos + new Love.Vector2(0, font.GetHeight() + PrintMarginY);
        }
        public static void PrintlnRect(DrawMode mode, string text, Font font, int margin = 10)
        {
            var width = font.GetWidth(text) + margin;
            var height = font.GetHeight() + margin;
            var topLeft = PrintPos + new Love.Vector2(-width / 2, -height);
            Love.Graphics.Rectangle(mode, topLeft.X, topLeft.Y, width, height);
        }
        public static void PrintVertical(string text, Love.Vector2 pos)
        {
            PrintVertical(text, pos.X, pos.Y);
        }
        public static void PrintVertical(string text, float x, float y)
        {
            var font = Love.Graphics.GetFont();
            var h = font.GetHeight();
            foreach (var ch in text)
            {
                var str = ch.ToString();
                var w = font.GetWidth(str);
                Love.Graphics.Print(str, x - w / 2, y);
                y += h;
            }
        }
    }

    public static class Vector4
    {

        public static Love.Vector4 FromColor(Color color)
        {
            return new Love.Vector4(color.Rf, color.Gf, color.Bf, color.Af);
        }
    }
    public static class Vector2
    {
        public static Love.Vector2 RandomDir()
        {
            var r = System.Random.Shared;
            return Love.Vector2.Normalize(new Love.Vector2(
                -1 + r.NextSingle() * 2,
                -1 + r.NextSingle() * 2
            ));
        }

        public static Love.Vector2 Random(float? w = null, float? h = null)
        {
            var xMax = w.GetValueOrDefault(Love.Graphics.GetWidth());
            var yMax = h.GetValueOrDefault(Love.Graphics.GetHeight());

            var r = System.Random.Shared;
            return new Love.Vector2(r.Next(0, (int)xMax), r.Next(0, (int)yMax));
        }
    }

    public static class Coroutine
    {
        public static async Co Sleep(float seconds)
        {
            var time = Love.Timer.GetTime();
            var startTime = time;
            while (seconds > 0)
            {
                var fps = Love.Timer.GetFPS() - 1;
                await Co.DelayCount((int)(fps * seconds));

                var now = Love.Timer.GetTime();
                seconds -= now - time;
                time = now;
            }

            //await Co.AwaitTask(Task.Delay((int)(seconds * 1000)));
            //Console.WriteLine(Love.Timer.GetTime() - startTime);
        }

        public static async Co Abyss()
        {
            while (true) { await Co.Yield(); }
        }

        public static async Co AwaitKey(KeyConstant waitKey)
        {
            var done = false;
            KeyHandler.KeyPressed handler = (key, code, repeat) =>
            {
                done = done || key == waitKey;
            };

            using var _ = KeyHandler.WithKeyPress(handler);
            await Co.While(() => !done);
        }

        public static async Co AwaitAnyKey()
        {
            var done = false;
            KeyHandler.KeyPressed keyHandler = (key, code, repeat) =>
            {
                done = true;
            };
            Action<Joystick, GamepadButton> gpadHandler = (Joystick js, GamepadButton button) =>
            {
                done = true;
            };

            KeyHandler.OnKeyPress += keyHandler;
            GamepadHandler.OnPress += gpadHandler;
            using var _ = Defer.Run(() =>
            {
                KeyHandler.OnKeyPress -= keyHandler;
                GamepadHandler.OnPress -= gpadHandler;
            }, nameof(AwaitKey));

            await Co.While(() => !done);
        }
    }

    public static class Mouse
    {
        static Dictionary<Love.MouseButton, (float, bool)> pressed = new Dictionary<Love.MouseButton, (float, bool)>();
        static Mouse()
        {
            MouseHandler.OnMousePress += OnPress;
            MouseHandler.OnMouseRelease += OnRelease;
        }

        private static void OnRelease(MouseHandler.ButtonEvent ev)
        {
            (float, bool) val;
            if (pressed.TryGetValue(ev.button, out val))
            {
                val.Item2 = false;
                pressed[ev.button] = val;
            }
        }

        private static void OnPress(MouseHandler.ButtonEvent ev)
        {
            pressed[ev.button] = (Love.Timer.GetTime(), true);
        }

        public static float ChargeTime(Love.MouseButton button)
        {
            (float, bool) val;
            if (pressed.TryGetValue(button, out val))
            {
                if (val.Item1 > 0 && val.Item2)
                {
                    var elapsed = Love.Timer.GetTime() - val.Item1;
                    return elapsed;
                }
            }
            return 0;
        }
        public static float ReleaseCharge(Love.MouseButton button)
        {

            (float, bool) val;
            if (pressed.TryGetValue(button, out val))
            {
                if (val.Item1 > 0 && !val.Item2)
                {
                    var elapsed = Love.Timer.GetTime() - val.Item1;
                    val.Item1 = -1;
                    pressed[button] = val;

                    return elapsed;
                }
            }
            return 0;
        }
    }

    public static class Gamepad
    {
        static Dictionary<Love.GamepadButton, (float, bool)> pressed = new Dictionary<Love.GamepadButton, (float, bool)>();
        static Gamepad()
        {
            GamepadHandler.OnPress += OnPress;
            GamepadHandler.OnRelease += OnRelease;
        }

        private static void OnRelease(Joystick js, GamepadButton button)
        {
            (float, bool) val;
            if (pressed.TryGetValue(button, out val))
            {
                val.Item2 = false;
                pressed[button] = val;
            }
        }

        private static void OnPress(Joystick js, GamepadButton button)
        {
            pressed[button] = (Love.Timer.GetTime(), true);
        }

        public static float ChargeTime(Love.GamepadButton button)
        {
            (float, bool) val;
            if (pressed.TryGetValue(button, out val))
            {
                if (val.Item1 > 0 && val.Item2)
                {
                    var elapsed = Love.Timer.GetTime() - val.Item1;
                    return elapsed;
                }
            }
            return 0;
        }
        public static float ReleaseCharge(Love.GamepadButton button)
        {

            (float, bool) val;
            if (pressed.TryGetValue(button, out val))
            {
                if (val.Item1 > 0 && !val.Item2)
                {
                    var elapsed = Love.Timer.GetTime() - val.Item1;
                    val.Item1 = -1;
                    pressed[button] = val;

                    return elapsed;
                }
            }
            return 0;
        }
    }

}