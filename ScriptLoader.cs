using System.Text.Json;
using Love;
using CSScriptLib;
using CSScripting;

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

	Gpr gpr;

	public ScriptLoader(SharedState state)
	{
		this.state = state;
		gpr = new Gpr();
	}

	public void Reload()
	{
		ClearErrors();
		try
		{
			if (script != null)
			{
				script.Unload();
				script = null;
			}

			//CSScriptLib.CSScript.Evaluator.LoadFile
			var newScript = CSScript.Evaluator.LoadFile<View>(filename, state);
			newScript.Load();
			lastLoad = Love.Timer.GetTime();
			loadError = "";


			script = newScript;
		}
		catch (Exception err)
		{
			loadError = err.Message;
			Console.WriteLine(err.StackTrace);
		}
	}

	public void Load()
	{
		if (watcher != null)
		{
			enabled = !enabled;
			return;
		}

		CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;
		CSScript.Evaluator.With(eval => eval.IsCachingEnabled = true);


		var title = Window.GetTitle();
		Window.SetTitle("loading script");
		script = CSScript.Evaluator.LoadFile<View>(filename, state);
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
				tryReload = true;
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
		if (tryReload)
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
				gpr.Print("draw error: " + e.StackTrace);
				Console.WriteLine("script update error: {0}", e.StackTrace);
			}
		}
	}
}

public class Gpr
{
	Vector2 pos = new Vector2(20, 20);
	Font font;
	int lineNum = 0;

	public Gpr()
	{
		font = Graphics.GetFont() ?? Graphics.NewFont(18);
	}

	public void ResetLine()
	{
		lineNum = 0;
	}

	public void Print(string format, params object[] args)
	{
		Graphics.SetFont(font);

		var str = args.Length > 0 ? string.Format(format, args) : format;
		Graphics.Print(str, pos.X, pos.Y + lineNum * font.GetHeight() * 1.3f);

		lineNum++;
		Graphics.SetFont(null);
	}
}

