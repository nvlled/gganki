
using Love;
using System;
using gganki_love;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AwaitableCoroutine;

public class Script : View
{
	//List<Monster> monsters = new List<Monster> { };
	List<View> subScripts = new List<View> { };

	System.Random rand = new System.Random();
	public SharedState state;

	//WeaponEntity sword;

	//GAxis axisLeft = new GAxis(GAxisSide.Left, 50) { pos = new Vector2(100, Graphics.GetHeight() - 100) };
	//GAxis axisRight = new GAxis(GAxisSide.Right, 50) { pos = new Vector2(Graphics.GetWidth() - 100, Graphics.GetHeight() - 100) };

	//Plotter plotter;

	Player testPlayer;
	Camera cam;
	World world;
	MonsterGroup monsters;

	GameWithCoroutines game;


	public Script(SharedState state)
	{
		this.state = state;
		subScripts = new List<View> { };

		game = new GameWithCoroutines(state);
	}

	public async Coroutine Test()
	{
		await Coroutine.DelayCount(60 * 2);
		Console.WriteLine("sub coroutine started?");
		await Coroutine.DelayCount(60 * 2);
		Console.WriteLine("sub coroutine done");
	}

	public void Load()
	{

		game.Load();

		/*
		for (var i = 0; i < 20; i++)
		{
			var text = cards?[i]?.fields?["VocabKanji"]?.value ?? defaultText;
			var mon = new Monster(TileID.RandomMonsterID(), text, state.fontAsian);
			mon.pos = new Vector2(
				state.rand.Next(-300, Graphics.GetWidth() + 300),
				 -20
			);
			mon.target = testPlayer.entity;
			monsters.Add(mon);
		}
		*/



		GamepadHandler.OnAxis += OnGamepadAxis;
		GamepadHandler.OnPress += OnGamepadPress;
	}

	public void OnGamepadAxis(Joystick _, GamepadAxis axis, float value)
	{
	}

	public void OnGamepadPress(Joystick _, GamepadButton button)
	{
	}

	public void Unload()
	{
		Console.WriteLine("unload script");
		game.Unload();
		GamepadHandler.OnAxis -= OnGamepadAxis;
		GamepadHandler.OnPress -= OnGamepadPress;

		state.windowEntity.ClearComponents();
	}

	public void Update()
	{

		UpdateSubScripts();
		GAxis.left.Update();
		GAxis.right.Update();

		GAxis.right.buildMomentum = Gamepad.IsDown(GamepadButton.LeftShoulder)
				 	 || Keyboard.IsDown(KeyConstant.LShift);

		game.Update();
	}

	public void Draw()
	{
		DrawSubScripts();

		game.Draw();

		GAxis.left.Draw();
		GAxis.right.Draw();

	}

	public void UpdateSubScripts()
	{
		foreach (var s in subScripts) s.Update();
	}
	public void DrawSubScripts()
	{
		foreach (var s in subScripts) s.Draw();
	}

	public Vector2 vec(float x, float y) { return new Vector2(x, y); }


}

public class Wut
{
	public static void huh(params object[] thingies)
	{
		var i = 0;
		foreach (var o in thingies)
		{
			Console.WriteLine("{0}) huh {1}", i + 1, o);
			i++;
		}
	}
}

public class WeaponEntity
{
	public enum State { OnHand, Throwing, Returning }
	class ThrowingData
	{
		public const float maxSteps = 25;
		public const float haltDelay = 2;

		public float steps = 0;

		public float haltElapsed = 0;
	}
	class ReturningData
	{
		public const float maxAccel = 80;
		public const float maxDelay = 0.5f;
		public float accel = 0;
		public float delay = 0;

	}

	public Entity entity;

	public Entity? holder;
	public Vector2 holdOffset;

	Vector2 handlePoint;
	Vector2 endPoint;
	Vector2 tileDir;

	float shiftAngle;
	public bool enabled = true;

	Vector2 lastEndPos = new Vector2();
	Vector2 velocity = new Vector2();

	public Vector2 pos
	{
		get { return entity.pos; }
		set { entity.pos = value; }
	}
	public Vector2 dir
	{
		get { return entity.dir; }
		set { entity.dir = value; }
	}


	State logicState = State.OnHand;
	ThrowingData throwing = new ThrowingData();
	ReturningData returning = new ReturningData();

	public WeaponEntity(Entity entity, Vector2 handlePoint, Vector2 endPoint)
	{
		this.entity = entity;
		this.handlePoint = handlePoint;
		this.endPoint = endPoint;
		this.tileDir = Vector2.Normalize(endPoint - handlePoint);

		var p = Polar.FromVector(this.tileDir);
		if (!float.IsNaN(p.angle))
		{
			shiftAngle = p.angle - MathF.PI / 2;
		}
		else
		{
			shiftAngle = MathF.PI / 2;
		}

		lastEndPos = GetEndPos();
	}
	public Vector2 GetRotatedDir()
	{
		var p = Polar.FromVector(this.tileDir);
		p.angle += entity.radianAngle;
		return Polar.ToVector(p);
	}

	public Vector2 GetHandlePos()
	{
		return entity.pos + GetRotatedDir() * new Vector2(entity.rect.Width / 2, entity.rect.Height / 2);
	}
	public Vector2 GetEndPos()
	{
		return entity.pos + -GetRotatedDir() * new Vector2(entity.rect.Width / 2, entity.rect.Height / 2) * 1.3f;
	}

	public Vector2 GetNewCenterPos(Vector2 handlePos)
	{
		return handlePos + -GetRotatedDir() * new Vector2(entity.rect.Width / 2, entity.rect.Height / 2);
	}

	public void SetHandlePosition(Vector2 newHandlePos)
	{
		entity.pos = GetNewCenterPos(newHandlePos);
	}

	public void AttachTo(Entity holder, Vector2 offset)
	{
		this.holder = holder;
		this.holdOffset = offset;
	}

	public void RotateAt(Vector2 dir)
	{
		entity.dir = dir;
		entity.radianAngle = shiftAngle + Polar.FromVector(dir).angle;
	}

	public void PointAt(Vector2 dir)
	{
		if (logicState == State.OnHand)
		{
			var oldPos = GetHandlePos();
			RotateAt(dir);
			SetHandlePosition(oldPos);
		}
		else if (logicState == State.Throwing)
		{
			//RotateAt(dir);
		}
	}
	public void DoAction()
	{
		switch (logicState)
		{
			case State.OnHand: Throw(); break;
			case State.Throwing: HaltThrow(); break;
		}
	}

	public void HaltThrow()
	{
		if (throwing.steps < ThrowingData.maxSteps)
		{
			throwing.steps = ThrowingData.maxSteps;
		}
		else if (throwing.haltElapsed > 0.2f)
		{
			Return();
		}
	}

	public void Throw()
	{
		if (logicState == State.OnHand)
		{
			logicState = State.Throwing;
			throwing.steps = 0;
			throwing.haltElapsed = 0;
		}
	}
	public void Return()
	{
		logicState = State.Returning;
		returning.accel = 1;
		returning.delay = 0;
	}

	public Ray2D GetRotatedRay()
	{
		var start = GetHandlePos();
		return new Ray2D(start, GetEndPos() - start);
	}


	public void Draw()
	{
		if (!enabled)
		{
			return;
		}

		entity.Draw();


		//var start = GetHandlePos();
		//var end = start + GetRotatedDir() * this.tileDir.Length();
		//var end = GetEndPos();
		//var r = GetRotatedRay();
		//Graphics.SetColor(Color.Red);
		//Graphics.Line(r.Original, r.Original + r.Direction);
	}

	public bool UpdateThrowing()
	{
		if (throwing.steps < ThrowingData.maxSteps)
		{
			entity.pos += entity.dir * throwing.steps * 10;
			throwing.steps++;
		}
		if (throwing.steps >= ThrowingData.maxSteps)
		{
			throwing.haltElapsed += Love.Timer.GetDelta();
			entity.radianAngle += 30 * MathF.PI / 180;
			if (entity.radianAngle > MathF.PI)
			{
				entity.radianAngle = 0;
			}
		}

		if (throwing.haltElapsed > ThrowingData.haltDelay)
		{
			Return();
		}

		return true;
	}

	public bool UpdateReturning()
	{
		if (holder is null) { return false; }

		var v = holder.pos - entity.pos;
		if (returning.delay >= ReturningData.maxDelay)
		{
			logicState = State.OnHand;
			return true;
		}
		if (v.Length() < 50)
		{
			entity.radianAngle += 50 * MathF.PI / 180;
			if (entity.radianAngle > MathF.PI)
			{
				entity.radianAngle = 0;
			}
			returning.delay += Love.Timer.GetDelta();
			return true;
		}

		var dir = Vector2.Normalize(v);
		RotateAt(-dir);
		entity.pos += dir * returning.accel;

		if (returning.accel < ReturningData.maxAccel)
		{
			returning.accel += 10;
		}

		return true;
	}

	public bool UpdateOnHand()
	{
		if (holder is null) { return false; }

		var offset = new Vector2(
			holder.flipX * holdOffset.X * holder.rect.Width / 2,
			holder.flipY * holdOffset.Y * holder.rect.Height / 2
		);
		SetHandlePosition(holder.pos + offset);
		return true;
	}


	public void Update()
	{
		_ = logicState switch
		{
			State.OnHand => UpdateOnHand(),
			State.Throwing => UpdateThrowing(),
			State.Returning => UpdateReturning(),
			_ => false,
		};

		entity.Update();
		var endPos = GetEndPos();
		velocity += (endPos - lastEndPos);
		velocity *= 0.5f;
		lastEndPos = endPos;
	}

	public bool HasHit(Monster m)
	{
		if (velocity.Length() < 20)
		{
			return false;
		}
		return m.rect.Contains(GetHandlePos()) || m.rect.Contains(GetEndPos()) || m.rect.Contains(entity.pos);
	}


	public static WeaponEntity RandomWeapon()
	{

		var i = Random.Shared.Next(0, TileID.weaponsTR.Length);
		var id = TileID.weaponsTR[i];
		var entity = Entity.Create(id);
		return new WeaponEntity(entity, new Vector2(1, 0), new Vector2(0, 1));
	}
}

public enum GAxisSide
{
	Left,
	Right
}
public class GAxis
{
	public static GAxis left;
	public static GAxis right;

	static GAxis()
	{
		//left = new GAxis(GAxisSide.Left) { noDraw = true };
		//right = new GAxis(GAxisSide.Right) { noDraw = true };
		left = new GAxis(GAxisSide.Left, 50) { pos = new Vector2(100, Graphics.GetHeight() - 100) };
		right = new GAxis(GAxisSide.Right, 50) { pos = new Vector2(Graphics.GetWidth() - 100, Graphics.GetHeight() - 100) };

		left.enableKeyboard = true;
		right.enableMouse = true;
	}

	GAxisSide side;
	public Vector2 activeDir;
	public Vector2 passiveDir;
	public Vector2 movingDir;

	float momentum;
	public bool buildMomentum = false;
	public bool noDraw = false;

	public Vector2 pos;
	public float radius;

	float pointing = 0;

	public KeyConstant Up = KeyConstant.W;
	public KeyConstant Down = KeyConstant.S;
	public KeyConstant Right = KeyConstant.A;
	public KeyConstant Left = KeyConstant.D;

	public bool enableMouse = false;
	public bool enableKeyboard = false;

	public GAxis(GAxisSide side, float radius = 50)
	{
		this.radius = radius;
		this.side = side;
		this.pos = Vector2.Zero;
		this.activeDir = Vector2.Zero;
		this.passiveDir = Vector2.UnitY;
		this.movingDir = Vector2.UnitY;
	}

