using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using AwaitableCoroutine;
using Love;
using MyNihongo.KanaDetector.Extensions;
using System.Text.Json;
using System.Text;

namespace gganki_love;

using IntPair = System.ValueTuple<int, int>;


public interface IPos
{
    public Vector2 pos { get; set; }
    //public Vector2 lastPos { get; set; }
}


public class Config
{
    public const int fontSize = 50;
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

    public Entity windowEntity = new Entity();

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

    public static IDisposable WithKeyPress(KeyPressed handler)
    {
        OnKeyPress += handler;
        return new DisposeKeyPress(handler);
    }

    class DisposeKeyPress : IDisposable
    {
        KeyPressed handler;
        public DisposeKeyPress(KeyPressed handler) => this.handler = handler;
        public void Dispose()
        {
            KeyHandler.OnKeyPress -= handler;
        }
    }

}
public class MouseHandler
{
    public record ButtonEvent(float x, float y, MouseButton button, bool isTouch)
    { }

    public record MoveEvent(float x, float y, float dx, float dy, bool isTouch)
    { }

    public delegate void MouseReleased(ButtonEvent ev);
    public delegate void MousePress(ButtonEvent ev);
    public delegate void MouseMoved(MoveEvent ev);
    public static event MousePress? OnMousePress;
    public static event MouseReleased? OnMouseRelease;
    public static event MouseMoved? OnMouseMove;

    public static void DispatchMousePress(ButtonEvent ev)
    {
        OnMousePress?.Invoke(ev);
    }
    public static void DispatchMouseRelease(ButtonEvent ev)
    {
        OnMouseRelease?.Invoke(ev);
    }

