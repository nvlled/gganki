
namespace gganki_love;

using System;
using gganki_love;
using Love;
using Co = AwaitableCoroutine.Coroutine;

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
	public static (string, string) DivideBy(this string text, string sub)
	{
		if (text == sub)
		{
			return ("", "");
		}
		var index = text.IndexOf(sub);
		if (index < 0)
		{
			return ("", "");
		}

		var pre = text[0..index];
		var post = text[(index + sub.Length)..];

		return (pre, post);
	}
}

public static class Xt
{
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
	}

	public static class Vector2
	{
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

			KeyHandler.OnKeyPress += handler;
			await Co.While(() => !done);
			KeyHandler.OnKeyPress -= handler;
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

			});

			await Co.While(() => !done);
		}
	}


}