	public void Update()
	{
		Joystick? gamepad = null;
		foreach (var js in Joystick.GetJoysticks())
		{
			if (js.IsGamepad() && js.IsConnected())
			{
				gamepad = js;
			}
		}

		float axisX = 0;
		float axisY = 0;


		if (gamepad != null)
		{

			var (xType, yType) = side == GAxisSide.Left
				? (GamepadAxis.LeftX, GamepadAxis.LeftY)
				: (GamepadAxis.RightX, GamepadAxis.RightY);

			axisX = gamepad.GetGamepadAxis(xType);
			axisY = gamepad.GetGamepadAxis(yType);
		}

		if (axisX + axisY == 0)
		{
			if (enableMouse)
			{
				var v = Mouse.GetPosition() - Mouse.GetPreviousPosition();
				if (v.Length() > 0)
				{
					var w = Vector2.Normalize(Mouse.GetPosition() - SharedState.self.center);
					//var w = Vector2.Normalize(Mouse.GetPosition() - Mouse.GetPreviousPosition());
					axisX = w.X;
					axisY = w.Y;
				}
			}


			if (enableKeyboard)
			{
				if (Keyboard.IsDown(Up))
				{
					axisY = -1;
				}
				else if (Keyboard.IsDown(Down))
				{
					axisY = 1;
				}
				if (Keyboard.IsDown(Right))
				{
					axisX = -1;
				}
				else if (Keyboard.IsDown(Left))
				{
					axisX = 1;
				}
			}
		}

		activeDir = new Vector2(axisX, 0) + Vector2.UnitY * activeDir;
		activeDir = new Vector2(0, axisY) + Vector2.UnitX * activeDir;


		var prevPassiveDir = passiveDir;
		if (axisX != 0 || axisY != 0)
		{
			passiveDir = new Vector2(axisX, 0) + Vector2.UnitY * passiveDir;
			passiveDir = new Vector2(0, axisY) + Vector2.UnitX * passiveDir;
		}
		passiveDir.Normalize();

		var dir = Polar.GetAngle(passiveDir) - Polar.GetAngle(prevPassiveDir);
		if (MathF.Abs(dir) > MathF.PI / 2 && MathF.Abs(dir) < MathF.PI)
		{
			dir = 0;
		}
		else if (activeDir.Length() > 0 && MathF.Abs(dir) > MathF.PI)
		{
			dir += -MathF.Sign(dir) * MathF.PI * 2;
		}

		if (activeDir.Length() > 0 && MathF.Abs(dir) < MathF.PI / 180)
		{
			pointing += Love.Timer.GetDelta();
		}
		else
		{
			pointing = 0;
		}




		movingDir = movingDir + activeDir * 0.5f;
		movingDir.Normalize();

		if (buildMomentum)
		{
			if (pointing > 0.5f)
			{
				momentum = 0;
			}
			momentum += dir;
			momentum *= 0.99f;
			movingDir = Polar.Rotate(momentum / 5, movingDir);
		}

	}

	public void Draw()
	{
		if (noDraw)
		{
			return;
		}

		//Graphics.SetColor(Color.White);
		//Graphics.Circle(DrawMode.Line, pos, radius);
		Graphics.SetColor(Color.DarkRed);
		Graphics.Circle(DrawMode.Fill, pos, radius);


		Graphics.Push();
		Graphics.SetLineWidth(12);
		Graphics.SetColor(Color.Green);
		Graphics.Line(pos, pos + activeDir * radius);

		Graphics.SetLineWidth(4);
		Graphics.SetColor(Color.Blue);
		Graphics.Line(pos, pos + passiveDir * radius);

		Graphics.SetLineWidth(2);
		Graphics.SetColor(Color.Yellow);
		Graphics.Line(pos, pos + movingDir * radius);

		Graphics.Pop();
	}
}
public class EntityUtil
{
	public static Vector2 RotateByPoint(Vector2 point, Vector2 pivot, float angle)
	{
		var p = Vector2.RotateRadian(point - pivot, angle);
		return pivot + p;
	}
	public static void RotateEntityByPoint(Entity entity, Vector2 coord, float angle)
	{
		var oldPos = entity.pos;
		var pivot = GetAbsolutePosition(entity, coord);
		var pos = RotateByPoint(entity.pos, pivot, angle);
		entity.pos = pos;
		entity.radianAngle = angle;
		var pivot2 = GetAbsolutePosition(entity, coord);
	}

	public static Vector2 GetAbsolutePosition(Entity entity, Vector2 coord)
	{
		var r = entity.rect;
		return new Vector2(
			r.X + r.Width * coord.X,
			r.Y + r.Height * coord.Y
		);
	}
}

/*
public class Plotter
{
	Entity entity;
	Vector2 point1;
	Vector2 point2;
	Vector2 point3;
	public bool enabled = true;

	SharedState state;
	float angle = 0;

	public Plotter(Entity entity, SharedState state)
	{
		this.entity = entity;
		this.state = state;

		//this.entity.radianAngle = 0.50f;

		var p = state.player;
		p.pos = state.center;

		var topRight = EntityUtil.GetAbsolutePosition(state.player, new Vector2(0, 1));
		point3 = EntityUtil.RotateByPoint(state.center, topRight, -0.30f);
	}

	public void Update()
	{
		if (!enabled)
		{
			return;
		}

		var p = state.player;
		var topRight = EntityUtil.GetAbsolutePosition(state.player, new Vector2(1.0f, 0.0f));
		point3 = EntityUtil.RotateByPoint(state.center, state.center + new Vector2(p.rect.Width / 2, -p.rect.Height / 2), angle);
		state.player.pos = point3;
		state.player.radianAngle = angle;
		//angle += 0.01005f;



		if (Mouse.IsDown(0) && entity.rect.Contains(Mouse.GetPosition()))
		{
			point1 = Mouse.GetPosition();
			point2 = EntityUtil.RotateByPoint(point1, entity.rect.Center, entity.radianAngle);

		}
	}

	public void Draw()
	{
		if (!enabled)
		{
			return;
		}

		var center = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);
		Graphics.SetColor(Color.White);
		Graphics.Circle(DrawMode.Fill, center, 10);

		Graphics.SetColor(Color.Red);
		Graphics.Circle(DrawMode.Fill, point1, 10);

		Graphics.SetColor(Color.Blue);
		Graphics.Circle(DrawMode.Fill, point2, 7);

		Graphics.SetColor(Color.Orange);
		Graphics.Circle(DrawMode.Fill, point3, 12);

		Graphics.SetColor(Color.White);
		Graphics.Rectangle(DrawMode.Line, entity.rect);

		var r = entity.rect;
		Graphics.Push();
		Graphics.SetColor(Color.Yellow);
		Graphics.Translate(entity.rect.Center);
		Graphics.Rotate(entity.radianAngle);
		Graphics.Line(
			new Vector2(r.Left, r.Top) - r.Center,
			new Vector2(r.Right, r.Top) - r.Center,
			new Vector2(r.Right, r.Bottom) - r.Center,
			new Vector2(r.Left, r.Bottom) - r.Center,
			new Vector2(r.Left, r.Top) - r.Center
		);
		Graphics.Pop();
	}
}
*/


public enum CardTextType
{
	Vocab,
	Example
}

public class MonsterGroup
{
	public event Action<Monster> OnKill = (m) => { };
	public event Action<Monster> OnHit = (m) => { };


	HashSet<Monster> killedMonsters = new HashSet<Monster>();
	HashSet<Monster> hitMonsters = new HashSet<Monster>();

	PartitionedList<Monster> monsters = new PartitionedList<Monster>(SharedState.self.worldTileSize);

	Entity? defaultTarget;
	World? world;

	public MonsterGroup() { }
	public MonsterGroup(World world, Entity target)
	{
		this.world = world;
		defaultTarget = target;
	}

	private void OnMonsterKill(Monster m)
	{
		killedMonsters.Add(m);
		// TODO: or Frame.RunEnd(() => OnKill(m));
	}

	private void OnMonsterHit(Monster m)
	{
		hitMonsters.Add(m);
	}

	public Monster SpawnMonster(string text, int? tileIDArg = null)
	{
		var tileID = tileIDArg.GetValueOrDefault(TileID.RandomMonsterID());
		//var (text, audio) = card.GetContents(type);
		var font = JP.HasJapanese(text) ? SharedState.self.fontAsian : SharedState.self.fontRegular;
		var mon = new Monster(tileID, text ?? "", font);

		mon.pos = world != null ? Xt.Vector2.Random(world.Width, world.Height) : Xt.Vector2.Random();
		mon.group = this;
		mon.OnMonsterKill += OnMonsterKill;
		mon.OnMonsterHit += OnMonsterHit;
		mon.target = defaultTarget;

		monsters.Add(mon);

		return mon;
	}


	//public Monster SpawnMonster(CardInfo card, CardInfo.ContentType type)
	//{
	//	var (text, audio) = card.GetContents(type);
	//	var font = SharedState.self.fontAsian;
	//	var mon = new Monster(TileID.RandomMonsterID(), text ?? "", font);

	//	mon.OnMonsterKill += OnMonsterKill;
	//	mon.group = this;
	//	mon.audioFilename = audio ?? "";
	//	mon.card = card;
	//	mon.pos = Xt.Vector2.Random();
	//	//mon.OnMonsterHit += OnMonsterHit;// TODO

	//	return mon;
	//}

	public void Clear()
	{
		monsters.Clear();
	}

	public void SpawnFromCards(List<CardInfo> cards)
	{
		var font = SharedState.self.fontAsian;
		foreach (var card in cards)
		{
			var text = card?.GetField("VocabKanji") ?? "";
			var audioFilename = card?.GetField("VocabAudio") ?? "";
			var damageText = card?.GetField("VocabDef") ?? "";
			var mon = SpawnMonster(text);

			mon.card = card;
			mon.audioFilename = audioFilename;
			mon.damageText = damageText;
			mon.target = defaultTarget;
		}
	}

	public void AddAll(IEnumerable<Monster> newMonsters)
	{
		foreach (var mon in newMonsters)
		{
			monsters.Add(mon);
			mon.group = this;
		}
	}
	public void Add(Monster mon)
	{
		monsters.Add(mon);
		mon.group = this;
	}

	public IEnumerable<Monster> Iterate()
	{
		return monsters.Iterate();
	}

	public IEnumerable<Monster> GetMonstersAt(RectangleF r)
	{
		return monsters.GetItemsAt(
			new Vector2(r.Left, r.Top),
			new Vector2(r.Left, r.Bottom),
			new Vector2(r.Right, r.Top),
			new Vector2(r.Right, r.Bottom)
		);
	}

	public IEnumerable<Monster> GetMonstersAt(params Vector2[] pos)
	{
		return monsters.GetItemsAt(pos);
	}

	public void Update()
	{
		foreach (var m in monsters.Iterate())
		{
			var p = m.pos;
			m.Update();
			monsters.Move(m);
		}

		if (killedMonsters.Count() > 0)
		{
			foreach (var m in killedMonsters)
			{
				OnKill(m);
				killedMonsters.Remove(m);
			}
			//killedMonsters.Clear();
		}
		if (hitMonsters.Count() > 0)
		{
			foreach (var m in hitMonsters)
			{
				OnHit(m);
				hitMonsters.Remove(m);
			}
			//hitMonsters.Clear();
		}
	}

	public void Draw()
	{
		foreach (var m in monsters.Iterate())
		{
			m.Draw();
		}
	}

	public void Dispose()
	{
		foreach (var m in monsters.Iterate()) m.Dispose();
		monsters = new PartitionedList<Monster>(monsters.partitionSize);
	}

	public void Remove(Monster m)
	{
		monsters.Remove(m);
	}

