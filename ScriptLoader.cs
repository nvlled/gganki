using System.Text.Json;
using Love;

namespace gganki_love;

public class ScriptLoader
{
	FileSystemWatcher? watcher;
	public bool enabled;
	View? script;

	string loadError = "";
	string updateError = "";
	string drawError = "";

	Color bgColor = new Color(20, 20, 20, 220);
	SharedState state;
	string filename = "Script.cs";

	float lastLoad = 0;

	bool tryReload = false;

	public ScriptLoader(SharedState state)
	{
		this.state = state;
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

			var newScript = CSScriptLib.CSScript.RoslynEvaluator.LoadFile<View>(filename, state);
			newScript.Load();
			lastLoad = Love.Timer.GetTime();
			loadError = "";


			script = newScript;
		}
		catch (Exception err)
		{
			loadError = err.Message;
			Console.WriteLine(err.Message);
		}
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
		script = CSScriptLib.CSScript.RoslynEvaluator.LoadFile<View>(filename, state);
		script.Load();
		lastLoad = Love.Timer.GetTime();
		Window.SetTitle(title);
		enabled = true;

		watcher = new FileSystemWatcher(".");

		watcher.NotifyFilter = NotifyFilters.LastWrite;

		watcher.Changed += (sender, e) =>
		{
			if (e.Name == filename && e.ChangeType == WatcherChangeTypes.Changed)
			{
				if (Love.Timer.GetTime() - lastLoad < 0.3)
				{
					return;
				}

				Console.WriteLine("changed: " + e.Name);
				tryReload = true;
				// Note: Löve API must be invoked only from the main thread
				// or else, some bugs will happen like text not rendering
			}
		};

		watcher.Error += (sender, e) =>
		{
			Console.WriteLine("error: " + e.ToString());
		};

		watcher.Filter = filename;
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
			updateError = e.Message;
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
		Graphics.Print("script running");

		var pos = new Vector2(50, 50);

		if (!string.IsNullOrEmpty(loadError))
		{
			Graphics.Print("load error: " + loadError, pos.X, pos.Y);
		}
		else if (!string.IsNullOrEmpty(updateError))
		{
			Graphics.Print("update error: " + updateError, pos.X, pos.Y);
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
				Graphics.Print("draw error: " + e.Message, pos.X, pos.Y);
				Console.WriteLine("script update error: {0}", e.Message);
			}
		}
	}
}

