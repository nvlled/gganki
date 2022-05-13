

using System.Text.Json;
using Love;

namespace gganki_love;

public class Program : Scene
{
	SharedState state;
	KeyHandler keyHandler = new KeyHandler();

	ScriptLoader scriptLoader;


	//FileSystemWatcher watcher;

	static void Main(string[] args)
	{
		Boot.Init(new BootConfig
		{
			WindowResizable = true,
			WindowTitle = "gganki",
		});
		Boot.Run(new Program());
	}

	public Program()
	{
		state = SharedState.instance;
		state.fontAsian = Graphics.NewFont("assets/togoshi.ttf", Config.fontSize);
		scriptLoader = new ScriptLoader(state);
		Keyboard.SetKeyRepeat(true);
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
			return;
		}

		var cardTask = AnkiConnect.FetchAvailableCards(deckName);
		var loader = new LoaderView(state);

		state.SetActiveView(loader);
		await Task.WhenAll(
			//loader.AwaitTask("delay", Task.Delay(2000)),
			loader.AddTask("deck cards", cardTask)
		);

		state.deckCards[deckName] = cardTask.Result.value;

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
		Graphics.SetFont(state.fontAsian);

		state.atlasImage = new AtlasImage(Graphics.NewImage("assets/atlas.png"));
		state.player = new Entity(state.atlasImage, TileID.player);

		state.lastDeckName = RestoreSavedState().lastDeckName;

		// TODO: remove
		state.player.pos = new Vector2(
			Graphics.GetWidth() / 2 - state.atlasImage.tileSize / 2,
			Graphics.GetHeight() / 2 - state.atlasImage.tileSize / 2
		);

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
	}

	public override void Update(float dt)
	{
		state.center = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);
		state.activeView.Update();
		//Lua.Update(dt);

		scriptLoader.Update();
	}

	public override void Draw()
	{
		state.atlasImage?.StartDraw();
		state.activeView.Draw();
		state.atlasImage?.EndDraw();

		scriptLoader.Draw();

		//Lua.Draw();
	}
}
