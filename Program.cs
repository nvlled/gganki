
using System.Text.Json;
using System.Threading;
using Love;

namespace gganki_love;

public class Program : Scene
{
    public static Thread MainThread;

    SharedState state;
    KeyHandler keyHandler = new KeyHandler();

    ScriptLoader scriptLoader;

    bool debugEnabled;


    //FileSystemWatcher watcher;

    static void Main(string[] args)
    {
        var debug = false;
        foreach (var arg in args)
        {
            if (arg == "--debug")
            {
                debug = true;
                break;
            }
        }

        Boot.Init(new BootConfig
        {
            WindowResizable = true,
            WindowTitle = "gganki",
            WindowWidth = 1366,
            WindowHeight = 800,
            WindowX = 10,
            WindowY = 10,
        });
        Boot.Run(new Program(debug));
    }


    public Program(bool debug)
    {
        debugEnabled = debug;
        state = SharedState.self;
        scriptLoader = new ScriptLoader(state);
        Keyboard.SetKeyRepeat(true);

        state.fontAsian = Graphics.NewFont("assets/han-serif.otf", Config.fontSize);
        state.fontMedium = Graphics.NewFont("assets/han-serif.otf", Config.fontSizeMedium);
        state.fontRegular = state.fontAsian;
        //state.fontAsian.SetLineHeight(0.1f);
        //state.fontRegular.SetLineHeight(20.0f);
    }

    public void OnLoadDone(DeckNames deckNames)
    {
        Console.WriteLine("loading next view");

        state.deckNames = deckNames;
        //state.cards = cards;

        var deckSelect = new DeckSelectView(state);
        deckSelect.OnSelect += OnSelectDeck;
        state.SetActiveView(deckSelect);
    }

    public async void OnSelectDeck(string deckName, ulong id)
    {
        GameView gameView;
        Console.WriteLine("opening: " + deckName);
        if (state.deckCards.ContainsKey(deckName))
        {
            gameView = new GameView(deckName, state);
            state.SetActiveView(gameView);

            if (debugEnabled)
            {
                scriptLoader.EnableDebug();
            }
            else
            {
                scriptLoader.StartLoad();
            }


            return;
        }

        var cardTask = AnkiConnect.FetchAvailableCards(deckName);
        var loader = new LoaderView(state);

        state.SetActiveView(loader);
        await Task.WhenAll(
            //loader.AwaitTask("delay", Task.Delay(2000)),
            loader.AddTask("deck cards", cardTask)
        );

        var cards = cardTask.Result.value;
        state.deckCards[deckName] = cards;

        Console.WriteLine("saving deck name " + deckName);
        gameView = new GameView(deckName, state);
        state.SetActiveView(gameView);

        WriteSaveState(new SavedState { lastDeckName = deckName });

        //var gameView = new GameView(state);
        //state.SetActiveView(gameView);
    }

    public void WriteSaveState(SavedState save)
    {
        var contents = JsonSerializer.Serialize(save);
        System.IO.File.WriteAllText(Config.savedStateFilename, contents);
    }

    public SavedState RestoreSavedState()
    {
        try
        {
            var contents = System.IO.File.ReadAllText(Config.savedStateFilename);
            var savedState = JsonSerializer.Deserialize<SavedState>(contents);
            return savedState ?? new SavedState();
        }
        catch (JsonException) { }
        catch (System.IO.FileNotFoundException) { }

        return new SavedState();
    }

    public override async void Load()
    {

        //Mouse.SetRelativeMode(true);
        //Mouse.SetGrabbed(true);
        Mouse.SetVisible(false);
        //var filename = "78de88070e17b513462f962a8a481c6d.ogg";
        //var source = await AudioManager.LoadAudio(filename);
        //source.Play();

        Graphics.SetFont(state.fontAsian);

        state.atlasImage = new AtlasImage(Graphics.NewImage("assets/atlas.png"));
        state.player = new Entity(state.atlasImage, TileID.player);

        state.windowEntity.rect = new RectangleF(0, 0, Graphics.GetWidth(), Graphics.GetHeight());
        state.windowEntity.pos = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);

        state.lastDeckName = RestoreSavedState().lastDeckName;

        var loader = new LoaderView(state);
        var tasks = new List<Task>();
        var deckName = state.lastDeckName ?? "";

        //loader.OnLoad += OnFetch;
        state.SetActiveView(loader);