	public Monster? GetRandom(Monster? except = null)
	{
		for (var tries = 0; tries < 5; tries++)
		{
			foreach (var m in monsters.Iterate())
			{
				if (except == m)
				{
					continue;
				}
				if (Random.Shared.NextSingle() < 0.3f)
				{
					return m;
				}
			}
		}
		return null;
	}
}

/*
class Monster;

void Update() {
	logicState.Update();
}

*/


public class Monster : IPos
{
	public event Action<Monster> OnMonsterKill = (e) => { };
	public event Action<Monster> OnMonsterHit = (e) => { };

	public enum State { Exploring, Fleeing, Approaching, Attacked, Dead }
	public struct Attacked
	{
		public float elapsed;
		public float numBlinks;
		public Vector2 dir;
		public State? prevState;

		public Vector2 popoverOffset;
	}

	public struct Approaching
	{
		public Vector2 dir;
		public float pause;
		public SortedSet<Monster> followers;

		public Vector2 diversionPoint;
		public int steps;
	}
	public struct Fleeing
	{
		public float time;
		public Vector2 dir;
	}
	public struct Dead
	{
		public float elapsed;
	}

	public Entity entity;
	public Entity? target;
	public string text;
	public Color? textColor;
	public string audioFilename;
	public string damageText;

	Text textObject;

	public MonsterGroup? group;

	public Attacked attacked = new Attacked();
	public Approaching approaching = new Approaching();
	public Fleeing fleeing = new Fleeing();
	public Dead dead = new Dead();
	public State logicState = State.Approaching;

	public CardInfo? card;

	int seed = Random.Shared.Next(0, 100);

	public float health = 100;
	public float defense = 1;

	public float speed
	{
		get { return entity.speed; }
		set { entity.speed = value; }
	}

	public Vector2 pos
	{
		get { return entity.pos; }
		set { entity.pos = value; }
	}
	//public Vector2 lastPos { get; set; }

	public RectangleF rect
	{
		get { return entity.rect; }
	}


	public Monster(int tileID, string text, Font? font = null)
	{
		entity = Entity.Create(tileID);
		speed = (float)(1 + Random.Shared.NextDouble() * 2);
		this.text = text;

		font ??= Graphics.GetFont();
		this.textObject = Graphics.NewText(font, text);
	}

	public Monster()
	{
		var font = Graphics.GetFont();
		this.entity = Entity.Create(0);
		this.textObject = Graphics.NewText(font, "");
	}

	public bool IsPaused()
	{
		return logicState != State.Approaching || approaching.pause > 0;
	}

	public void Pause(float seconds)
	{
		if (logicState == State.Approaching)
		{
			approaching.pause = seconds;
		}
	}

	int RandomSign()
	{
		return Random.Shared.Next(0, 2) == 1 ? 1 : -1;
	}

	public bool HasMonstersAround(Monster m)
	{
		//Console.WriteLine("huh: {0}, {1}, {2}", pos, m.pos, Vector2.Distance(pos, m.pos));
		return Vector2.Distance(pos, m.pos) < rect.DiagonalLength() * 2.0f;
	}

	public bool DivertFromCollision()
	{
		if (group == null)
		{
			return false;
		}

		var distanceFromTarget = Vector2.Distance(pos, (target?.pos) ?? Vector2.Zero);
		var hasCollision = false;

		if (distanceFromTarget > 900)
		{
			return false;
		}

		foreach (var m in group.GetMonstersAt(rect))
		{
			if (m == this)
			{
				continue;
			}
			if (!HasMonstersAround(m))
			{
				return false;
			}

			var v = m.pos - pos;
			if (v.Length() == 0)
			{
				v.X = (0.1f + Random.Shared.NextSingle()) * RandomSign();
				v.Y = (0.1f + Random.Shared.NextSingle()) * RandomSign();
			}
			var dir = Vector2.Normalize(v);
			pos += -dir * entity.speed * 0.31f;
			hasCollision = true;
		}

		return hasCollision;
	}


	public void UpdateFleeing()
	{
		fleeing.time -= Love.Timer.GetDelta();
		pos += fleeing.dir * entity.speed * 1.2f;
		if (fleeing.time <= 0)
		{
			logicState = State.Approaching;
			fleeing.time = 0;
		}
	}

	public void UpdateAttacked()
	{
		if (logicState == State.Attacked)
		{
			if (attacked.numBlinks > 2)
			{
				approaching.pause = 0;
				logicState = attacked.prevState ?? State.Approaching;
				entity.color = Color.White;
			}
			else if (attacked.elapsed == 0 || attacked.elapsed > .50f)
			{
				var entityColor = (attacked.numBlinks * 10) % 2 == 0 ? Color.Red : Color.White;
				attacked.elapsed = 0;
				attacked.numBlinks += 0.5f;
				entity.color = entityColor;
			}
			pos += -attacked.dir * 1.5f;
			attacked.elapsed += Love.Timer.GetDelta();
			attacked.popoverOffset += attacked.dir * 0.3f; ;
		}

	}

	public void UpdateApproach()
	{

		if (target == null)
		{
			return;
		}

		if (approaching.pause > 0)
		{
			approaching.pause -= Love.Timer.GetDelta();
		}

		var targetVec = target.pos - pos;
		if (targetVec.Length() > 500)
		{
			var point = approaching.diversionPoint;
			if (approaching.steps % 200 == 0 || Vector2.Distance(point, pos) < 10)
			{
				var n = Mathf.Random(800, 1900) * RandomSign();
				approaching.diversionPoint = target.pos + Vector2.Normalize(Vector2.Rotate(targetVec, 90)) * n;
			}
			approaching.steps++;
			if (approaching.steps > 1000)
			{
				approaching.steps = 0;
			}

			pos += Vector2.Normalize(point - pos) * entity.speed * 1.2f;
		}
		else
		{
			if (!DivertFromCollision())
			{
				pos += Vector2.Normalize(targetVec) * entity.speed;
			}
		}


	}

	public void Update()
	{
		entity.Update();
		if (logicState == State.Approaching)
		{
			UpdateApproach();
		}
		else if (logicState == State.Attacked)
		{
			UpdateAttacked();
		}
		else if (logicState == State.Fleeing)
		{
			UpdateFleeing();
		}

		if (logicState != State.Dead && health <= 0)
		{
			logicState = State.Dead;
			OnMonsterKill(this);
		}
	}

	public void Draw()
	{
		entity.Draw();
		//Graphics.Rectangle(DrawMode.Line, entity.rect);


		var t = textObject;
		var w = t.GetWidth();
		var h = t.GetHeight();
		var x = entity.rect.Center.X - w / 2;
		var y = entity.rect.Top - h * 3 / 4;
		var r = new RectangleF(x, y, w, h);

		Graphics.SetColor(IsAlive() ? Color.White : Color.WhiteSmoke);
		Graphics.SetFont(SharedState.self.fontAsian);

		Graphics.SetColor(textColor ?? entity.color);
		Graphics.Draw(textObject, x, y);

		Graphics.SetColor(entity.color);
		if (logicState != State.Dead)
		{
			var healthH = 5;
			Graphics.SetColor(Color.Teal);
			Graphics.Rectangle(DrawMode.Fill, entity.rect.Left, entity.rect.Bottom, (entity.rect.Width * health) / 100, healthH);
			Graphics.SetColor(Color.SkyBlue);
			Graphics.Rectangle(DrawMode.Line, entity.rect.Left, entity.rect.Bottom, entity.rect.Width, healthH);
		}


		if (logicState == State.Attacked && damageText != null && health < 50)
		{
			var c = pos + attacked.popoverOffset;
			Graphics.SetColor(Color.WhiteSmoke);
			Graphics.SetFont(SharedState.self.fontTiny);
			Graphics.Print(damageText, c.X, c.Y);
			Graphics.SetFont();
		}
		else if (!IsAlive())
		{
			var c = pos;
			Graphics.SetColor(Color.WhiteSmoke);
			Graphics.SetFont(SharedState.self.fontSmall);
			Graphics.Print(damageText, c.X, c.Y);
			Graphics.SetFont();
		}
	}
	public bool CanDamage()
	{
		return logicState != State.Dead;
	}

	public void Flee(float seconds = 2)
	{
		logicState = State.Fleeing;
		fleeing.time += seconds;
		if (target != null)
		{
			var angle = Random.Shared.Next(-45, 45);
			fleeing.dir = Polar.Rotate(angle * MathF.PI / 180, Vector2.Normalize(pos - target.pos));
		}
	}

	public void Hit(Vector2? weapon = null)
	{
		if (logicState == State.Attacked || logicState == State.Dead || logicState == State.Fleeing)
		{
			return;
		}

		if (health > 0)
		{
			OnMonsterHit(this);
		}
	}

	public void Attack(WeaponEntity? weapon = null, float damage = 1)
	{
		var attackDir = Vector2.Zero;
		var attackPos = weapon?.pos ?? target?.pos;
		if (attackPos.HasValue)
		{
			attackDir = Vector2.Normalize(attackPos.GetValueOrDefault() - pos);
		}
		else
		{
			attackDir = Vector2.Normalize(new Vector2(
				Random.Shared.Next(1, 2),
				Random.Shared.Next(1, 2)
			));
		}

		Attack(attackDir, damage);

	}
	public void Attack(Vector2 attackDir, float damage = 1)
	{
		if (health > 0)
		{
			health -= damage / defense;
			if (health <= 0)
			{
				health = 0;
				dead.elapsed = 0;
				//logicState = State.Dead;
				entity.color = Color.DarkKhaki;
				entity.radianAngle = RandomSign() * MathF.PI / 4;

				return;
			}
		}

		attacked.prevState = logicState;
		logicState = State.Attacked;
		attacked.numBlinks = 0;
		attacked.elapsed = 0;
		attacked.popoverOffset = Vector2.Zero;
		attacked.dir = attackDir;

	}

	public void Dispose()
	{
		textObject.Clear();
		textObject.Dispose();
	}

	public bool IsAlive()
	{
		return health > 0;
	}

	public Monster Clone()
	{
		var m = new Monster(this.entity.tileID, this.text);
		m.pos = pos;
		//m.entity.rect = rect;
		m.logicState = logicState;
		m.speed = speed;
		m.group = group;
		m.card = card;
		m.target = target;

		m.health = 100;
		m.textObject = Graphics.NewText(textObject.GetFont(), m.text);
		m.logicState = State.Approaching;

		return m;
	}

	public float GetTextSize(Font? font = null)
	{
		font ??= textObject.GetFont();
		return font.GetWidth(text);
	}
}


public class Player
{
	enum State { Normal, Dashing }
	class Dashing
	{
		public const float maxCooldown = 0.5f;
		public const float maxDistance = 35f;
		public const float startingStepSize = 10;


		public float steps = 0;
		public float stepSize = 0f;
		public float cooldown = 0f;
		public Vector2 dir = Vector2.Zero;
	}

	// -----------------------------------

	public Vector2 pos
	{
		get { return entity.pos; }
		set { entity.pos = value; }
	}
	public Vector2 dir
	{
		get { return entity.dir; }
		set { entity.dir = value; }
	}
	public RectangleF rect
	{
		get { return entity.rect; }
	}


	public Entity entity;
	public WeaponEntity sword;

	State logicState = State.Normal;
	Dashing dashing = new Dashing();

	public float health = 100;
	public float damageElapse = 0;


	public Player()
	{

		var i = Random.Shared.Next(0, TileID.players.Length);
		entity = Entity.Create(TileID.players[i]);
		sword = WeaponEntity.RandomWeapon();

		//sword.AttachTo(entity, new Vector2(-50, 0));
		sword.AttachTo(entity, new Vector2(-0.5f, 0.1f));
		sword.entity.scale = 2;
	}

