using System.Text.RegularExpressions;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Love;

namespace gganki_love;

using IntPair = System.ValueTuple<int, int>;


public interface IPos
{
	public Vector2 pos { get; set; }
	//public Vector2 lastPos { get; set; }
}


public class Config
{
	public const int fontSize = 52;
	public const int fontSizeTiny = 14;
	public const int fontSizeSmall = 18;
	public const int fontSizeMedium = 32;
	public const string savedStateFilename = ".gganki-session";
}


public class SharedState
{
	public static readonly SharedState self = new SharedState();

	public int worldTileSize = 200;

	public View activeView = new NopView();
	public DeckNames deckNames = new DeckNames();
	public Dictionary<string, CardInfo[]?> deckCards = new Dictionary<string, CardInfo[]?>();

	//public CardInfo[] cards = new CardInfo[0];

	public Font fontAsian = Graphics.NewFont(Config.fontSize);
	public Font fontRegular = Graphics.NewFont(Config.fontSize);
	public Font fontTiny = Graphics.NewFont(Config.fontSizeTiny);
	public Font fontSmall = Graphics.NewFont(Config.fontSizeSmall);
	public Font fontMedium = Graphics.NewFont(Config.fontSizeMedium);

	public AtlasImage? atlasImage;

	public Entity? player;

	public string? lastDeckName;

	public Vector2 center = Vector2.Zero;
	public Vector2 centerTop = Vector2.Zero;
	public Vector2 centerBottom = Vector2.Zero;

	public bool uninitializedView = false;

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
		uninitializedView = true;
		//activeView.Load();
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


public class PartitionedList<T> where T : IPos
{
	public class Partition : HashSet<T> { }
	public class Mapping : Dictionary<IntPair, Partition> { }

	public int partitionSize { get; init; }

	Mapping items = new Mapping();

	Dictionary<T, Vector2> itemSet = new Dictionary<T, Vector2>();

	public readonly Partition emptyEntry = new Partition();

	public PartitionedList(int partitionSize)
	{
		this.partitionSize = partitionSize;
	}

	public void AddAll(IEnumerable<T> newItems)
	{
		foreach (var item in newItems)
		{
			Add(item);
		}
	}
	public void Add(T item)
	{
		if (itemSet.ContainsKey(item))
		{
			return;
		}

		//item.lastPos = item.pos;

		Partition? partition = null;
		var key = GetKey(item.pos);
		if (!items.TryGetValue(key, out partition))
		{
			partition = new Partition();
			items.Add(key, partition);
		}

		itemSet.Add(item, item.pos);
		partition.Add(item);
	}

	public void Move(T item)
	{
		//if (!itemSet.ContainsKey(item) || item.pos == item.lastPos)
		Vector2 lastPos;
		if (!itemSet.TryGetValue(item, out lastPos))
		{
			return;
		}

		if (item.pos == lastPos)
		{
			return;
		}

		Partition? partition = null;

		var oldKey = GetKey(lastPos);
		if (items.TryGetValue(oldKey, out partition))
		{
			partition.Remove(item);
		}

		var newKey = GetKey(item.pos);
		if (!items.TryGetValue(newKey, out partition))
		{
			partition = new Partition();
			items.Add(newKey, partition);
		}

		//item.lastPos = item.pos;
		itemSet[item] = item.pos;
		partition.Add(item);

	}

	public (int, int) GetKey(Vector2 pos)
	{
		return (
			(int)(pos.X / partitionSize),
			(int)(pos.Y / partitionSize)
		);
	}
	public static (int, int) GetKey(Vector2 pos, int partitionSize)
	{
		return (
			(int)(pos.X / partitionSize),
			(int)(pos.Y / partitionSize)
		);
	}

	public IEnumerable<T> Iterate()
	{
		return itemSet.Keys;
	}


	// this isn't safe for concurrent invocation
	// but it should be fine for mostly single-thread games like this
	// TODO: use a simple object pool, free object on return
	HashSet<IntPair> _getItemsSet = new HashSet<IntPair>();
	public IEnumerable<T> GetItemsAt(params Vector2[] positions)
	{
		foreach (var pos in positions)
		{
			_getItemsSet.Add(GetKey(pos));
		}
		foreach (var k in _getItemsSet)
		{
			_getItemsSet.Remove(k);
			foreach (var item in items.GetValueOrDefault(k, emptyEntry))
			{
				yield return item;
			}
		}
	}

