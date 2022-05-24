
using Love;
using System;
using gganki_love;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

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

	Game game;


	public Script(SharedState state)
	{
		this.state = state;
		subScripts = new List<View> { };

		game = new Game(state);
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
		game.Unload();
		GamepadHandler.OnAxis -= OnGamepadAxis;
		GamepadHandler.OnPress -= OnGamepadPress;
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



public class MonsterGroup
{
	HashSet<Monster> hostileMonsters = new HashSet<Monster>();

	PartitionedList<Monster> monsters = new PartitionedList<Monster>(SharedState.self.worldTileSize);

	public MonsterGroup() { }

	/*
		public static MonsterGroup CreateFromTexts(IEnumerable<string> texts)
		{
			var group = new MonsterGroup();
			var font = SharedState.self.fontAsian;
			foreach (var text in texts)
			{
				var mon = new Monster(TileID.RandomMonsterID(), text, font);
				mon.pos = new Vector2(
					Random.Shared.Next(-100, Graphics.GetWidth() + 100),
					Random.Shared.Next(-100, Graphics.GetWidth() + 100)
				//50, 50
				);
				//mon.target = testPlayer.entity;
				mon.group = group;
				group.monsters.Add(mon);
			}
			return group;
		}
	*/
	public static MonsterGroup CreateFromCards(List<CardInfo> cards)
	{
		var group = new MonsterGroup();
		var font = SharedState.self.fontAsian;
		foreach (var card in cards)
		{
			var text = card?.GetField("VocabKanji") ?? "";
			var audioFilename = card?.GetField("VocabAudio") ?? "";
			var damageText = card?.GetField("VocabDef") ?? "";
			var mon = new Monster(TileID.RandomMonsterID(), text, font);

			mon.pos = new Vector2(
				Random.Shared.Next(-100, Graphics.GetWidth() + 100),
				Random.Shared.Next(-100, Graphics.GetWidth() + 100)
			);

			group.monsters.Add(mon);
			mon.group = group;
			mon.card = card;
			mon.audioFilename = audioFilename;
			mon.damageText = damageText;
		}
		return group;
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

	public IEnumerable<Monster> GetMonstersAt(Vector2 pos)
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
		//foreach (var m in monsters.Iterate()) m.Dispose();
		monsters = new PartitionedList<Monster>(monsters.partitionSize);
	}

	public void Remove(Monster m)
	{
		monsters.Remove(m);
	}

	public Monster? GetRandom(Monster except)
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


public class Monster : IPos
{
	public event Action<Monster> OnMonsterKill = (e) => { };

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
		var y = entity.rect.Top - h / 2;
		var r = new RectangleF(x, y, w, h);

		Graphics.SetColor(IsAlive() ? Color.White : Color.WhiteSmoke);
		Graphics.SetFont(SharedState.self.fontAsian);

		Graphics.SetColor(entity.color);
		Graphics.Draw(textObject, x, y);

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

	public void Hit(Vector2? weapon = null, float damage = 1)
	{
		if (logicState == State.Attacked || logicState == State.Dead || logicState == State.Fleeing)
		{
			return;
		}

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

		var attackPos = weapon ?? target?.pos;
		if (attackPos.HasValue)
		{
			attacked.dir = Vector2.Normalize(attackPos.GetValueOrDefault() - pos);
		}
		else
		{
			attacked.dir = Vector2.Normalize(new Vector2(
				Random.Shared.Next(1, 2),
				Random.Shared.Next(1, 2)
			));
		}
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
	public float zoom = 1.0f;

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
		Graphics.Circle(DrawMode.Fill, SharedState.self.center, 5);
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
}

enum WeaponState
{
	OnHand,
	Attacking,
	Returning
}



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
				newMonster.OnMonsterKill += OnMonsterKill;
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
				newMonster.pos = Vector2Ext.Random(Graphics.GetWidth());
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

// TODO: programming intermission: check out coroutine library
// and see if it simplifies some complicated state transitions

// - target monsters with card.type == new
//   -  change VocabKanji to SentKanji
//   -  change VocabDef to SentEng
//   - split monster to several monsters by each SentKanji character on death
//   - make it boss-like, larger and more health and damage
//     - less monsters for less noise


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
// TODO: z-order, sort by y

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