	public void UpdateCharacter()
	{
		if (logicState == State.Normal)
		{
			entity.pos += entity.speed * GAxis.left.activeDir;
			dashing.cooldown += Love.Timer.GetDelta();
		}
		else if (logicState == State.Dashing)
		{
			entity.pos += dashing.dir * dashing.steps * dashing.stepSize;
			dashing.steps += dashing.stepSize;

			if (dashing.stepSize > 1)
			{
				dashing.stepSize -= 1.5f;
				if (dashing.stepSize <= 0)
				{
					dashing.stepSize = 1;
				}
			}

			if (dashing.steps >= Dashing.maxDistance)
			{
				logicState = State.Normal;
			}
		}
	}
	public void UpdateWeapon()
	{
		sword.Update();
		sword.PointAt(GAxis.right.movingDir);
		GAxis.right.buildMomentum = Gamepad.IsDown(GamepadButton.LeftShoulder);
	}
	public void DoAction1()
	{
		sword.DoAction();
	}
	public void DoAction2()
	{
		var canDash = dashing.cooldown >= Dashing.maxCooldown && dir.Length() > 0;
		if (logicState == State.Normal && canDash)
		{
			Dash();
		}
	}

	public void Hit(float damage = 0.3f)
	{
		if (health > 0)
		{
			health -= damage;
			damageElapse = 1;
		}
	}

	public void Dash()
	{
		dashing.steps = 0;
		dashing.cooldown = 0;
		dashing.dir = Vector2.Normalize(entity.dir);
		dashing.stepSize = Dashing.startingStepSize;
		logicState = State.Dashing;
	}

	public void Update()
	{
		UpdateCharacter();
		UpdateWeapon();

		entity.dir = GAxis.left.activeDir;
		entity.FaceDirectionX(GAxis.right.passiveDir);
		entity.Update();
	}

	public void Draw()
	{
		if (logicState == State.Dashing)
		{
			var pos = entity.pos;
			for (var i = 1; i <= 3; i++)
			{
				entity.pos = pos + -dashing.dir * (i * 30);
			}
			entity.Draw();

			entity.pos = pos;
		}

		damageElapse -= Love.Timer.GetDelta();

		entity.color = damageElapse > 0 ? Color.Red : Color.White;
		entity.Draw();
		sword.Draw();

		//Graphics.Rectangle(DrawMode.Line, rect);
		//Graphics.Circle(DrawMode.Line, entity.pos, 10);

	}

	public void DrawInterface()
	{
		var healthW = 300;
		Graphics.SetColor(Color.Red);
		Graphics.Rectangle(DrawMode.Fill, 20, 20, (healthW * health) / 100, 30);
		Graphics.SetColor(Color.Orange);
		Graphics.Rectangle(DrawMode.Line, 20, 20, healthW, 30);
	}
}

public class Camera
{
	public float zoom = 0.9f;

	public Vector2 pos;
	public RectangleF innerRect;
	public Font font = Graphics.NewFont(18);

	public Camera()
	{
		var center = SharedState.self.center;
		var width = Graphics.GetWidth() * 0.55f;
		var height = Graphics.GetHeight() * 0.55f;
		var pos = center - new Vector2(width / 2, height / 2);
		innerRect = new RectangleF(
			pos.X,
			pos.Y,
			width / zoom,
			height / zoom
		);
	}
	public void CenterAt(Vector2 p)
	{
		innerRect.Center = p;
	}

	public void RestrictWithin(Vector2 p)
	{
		if (p.X < innerRect.Left)
		{
			innerRect.Left = p.X;
		}
		else if (p.X > innerRect.Right)
		{
			innerRect.Right = p.X;
		}

		if (p.Y < innerRect.Top)
		{
			innerRect.Top = p.Y;
		}
		else if (p.Y > innerRect.Bottom)
		{
			innerRect.Bottom = p.Y;
		}
	}

	public void Update()
	{

	}

	public void Draw()
	{
	}

	public void StartDraw()
	{
		Graphics.Push();
		var center = SharedState.self.center;
		var dx = center.X / zoom - innerRect.Width / 2;
		var dy = center.Y / zoom - innerRect.Height / 2;

		Graphics.Scale(zoom, zoom);
		Graphics.Translate(-innerRect.Left + dx, -innerRect.Top + dy);
	}

	public void EndDraw()
	{
		//Graphics.Rectangle(DrawMode.Line, innerRect);
		Graphics.Pop();

		//Graphics.SetFont(font);
		//Graphics.Print(string.Format("{0},  {1}", (innerRect.Width), (innerRect.Width / zoom)), Graphics.GetWidth() - 300, 50);
		//Graphics.SetFont(null);

		Graphics.SetColor(Color.White);
		//Graphics.Circle(DrawMode.Fill, SharedState.self.center, 5);
	}

	public void Dipose()
	{
		font.Dispose();
	}
}



public class World
{
	Vector2 size = new Vector2(5000, 5000);
	Vector2 origin = new Vector2(0, 0);
	int tileSize = SharedState.self.worldTileSize;
	Color gridColor = new Color(80, 80, 80, 255);

	Canvas gridCanvas;
	Player player;

	public Vector2 Center
	{
		get
		{
			return origin + new Vector2(size.X / 2, size.Y / 2);
		}
	}

	public float Width { get { return size.X; } }
	public float Height { get { return size.Y; } }

	public World(Player player)
	{
		this.player = player;

		gridCanvas = Graphics.NewCanvas((int)size.X, (int)size.Y);
		Graphics.SetCanvas(gridCanvas);
		Graphics.SetColor(gridColor);
		for (var x = 0f; x < gridCanvas.GetWidth(); x += tileSize)
		{
			for (var y = 0f; y < gridCanvas.GetHeight(); y += tileSize)
			{
				Graphics.Rectangle(DrawMode.Line, x, y, tileSize, tileSize);
			}

		}
		Graphics.SetCanvas();
	}


	public void Draw()
	{
		var tile = (
			(int)(player.pos.X / tileSize),
			(int)(player.pos.Y / tileSize)
		);

		Graphics.Push();
		//Graphics.Translate(-gridCanvas.GetWidth() / 2, -gridCanvas.GetHeight() / 2);
		Graphics.Draw(gridCanvas, origin.X, origin.Y);

		//Graphics.SetColor(Color.Red);
		//Graphics.Rectangle(DrawMode.Fill, tile.Item1 * tileSize, tile.Item2 * tileSize, tileSize, tileSize);

		Graphics.Pop();
	}

	public void Dispose()
	{
		gridCanvas.Dispose();
	}
}

enum WeaponState
{
	OnHand,
	Attacking,
	Returning
}

public class GameWithCoroutines : View
{
	/*
	public async Coroutine ShowBigKanji() { }

	public async Coroutine StartGameScript()
	{
		await ShowBigKanji();

		var rounds = 0;

		bool hasExample;
		bool singleChar = false;
		if (!hasExample)
		{
			rounds += 2;
		}
		if (singleChar)
		{
			rounds += 2;
		}

		await StartHuntVocab(rounds);

		if (singleChar)
		{
			await StartHuntVocabParts();
		}

		if (hasExample)
		{
			await StartHuntExample();
			await StartHuntExampleParts();
		}
		await ShowResult();
	}

	*/

	public enum State { Initializing, Playing, Clear, Gameover }
	public enum HuntState { Init, Vocab, VocabParts, Example, ExampleParts, Clear }

	public class Playing
	{
		public int subIndex = 0;
		public CardInfo card = new CardInfo();
		public HuntState state = HuntState.Vocab;
		public List<Monster> subTargets = new List<Monster>();
		public Monster target = new Monster();
		public int hunts;
		public int maxHunts;

		//public GameWithCoroutines game;

		//public Playing(GameWithCoroutines game) { this.game = game; }


	}

	public class Clear
	{
		public float width = Graphics.GetWidth() * 9 / 10;
		public float height = Graphics.GetHeight() * 3 / 4;
		public Color bgColor = new Color(15, 15, 15, 180);
		public RectangleF rect;

		public Gpr gpr = new Gpr();

		public Clear()
		{
			var x = Graphics.GetWidth() / 2 - width / 2;
			var y = Graphics.GetHeight() / 2 - height / 2;
			rect = new RectangleF(
				x,
				y,
				width,
				height
			);

			gpr.pos = new Vector2(x + 20, y + 20);
		}

	}

	public class Gameover
	{
		public float opacity = 0;
		public Color color = new Color(0.5f, 0, 0, 1);
		public Font bigFont = Graphics.NewFont(120);
		public Font smallFont = Graphics.NewFont(30);
		public Text gameoverText;
		public Text subText;

		public Gameover()
		{
			gameoverText = Graphics.NewText(bigFont, "Game over");
			subText = Graphics.NewText(smallFont, "<Press any key to play again>");
		}
	}


	SharedState state;
	Player player;
	Camera cam;
	World world;
	MonsterGroup monsterGroup;

	ComponentRegistry components = new ComponentRegistry();
	Corunner runner = new Corunner();

	List<CardInfo> cards = new List<CardInfo>();

	public State gameState = State.Playing;
	public Gameover gameover = new Gameover();
	public Playing playing = new Playing();
	public Clear clear = new Clear();
	public float elapsed = 0;

	//public List<Monster> killedMonsters = new List<Monster>();

	public GameWithCoroutines(SharedState state)
	{
		this.state = state;

		cam = new Camera();
		player = new Player();
		world = new World(player);
		monsterGroup = new MonsterGroup();

	}
	public void Load()
	{
		StartGame();
	}

	/*
	private void GamepadPressed(Joystick arg1, GamepadButton arg2)
	{
		HandleGameoverInput();
	}

	private void KeyPressed(KeyConstant key, Scancode scancode, bool isRepeat)
	{
		HandleGameoverInput();
	}

	public void HandleGameoverInput()
	{
		if (gameState == State.Gameover && gameover.opacity >= 1)
		{
			monsterGroup.Dispose();
			StartGame();
		}
	}
	*/

	public void RemoveUnwantedText(CardInfo card, string field)
	{
		if (card.HasField(field))
		{
			var text = card.fields?[field]?.value;
			if (text != null)
			{
				text = Regex.Replace(text, @"\[.*?\]", " ");
				text = Regex.Replace(text, @"\<.*?\>", " ");
				card.fields[field].value = text;
			}
		}
	}

	public List<CardInfo> CreateCards()
	{
		var numText = 30;
		var deckName = state.lastDeckName ?? state.deckNames.Keys.First();
		var cards = state.deckCards?[deckName]?.ToList() ?? new List<CardInfo>();

		cards.Sort((a, b) => Random.Shared.Next(-1, 2));

		cards = cards.Take(numText).ToList();

		foreach (var card in cards)
		{
			RemoveUnwantedText(card, "VocabKanji");
			RemoveUnwantedText(card, "SentKanji");
			RemoveUnwantedText(card, "SentEng");

			//card.fields["VocabKanji"].value += card.fields["VocabKanji"].value;


			Console.WriteLine("Card> {0},{1}", card.cardId, card.GetField("SentKanji"));
		}

		return cards;
	}

	// TODO: move to monstergroup
	public async Coroutine<Monster> GetMonsterKill()
	{
		Monster? monster = null;
		var onKill = (Monster m) =>
		{
			monster = m;
		};
		monsterGroup.OnKill += onKill;
		using var _ = Defer.Run(() => monsterGroup.OnKill -= onKill);

		while (monster == null)
		{
			await Coroutine.Yield();
		}

		return monster;
	}

	public async Coroutine<Monster> GetHuntedKill()
	{
		while (true)
		{
			var m = await GetMonsterKill();
			var hunted = playing.subTargets[playing.subIndex];
			if (m != hunted)
			{
				if (m.text != hunted.text)
				{
					continue;
				}

				playing.subTargets[playing.subIndex] = m;
				hunted = m;
			}
			return hunted;
		}
	}