	public void Remove(T item)
	{
		Vector2 lastPos;
		Partition? partition = null;
		if (itemSet.TryGetValue(item, out lastPos))
		{
			var oldKey = GetKey(lastPos);
			if (items.TryGetValue(oldKey, out partition))
			{
				partition.Remove(item);
			}
		}

		if (lastPos != item.pos)
		{
			if (items.TryGetValue(GetKey(item.pos), out partition))
			{
				partition.Remove(item);
			}
		}

		itemSet.Remove(item);
	}
}
public class AnkiAudioPlayer
{
	static Dictionary<string, Love.Source> data = new Dictionary<string, Source>();

	public static async void LoadCardAudios(IEnumerable<CardInfo> cards)
	{
		foreach (var c in cards)
		{
			var entry = new CardInfoFieldEntry();
			var vocab = c.fields?.GetValueOrDefault("VocabAudio", entry)?.value;
			var sent = c.fields?.GetValueOrDefault("SentAudio", entry)?.value;

			if (vocab != null)
			{
				Console.WriteLine("{0} loading audio {1}", c.cardId, vocab);
				await LoadFile(vocab);
			}
			if (sent != null)
			{
				Console.WriteLine("{0} loading audio {1}", c.cardId, sent);
				await LoadFile(sent);

			}
		}
	}

	public static string? GetSoundFilename(string? embeddedFilename)
	{
		if (embeddedFilename != null)
		{
			var matches = Regex.Matches(embeddedFilename, @"\[sound:(.*?)\]");
			if (matches.Count() > 0)
			{
				return matches?[0]?.Groups?[1].Value ?? "";
			}
		}

		return null;
	}

	public static async Task LoadFile(string? embeddedFilename)
	{
		if (embeddedFilename == null) { return; }

		var filename = GetSoundFilename(embeddedFilename) ?? embeddedFilename;

		var source = await AudioManager.LoadAudio(filename);
		if (source != null)
		{
			data[filename] = source;
		}
	}

	public static void Play(string? embeddedFilename)
	{
		if (embeddedFilename == null) { return; }

		var filename = GetSoundFilename(embeddedFilename) ?? embeddedFilename;

		Love.Source? source;
		if (data.TryGetValue(filename, out source))
		{
			source.Play();
		}
	}
}

public class AudioManager
{
	const string cacheDir = "cached_audio";
	public static System.IO.Stream DecodeBase64(string data)
	{
		var bytes = Convert.FromBase64String(data);
		return new MemoryStream(bytes);
	}

	// LoadAudio("35cb9c96cc03ad5ed6e8cf7b38a62b85.ogg")
	// - check cache file
	// - fetch from anki server if none
	public static async Task<Love.Source> LoadAudio(string ankiFile)
	{
		var audio = GetCacheAudio(ankiFile);
		if (audio != null)
		{
			return audio;
		}


		var resp = await AnkiConnect.GetMedia(ankiFile);
		System.Diagnostics.Debug.Assert(resp.error == null);
		System.Diagnostics.Debug.Assert(resp.value != null);


		var base64 = resp.value;
		var stream = DecodeBase64(base64);

		FileSystem.CreateDirectory(cacheDir);
		await ConvertAndSave(stream, Path.Join(cacheDir, ankiFile));


		audio = GetCacheAudio(ankiFile);
		System.Diagnostics.Debug.Assert(audio != null);

		return audio;
	}

	public static async Task ConvertAndSave(System.IO.Stream stream, string filename)
	{
		FFMpegArguments
		    .FromPipeInput(new StreamPipeSource(stream))
		    .OutputToFile(filename, false, options => options
			.WithAudioCodec(AudioCodec.LibVorbis)
			.WithFastStart())
		    .ProcessSynchronously();

	}

	public static Love.Source? GetCacheAudio(string ankiFile)
	{
		// TODO: handle if file is corrupted/not supported
		try
		{
			return Audio.NewSource(Path.Join(cacheDir, ankiFile), SourceType.Static);
		}
		catch (Exception e)
		{
			return null;
		}
	}
}