using System.Diagnostics;
using Love;

namespace gganki_love;


record GameObject(CardInfo card, string text, Font font)
{
	public Vector2 pos { get; set; }
	public Vector2 dir { get; set; }
}


// TODO: handle other types of card models
public class GameView : View
{
	string deckName;
	SharedState state;
	List<GameObject> objects = new List<GameObject>();


	public GameView(string deckName, SharedState state)
	{
		this.deckName = deckName;
		this.state = state;
	}

	void MovePlayer()
	{
		var player = state.player;
		if (player is not null)
		{
			var vy = Vector2.Zero;
			var vx = Vector2.Zero;
			if (Keyboard.IsDown(KeyConstant.Up))
			{
				vy += Directions.Up;
			}
			else if (Keyboard.IsDown(KeyConstant.Down))
			{
				vy += Directions.Down;
			}

			if (Keyboard.IsDown(KeyConstant.Left))
			{
				vx += Directions.Left;
			}
			else if (Keyboard.IsDown(KeyConstant.Right))
			{
				vx += Directions.Right;
			}

			player.pos += vx * player.speed;
			player.pos += vy * player.speed;

			if (vx.Length() > 0)
			{
				player.dir = Vector2.Normalize(vy + vx);
			}
			else
			{
				player.dir = Vector2.Normalize(vy + player.dir * Vector2.UnitX);
			}
		}
	}

	void View.Unload()
	{

	}

	void View.Load()
	{

		CardInfo[]? cards;
		if (!state.deckCards.TryGetValue(deckName, out cards))
		{
			return;
		}

		if (cards is null) return;


		var newObjects = new List<GameObject>();
		foreach (var card in cards)
		{
			var text = card.fields?["VocabKanji"]?.value;
			if (text == null) continue;

			newObjects.Add(new GameObject(card, text, state.fontAsian));
		}

		// TODO: state.font.GetWidth() is broken
		foreach (var obj in newObjects)
		{
			var width = state.fontAsian.GetWidth("w");
			obj.pos = new Vector2(
				Random.Shared.Next(0, Graphics.GetWidth() - width * obj.text.Length),
				Random.Shared.Next(0, Graphics.GetHeight() - state.fontAsian.GetHeight())
			);
			obj.dir = Vector2.Normalize(new Vector2(
				(float)(-1.0 + Random.Shared.NextDouble() * 2.0),
				(float)(-1.0 + Random.Shared.NextDouble() * 2.0)
			));
		}
		newObjects.Sort((a, b) => Random.Shared.Next(-1, 2));
		objects = newObjects.GetRange(0, 30);
	}

	void View.Draw()
	{
		/*
		Graphics.SetFont(state.fontAsian);
		foreach (var obj in objects)
		{
			Graphics.SetColor(Color.Red);
			//Graphics.SetFont(state.fontRegular);
			Graphics.Print(obj.text, obj.pos.X, obj.pos.Y);
		}
		*/
	}

	void View.Update()
	{
		//state.player?.Update();
		//MovePlayer();
		/*
		foreach (var obj in objects)
		{
			obj.pos += obj.dir;
		}
		*/
	}
}