	public void InitGame()
	{
		cam = new Camera();
		player = new Player();
		world = new World(player);
		cards = CreateCards();


		AnkiAudioPlayer.LoadCardAudios(cards);
		player.entity.pos = world.Center;
		cam.CenterAt(player.pos);

		playing.card = cards[0];

		//monsterGroup = MonsterGroup.CreateFromCards(cards);
		monsterGroup = new MonsterGroup(world, player.entity);
		monsterGroup.OnKill += OnMonsterKill;
		monsterGroup.OnHit += OnDefaultMonsterHit;


		/*
		var monsters = monsterGroup.Iterate().ToArray();
		if (monsters.Length > 0)
		{
			playing.subIndex = 0;
			playing.target = monsters[0];
			playing.state = HuntState.Init;
			//NextHuntState();
		}
		*/

	}


	public void StartGame()
	{
		runner.Cancel();
		runner.Create(StartGameScript);
	}


	public async Coroutine GameoverScript()
	{
		InitGameover();
		using var _1 = components.AddUpdate(UpdateGameover);
		using var _2 = components.AddDraw(DrawGameover);
		components.RemoveUpdate(UpdatePlayerAction);
		components.RemoveUpdate(UpdateMonsterCollision);
		await Xt.Coroutine.AwaitAnyKey();
	}

	public async Coroutine StartGameScript()
	{
		while (true)
		{
			InitGame();
			//monsterGroup.SpawnFromCards(cards);
			//var m = monsterGroup.Iterate().First();
			//if (m != null)
			//{
			//	playing.subIndex = 0;
			//	playing.target = m;
			//	playing.state = HuntState.Init;
			//}


			using var _1 = components.AddUpdate(UpdatePlayerAction);
			using var _2 = components.AddUpdate(UpdateMonsterCollision);
			using var _3 = components.AddUpdate(UpdateMonsters);
			using var _4 = components.AddDraw(DrawInterface);

			gameState = State.Playing;
			var gameScript = StartHuntScript();

			await Coroutine.While(() => gameState == State.Playing);
			if (!gameScript.IsCompleted) { gameScript.Cancel(); }

			if (gameState == State.Clear)
			{
				// TODO:
			}
			else if (gameState == State.Gameover)
			{
				Console.WriteLine("gameover");
				await GameoverScript();
			}
		}
	}

	void ClearSubTargets()
	{
		playing.hunts = 0;
		playing.maxHunts = 1;
		playing.subIndex = 0;
		foreach (var m in playing.subTargets)
		{
			monsterGroup.Remove(m);
			playing.target.pos = m.pos;
		}
		playing.subTargets.Clear();
	}

	public async Coroutine<bool> StartChooseAnswerScript()
	{
		Color messageColor = Color.White;
		Color bgColor = new Color(20, 20, 20, 200);
		string message = "";
		Monster? killed = null;

		var onHit = (Monster m) =>
		{
			var damage = Random.Shared.Next(40, 60);
			var filename = m.audioFilename;
			AnkiAudioPlayer.Play(filename);
			m.Attack(Vector2.Zero, damage);
		};
		var onKill = (Monster m) =>
		{
			killed = m;
		};
		var draw = () =>
		{
			if (message != "")
			{
				var pos = SharedState.self.centerTop + Vector2.UnitY * 50;
				Graphics.SetColor(bgColor);
				Xt.Graphics.PrintlnRect(DrawMode.Fill, message, SharedState.self.fontRegular);
				Graphics.SetColor(messageColor);
				Xt.Graphics.PrintPos = pos;
				Xt.Graphics.Println(message, SharedState.self.fontRegular);
			}
		};

		monsterGroup.OnHit += onHit;
		monsterGroup.OnKill += onKill;
		monsterGroup.OnHit -= OnDefaultMonsterHit;
		components.RemoveDraw(DrawInterface);
		components.AddDraw(draw);

		using var _ = Defer.Run(() =>
		{
			monsterGroup.OnHit -= onHit;
			monsterGroup.OnKill -= onKill;
			monsterGroup.OnHit += OnDefaultMonsterHit;
			components.RemoveDraw(draw);
		});

		var card = playing.card;
		var choices = CreateChoices(card, 4);
		var (pre, vocab, post) = CreateQuestion(card);



		// TODO: add wandering monster state
		// TODO: flee choice monsters when hit

		// TODO: in-game zoom in/out
		// set camera on question
		// pan to player 
		// await camera.panTo()
		// await camera.zoomIn()
		// await camera.zoomOut()

		await Coroutine.While(() => killed == null);
		Console.WriteLine(killed.text);


		var correct = killed.text == playing.card.GetField("VocabDef");
		if (correct)
		{
			message = "/ okay";
			messageColor = Color.Green;
		}
		else
		{
			message = "x nope";
			messageColor = Color.Red;
		}

		await Xt.Coroutine.Sleep(2);
		return correct;
	}
	List<Monster> CreateChoices(CardInfo card, int count = 4)
	{
		var choices = new List<Monster>();
		for (var i = 0; i < count - 1; i++)
		{
			var otherCard = cards.GetRandom(c => c != card);
			if (otherCard == null)
			{
				continue;
			}
			var m = monsterGroup.SpawnMonster(otherCard.GetVocabDef() ?? "*");
			m.audioFilename = otherCard.GetField("VocabAudio");
			choices.Add(m);
		}

		var correct = monsterGroup.SpawnMonster(card.GetVocabDef() ?? "*");
		correct.audioFilename = card.GetField("VocabAudio");
		choices.Add(correct);
		choices.Sort((a, b) => Random.Shared.Next(-1, 2));

		var angleStep = (360 / choices.Count()) * MathF.PI / 180;
		var d = player.entity.rect.DiagonalLength() * 2.5f;
		var polar = new Polar(35, d);
		foreach (var m in choices)
		{
			m.pos = player.pos + polar.ToVector();
			m.target = null;
			polar.angle += angleStep;
		}

		return choices;
	}

	(Monster?, Monster, Monster?) CreateQuestion(CardInfo card)
	{
		var vocab = card.GetVocab() ?? "";
		var example = card.GetExample() ?? "";

		var mid = monsterGroup.SpawnMonster(vocab);
		if (!card.HasExample())
		{
			return (null, mid, null);
		}

		var (a, b) = example.DivideBy(vocab);
		var pre = string.IsNullOrEmpty(a.Trim()) ? null : monsterGroup.SpawnMonster(a);
		var post = string.IsNullOrEmpty(a.Trim()) ? null : monsterGroup.SpawnMonster(b);

		if (pre != null) { pre.audioFilename = card.GetField("SentAudio"); }
		if (post != null) { post.audioFilename = card.GetField("SentAudio"); }
		mid.audioFilename = card.GetField("VocabAudio");

		var qpos = player.pos + Vector2.UnitY * -player.entity.rect.DiagonalLength() * 4.5f;
		var fontAsian = SharedState.self.fontAsian;
		if (pre != null)
		{
			pre.pos = qpos - Vector2.UnitX * (mid.GetTextSize(fontAsian) / 2 + pre.GetTextSize(fontAsian) / 2);
			pre.target = null;
		}

		mid.pos = qpos;
		mid.target = null;
		mid.textColor = Color.Yellow;

		if (post != null)
		{
			post.pos = qpos + Vector2.UnitX * (mid.GetTextSize(fontAsian) / 2 + post.GetTextSize(fontAsian) / 2);
			post.target = null;
		}


		return (pre, mid, post);
	}

	public async Coroutine StartHuntScript()
	{
		var correct = await StartChooseAnswerScript();

		if (correct)
		{
			Console.WriteLine("TODO");
			await Xt.Coroutine.Abyss();
			return;
		}

		// TODO: await StartSelectVocabDef();
		// Shows Example sentence with Vocab highligted (like in anki)
		// monsters are lined up, semi-moving
		// choose correct VocabDef
		// if correct, skip to next card
		// if wrong, show complete example translation too
		// then proceed

		ClearSubTargets();
		monsterGroup.Clear();
		monsterGroup.SpawnFromCards(cards);
		components.AddDraw(DrawInterface);

		var text = playing.card.GetVocab() ?? "";
		var target = monsterGroup.GetRandom();
		playing.target = target;

		await ReadyGameScript();

		await StartVocabScript();

		// TODO: pause show message here

		if (target.text.Length > 1)
		{
			await StartVocabPartsScript();
		}

		// TODO: animation, merge back monsters back to last kill

		if (playing.card.HasExample())
		{
			// TODO:
			await StartExampleScript();
			await StartExamplePartsScript();
		}
	}
	public async Coroutine StartVocabPartsScript()
	{
		playing.state = HuntState.VocabParts;

		var target = playing.target;
		var pos = target.pos;
		if (playing.subTargets.Count() > 0)
		{
			pos = playing.subTargets[^1].pos;
			Console.WriteLine(pos);
		}

		ClearSubTargets();
		playing.subTargets.AddRange(SplitMonsterBy(target, "VocabKanji"));
		playing.maxHunts = 1;

		foreach (var m in playing.subTargets)
		{
			m.pos = pos;
		}

		while (true)
		{
			while (playing.subIndex < playing.subTargets.Count())
			{
				var hunted = await GetHuntedKill();
				playing.subIndex++;
				// TODO: wait for weapon to return first, player.OnWeaponBack or something
			}
			playing.subIndex = 0;
			playing.hunts++;
			if (playing.hunts >= playing.maxHunts)
			{
				break;
			}
			playing.subTargets.Clear();
			playing.subTargets.AddRange(SplitMonsterBy(target, "VocabKanji"));
		}
	}
	public async Coroutine StartExampleScript() { }
	public async Coroutine StartExamplePartsScript() { }

	public async Coroutine StartVocabScript()
	{
		playing.state = HuntState.Vocab;
		var target = playing.target;
		var skipVocabParts = target.text.Length <= 1;

		playing.subTargets.Add(target);

		if (skipVocabParts)
		{
			playing.maxHunts += 2;
		}
		if (!playing.card.HasExample())
		{
			playing.maxHunts += 2;
		}

		while (true)
		{
			var hunted = await GetHuntedKill();
			playing.hunts++;
			if (playing.hunts >= playing.maxHunts)
			{
				break;
			}

			var font = SharedState.self.fontAsian;
			//var newMonster = new Monster(TileID.RandomMonsterID(), hunted.text.ToString(), font);
			var newMonster = monsterGroup.SpawnMonster(hunted.text);
			newMonster.target = hunted.target;
			newMonster.card = hunted.card;
			newMonster.audioFilename = hunted.audioFilename;
			newMonster.Flee(1);

			playing.subTargets.Clear();
			playing.subTargets.Add(newMonster);

		}

	}

	public async Coroutine ReadyGameScript()
	{
		var target = playing.target;
		var card = target.card;
		var vocab = card?.GetVocab() ?? "no card";
		var scriptFn = new ComponentView();
		var state = SharedState.self;
		var fontAsian = state.fontAsian;
		var fontEng = state.fontMedium;
		var center = state.center - new Vector2(0, (fontAsian.GetHeight() + fontEng.GetHeight()) / 2);
		var countdown = 4;

		scriptFn.DrawComponent = () =>
		{
			var width = Graphics.GetWidth() / 2;
			var ready = countdown == 0 ? "start!" : string.Format("ready {0}", countdown);

			//Xt.Graphics.PrintWidth = width;
			Xt.Graphics.PrintPos = center;
			Xt.Graphics.Println(vocab, fontAsian);
			Xt.Graphics.Println(ready, fontEng);

			var w = fontAsian.GetWidth(vocab);
			//Love.Graphics.Printf(vocab, center.X - w / 2, center.Y, w, AlignMode.Center);
		};

		using var _ = components.AddComponent(scriptFn);
		//using var cleanup = Defer.Run(() =>
		//{
		//	Console.WriteLine("cleanup");
		//	components.RemoveComponent(scriptFn);
		//});

		while (countdown > 0)
		{
			countdown--;
			await Xt.Coroutine.Sleep(0.5f);
		}
	}

