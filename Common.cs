using Love;

namespace gganki_love;

public class Config
{
	public const int fontSize = 52;
	public const string savedStateFilename = ".gganki-session";
}


public class SharedState
{
	public static readonly SharedState instance = new SharedState();

	public View activeView = new NopView();
	public DeckNames deckNames = new DeckNames();
	public Dictionary<string, CardInfo[]?> deckCards = new Dictionary<string, CardInfo[]?>();

	//public CardInfo[] cards = new CardInfo[0];

	public Font fontAsian;
	public Font fontRegular = Graphics.NewFont(Config.fontSize);

	public Random rand = new Random();

	public AtlasImage? atlasImage;

	public Entity? player;

	public string? lastDeckName;

	public Vector2 center = Vector2.Zero;

	public void SetActiveView(View view)
	{
		if (activeView == view)
		{
			return;
		}

		if (activeView != null)
		{
			activeView.Unload();
		}
		activeView = view;
		activeView.Load();
	}
}

public class SavedState
{
	public string? lastDeckName { get; set; }
}

public class GamepadHandler
{

	public static event Action<Joystick, GamepadAxis, float>? OnAxis;
	public static event Action<Joystick, GamepadButton>? OnPress;
	public static event Action<Joystick, GamepadButton>? OnRelease;

	public static void DispatchAxis(Joystick joystick, GamepadAxis axis, float value)
	{
		if (OnAxis != null) OnAxis(joystick, axis, value);
	}

	public static void DispatchPress(Joystick joystick, GamepadButton button)
	{
		if (OnPress != null) OnPress(joystick, button);
	}

	public static void DispatchRelease(Joystick joystick, GamepadButton button)
	{
		if (OnRelease != null) OnRelease(joystick, button);
	}
}

public class KeyHandler
{
	public delegate void KeyReleased(KeyConstant key, Scancode scancode);
	public delegate void KeyPressed(KeyConstant key, Scancode scancode, bool isRepeat);
	public static event KeyPressed? OnKeyPress;
	public static event KeyReleased? OnKeyRelease;

	public static void DispatchKeyPress(KeyConstant key, Scancode scancode, bool isRepeat)
	{
		if (OnKeyPress != null)
		{
			OnKeyPress(key, scancode, isRepeat);
		}
	}

	public static void DispatchKeyRelease(KeyConstant key, Scancode scancode)
	{
		if (OnKeyRelease != null)
		{
			OnKeyRelease(key, scancode);
		}
	}
}


public class Directions
{
	public static Vector2 Up = new Vector2(0, -1);
	public static Vector2 Down = new Vector2(0, 1);
	public static Vector2 Left = new Vector2(-1, 0);
	public static Vector2 Right = new Vector2(1, 0);
}

public class AtlasImage
{
	public Image image;
	public int tileSize;
	int rows;
	int cols;
	SpriteBatch spriteBatch;

	public AtlasImage(Image image, int tileSize = 32)
	{
		this.image = image;
		this.tileSize = tileSize;
		this.rows = image.GetHeight() / tileSize;
		this.cols = image.GetWidth() / tileSize;
		spriteBatch = Graphics.NewSpriteBatch(image, 2000, SpriteBatchUsage.Dynamic);
	}

	public Quad GetQuad(int tileID)
	{
		return Graphics.NewQuad(
			x: (tileID % cols) * tileSize,
			y: (tileID / cols) * tileSize,
			w: tileSize,
			h: tileSize,
			sw: image.GetWidth(),
			sh: image.GetHeight()
		);
	}
	public void StartDraw()
	{
		spriteBatch.Clear();
	}

	public void BatchDraw(Quad quad, float x, float y, float angle = 0, float sx = 1, float sy = 1, float ox = 0, float oy = 0, float kx = 0, float ky = 0)
	{
		spriteBatch.Add(quad, x, y, angle, sx, sy, ox, oy, kx, ky);

	}
	public void Draw(Quad quad, float x, float y, float angle = 0, float sx = 1, float sy = 1, float ox = 0, float oy = 0, float kx = 0, float ky = 0)
	{

		Graphics.Draw(quad, image, x, y, angle, sx, sy, ox, oy, kx, ky);
	}

	public void EndDraw()
	{
		Graphics.SetColor(Color.White);
		Graphics.Draw(spriteBatch);
	}
}

public class Polar
{
	public float radius;
	public float angle;

	public Polar(float angle, float radius)
	{
		this.angle = angle;
		this.radius = radius;
	}

	public static Polar FromVector(Vector2 v)
	{
		return new Polar(MathF.Atan2(v.Y, v.X), v.Length());

	}
	public static float GetAngle(Vector2 v)
	{
		return MathF.Atan2(v.Y, v.X);
	}
	public static Vector2 Rotate(float angle, Vector2 v)
	{
		//var u = Vector2.Rotate(v, angle < 0 ? -90 : 90);
		//v += Vector2.Normalize(u) * 50;
		//Console.WriteLine(u);
		//return v;

		var p = FromVector(v);
		v.X = MathF.Cos(p.angle + angle) * p.radius;
		v.Y = MathF.Sin(p.angle + angle) * p.radius;
		return v;
	}

	public static Vector2 ToVector(Polar p)
	{
		var x = MathF.Cos(p.angle) * p.radius;
		var y = MathF.Sin(p.angle) * p.radius;
		return new Vector2(x, y) * p.radius;
	}
}

public class Gamepad
{
	static List<Joystick> joysticks = new List<Joystick>();

	public static void Add(Joystick js)
	{
		var index = joysticks.FindIndex(j => j.GetID() == js.GetID());
		if (index >= 0)
		{
			joysticks[index] = js;
		}
		else
		{
			joysticks.Add(js);
		}
	}

	public static void Remove(Joystick js)
	{
		joysticks.RemoveAll(j => j.GetID() == js.GetID());
	}

	public static bool IsDown(GamepadButton button)
	{
		foreach (var js in joysticks)
		{
			if (js.IsGamepadDown(button))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsPressed(GamepadButton button)
	{
		foreach (var js in joysticks)
		{
			if (js.IsGamepadPressed(button))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsReleased(GamepadButton button)
	{
		foreach (var js in joysticks)
		{
			if (js.IsGamepadReleased(button))
			{
				return true;
			}
		}
		return false;
	}
}