        Task<AnkiConnectResponse<CardInfo[]>>? cardTask = null;
        if (!string.IsNullOrEmpty(deckName))
        {
            cardTask = AnkiConnect.FetchAvailableCards(deckName);
            tasks.Add(loader.AddTask("card", cardTask));
        }

        var deckTask = AnkiConnect.FetchDecks();
        tasks.Add(loader.AddTask("decks", deckTask));

        await Task.WhenAll(tasks);

        state.deckCards[deckName] = cardTask?.Result?.value;
        state.deckNames = deckTask?.Result?.value ?? state.deckNames;


        if (!string.IsNullOrEmpty(deckName) && state.deckCards.ContainsKey(deckName))
        {
            var id = state.deckNames[deckName];
            OnSelectDeck(deckName, id);
        }
        else
        {
            OnLoadDone(state.deckNames);
        }


    }
    public override void MouseMoved(float x, float y, float dx, float dy, bool isTouch)
    {
        base.MouseMoved(x, y, dx, dy, isTouch);
        MouseHandler.DispatchMouseMove(new MouseHandler.MoveEvent(x, y, dx, dy, isTouch));
    }

    public override void MousePressed(float x, float y, int button, bool isTouch)
    {
        base.MousePressed(x, y, button, isTouch);
        MouseHandler.DispatchMousePress(new MouseHandler.ButtonEvent(x, y, (MouseButton)button, isTouch));
    }
    public override void MouseReleased(float x, float y, int button, bool isTouch)
    {
        base.MouseReleased(x, y, button, isTouch);
        MouseHandler.DispatchMouseRelease(new MouseHandler.ButtonEvent(x, y, (MouseButton)button, isTouch));
    }

    public override void JoystickGamepadAxis(Joystick joystick, GamepadAxis axis, float value)
    {
        base.JoystickGamepadAxis(joystick, axis, value);
        GamepadHandler.DispatchAxis(joystick, axis, value);
    }
    public override void JoystickGamepadPressed(Joystick joystick, GamepadButton button)
    {
        base.JoystickGamepadPressed(joystick, button);
        GamepadHandler.DispatchPress(joystick, button);
    }
    public override void JoystickGamepadReleased(Joystick joystick, GamepadButton button)
    {
        base.JoystickGamepadReleased(joystick, button);
        GamepadHandler.DispatchRelease(joystick, button);
    }

    public override void KeyReleased(KeyConstant key, Scancode scancode)
    {
        base.KeyReleased(key, scancode);
        KeyHandler.DispatchKeyRelease(key, scancode);
    }
    public override void JoystickAdded(Joystick joystick)
    {
        if (joystick.IsGamepad())
        {
            Gamepad.Add(joystick);
        }
    }
    public override void JoystickRemoved(Joystick joystick)
    {
        if (joystick.IsGamepad())
        {
            Gamepad.Remove(joystick);
        }
    }

    public override void KeyPressed(KeyConstant key, Scancode scancode, bool isRepeat)
    {
        base.KeyPressed(key, scancode, isRepeat);
        KeyHandler.DispatchKeyPress(key, scancode, isRepeat);

        if (key == KeyConstant.Escape)
        {
            Love.Event.Quit();
        }
        else if (key == KeyConstant.F12)
        {
            scriptLoader.Load();
        }
        else if (key == KeyConstant.F5)
        {
            scriptLoader.Reload();
        }
        else if (key == KeyConstant.F8)
        {
            scriptLoader.EnableDebug();
        }
    }

    public override void Update(float dt)
    {
        Callbacks.PreUpdate();

        state.center = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);
        state.centerTop = new Vector2(Graphics.GetWidth() / 2, 0);
        state.centerBottom = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight());

        if (state.uninitializedView)
        {
            state.activeView.Load();
            state.uninitializedView = false;

        }
        state.activeView.Update();
        scriptLoader.Update();

        Callbacks.Update();

        var win = state.windowEntity;
        foreach (var c in win.GetComponents())
        {
            c.Update(win);
        }

        Callbacks.PostUpdate();
    }


    public override void Draw()
    {
        Callbacks.PreDraw();

        state.activeView.Draw();
        scriptLoader.Draw();

        Callbacks.Draw();
        var win = state.windowEntity;
        foreach (var c in win.GetComponents())
        {
            c.Draw(win);
        }

        Graphics.SetFont(state.fontSmall);
        Graphics.Print(Love.Timer.GetFPS().ToString(), 20, Graphics.GetHeight() - Graphics.GetFont().GetHeight() * 1.2f);

        //Lua.Draw();
        Callbacks.PostDraw();
    }
}