	public Monster CreateExampleMonster(Monster m)
	{
		var font = SharedState.self.fontAsian;
		var text = m.card?.GetField("SentKanji") ?? m.text;
		var audioFilename = m.card?.GetField("SentAudio") ?? m.text;
		//var newMonster = new Monster(m.entity.tileID, text, font);
		var newMonster = monsterGroup.SpawnMonster(text, m.entity.tileID);

		newMonster.target = m.target;
		newMonster.audioFilename = audioFilename;
		newMonster.pos = m.pos;
		newMonster.card = m.card;
		newMonster.entity.scale = 5;
		newMonster.defense = 5;
		newMonster.Flee(3);

		return newMonster;
	}

	public Monster[] SplitMonsterBy(Monster m, string fieldName)
	{
		var text = m.card?.GetField(fieldName) ?? m.text;
		var subMonsters = new List<Monster>();
		var font = SharedState.self.fontAsian;
		var audioFilename = m.card?.GetField(fieldName == "SentKanji" ? "SentAudio" : "VocabAudio") ?? m.text;
		foreach (var ch in text)
		{
			if (char.IsWhiteSpace(ch))
			{
				continue;
			}
			Console.WriteLine("> {0}", ch);
			var newMonster = monsterGroup.SpawnMonster(ch.ToString());
			newMonster.pos = m.pos;
			newMonster.target = m.target;
			newMonster.card = m.card;
			newMonster.audioFilename = audioFilename;
			newMonster.defense = 0.3f;
			newMonster.Flee(3);

			subMonsters.Add(newMonster);
		}
		return subMonsters.ToArray();
	}

	/*
		public Monster[] SplitMonsterByExample(Monster m)
		{
			//var text = m.card?.GetField("SentKanji") ?? m.text;
			if (!m.card.HasExample())
			{
				Console.WriteLine("no SentKanji");
				return new Monster[0];
			}

			var text = m.card?.GetField("SentKanji") ?? m.text;
			var subMonsters = new List<Monster>();
			var font = SharedState.self.fontAsian;
			var audioFilename = m.card?.GetField("SentAudio") ?? m.text;
			foreach (var ch in text)
			{
				if (char.IsWhiteSpace(ch))
				{
					continue;
				}
				var newMonster = new Monster(m.entity.tileID, ch.ToString(), font);
				newMonster.pos = m.pos;
				newMonster.target = m.target;
				newMonster.card = m.card;
				newMonster.audioFilename = audioFilename;
				subMonsters.Add(newMonster);
			}
			return subMonsters.ToArray();
		}
	*/

	public void Draw()
	{

		cam.StartDraw();
		{
			world.Draw();
			player.Draw();
			monsterGroup.Draw();
			DrawTargets();
		}
		cam.EndDraw();

		components.Draw();

		/*
		if (gameState == State.Playing)
		{
			DrawInterface();
		}
		else if (gameState == State.Gameover)
		{
			DrawGameover();
		}
		else if (gameState == State.Clear)
		{
			DrawClear();
		}
		*/

	}

	public void DrawTargets()
	{
		if (playing.subTargets.Count() == 0)
		{
			return;
		}
		//var targetMonster = playing.subTargets[playing.targetIndex];
		foreach (var m in playing.subTargets)
		{
			//Graphics.SetColor(m == targetMonster ? Color.Red : Color.White);
			//Graphics.Rectangle(DrawMode.Line, m.rect);
		}
	}

	public void DrawInterface()
	{
		player.DrawInterface();


		{
			var i = 0;
			var coloredText = new List<ColoredString>();
			foreach (var mon in playing.subTargets)
			{
				var c = i < playing.subIndex ? Color.White
					: i == playing.subIndex ? Color.PaleGreen
					: Color.Gray;
				i++;
				coloredText.Add(new ColoredString(mon.text, c));
			}

			var text = new ColoredStringArray(coloredText.ToArray());
			var font = SharedState.self.fontAsian;
			var pos = new Vector2(0, Graphics.GetHeight() - font.GetHeight() * 1.2f);
			Graphics.SetFont(SharedState.self.fontAsian);
			Graphics.SetColor(Color.White);
			Graphics.Printf(text, pos.X, pos.Y, Graphics.GetWidth(), AlignMode.Center);

			font = SharedState.self.fontMedium;
			Graphics.SetFont(font);
			Graphics.Printf(
				string.Format("{0}/{1}", playing.hunts, playing.maxHunts),
				pos.X, pos.Y - font.GetHeight(), Graphics.GetWidth(), AlignMode.Center
			);
		}

		{
			var font = SharedState.self.fontMedium;
			var pos = new Vector2(Graphics.GetWidth() / 2, font.GetHeight() / 2);
			var card = playing.target.card;
			var s = playing.state;
			var text = s == HuntState.VocabParts || (playing.hunts >= 2 && s == HuntState.Vocab)
				? card?.GetField("VocabDef")
				: s == HuntState.VocabParts
				? card?.GetField("SentEng")
				: "";

			Graphics.SetColor(Color.White);
			Graphics.SetFont(font);
			Graphics.Printf(text, pos.X, pos.Y, Graphics.GetWidth() / 2 - 20, AlignMode.Right);
		}
	}

	public void UpdateClear()
	{
		clear.gpr.ResetLine();
	}
	public void DrawClear()
	{
		var card = playing.target.card;

		Graphics.SetColor(clear.bgColor);
		Graphics.Rectangle(DrawMode.Fill, clear.rect);
		Graphics.SetColor(Color.White);

		clear.gpr.font = SharedState.self.fontSmall;
		clear.gpr.Print("id={0}", card.cardId);

		clear.gpr.font = SharedState.self.fontRegular;
		clear.gpr.Print("Elapsed: {0} seconds", MathF.Floor(elapsed));
		clear.gpr.font = SharedState.self.fontAsian;
		clear.gpr.Print("{0} / {1}", card.GetField("VocabKanji"), card.GetField("VocabDef"));


		if (card.HasExample())
		{
			clear.gpr.font = SharedState.self.fontSmall;
			clear.gpr.Print(" ");
			clear.gpr.font = SharedState.self.fontAsian;
			clear.gpr.Print(card.GetField("SentKanji"));
			clear.gpr.font = SharedState.self.fontSmall;
			clear.gpr.Print(" ");
			clear.gpr.font = SharedState.self.fontMedium;
			clear.gpr.Print(card.GetField("SentEng"));
		}

	}


	public void InitGameover()
	{
		gameover.opacity += 0;
	}
	public void UpdateGameover()
	{
		if (gameover.opacity < 1)
		{
			gameover.opacity += 0.01f;
			if (gameover.opacity > 1)
			{
				gameover.opacity = 1;
			}
			gameover.color.Rf = gameover.opacity;
		}
	}
	public void DrawGameover()
	{

		var bigText = gameover.gameoverText;
		var smallText = gameover.subText;

		Graphics.SetColor(gameover.color);
		Graphics.SetFont(gameover.bigFont);
		Graphics.Draw(bigText, state.center.X - bigText.GetWidth() / 2, state.center.Y - bigText.GetHeight() / 2);

		Graphics.SetFont();
		if (gameover.opacity >= 1)
		{
			Graphics.Draw(smallText, state.center.X - smallText.GetWidth() / 2, state.center.Y + bigText.GetHeight() - smallText.GetHeight() / 2);
		}
	}


	public void OnMonsterKill(Monster m)
	{
	}

	public void OnDefaultMonsterHit(Monster m)
	{
		var targetMonster = playing.subIndex >= 0 && playing.subIndex < playing.subTargets.Count()
			? playing.subTargets[playing.subIndex] : null;
		var isTarget = m.text == targetMonster?.text;
		var damage = !isTarget ? Random.Shared.Next(20, 30) : Random.Shared.Next(30, 60);

		m.Attack(player.sword, damage);

		var filename = m.audioFilename;
		AnkiAudioPlayer.Play(filename);
	}


	/*
	public void UpdateTargets()
	{
		foreach (var m in killedMonsters)
		{
		}
	}

	public void NextHuntState()
	{
		playing.targetIndex = 0;

		foreach (var m in playing.subTargets)
		{
			monsterGroup.Remove(m);
			playing.target.pos = m.pos;
		}

		playing.subTargets.Clear();
		playing.maxRounds = 2;
		playing.rounds = 1;

		var current = playing.state;
		var target = playing.target;
		if (current == HuntState.Init)
		{
			playing.subTargets.Add(target);

			if (target.text.Length <= 1)
			{
				playing.maxRounds += 2;
			}
			if (!target.card.HasExample())
			{
				playing.maxRounds += 2;
			}

			playing.state = HuntState.Vocab;
		}
		else if (current == HuntState.Vocab)
		{
			TransitionVocab(target);
		}
		else if (current == HuntState.VocabParts)
		{
			TransitionVocabParts(target);
		}
		else if (current == HuntState.Example)
		{
			TransitionExample(target);
		}
		else if (current == HuntState.ExampleParts)
		{
			TransitionExampleParts(target);
		}
		else
		{
			Console.WriteLine("huh: {0}", playing.state);
			ClearLevel();
		}

		monsterGroup.AddAll(playing.subTargets);

	}

	private void TransitionVocab(Monster target)
	{

		if (target.text.Length <= 1)
		{
			TransitionVocabParts(target);
		}
		else
		{
			playing.subTargets.AddRange(SplitMonsterBy(target, "VocabKanji"));
			playing.state = HuntState.VocabParts;
		}
	}
	private void TransitionVocabParts(Monster target)
	{
		if (target?.card?.HasExample() ?? false)
		{
			Console.WriteLine("to example {0}", target.card.GetField("SentKanji"));
			playing.subTargets.Add(CreateExampleMonster(target));
			playing.state = HuntState.Example;
		}
		else
		{
			ClearLevel();
		}
	}
	private void TransitionExample(Monster target)
	{
		Console.WriteLine("to example parts");
		playing.subTargets.AddRange(SplitMonsterBy(target, "SentKanji"));
		playing.state = HuntState.ExampleParts;
	}
	private void TransitionExampleParts(Monster target)
	{
		ClearLevel();
	}


	public void ClearLevel()
	{

		playing.state = HuntState.Clear;
		gameState = State.Clear;
	}
	*/


	public void UpdatePlayerAction()
	{
		player.Update();

		if (Gamepad.IsPressed(GamepadButton.RightShoulder) || Mouse.IsPressed(MouseButton.LeftButton))
		{
			player.DoAction1();
		}
		else if (Gamepad.IsPressed(GamepadButton.A) || Keyboard.IsPressed(KeyConstant.Space))
		{
			player.DoAction2();
		}

		if (player.health <= 0)
		{
			gameState = State.Gameover;
		}
	}

	public void UpdateMonsterCollision()
	{
		//var targetMonster = playing.subIndex >= 0 && playing.subIndex < playing.subTargets.Count()
		//? playing.subTargets[playing.subIndex] : null;

		var sword = player.sword;
		foreach (var m in monsterGroup.GetMonstersAt(sword.GetHandlePos(), sword.GetEndPos()))
		{
			var hit = sword.HasHit(m);
			if (hit && sword.enabled && m.IsAlive())
			{
				m.Hit(sword.pos);

				/*
				var isTarget = m.text == targetMonster?.text;
				var damage = !isTarget ? Random.Shared.Next(20, 30) : Random.Shared.Next(30, 60);
				if (isTarget)
				{
					Console.WriteLine("damage: {0}", damage);
				}
				m.Hit(sword.pos, damage);

				// TODO: move to on hit
				var filename = m.audioFilename;
				AnkiAudioPlayer.Play(filename);
				*/
			}
		}

		foreach (var m in monsterGroup.GetMonstersAt(player.pos))
		{
			if (player.entity.CollidesWith(m.entity) && m.CanDamage())
			{
				player.Hit();
			}
		}


		//UpdateTargets();

	}
	public void UpdateMonsters()
	{
		monsterGroup.Update();
	}

