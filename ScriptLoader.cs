using System.Text.Json;
using Love;

namespace gganki_love;

public class ScriptLoader
{
    FileSystemWatcher? watcher;
    public bool enabled;
    public bool needsRecompile;

    View? script;

    string loadError = "";
    string updateError = "";
    string drawError = "";

    Color bgColor = new Color(20, 20, 20, 220);
    SharedState state;
    string filename = "Script.cs";

    float lastLoad = 0;

    bool tryReload = false;
    bool startLoad = false;
    bool startDebug = false;
    bool enableDebug = false;

    Gpr gpr;

    public ScriptLoader(SharedState state)
    {
        this.state = state;
        gpr = new Gpr();
    }

    public void Reload()
    {
        enableDebug = false;

        ClearErrors();
        try
        {
            if (script != null)
            {
                script.Unload();
                script = null;
            }

            //CSScriptLib.CSScript.Evaluator.LoadFile
            lastLoad = Love.Timer.GetTime();
            loadError = "";

            script = new Script(state);
            script.Load();
        }
        catch (Exception err)
        {
            if (enableDebug)
            {
                throw;
            }
            loadError = err.Message;
            Console.WriteLine(err.StackTrace);
        }
    }

    public void StartLoad()
    {
        startLoad = true;
        enableDebug = false;
        startDebug = false;
    }
    public void EnableDebug()
    {
        enabled = true;
        enableDebug = true;
        startDebug = true;
        startLoad = false;
        tryReload = false;
    }

    public void Load()
    {

        if (watcher != null)
        {
            enabled = !enabled;
            return;
        }



        var title = Window.GetTitle();
        Window.SetTitle("loading script");
        script = new Script(state);
        script.Load();
        lastLoad = Love.Timer.GetTime();
        Window.SetTitle(title);
        enabled = true;

        watcher = new FileSystemWatcher(".");

        watcher.NotifyFilter = NotifyFilters.LastWrite;

        watcher.Changed += (sender, e) =>
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            if (Love.Timer.GetTime() - lastLoad < 0.3)
            {
                return;
            }

            Console.WriteLine("changed: " + e.Name);

            if (e.Name != filename)
            {
                needsRecompile = true;
            }
            else
            {
                // Note: Löve API must be invoked only from the main thread
                // or else, some bugs will happen like text not rendering
                if (!enableDebug)
                {
                    tryReload = true;
                }
            }

        };

        watcher.Error += (sender, e) =>
        {
            Console.WriteLine("error: " + e.ToString());
        };

        watcher.Filter = "*.cs";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
    }

    public void ClearErrors()
    {
        loadError = "";
        updateError = "";
        drawError = "";
    }
    public bool HasError()
    {
        return loadError != "" || updateError != "" || drawError != "";
    }

    public void Update()
    {
        gpr.ResetLine();
        if (startDebug)
        {
            script?.Unload();
            script = new Script(state);
            script.Load();
            startDebug = false;
        }
        if (startLoad)
        {
            Load();
            startLoad = false;
        }
        else if (tryReload)
        {
            Reload();
            tryReload = false;
        }

        if (!enabled || HasError())
        {
            return;
        }

        try
        {
            script?.Update();
            updateError = "";
        }
        catch (Exception e)
        {
            if (enableDebug)
            {
                throw;
            }
            updateError = e.StackTrace;
            Console.WriteLine("script update error: {0}", e.Message);
        }
    }
    public void Draw()
    {
        if (!enabled)
        {
            return;
        }

        //Graphics.SetColor(bgColor);
        //Graphics.Rectangle(DrawMode.Fill, 0, 0, Graphics.GetWidth(), Graphics.GetHeight());
        Graphics.SetColor(Color.White);
        gpr.Print("# script running");
        if (needsRecompile)
        {
            gpr.Print("*** Non-script source changed, please recompile project");
        }

        var pos = new Vector2(50, 50);

        if (!string.IsNullOrEmpty(loadError))
        {
            gpr.Print("load error: " + loadError);
        }
        else if (!string.IsNullOrEmpty(updateError))
        {
            gpr.Print("update error: " + updateError);
        }
        else
        {
            try
            {
                script?.Draw();
                drawError = "";
            }
            catch (Exception e)
            {
                if (enableDebug)
                {
                    throw;
                }
                gpr.Print("draw error: " + e.StackTrace);
                Console.WriteLine("script update error: {0}", e.StackTrace);
            }
        }
    }
}

public class Gpr
{
    public Vector2 pos = new Vector2(20, 20);
    public Font font;
    float yOffset = 0;

    public Gpr()
    {
        font = Graphics.GetFont() ?? Graphics.NewFont(18);
    }

    public void ResetLine()
    {
        yOffset = 0;
    }

    public void Print(string format, params object[] args)
    {
        Graphics.SetFont(font);

        var str = args.Length > 0 ? string.Format(format, args) : format;
        Graphics.Print(str, pos.X, pos.Y + yOffset);

        yOffset += font.GetHeight() * 1.1f;
        Graphics.SetFont(null);
    }

    public void Printf(string str, float maxWidth, AlignMode align = AlignMode.Left)
    {
        Graphics.SetFont(font);

        Graphics.Printf(str, pos.X, pos.Y + yOffset, maxWidth, align);

        var lines = MathF.Floor(font.GetWidth(str) / maxWidth) + 1;
        yOffset += lines * font.GetHeight() * 1.1f;
        Graphics.SetFont(null);
    }
}

