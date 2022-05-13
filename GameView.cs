using System.Diagnostics;
using Love;

namespace gganki_love;

public class Entity
{
	public Vector2 pos { get; set; }
	public Vector2 dir { get; set; }
	public float radianAngle { get; set; } = 0f;
	public float speed { get; set; } = 5f;

	public AtlasImage atlasImage { get; set; }
	public Quad quad { get; set; }
	public bool reversedX;

	public bool debug = false;
	public float scale = 3;
	public RectangleF rect = new RectangleF();

	public Color color = Color.White;

	public Entity(AtlasImage atlasImage, int tileID)
	{
		this.atlasImage = atlasImage;
		quad = atlasImage.GetQuad(tileID);

		var n = atlasImage.tileSize;
		rect.X = pos.X - n * scale / 2;
		rect.Y = pos.Y - n * scale / 2;
		rect.Width = n * scale;
		rect.Height = n * scale;
	}

	public static Entity Create(int tileID, SharedState? state = null)
	{
		if (state is null)
		{
			state = SharedState.instance;
		}
		Debug.Assert(state.atlasImage != null);
		return new Entity(state.atlasImage, tileID);
	}


	public void Update()
	{
		var n = atlasImage.tileSize;
		rect.X = pos.X - n * scale / 2;
		rect.Y = pos.Y - n * scale / 2;
		rect.Width = n * scale;
		rect.Height = n * scale;
	}


	public void Draw()
	{
		var n = atlasImage.tileSize;
		//var xt = Vector2.Dot(dir, Directions.Right) > 0 ? -1 : 1;
		var xt = 1;

		Graphics.SetColor(color);
		atlasImage.Draw(quad, pos.X, pos.Y, radianAngle, scale * xt, scale, n / 2, n / 2);

	}
}

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
				state.rand.Next(0, Graphics.GetWidth() - width * obj.text.Length),
				state.rand.Next(0, Graphics.GetHeight() - state.fontAsian.GetHeight())
			);
			obj.dir = Vector2.Normalize(new Vector2(
				(float)(-1.0 + state.rand.NextDouble() * 2.0),
				(float)(-1.0 + state.rand.NextDouble() * 2.0)
			));
		}
		newObjects.Sort((a, b) => state.rand.Next(-1, 2));
		objects = newObjects.GetRange(0, 30);
	}

	void View.Draw()
	{
		state.player?.Draw();

		Graphics.SetFont(state.fontAsian);
		foreach (var obj in objects)
		{
			Graphics.SetColor(Color.Red);
			//Graphics.SetFont(state.fontRegular);
			Graphics.Print(obj.text, obj.pos.X, obj.pos.Y);
		}
	}

	void View.Update()
	{
		state.player?.Update();
		MovePlayer();

		// TODO: weapon throwing
		// TODO: implement game stages (see notebook)
		// TODO: sort by review, learn then new des
		foreach (var obj in objects)
		{
			obj.pos += obj.dir;
		}
	}
}