	public void Update()
	{
		elapsed += Love.Timer.GetDelta();

		cam.Update();
		cam.RestrictWithin(player.pos);

		components.Update();
		runner.Update();

		/*
		if (gameState == State.Playing)
		{
			UpdatePlaying();
		}
		else if (gameState == State.Gameover)
		{
			UpdateGameover();
		}
		else if (gameState == State.Clear)
		{
			UpdateClear();
		}
		else if (gameState == State.Initializing)
		{
			gameState = State.Playing;
		}
		*/

	}

	public void Unload()
	{
		monsterGroup.Dispose();
		runner.Cancel();
		AnkiAudioPlayer.Clear();
		world.Dispose();
		cam.Dipose();
		gameover.bigFont.Dispose();
		gameover.smallFont.Dispose();
	}
}


/*
public class Game : View
{
	public enum State { Initializing, Playing, Clear, Gameover }
	public enum HuntState { Init, Vocab, VocabParts, Example, ExampleParts, Clear }

	public class Playing
	{
		public int targetIndex = 0;
		public HuntState state = HuntState.Vocab;
		public List<Monster> subTargets = new List<Monster>();
		public Monster target = new Monster();
		public int rounds;
		public int maxRounds;
	}

	public class Clear
	{
		public float width = Graphics.GetWidth() * 9 / 10;
		public float height = Graphics.GetHeight() * 3 / 4;
		public Color bgColor = new Color(15, 15, 15, 180);
		public RectangleF rect;

		public Gpr gpr = new Gpr();

		public Clear()
		{
			var x = Graphics.GetWidth() / 2 - width / 2;
			var y = Graphics.GetHeight() / 2 - height / 2;
			rect = new RectangleF(
				x,
				y,
				width,
				height
			);

			gpr.pos = new Vector2(x + 20, y + 20);
		}

	}

	public class Gameover
	{
		public float opacity = 0;
		public Color color = new Color(0.5f, 0, 0, 1);
		public Font bigFont = Graphics.NewFont(120);
		public Font smallFont = Graphics.NewFont(30);
		public Text gameoverText;
		public Text subText;

		public Gameover()
		{
			gameoverText = Graphics.NewText(bigFont, "Game over");
			subText = Graphics.NewText(smallFont, "<Press any key to play again>");
		}
	}


	SharedState state;
	Player player;
	Camera cam;
	World world;
	MonsterGroup monsterGroup;

	public State gameState = State.Playing;
	public Gameover gameover = new Gameover();
	public Playing playing = new Playing();
	public Clear clear = new Clear();
	public float elapsed = 0;

	public List<Monster> killedMonsters = new List<Monster>();

	public Game(SharedState state)
	{
		this.state = state;

		cam = new Camera();
		player = new Player();
		world = new World(player);
		monsterGroup = new MonsterGroup();

	}
	public void Load()
	{
		KeyHandler.OnKeyPress += KeyPressed;
		GamepadHandler.OnPress += GamepadPressed;

		StartGame();
	}

	private void GamepadPressed(Joystick arg1, GamepadButton arg2)
	{
		HandleGameoverInput();
	}

	private void KeyPressed(KeyConstant key, Scancode scancode, bool isRepeat)
	{
		HandleGameoverInput();
	}

	public void HandleGameoverInput()
	{
		if (gameState == State.Gameover && gameover.opacity >= 1)
		{
			monsterGroup.Dispose();
			StartGame();
		}
	}

	public void RemoveUnwantedText(CardInfo card, string field)
	{
		if (card.HasField(field))
		{
			var text = card.fields?[field]?.value;
			if (text != null)
			{
				text = Regex.Replace(text, @"\[.*?\]", " ");
				text = Regex.Replace(text, @"\<.*?\>", " ");
				card.fields[field].value = text;
			}
		}
	}

	public List<CardInfo> CreateCards()
	{
		var numText = 15;
		var deckName = state.lastDeckName ?? state.deckNames.Keys.First();
		var cards = state.deckCards?[deckName]?.ToList() ?? new List<CardInfo>();

		cards.Sort((a, b) => Random.Shared.Next(-1, 2));

		cards = cards.Take(numText).ToList();

		foreach (var card in cards)
		{
			RemoveUnwantedText(card, "VocabKanji");
			RemoveUnwantedText(card, "SentKanji");
			RemoveUnwantedText(card, "SentEng");

			//card.fields["VocabKanji"].value += card.fields["VocabKanji"].value;


			Console.WriteLine("Card> {0},{1}", card.cardId, card.GetField("SentKanji"));
		}

		return cards;
	}


	public void StartGame()
	{

		cam = new Camera();
		player = new Player();
		world = new World(player);
		var cards = CreateCards();

		AnkiAudioPlayer.LoadCardAudios(cards);

		player.entity.pos = state.center;

		//cards.AddRange(cards);

		monsterGroup = MonsterGroup.CreateFromCards(cards);

		var monsters = monsterGroup.Iterate().ToArray();
		foreach (var m in monsters)
		{
			m.target = player.entity;
			m.OnMonsterKill += OnMonsterKill;
		}


		if (monsters.Length > 0)
		{
			playing.targetIndex = 0;
			playing.target = monsters[0];


			playing.state = HuntState.Init;
			NextHuntState();

		}

		gameState = State.Playing;
	}

	public Monster CreateExampleMonster(Monster m)
	{
		var font = SharedState.self.fontAsian;
		var text = m.card?.GetField("SentKanji") ?? m.text;
		var audioFilename = m.card?.GetField("SentAudio") ?? m.text;
		var newMonster = new Monster(m.entity.tileID, text, font);

		newMonster.target = m.target;
		newMonster.audioFilename = audioFilename;
		newMonster.pos = m.pos;
		newMonster.card = m.card;
		newMonster.entity.scale = 5;
		newMonster.defense = 5;
		newMonster.OnMonsterKill += OnMonsterKill;
		newMonster.Flee(3);

		return newMonster;
	}

	public Monster[] SplitMonsterBy(Monster m, string fieldName)
	{
		var text = m.card?.GetField(fieldName) ?? m.text;
		var subMonsters = new List<Monster>();
		var font = SharedState.self.fontAsian;
		var audioFilename = m.card?.GetField(fieldName == "SentKanji" ? "SentAudio" : "VocabAudio") ?? m.text;
		foreach (var ch in text)
		{
			if (char.IsWhiteSpace(ch))
			{
				continue;
			}
			var newMonster = new Monster(TileID.RandomMonsterID(), ch.ToString(), font);
			newMonster.pos = m.pos;
			newMonster.target = m.target;
			newMonster.card = m.card;
			newMonster.audioFilename = audioFilename;
			newMonster.OnMonsterKill += OnMonsterKill;
			newMonster.defense = 0.3f;
			newMonster.Flee(3);

			subMonsters.Add(newMonster);
		}
		return subMonsters.ToArray();
	}

		//public Monster[] SplitMonsterByExample(Monster m)
		//{
		//	//var text = m.card?.GetField("SentKanji") ?? m.text;
		//	if (!m.card.HasExample())
		//	{
		//		Console.WriteLine("no SentKanji");
		//		return new Monster[0];
		//	}

		//	var text = m.card?.GetField("SentKanji") ?? m.text;
		//	var subMonsters = new List<Monster>();
		//	var font = SharedState.self.fontAsian;
		//	var audioFilename = m.card?.GetField("SentAudio") ?? m.text;
		//	foreach (var ch in text)
		//	{
		//		if (char.IsWhiteSpace(ch))
		//		{
		//			continue;
		//		}
		//		var newMonster = new Monster(m.entity.tileID, ch.ToString(), font);
		//		newMonster.pos = m.pos;
		//		newMonster.target = m.target;
		//		newMonster.card = m.card;
		//		newMonster.audioFilename = audioFilename;
		//		newMonster.OnMonsterKill += OnMonsterKill;
		//		subMonsters.Add(newMonster);
		//	}
		//	return subMonsters.ToArray();
		//}

	public void Draw()
	{

		cam.StartDraw();
		{
			world.Draw();
			player.Draw();
			monsterGroup.Draw();
			DrawTargets();
		}

		cam.EndDraw();

		if (gameState == State.Playing)
		{
			DrawInterface();
		}
		else if (gameState == State.Gameover)
		{
			DrawGameover();
		}
		else if (gameState == State.Clear)
		{
			DrawClear();
		}

	}

	public void DrawTargets()
	{
		if (playing.subTargets.Count() == 0)
		{
			return;
		}
		var targetMonster = playing.subTargets[playing.targetIndex];
		foreach (var m in playing.subTargets)
		{
			//Graphics.SetColor(m == targetMonster ? Color.Red : Color.White);
			//Graphics.Rectangle(DrawMode.Line, m.rect);
		}
	}

	public void DrawInterface()
	{
		player.DrawInterface();

		{
			var i = 0;
			var coloredText = new List<ColoredString>();
			foreach (var mon in playing.subTargets)
			{
				var c = i < playing.targetIndex ? Color.White
					: i == playing.targetIndex ? Color.PaleGreen
					: Color.Gray;
				i++;
				coloredText.Add(new ColoredString(mon.text, c));
			}

			var text = new ColoredStringArray(coloredText.ToArray());
			var font = SharedState.self.fontAsian;
			var pos = new Vector2(0, Graphics.GetHeight() - font.GetHeight() * 1.2f);
			Graphics.SetFont(SharedState.self.fontAsian);
			Graphics.SetColor(Color.White);
			Graphics.Printf(text, pos.X, pos.Y, Graphics.GetWidth(), AlignMode.Center);

			font = SharedState.self.fontMedium;
			Graphics.SetFont(font);
			Graphics.Printf(
				string.Format("{0}/{1}", playing.rounds, playing.maxRounds),
				pos.X, pos.Y - font.GetHeight(), Graphics.GetWidth(), AlignMode.Center
			);
		}

		{
			var font = SharedState.self.fontMedium;
			var pos = new Vector2(Graphics.GetWidth() / 2, font.GetHeight() / 2);
			var card = playing.target.card;
			var s = playing.state;
			var text = s == HuntState.VocabParts || (playing.rounds >= 2 && s == HuntState.Vocab)
				? card?.GetField("VocabDef")
				: s == HuntState.VocabParts
				? card?.GetField("SentEng")
				: "";

			Graphics.SetColor(Color.White);
			Graphics.SetFont(font);
			Graphics.Printf(text, pos.X, pos.Y, Graphics.GetWidth() / 2 - 20, AlignMode.Right);
		}
	}

	public void UpdateClear()
	{
		clear.gpr.ResetLine();
	}

	public void DrawClear()
	{
		var card = playing.target.card;

		Graphics.SetColor(clear.bgColor);
		Graphics.Rectangle(DrawMode.Fill, clear.rect);
		Graphics.SetColor(Color.White);

		clear.gpr.font = SharedState.self.fontSmall;
		clear.gpr.Print("id={0}", card.cardId);

		clear.gpr.font = SharedState.self.fontRegular;
		clear.gpr.Print("Elapsed: {0} seconds", MathF.Floor(elapsed));
		clear.gpr.font = SharedState.self.fontAsian;
		clear.gpr.Print("{0} / {1}", card.GetField("VocabKanji"), card.GetField("VocabDef"));


		if (card.HasExample())
		{
			clear.gpr.font = SharedState.self.fontSmall;
			clear.gpr.Print(" ");
			clear.gpr.font = SharedState.self.fontAsian;
			clear.gpr.Print(card.GetField("SentKanji"));
			clear.gpr.font = SharedState.self.fontSmall;
			clear.gpr.Print(" ");
			clear.gpr.font = SharedState.self.fontMedium;
			clear.gpr.Print(card.GetField("SentEng"));
		}

	}

	public void DrawGameover()
	{

		var bigText = gameover.gameoverText;
		var smallText = gameover.subText;

		Graphics.SetColor(gameover.color);
		Graphics.SetFont(gameover.bigFont);
		Graphics.Draw(bigText, state.center.X - bigText.GetWidth() / 2, state.center.Y - bigText.GetHeight() / 2);

		Graphics.SetFont();
		if (gameover.opacity >= 1)
		{
			Graphics.Draw(smallText, state.center.X - smallText.GetWidth() / 2, state.center.Y + bigText.GetHeight() - smallText.GetHeight() / 2);
		}
	}

	public void UpdateGameover()
	{
		if (gameover.opacity < 1)
		{
			gameover.opacity += 0.01f;
			if (gameover.opacity > 1)
			{
				gameover.opacity = 1;
			}
			gameover.color.Rf = gameover.opacity;
		}
	}

	public void OnMonsterKill(Monster m)
	{
		killedMonsters.Add(m);
	}

	public void UpdateTargets()
	{
		foreach (var m in killedMonsters)
		{
			if (playing.targetIndex >= playing.subTargets.Count())
			{
				break;
			}
			Console.WriteLine("killed {0}", m.text);

			var target = playing.subTargets[playing.targetIndex];
			if (m != target)
			{
				if (m.text != target.text)
				{
					continue;
				}

				playing.subTargets[playing.targetIndex] = m;
				target = m;
			}


			playing.rounds++;
			if (playing.rounds < playing.maxRounds)
			{

				var font = SharedState.self.fontAsian;
				var newMonster = new Monster(TileID.RandomMonsterID(), target.text.ToString(), font);
				newMonster.pos = Xt.Vector2.Random();
				newMonster.target = m.target;
				newMonster.card = m.card;
				newMonster.audioFilename = target.audioFilename;
				newMonster.OnMonsterKill += OnMonsterKill;
				newMonster.Flee(1);

				playing.subTargets.Clear();
				playing.subTargets.Add(newMonster);
				monsterGroup.Add(newMonster);

				continue;
			}

			playing.targetIndex++;

			if (playing.targetIndex >= playing.subTargets.Count())
			{

				Console.WriteLine("next hunt");
				NextHuntState();
			}
		}
	}

	public void NextHuntState()
	{
		playing.targetIndex = 0;

		foreach (var m in playing.subTargets)
		{
			monsterGroup.Remove(m);
			playing.target.pos = m.pos;
		}

		playing.subTargets.Clear();
		playing.maxRounds = 2;
		playing.rounds = 1;

		var current = playing.state;
		var target = playing.target;
		if (current == HuntState.Init)
		{
			playing.subTargets.Add(target);

			if (target.text.Length <= 1)
			{
				playing.maxRounds += 2;
			}
			if (!target.card.HasExample())
			{
				playing.maxRounds += 2;
			}

			playing.state = HuntState.Vocab;
		}
		else if (current == HuntState.Vocab)
		{
			TransitionVocab(target);
		}
		else if (current == HuntState.VocabParts)
		{
			TransitionVocabParts(target);
		}
		else if (current == HuntState.Example)
		{
			TransitionExample(target);
		}
		else if (current == HuntState.ExampleParts)
		{
			TransitionExampleParts(target);
		}
		else
		{
			Console.WriteLine("huh: {0}", playing.state);
			ClearLevel();
		}

		monsterGroup.AddAll(playing.subTargets);

	}

	private void TransitionVocab(Monster target)
	{

		if (target.text.Length <= 1)
		{
			TransitionVocabParts(target);
		}
		else
		{
			playing.subTargets.AddRange(SplitMonsterBy(target, "VocabKanji"));
			playing.state = HuntState.VocabParts;
		}
	}
	private void TransitionVocabParts(Monster target)
	{
		if (target?.card?.HasExample() ?? false)
		{
			Console.WriteLine("to example {0}", target.card.GetField("SentKanji"));
			playing.subTargets.Add(CreateExampleMonster(target));
			playing.state = HuntState.Example;
		}
		else
		{
			ClearLevel();
		}
	}
	private void TransitionExample(Monster target)
	{
		Console.WriteLine("to example parts");
		playing.subTargets.AddRange(SplitMonsterBy(target, "SentKanji"));
		playing.state = HuntState.ExampleParts;
	}
	private void TransitionExampleParts(Monster target)
	{
		ClearLevel();
	}


	public void ClearLevel()
	{

		playing.state = HuntState.Clear;
		gameState = State.Clear;
	}

	public void UpdatePlaying()
	{
		player.Update();

		if (Gamepad.IsPressed(GamepadButton.RightShoulder) || Mouse.IsPressed(MouseButton.LeftButton))
		{
			player.DoAction1();
		}
		else if (Gamepad.IsPressed(GamepadButton.A) || Keyboard.IsPressed(KeyConstant.Space))
		{
			player.DoAction2();
		}

		var targetMonster = playing.targetIndex >= 0 && playing.targetIndex < playing.subTargets.Count()
				? playing.subTargets[playing.targetIndex] : null;

		var sword = player.sword;
		foreach (var m in monsterGroup.GetMonstersAt(sword.GetEndPos()))
		{
			var hit = sword.HasHit(m);
			if (hit && sword.enabled && m.IsAlive())
			{
				var isTarget = m.text == targetMonster?.text;
				var damage = !isTarget ? 0 : Random.Shared.Next(60, 100);
				if (isTarget)
				{

					Console.WriteLine("damage: {0}", damage);
				}
				m.Hit(sword.pos, damage);

				var filename = m.audioFilename;
				AnkiAudioPlayer.Play(filename);
			}
		}
		foreach (var m in monsterGroup.GetMonstersAt(player.pos))
		{
			if (player.entity.CollidesWith(m.entity) && m.CanDamage())
			{
				player.Hit();
			}
		}

		if (player.health <= 0)
		{
			gameover.opacity = 0;
			gameState = State.Gameover;
			Console.WriteLine("gameover");
		}

		UpdateTargets();
	}

	public void Update()
	{
		elapsed += Love.Timer.GetDelta();

		cam.Update();
		cam.CenterAt(player.pos);

		monsterGroup.Update();

		if (gameState == State.Playing)
		{
			UpdatePlaying();
		}
		else if (gameState == State.Gameover)
		{
			UpdateGameover();
		}
		else if (gameState == State.Clear)
		{
			UpdateClear();
		}
		else if (gameState == State.Initializing)
		{
			gameState = State.Playing;
		}

		if (killedMonsters.Count() > 0)
		{
			killedMonsters.Clear();
		}
	}

	public void Unload()
	{
		monsterGroup.Dispose();
	}

}
*/

