
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


    public override async void Load()
    {
        Window.SetFullscreen(true);

        Mouse.SetRelativeMode(true);
        Mouse.SetVisible(false);

        Graphics.SetFont(state.fontAsian);

        state.atlasImage = new AtlasImage(Graphics.NewImage("assets/atlas.png"));
        state.player = new Entity(state.atlasImage, TileID.player);

        state.windowEntity.rect = new RectangleF(0, 0, Graphics.GetWidth(), Graphics.GetHeight());
        state.windowEntity.pos = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);

        if (debugEnabled)
        {
            scriptLoader.EnableDebug();
        }
        else
        {
            scriptLoader.StartLoad();
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
            c.Update();
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
            c.Draw();
        }

        Graphics.SetFont(state.fontSmall);
        Graphics.Print(Love.Timer.GetFPS().ToString(), 20, Graphics.GetHeight() - Graphics.GetFont().GetHeight() * 1.2f);

        Callbacks.PostDraw();
    }
}