    internal static void DispatchMouseMove(MouseHandler.MoveEvent moveEvent)
    {
        OnMouseMove?.Invoke(moveEvent);
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

public struct Polar
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

    public Vector2 ToVector()
    {
        var x = MathF.Cos(angle) * radius;
        var y = MathF.Sin(angle) * radius;
        return new Vector2(x, y);
    }

    public static Vector2 ToVector(Polar p)
    {
        var x = MathF.Cos(p.angle) * p.radius;
        var y = MathF.Sin(p.angle) * p.radius;
        return new Vector2(x, y);
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


public class PartitionedList<T> : IDisposable where T : IPos
{
    public class Partition : HashSet<T> { }
    public class Mapping : Dictionary<IntPair, Partition> { }

    public int partitionSize { get; init; }

    Mapping items = new Mapping();

    Dictionary<T, Vector2> itemSet = new Dictionary<T, Vector2>();

    List<T> removeQueue = new List<T>();

    public readonly Partition emptyEntry = new Partition();

    public PartitionedList(int partitionSize)
    {
        this.partitionSize = partitionSize;
        Callbacks.AddPostUpdate(RemoveAllQueued);
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

    public void RemoveQueue(T item)
    {
        removeQueue.Add(item);
    }

    private void RemoveAllQueued()
    {
        for (var i = removeQueue.Count() - 1; i >= 0; i--)
        {
            Remove(removeQueue[i]);
            removeQueue.RemoveAt(i);
        }
    }

    public void Clear()
    {
        items.Clear();
        itemSet.Clear();
    }

    public void Dispose()
    {
        Clear();
        Callbacks.RemovePostUpdate(RemoveAllQueued);
    }
}
public class AnkiAudioPlayer
{
    static Dictionary<string, Love.Source> data = new Dictionary<string, Source>();

    public static void Clear()
    {
        foreach (var sound in data.Values)
        {
            sound.Dispose();
        }
        data.Clear();
    }

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

    public static async Task PlayWait(string? embeddedFilename)
    {

        if (embeddedFilename == null) { return; }

        var filename = GetSoundFilename(embeddedFilename) ?? embeddedFilename;

        Love.Source? source;
        if (data.TryGetValue(filename, out source))
        {
            source = source.Clone();
            using (source)
            {
                var duration = source.GetDuration();
                source.Play();
                await Task.Delay((int)(duration * 1000));
            }
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

public interface IComponent
{
    void Draw(Entity entity);
    void Update(Entity entity);
}

public class Component : IComponent
{
    public bool EnableDraw { get; set; } = true;
    public bool EnableUpdate { get; set; } = true;

    public Action<Entity>? DrawComponent { get; set; }
    public Action<Entity>? UpdateComponent { get; set; }

    public void Draw(Entity entity)
    {
        if (EnableDraw && DrawComponent != null)
        {
            DrawComponent(entity);
        }
    }

    public void Update(Entity entity)
    {
        if (EnableUpdate && UpdateComponent != null)
        {
            UpdateComponent(entity);
        }
    }
}

public class ComponentView : IComponent
{
    public bool EnableDraw { get; set; } = true;
    public bool EnableUpdate { get; set; } = true;

    public Action? DrawComponent { get; set; }
    public Action? UpdateComponent { get; set; }

    public void Draw(Entity entity)
    {
        if (EnableDraw && DrawComponent != null)
        {
            DrawComponent();
        }
    }

    public void Update(Entity entity)
    {
        if (EnableUpdate && UpdateComponent != null)
        {
            UpdateComponent();
        }
    }
}

public class ComponentRegistry
{
    public struct ActionDispose : IDisposable
    {
        Action action;
        bool isDraw;
        bool remove = true;
        ComponentRegistry reg;
        public ActionDispose(Action action, ComponentRegistry reg, bool isDraw, bool remove = true)
        {
            this.action = action;
            this.reg = reg;
            this.isDraw = isDraw;
            this.remove = remove;
        }
        public void Dispose()
        {
            //Console.WriteLine("disposed");
            if (remove)
            {

                if (isDraw)
                {
                    reg.RemoveDraw(action);
                }
                else
                {
                    reg.RemoveUpdate(action);
                }
            }
            else
            {
                if (isDraw)
                {
                    reg.AddDraw(action);
                }
                else
                {
                    reg.AddUpdate(action);
                }

            }
        }
    }

    public struct CompDispose : IDisposable
    {
        IComponent comp;
        ComponentRegistry reg;
        public CompDispose(IComponent c, ComponentRegistry r) { comp = c; reg = r; }
        public void Dispose()
        {
            //Console.WriteLine("disposed");
            reg.RemoveComponent(comp);
        }
    }

    Entity windowEntity;
    public HashSet<IComponent> components = new HashSet<IComponent>();
    public HashSet<Action> drawFunctions = new HashSet<Action>();
    public HashSet<Action> updateFunctions = new HashSet<Action>();

    public ComponentRegistry()
    {
        windowEntity = SharedState.self.windowEntity;
    }

    public IDisposable AddComponent(IComponent comp)
    {
        components.Add(comp);
        return new CompDispose(comp, this);
    }
    public void RemoveComponent(IComponent comp) { components.Remove(comp); }

    public IDisposable AddDraw(Action action)
    {
        drawFunctions.Add(action);
        return new ActionDispose(action, this, true, remove: true);
    }
    public void RemoveDraw(Action action) { drawFunctions.Remove(action); }

    public IDisposable AddUpdate(Action action)
    {
        updateFunctions.Add(action);
        return new ActionDispose(action, this, false, remove: true);
    }
    public void RemoveUpdate(Action action)
    {
        updateFunctions.Remove(action);
    }

    public IDisposable TemporaryRemoveUpdate(Action action)
    {
        updateFunctions.Remove(action);
        return new ActionDispose(action, this, isDraw: false, remove: false);
    }

    public IDisposable TemporaryRemoveDraw(Action action)
    {
        drawFunctions.Remove(action);
        return new ActionDispose(action, this, isDraw: true, remove: false);
    }

    public IEnumerable<IComponent> GetComponents() { return components; }

    public void Draw()
    {
        foreach (var c in components)
        {
            c.Draw(windowEntity);
        }
        foreach (var fn in drawFunctions)
        {
            fn();
        }
    }

    public void Update()
    {
        foreach (var c in components)
        {
            c.Update(windowEntity);
        }
        foreach (var fn in updateFunctions)
        {
            fn();
        }
    }

    public void Dispose()
    {
        components.Clear();
    }

}

public class Entity
{
    public Vector2 pos { get; set; }
    public Vector2 dir { get; set; }
    public float radianAngle { get; set; } = 0f;
    public float speed { get; set; } = 5f;

    public AtlasImage atlasImage { get; set; }
    public int tileID { get; set; }
    public Quad quad { get; set; }
    public bool reversedX;

    public bool debug = false;
    public float scale = 2;
    public RectangleF rect = new RectangleF();

    public float flipX = 1;
    public float flipY = 1;

    public bool mirroredX = false;
    public bool mirroredY = false;

    public Color color = Color.White;

    public HashSet<IComponent> components = new HashSet<IComponent>();

    public Entity(AtlasImage atlasImage, int tileID)
    {
        this.atlasImage = atlasImage;
        this.tileID = tileID;
        quad = atlasImage.GetQuad(tileID);

        var n = atlasImage.tileSize;
        rect.X = pos.X - n * scale / 2;
        rect.Y = pos.Y - n * scale / 2;
        rect.Width = n * scale;
        rect.Height = n * scale;
    }
    public Entity()
    {
        var empty = new Vector4[,] { { Vector4.Zero } };
        var data = Image.NewImageData(empty, ImageDataPixelFormat.RGBA16);
        var image = Graphics.NewImage(data);

        this.tileID = -1;
        this.atlasImage = new AtlasImage(image);
        this.quad = Graphics.NewQuad(0, 0, 0, 0, 0, 0);
    }

    public static Entity Create(int tileID, SharedState? state = null)
    {
        if (state is null)
        {
            state = SharedState.self;
        }
        Debug.Assert(state.atlasImage != null);
        return new Entity(state.atlasImage, tileID);
    }


    public void AddComponent(IComponent comp) { components.Add(comp); }
    public void RemoveComponent(IComponent comp) { components.Remove(comp); }
    public IEnumerable<IComponent> GetComponents() { return components; }
    public void ClearComponents()
    {
        components.Clear();
    }

    public void Update()
    {
        var n = atlasImage.tileSize;
        rect.X = pos.X - n * scale / 2;
        rect.Y = pos.Y - n * scale / 2;
        rect.Width = n * scale;
        rect.Height = n * scale;

        foreach (var c in components)
        {
            c.Update(this);
        }
    }

    public void FaceDirectionX(Vector2 dir)
    {
        flipX = Vector2.Dot(dir, Directions.Right) > 0 ? -1 : 1;
    }


    public void Draw()
    {
        var n = atlasImage.tileSize;
        Graphics.SetColor(color);

        var xt = (mirroredX ? -1 : 1) * flipX;
        var yt = (mirroredY ? -1 : 1) * flipY;
        atlasImage.Draw(quad, pos.X, pos.Y, radianAngle, scale * xt, scale * yt, n / 2, n / 2);

        foreach (var c in components)
        {
            c.Draw(this);
        }
    }

    public bool CollidesWith(Entity e)
    {
        return Vector2.Distance(pos, e.pos) < rect.DiagonalLength();
    }

}

public class Corunner
{
    public CoroutineRunner runner = new CoroutineRunner();
    List<Coroutine> coroutines = new List<Coroutine>();

    public bool IsUpdating => runner.IsUpdating;


    public Coroutine Create(Func<Coroutine> init)
    {
        var co = runner.Create(init);
        coroutines.Add(co);
        return co;
    }

    public Coroutine<T> Create<T>(Func<Coroutine<T>> init)
    {
        var co = runner.Create(init);
        coroutines.Add(co);
        return co;

    }

    public void Update() { runner.Update(); }

    public void Cancel()
    {
        foreach (var co in coroutines)
        {
            if (!co.IsCompleted) co.Cancel();
        }
    }
}

public class JP
{
    public enum Type { None, Other, KanaOrKanji };
    public record SplitEnty
    {
        public Type type { get; set; } = Type.None;
        public string text { get; set; } = "";
        public int index { get; set; } = 0;
        public int kIndex { get; set; } = 0;
    }
    public static bool IsKanaOrKanji(char ch)
    {
        return ch.IsKanaOrKanji() && ch != '、' && ch != '。';
    }
    public static SplitEnty[] SplitText(string text)
    {
        text = text.Trim();

        var sb = new System.Text.StringBuilder();
        var result = new List<SplitEnty>();
        var type = Type.Other;

        var index = 0;
        var kIndex = 0;
        foreach (var ch in text)
        {
            type = IsKanaOrKanji(ch) ? Type.KanaOrKanji : Type.Other;
            if (type == Type.KanaOrKanji)
            {
                if (sb.Length > 0)
                {
                    var other = sb.ToString();
                    result.Add(new SplitEnty
                    {
                        index = index - other.Length,
                        type = Type.Other,
                        text = other,
                    });
                    sb.Clear();
                }
                result.Add(new SplitEnty
                {
                    index = index,
                    kIndex = kIndex++,
                    type = Type.KanaOrKanji,
                    text = ch.ToString(),
                });
            }
            else
            {
                sb.Append(ch);
            }

            index++;
        }
        if (sb.Length > 0)
        {
            var other = sb.ToString();
            result.Add(new SplitEnty
            {
                index = index - other.Length,
                type = Type.Other,
                text = other,
            });
            sb.Clear();
        }
        //result.Add(new SplitEnty
        //{
        //	index = index,
        //	text = sb.ToString(),
        //	type = type,
        //});

        return result.ToArray();
    }


    public static bool HasJapanese(string text)
    {
        var jpCount = 0;
        var romanCount = 0;
        foreach (var ch in text)
        {
            if (ch.IsKanaOrKanji())
            {
                jpCount++;
            }
            else if (ch.IsKanaOrKanji())
            {
                romanCount++;

            }
        }
        return jpCount >= romanCount;
    }
}

public class CoroutineControl
{
    bool cancel = false;

    public void Cancel(Coroutine? co = null)
    {
        co?.TryCancel();
        cancel = true;
    }

    public YieldAwaitable Yield()
    {
        if (cancel) Coroutine.ThrowCancel();
        return Coroutine.Yield();
    }
    public async Coroutine DelayCount(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Yield();
        }
    }
    public async Coroutine While(Func<bool> predicate)
    {
        while (predicate())
        {
            await Yield();
        }
    }
    public async Coroutine AwaitTask(Task task)
    {
        while (!task.IsCompletedSuccessfully)
        {
            if (task.IsCanceled)
            {
                Coroutine.ThrowCancel("task is canceled");
            }

            if (task.IsFaulted)
            {
                Coroutine.ThrowCancel("task is faulted");
            }
            await Yield();
        }
    }

    public async Coroutine<T> AwaitTask<T>(Task<T> task)
    {
        while (!task.IsCompletedSuccessfully)
        {
            if (task.IsCanceled)
            {
                Coroutine.ThrowCancel("task is canceled");
            }

            if (task.IsFaulted)
            {
                Coroutine.ThrowCancel("task is faulted");
            }
            await Yield();
        }

        return task.Result;
    }


    public async Coroutine Sleep(float seconds)
    {
        var time = Love.Timer.GetTime();
        var startTime = time;
        while (seconds > 0)
        {
            var fps = Love.Timer.GetFPS() - 1;
            await DelayCount((int)(fps * seconds));

            var now = Love.Timer.GetTime();
            seconds -= now - time;
            time = now;
        }
    }

    public async Coroutine Abyss()
    {
        while (true)
        {
            await Yield();
        }
    }

    public async Coroutine AwaitKey(KeyConstant waitKey)
    {
        var done = false;
        KeyHandler.KeyPressed handler = (key, code, repeat) =>
        {
            done = done || key == waitKey;
        };

        KeyHandler.OnKeyPress += handler;
        using var _ = Defer.Run(() => KeyHandler.OnKeyPress -= handler);
        while (!done) await Yield();
    }

    public async Coroutine AwaitAnyKey()
    {
        var done = false;
        KeyHandler.KeyPressed keyHandler = (key, code, repeat) => { done = true; };
        Action<Joystick, GamepadButton> gpadHandler = (Joystick js, GamepadButton button) => { done = true; };

        KeyHandler.OnKeyPress += keyHandler;
        GamepadHandler.OnPress += gpadHandler;

        using var _ = Defer.Run(() =>
        {
            KeyHandler.OnKeyPress -= keyHandler;
            GamepadHandler.OnPress -= gpadHandler;
        });

        while (!done) await Yield();
    }

    public void Reset()
    {
        cancel = false;
    }
}

public class Callbacks
{
    static event Action OnPostUpdate = () => { };
    static event Action OnPreUpdate = () => { };
    static event Action OnUpdate = () => { };
    static event Action OnDraw = () => { };
    static event Action OnPreDraw = () => { };
    static event Action OnPostDraw = () => { };

    public static void Draw() => OnDraw();
    public static void PreDraw() => OnPreDraw();
    public static void PostDraw() => OnPostDraw();
    public static void Update() => OnUpdate();
    public static void PreUpdate() => OnPreUpdate();
    public static void PostUpdate() => OnPostUpdate();

    public static void AddUpdate(Action fn)
    {
        OnUpdate -= fn;
        OnUpdate += fn;
    }
    public static void AddPreUpdate(Action fn)
    {
        OnPreUpdate -= fn;
        OnPreUpdate += fn;
    }
    public static void AddPostUpdate(Action fn)
    {
        OnPostUpdate -= fn;
        OnPostUpdate += fn;
    }
    public static void RemoveUpdate(Action fn) => OnUpdate -= fn;
    public static void RemovePreUpdate(Action fn) => OnPreUpdate -= fn;
    public static void RemovePostUpdate(Action fn) => OnPostUpdate -= fn;

    public static void AddDraw(Action fn)
    {
        OnDraw -= fn;
        OnDraw += fn;
    }
    public static void AddPreDraw(Action fn)
    {
        OnPreDraw -= fn;
        OnPreDraw += fn;
    }
    public static void AddPostDraw(Action fn)
    {
        OnPostDraw -= fn;
        OnPostDraw += fn;
    }

    public static void RemoveDraw(Action fn) => OnDraw -= fn;
    public static void RemovePreDraw(Action fn) => OnPreDraw -= fn;
    public static void RemovePostDraw(Action fn) => OnPostDraw -= fn;
}

public class Defer
{
    struct Disposeable : IDisposable, IAsyncDisposable
    {
        Action? action;
        string name = "";
        public Disposeable(Action action, string name = "") { this.action = action; this.name = name; }
        public void Dispose()
        {
            if (action != null) action();
            action = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    public static IDisposable Run(Action action, string name = "")
    {
        return new Disposeable(action, name);
    }
}

public class RandomContainer
{
    public static Random Rand = Random.Shared;
}

public class DebugUtils
{
    public static string DumpVar(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }
    public static void PrintVar(params object[] objs)
    {
        var sb = new StringBuilder();
        foreach (var obj in objs)
        {

            var formatted = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
            sb.Append(formatted);
            sb.Append(" ");
        }
        Console.WriteLine(sb.ToString());
        sb.Clear();
    }
}




public enum GamepadInput
{
    A = GamepadButton.A,
    B = GamepadButton.B,
    X = GamepadButton.X,
    Y = GamepadButton.Y,
    Back = GamepadButton.Back,
    Guide = GamepadButton.Guide,
    Start = GamepadButton.Start,
    LeftStick = GamepadButton.LeftStick,
    RightStick = GamepadButton.RightStick,
    LeftShoulder = GamepadButton.LeftShoulder,
    RightShoulder = GamepadButton.RightShoulder,
    DPadUp = GamepadButton.DPadUp,
    DPadDown = GamepadButton.DPadDown,
    DPadLeft = GamepadButton.DPadLeft,
    DPadRight = GamepadButton.DPadRight,

    LeftStickUp = 16,
    LeftStickDown = 17,
    LeftStickLeft = 18,
    LeftStickRight = 19,

    RightStickUp = 20,
    RightStickDown = 21,
    RightStickLeft = 22,
    RightStickRight = 23,
}

public enum MouseInput
{
    LeftButton = MouseButton.LeftButton,
    RightButton = MouseButton.RightButton,
    MiddleButton = MouseButton.MiddleButton,
    ExtendedButton1 = MouseButton.ExtendedButton1,
    ExtendedButton2 = MouseButton.ExtendedButton2,
    ExtendedButton3 = MouseButton.ExtendedButton3,
    ExtendedButton4 = MouseButton.ExtendedButton4,
    ExtendedButton5 = MouseButton.ExtendedButton5,

    MoveUp = MouseButton.ExtendedButton5 + 1,
    MoveDown = MouseButton.ExtendedButton5 + 2,
    MoveLeft = MouseButton.ExtendedButton5 + 3,
    MoveRight = MouseButton.ExtendedButton5 + 4,
}

public enum ActionType
{
    Up,
    Down,
    Left,
    Right,
    Up2,
    Down2,
    Left2,
    Right2,
    ButtonA,
    ButtonB,
    ButtonX,
    ButtonY,
}

public class GameInput : IDisposable
{
    public event Action<ActionType> OnPress = (a) => { };
    public event Action<ActionType> OnRelease = (a) => { };
    public event Action<Vector2> OnMotion1 = (v) => { };
    public event Action<Vector2> OnMotion2 = (v) => { };

    public Dictionary<KeyConstant, ActionType> keyMap = new()
    {
        {KeyConstant.Up, ActionType.Up },
        {KeyConstant.Down, ActionType.Down },
        {KeyConstant.Left, ActionType.Left },
        {KeyConstant.Right, ActionType.Right },

        {KeyConstant.W, ActionType.Up },
        {KeyConstant.S, ActionType.Down },
        {KeyConstant.A, ActionType.Left },
        {KeyConstant.D, ActionType.Right },

        {KeyConstant.Space, ActionType.ButtonX },
        {KeyConstant.Enter, ActionType.ButtonA },
    };

    public Dictionary<MouseInput, ActionType> mouseMap = new()
    {
        {MouseInput.LeftButton, ActionType.ButtonA },
        {MouseInput.RightButton, ActionType.ButtonB },

        {MouseInput.MoveUp, ActionType.Up2 },
        {MouseInput.MoveDown, ActionType.Down2 },
        {MouseInput.MoveLeft, ActionType.Left2 },
        {MouseInput.MoveRight, ActionType.Right2 },
    };
    public Dictionary<GamepadInput, ActionType> gamepadMap = new()
    {
        { gganki_love.GamepadInput.DPadUp, ActionType.Up },
        { gganki_love.GamepadInput.DPadDown, ActionType.Down },
        { gganki_love.GamepadInput.DPadLeft, ActionType.Left },
        { gganki_love.GamepadInput.DPadRight, ActionType.Right },

        { gganki_love.GamepadInput.LeftStickUp, ActionType.Up },
        { gganki_love.GamepadInput.LeftStickDown, ActionType.Down },
        { gganki_love.GamepadInput.LeftStickLeft, ActionType.Left },
        { gganki_love.GamepadInput.LeftStickRight, ActionType.Right },

        { gganki_love.GamepadInput.RightStickUp, ActionType.Up2 },
        { gganki_love.GamepadInput.RightStickDown, ActionType.Down2 },
        { gganki_love.GamepadInput.RightStickLeft, ActionType.Left2 },
        { gganki_love.GamepadInput.RightStickRight, ActionType.Right2 },

        { gganki_love.GamepadInput.A, ActionType.ButtonA },
        { gganki_love.GamepadInput.B, ActionType.ButtonB },
    };

    public bool Disabled { get; set; } = false;

    Dictionary<ActionType, float> actionMap = new();
    Dictionary<ActionType, bool> actionPressed = new();
    Dictionary<ActionType, float> actionChargeTime = new();

    Vector2 mouseAxis = Vector2.Zero;
    Vector2 gpadLeftAxis = Vector2.Zero;
    Vector2 gpadRightAxis = Vector2.Zero;
    public float sensitivity = 0.8f;

    public GameInput()
    {
        KeyHandler.OnKeyPress += OnKeyPress;
        KeyHandler.OnKeyRelease += OnKeyRelease;
        MouseHandler.OnMousePress += OnMousePress;
        MouseHandler.OnMouseRelease += OnMouseRelease;
        MouseHandler.OnMouseMove += OnMouseMove;
        GamepadHandler.OnPress += OnGamepadPress;
        GamepadHandler.OnRelease += OnGamepadRelease;
        GamepadHandler.OnAxis += OnGamepadAxis;
        Callbacks.AddPostUpdate(OnPostUpdate);
    }

    public void Dispose()
    {
        KeyHandler.OnKeyPress -= OnKeyPress;
        KeyHandler.OnKeyRelease -= OnKeyRelease;
        MouseHandler.OnMousePress -= OnMousePress;
        MouseHandler.OnMouseRelease -= OnMouseRelease;
        MouseHandler.OnMouseMove -= OnMouseMove;
        GamepadHandler.OnPress -= OnGamepadPress;
        GamepadHandler.OnRelease -= OnGamepadRelease;
        GamepadHandler.OnAxis -= OnGamepadAxis;
        Callbacks.RemovePostUpdate(OnPostUpdate);
    }

    private void OnGamepadAxis(Joystick arg1, GamepadAxis axisType, float value)
    {
        if (axisType == GamepadAxis.TriggerLeft || axisType == GamepadAxis.TriggerRight)
        {
            return;
        }

        Vector2 axis = axisType == GamepadAxis.LeftX || axisType == GamepadAxis.LeftY
                ? gpadLeftAxis
                : gpadRightAxis;

        if (axisType == GamepadAxis.LeftX || axisType == GamepadAxis.RightX)
        {
            axis.X = value;

            var val = MathF.Abs(axis.X);
            var rightType = ActionType.Right;
            var leftType = ActionType.Left;
            var (gpadLeft, gpadRight) = axisType == GamepadAxis.LeftX
                                      ? (GamepadInput.LeftStickLeft, GamepadInput.LeftStickRight)
                                      : (GamepadInput.RightStickLeft, GamepadInput.RightStickRight);

            gamepadMap.TryGetValue(gpadLeft, out leftType);
            gamepadMap.TryGetValue(gpadRight, out rightType);

            if (axis.X > 0)
            {
                SetActionMapValue(rightType, val);
                SetActionMapValue(leftType, 0);
            }
            else if (axis.X < 0)
            {
                SetActionMapValue(rightType, 0);
                SetActionMapValue(leftType, val);
            }
            else
            {
                SetActionMapValue(rightType, 0);
                SetActionMapValue(leftType, 0);
            }
        }

        if (axisType == GamepadAxis.LeftY || axisType == GamepadAxis.RightY)
        {
            axis.Y = value;

            var val = MathF.Abs(axis.Y);
            var upType = ActionType.Up;
            var downType = ActionType.Down;
            var (gpadDown, gpadUp) = axisType == GamepadAxis.LeftY
                                      ? (GamepadInput.LeftStickDown, GamepadInput.LeftStickUp)
                                      : (GamepadInput.RightStickDown, GamepadInput.RightStickUp);

            gamepadMap.TryGetValue(gpadUp, out upType);
            gamepadMap.TryGetValue(gpadDown, out downType);

            if (axis.Y > 0)
            {
                SetActionMapValue(upType, 0);
                SetActionMapValue(downType, val);
            }
            else if (axis.Y < 0)
            {
                SetActionMapValue(upType, val);
                SetActionMapValue(downType, 0);
            }
            else
            {
                SetActionMapValue(upType, 0);
                SetActionMapValue(downType, 0);
            }
        }
    }

    private void OnGamepadRelease(Joystick js, GamepadButton button)
    {
        ActionType type;
        if (gamepadMap.TryGetValue((GamepadInput)button, out type))
        {
            SetActionMapValue(type, 0);
        }
    }

    private void OnGamepadPress(Joystick js, GamepadButton button)
    {
        ActionType type;
        if (gamepadMap.TryGetValue((GamepadInput)button, out type))
        {
            SetActionMapValue(type, 1);
        }
    }

    public void HoldAction(ActionType type, float value = 1)
    {
        SetActionMapValue(type, value);
    }
    public void ReleaseAction(ActionType type)
    {
        SetActionMapValue(type, 0);
    }

    private void OnMouseMove(MouseHandler.MoveEvent ev)
    {
        mouseAxis.X = Xt.MathF.Clamp(mouseAxis.X + ev.dx / 200 * sensitivity, -1, 1);
        mouseAxis.Y = Xt.MathF.Clamp(mouseAxis.Y + ev.dy / 200 * sensitivity, -1, 1);

        var xVal = MathF.Abs(mouseAxis.X);
        var yVal = MathF.Abs(mouseAxis.Y);


        if (ev.dx != 0)
        {
            var rightType = ActionType.Right2;
            var leftType = ActionType.Left2;
            var hasRight = mouseMap.TryGetValue(MouseInput.MoveRight, out rightType);
            var hasLeft = mouseMap.TryGetValue(MouseInput.MoveLeft, out leftType);

            //Console.WriteLine("{0}, {1}", mouseAxis, (xVal, yVal));
            if (mouseAxis.X > 0 && hasRight)
            {
                SetActionMapValue(rightType, xVal);
                SetActionMapValue(leftType, 0);
            }
            else if (mouseAxis.X < 0 && hasLeft)
            {
                SetActionMapValue(rightType, 0);
                SetActionMapValue(leftType, xVal);
            }
            else
            {
                SetActionMapValue(rightType, 0);
                SetActionMapValue(leftType, 0);
            }
        }

        if (ev.dy != 0)
        {
            var upType = ActionType.Up2;
            var downType = ActionType.Down2;
            var hasUp = mouseMap.TryGetValue(MouseInput.MoveUp, out upType);
            var hasDown = mouseMap.TryGetValue(MouseInput.MoveDown, out downType);

            if (mouseAxis.Y > 0 && hasDown)
            {
                SetActionMapValue(downType, yVal);
                SetActionMapValue(upType, 0);
            }
            else if (mouseAxis.Y < 0 && hasUp)
            {
                SetActionMapValue(upType, yVal);
                SetActionMapValue(downType, 0);
            }
            else
            {
                SetActionMapValue(upType, 0);
                SetActionMapValue(downType, 0);
            }
        }
    }

    private void OnMouseRelease(MouseHandler.ButtonEvent ev)
    {
        ActionType type;
        if (mouseMap.TryGetValue((MouseInput)ev.button, out type))
        {
            SetActionMapValue(type, 0);
        }
    }

    private void OnMousePress(MouseHandler.ButtonEvent ev)
    {
        ActionType type;
        if (mouseMap.TryGetValue((MouseInput)ev.button, out type))
        {
            SetActionMapValue(type, 1);
        }
    }

    private void OnKeyRelease(KeyConstant key, Scancode scancode)
    {
        ActionType type;
        if (keyMap.TryGetValue(key, out type))
        {
            SetActionMapValue(type, 0);
        }
    }

    private void OnKeyPress(KeyConstant key, Scancode scancode, bool isRepeat)
    {
        ActionType type;
        if (keyMap.TryGetValue(key, out type))
        {
            SetActionMapValue(type, 1);
        }
    }

    public Vector2 GetMotion()
    {
        var v = new Vector2(
            -GetActionMapValue(ActionType.Left) + GetActionMapValue(ActionType.Right),
            -GetActionMapValue(ActionType.Up) + GetActionMapValue(ActionType.Down)
        );

        if (v.Length() != 0)
            v = Vector2.Normalize(v);

        return v;
    }
    public Vector2 GetMotion2()
    {
        var v = new Vector2(
            -GetActionMapValue(ActionType.Left2) + GetActionMapValue(ActionType.Right2),
            -GetActionMapValue(ActionType.Up2) + GetActionMapValue(ActionType.Down2)
        );

        if (v.Length() != 0)
            v = Vector2.Normalize(v);

        return v;


    }

    public bool IsDown(ActionType type)
    {
        float value;
        if (actionMap.TryGetValue(type, out value))
        {
            return value > 0;
        }
        return false;
    }

    public bool IsPressed(ActionType type)
    {
        if (!IsDown(type))
        {
            return false;
        }

        return actionPressed.GetValueOrDefault(type, false);
    }
    public float GetChargeTime(ActionType type)
    {
        if (IsDown(type))
        {
            var now = Love.Timer.GetTime();
            var start = actionChargeTime.GetValueOrDefault(type, now);
            return now - MathF.Abs(start);
        }
        return 0;
    }
    public float GetReleaseChargeTime(ActionType type)
    {
        var now = Love.Timer.GetTime();
        var start = actionChargeTime.GetValueOrDefault(type, now);
        if (!IsDown(type) && start < 0)
        {
            return MathF.Abs(now - start);
        }
        return 0;
    }

    public async Coroutine<ActionType> PressAny(CoroutineControl ctrl, params ActionType[] actions)
    {
        ActionType? type = null;
        var onPress = (ActionType t) =>
        {
            foreach (var t2 in actions)
            {
                if (t == t2)
                {
                    type = t;
                    break;
                }
            }
        };

        OnPress += onPress;
        using var _ = Defer.Run(() => OnPress -= onPress);
        while (type is null) await ctrl.Yield();

        return type.Value;
    }

    private float GetActionMapValue(ActionType type)
    {
        return actionMap.GetValueOrDefault(type, 0);
    }

    private void SetActionMapValue(ActionType type, float value)
    {
        if (Disabled)
        {
            return;
        }

        var prevValue = actionMap.GetValueOrDefault(type, 0);
        if (prevValue == 0 && value > 0)
        {
            actionPressed[type] = true;
            actionChargeTime[type] = Love.Timer.GetTime();
            OnPress(type);
        }
        else if (value == 0 && prevValue > 0)
        {
            OnRelease(type);
        }


        actionMap[type] = value;

        if (value != prevValue)
        {
            var isMotion1 = type == ActionType.Up
                         || type == ActionType.Down
                         || type == ActionType.Left
                         || type == ActionType.Right;
            var isMotion2 = type == ActionType.Up2
                         || type == ActionType.Down2
                         || type == ActionType.Left2
                         || type == ActionType.Right2;

            if (isMotion1)
                OnMotion1(GetMotion());
            else if (isMotion2)
                OnMotion2(GetMotion2());
        }

    }


    private void OnPostUpdate()
    {
        foreach (var actionType in Enum.GetValues(typeof(ActionType)).Cast<ActionType>())
        {
            actionPressed[actionType] = false;
            var value = actionMap.GetValueOrDefault(actionType, 0);
            if (value == 0)
            {
                var chargeTime = actionChargeTime.GetValueOrDefault(actionType, 0);
                if (chargeTime > 0)
                {
                    // set to negative to clear on next frame update
                    actionChargeTime[actionType] = -chargeTime;
                }
                else if (chargeTime < 0)
                {
                    actionChargeTime[actionType] = 0;
                }
            }
        }
    }
}

/*
Playing menu
> Select deck
> Restart
> Quit

Clear menu
> Next
> play again

Gameover menu
> retry
> quit



*/


public class MouseAxis : IDisposable
{
    Vector2 axis = Vector2.Zero;
    public MouseAxis()
    {
        MouseHandler.OnMouseMove += OnMouseMove;
    }

    public void Reset()
    {
        axis = Vector2.Zero;
    }

    public Vector2 GetDirection()
    {
        if (axis.Length() == 0)
        {
            return axis;
        }
        return Vector2.Normalize(axis);
    }

    private void OnMouseMove(MouseHandler.MoveEvent ev)
    {
        axis.X += ev.dx;
        axis.Y += ev.dy;

        axis.X = Xt.MathF.Clamp(axis.X, -1, 1);
        axis.Y = Xt.MathF.Clamp(axis.Y, -1, 1);
    }

    public void Dispose()
    {
        MouseHandler.OnMouseMove -= OnMouseMove;
    }
}

[Flags]
public enum PosAlign
{
    Center = 0,
    StartX = 1,
    EndX = 2,
    StartY = 4,
    EndY = 8,
    Start = StartX | StartY,
    End = EndX | EndY,
}