/*
Looking at this closer, it's clear that
it won't be able to handle more complex cases,
if the example is any indication.
For one, the event doesn't support arguments.
Two, it doesn't support combining wait events.
Basically, what I want is something like async Task.
Wait a minute, why shouldn't I just use Task then!?
Initially, I was thinking it might have too
much overhead and hence too slow for games,
but does it even matter for simple games like this.
There's no way I'm forking the Coroutine library
just to make it cover more complex cases.

I found another library, AwaitableCoroutine, 
it seems to cover more complex usage.


Event KeyEvent = new Event();
Event MonsterHit = new Event();

public async void StartGame()
{

}

class TextComponent : Component
{
	string text;
	public void Update(Entity entity)
	{

	}

	public void Draw(Entity entity)
	{

	}
}


Entity windowEntity;


*/




// TODO: show centered big kanji at game start
// TODO: try the coroutine for monster re-merging effect
// add Component{Update,Draw} on entities

// TODO: group non-kanji as one in examples

// TODO:
// At start, show example, highlight vocab
// choices are monsters semi-moving
// with translation text above their heads
// wrong answer transitions to playing,
// correct answer, choice to train or to go to next card



// TODO: game scenes/stages
// - monster target hunting
//   - hunt N monsters, .e.g. hunt 5 草
//   - split monster to several monsters by each SentKanji character on death
//   - alternate between VocabKanji and SentKanji
//   - show target kanji and count on UI
//   - on successful hunt, end current level
//     - show card details

// actually, what about SentEng?
// where or when should it be shown?
// Some example sentences are quite long though.
// Huh, even the SentKanji can get too long

// Example kanji: 大学生
// 1. game start, show large kanji at midscreen (no audio)
// 2. move to playing state
//    show the kanji at bottom and add a counter (0/5)
// 3. At 1st kill, if kanji.length > 1
//    split kanji into several monsters, fleeing
//    each monster should take one hit
//    but monsters can only be killed in order
//    大 first, then 学, then last 生
//    highlight which should be targeted next at the bottom
// 4. After hunting sub-monster, the sub-monsters will merge back again
//    into one, change audio to SentKanji, but keep VocabText above monster
// 5. At 2nd kill, split monsters again, but with SentKanji
//    姉は大学生です。 Same process as (3)
//    Show SentEng somewhere, maybe at the top?
//    Oh too much visual noise already?
// 6. End level, show time taken and card details

// implementation notes:
// enum HuntState { Vocab, VocabParts, Example, ExampleParts}
// targetMonsters = []
// targetIndex = 0
// if targetMonsters[targetIndex].IsDead() { next() }
// - non-target monsters take less damage


// see Lemonia game for design ideas
// - particularly the simple effects and bobbing motion


// - monster  (re)spawning
// - pickables (sword, health, bomb)

// TODO: skirmish mode (no targets, just survive)

// TODO: random terrain
// TODO: add a silly bobbing walking motion

// TODO: cast spells 
// TODO: add one more weapon (change with B button)
// TODO: add more fun attack variations

// TODO: snake-like formation of monsters
// TODO: implement other monster logic states

// TODO: Canvas layers (background, floor, entities, roof,  interface)
// TODO: sort one partition per frame (add flag to skip if not modified)
// TODO: implement a proper component-entity system


// TODO: add SFX and BGM?
// It's a good chance to create a full working game
// so I might as well add it
// I don't think I can go another month or two
// without a job
// so I can probably use this project
// when applying
// I don't know how would that work though
// "Hey, I made a shitty game, please hire me"
// like that

// TODO: remove xFFmpeg.NET dependency
//       and create separate tool for extracting/converting the audio

// TODO: remove anki dependecy
//       and create separate tool for extracting the card data
// but that means I have to implement my own spacing algorithm
// this is probably not easy to do
// besides, being able use anki 
// as the interface for managing the deck
// saves me time from reimplementing a bunch of features
// what if create to separate projects
// one standalone game, and one anki game UI 


// TODO: Make a proper Card type
// Change VocabKanji -> Vocab 
// Change SentKanji  -> Example 
// ... etcetera


class Defer
{
	struct Disposeable : IDisposable
	{
		Action? action;
		public Disposeable(Action action) { this.action = action; }
		public void Dispose()
		{
			if (action != null) action();
			action = null;
		}
	}

	public static IDisposable Run(Action action)
	{
		return new Disposeable(action);
	}
}

public class JP
{
	public static bool HasJapanese(string text)
	{
		return true;
		// TODO: 
		//return Regex.IsMatch(text, @"[一-龯]");
	}
}