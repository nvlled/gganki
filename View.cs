
using Love;

namespace gganki_love;

public enum ViewState
{
	Loading,
	DeckSelect,
	Playing,
	GameOver,
}

public interface View
{
	public void Load() { }

	public void Unload() { }
	public void Draw();
	public void Update();


}

public interface ScriptView : View
{
	public void SetState(SharedState state);
}


public class NopView : View
{
	public void Draw() { }

	public void Update() { }
}

/*
state.SetActiveView(loader);
var cards = await loader.AddTask("deck cards", fetch(deckName));
state.deckCards[deckName] = cards;
state.SetActiveView(game);

-------------------

loading decks...
loading deck cards...

*/

public class LoaderView : View
{
	public event Action<DeckNames>? OnLoad;
	//public event Action<string, CardInfo[]>? OnLoadDeck;
	//public DeckNames deckNames = new DeckNames();
	//public CardInfo[] cards = new CardInfo[0];

	//public Dictionary<ulong, CardInfo[]?> DeckCards = new Dictionary<ulong, CardInfo[]?>();

	public List<(string, bool)> queue = new List<(string, bool)>();

	SharedState state;

	public LoaderView(SharedState state)
	{
		this.state = state;
	}

	public async Task AwaitTask(string name, Task task)
	{
		var index = queue.Count();
		queue.Add((name, false));

		await task;

		if (index < queue.Count())
		{
			queue[index] = (name, true);
		}
	}

	public async Task<T> AddTask<T>(string name, Task<T> task)
	{
		var index = queue.Count();
		queue.Add((name, false));

		var result = await task;

		if (index < queue.Count())
		{
			queue[index] = (name, true);
		}

		return result;
	}

	public async void Load()
	{
		Console.WriteLine("fetching decks and cards");
		var deckNames = await LoadDecks();


		Console.WriteLine("fetch complete");
		//await Task.Delay(3000);
		if (OnLoad != null)
		{
			OnLoad(deckNames ?? new DeckNames());
		}
	}

	public async Task<CardInfo[]> LoadDeck(string deckName)
	{
		var emptyCards = new CardInfo[0];
		var cardIdResp = await AnkiConnect.FetchAvailableCardIds(deckName);
		if (cardIdResp.error != null)
		{
			return emptyCards;
		}
		var cardResp = await AnkiConnect.FetchCardInfo(cardIdResp.value ?? new ulong[0]);
		if (cardResp.error != null)
		{
			return emptyCards;
		}
		return cardResp.value ?? emptyCards;
	}

	public async Task<DeckNames?> LoadDecks()
	{
		Console.WriteLine("fetching decks");
		var response = await AnkiConnect.FetchDecks();
		if (response == null)
		{
			Console.WriteLine("Got a null response while fetching decks");
		}
		else if (response.error != null)
		{
			Console.WriteLine("error: " + response.error);
		}
		else
		{
			return response.value;
		}
		return null;
	}

	public void Draw()
	{
		var i = 0;
		var spaceY = 10;
		foreach (var (name, done) in queue)
		{
			var statusText = done ? "done" : "loading";
			Graphics.SetFont(state.fontRegular);
			Graphics.Print(statusText + " " + name + "...", 20, i * (spaceY + state.fontRegular.GetHeight()));
			i++;
		}
	}

	public void Update()
	{
	}
}

public class DeckSelectView : View
{
	public event Action<string, ulong>? OnSelect;

	SharedState state;
	int selectedIndex = 0;
	int menuWidth = 0;
	int menuHeight = 0;

	string[] names = new string[0];

	Vector2 basePos = new Vector2(10, 10);


	static class Style
	{
		public const int padding = 10;
		public static Color selectedColor = Color.CornflowerBlue;
		public static Color textColor = Color.White;
	}


	public DeckSelectView(SharedState state)
	{
		this.state = state;
	}
	public void Load()
	{
		KeyHandler.OnKeyPress += KeyPressed;

		var ns = new List<string>();
		foreach (var kv in state.deckNames)
		{
			menuWidth = Math.Max(menuWidth, state.fontAsian.GetWidth(kv.Key));
			ns.Add(kv.Key);
		}
		names = ns.ToArray();

		menuHeight = state.deckNames.Count() * (Style.padding + Config.fontSize) + Style.padding;
		basePos = new Vector2(
			Graphics.GetWidth() / 2 - menuWidth / 2,
			Graphics.GetHeight() / 2 - menuHeight / 2
		);
	}
	public void Unload()
	{
		KeyHandler.OnKeyPress -= KeyPressed;
	}

	public void KeyPressed(KeyConstant key, Scancode scancode, bool isRepeat)
	{
		switch (key)
		{
			case KeyConstant.Up:
				selectedIndex = selectedIndex != 0 ? selectedIndex - 1 : state.deckNames.Count() - 1;
				break;
			case KeyConstant.Down:
				selectedIndex = (selectedIndex + 1) % state.deckNames.Count();
				break;

			case KeyConstant.Enter:
				{
					if (OnSelect != null)
					{
						var name = names[selectedIndex];
						var id = state.deckNames[name];
						OnSelect(name, id);
					}
					break;
				}
		}
	}

	public void Draw()
	{
		Graphics.SetFont(state.fontRegular);
		var deckNames = state.deckNames;
		if (deckNames.Count() == 0)
		{
			Graphics.Print("No deck available", basePos.X, basePos.Y);
			return;
		}

		Graphics.Push();
		var i = 0;
		foreach (var kv in deckNames)
		{
			var p = Style.padding;
			Graphics.SetColor(Color.White);
			Graphics.Rectangle(DrawMode.Line, basePos.X, basePos.Y, menuWidth, menuHeight);
			Graphics.SetColor(i == selectedIndex ? Style.selectedColor : Style.textColor);
			Graphics.Print(kv.Key, p + basePos.X, p + basePos.Y + i * (32 + p));

			if (i == selectedIndex)
			{
				Graphics.Rectangle(DrawMode.Line, basePos.X, basePos.Y, menuWidth, menuHeight);
			}

			i++;
		}
		Graphics.Pop();
	}

	public void Update()
	{
	}
}
