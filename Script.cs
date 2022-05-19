
using Love;
using System;
using gganki_love;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

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
		GAxis.right.buildMomentum = Gamepad.IsDown(GamepadButton.LeftShoulder);

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
		if (gamepad is null)
		{
			return;
		}

		var (xType, yType) = side == GAxisSide.Left
			? (GamepadAxis.LeftX, GamepadAxis.LeftY)
			: (GamepadAxis.RightX, GamepadAxis.RightY);

		var axisX = gamepad.GetGamepadAxis(xType);
		var axisY = gamepad.GetGamepadAxis(yType);

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

	PartitionedList<Monster> monsters = new PartitionedList<Monster>(SharedState.instance.worldTileSize);

	public MonsterGroup() { }

	public static MonsterGroup CreateFromTexts(IEnumerable<string> texts)
	{
		var group = new MonsterGroup();
		var font = SharedState.instance.fontAsian;
		foreach (var text in texts)
		{
			var mon = new Monster(TileID.RandomMonsterID(), text, font);
			mon.pos = new Vector2(
				Random.Shared.Next(-100, Graphics.GetWidth() + 100),
				Random.Shared.Next(-100, Graphics.GetWidth() + 100)
			//50, 50
			);
			//mon.target = testPlayer.entity;
			group.monsters.Add(mon);
			mon.group = group;
		}
		return group;
	}
	public static MonsterGroup CreateFromCards(List<CardInfo> cards)
	{
		var group = new MonsterGroup();
		var font = SharedState.instance.fontAsian;
		foreach (var card in cards)
		{
			var text = card.fields?["VocabKanji"]?.value ?? "";
			var mon = new Monster(TileID.RandomMonsterID(), text, font);
			mon.pos = new Vector2(
				Random.Shared.Next(-100, Graphics.GetWidth() + 100),
				Random.Shared.Next(-100, Graphics.GetWidth() + 100)
			);
			group.monsters.Add(mon);
			mon.group = group;
			mon.card = card;
		}
		return group;
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

}


public class Monster : IPos
{
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
	public struct Dead
	{
		public float elapsed;
	}

	public Entity entity;
	public Entity? target;
	string text;

	Text textObject;

	public MonsterGroup? group;

	public Attacked attacked = new Attacked();
	public Approaching approaching = new Approaching();
	public Dead dead = new Dead();
	public State logicState = State.Approaching;

	public CardInfo? card;

	int seed = Random.Shared.Next(0, 100);

	public float health = 100;


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
		Graphics.SetFont(SharedState.instance.fontAsian);

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


		var popover = card?.fields?["VocabDef"]?.value;
		if (logicState == State.Attacked && popover != null && health < 50)
		{
			var c = pos + attacked.popoverOffset;
			Graphics.SetColor(Color.WhiteSmoke);
			Graphics.SetFont(SharedState.instance.fontTiny);
			Graphics.Print(popover, c.X, c.Y);
			Graphics.SetFont();
		}
		else if (!IsAlive())
		{
			var c = pos;
			Graphics.SetColor(Color.WhiteSmoke);
			Graphics.SetFont(SharedState.instance.fontSmall);
			Graphics.Print(popover, c.X, c.Y);
			Graphics.SetFont();
		}
	}
	public bool CanDamage()
	{
		return logicState != State.Dead;
	}

	public void Hit(Vector2? weapon = null)
	{
		if (logicState == State.Attacked || logicState == State.Dead)
		{
			return;
		}

		if (health > 0)
		{
			health -= Random.Shared.Next(20, 40);

			if (health <= 0)
			{
				health = 0;
				dead.elapsed = 0;
				logicState = State.Dead;
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
		var center = SharedState.instance.center;
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
		var center = SharedState.instance.center;
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
		Graphics.Circle(DrawMode.Fill, SharedState.instance.center, 5);
	}
}



public class World
{
	Vector2 size = new Vector2(5000, 5000);
	Vector2 origin = new Vector2(0, 0);
	int tileSize = SharedState.instance.worldTileSize;
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
	public enum State { Initializing, Playing, Gameover }
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
	MonsterGroup monsters;

	public State gameState = State.Playing;
	public Gameover gameover = new Gameover();

	public Game(SharedState state)
	{
		this.state = state;

		cam = new Camera();
		player = new Player();
		world = new World(player);
		monsters = new MonsterGroup();

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
			monsters.Dispose();
			StartGame();
		}
	}
	public void StartGame()
	{

		cam = new Camera();
		player = new Player();
		world = new World(player);

		player.entity.pos = state.center;
		var numText = 20;
		var deckName = state.lastDeckName ?? state.deckNames.Keys.First();
		var cards = state.deckCards?[deckName]?.ToList() ?? new List<CardInfo>();
		cards.Sort((a, b) => Random.Shared.Next(-1, 2));
		cards = cards.Take(numText).ToList();

		AnkiAudioPlayer.LoadCardAudios(cards);

		monsters = MonsterGroup.CreateFromCards(cards);
		foreach (var m in monsters.Iterate())
		{
			m.target = player.entity;
		}

		gameState = State.Initializing;
	}

	public void Draw()
	{

		cam.StartDraw();
		{
			world.Draw();
			player.Draw();
			monsters.Draw();
		}
		cam.EndDraw();

		if (gameState == State.Playing)
		{
			player.DrawInterface();
		}
		else if (gameState == State.Gameover)
		{
			DrawGameover();
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

	public void UpdatePlaying()
	{
		player.Update();

		if (Gamepad.IsPressed(GamepadButton.A))
		{
			player.DoAction1();
		}
		else if (Gamepad.IsPressed(GamepadButton.B))
		{
			player.DoAction2();
		}


		var sword = player.sword;
		foreach (var m in monsters.GetMonstersAt(sword.GetEndPos()))
		{
			var hit = sword.HasHit(m);
			if (hit && sword.enabled && m.IsAlive())
			{
				m.Hit(sword.pos);

				var filename = m?.card?.fields?["VocabAudio"].value;
				AnkiAudioPlayer.Play(filename);
			}
		}
		foreach (var m in monsters.GetMonstersAt(player.pos))
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
	}

	public void Update()
	{
		cam.Update();
		cam.CenterAt(player.pos);

		monsters.Update();

		if (gameState == State.Playing)
		{
			UpdatePlaying();
		}
		else if (gameState == State.Gameover)
		{
			UpdateGameover();
		}
		else if (gameState == State.Initializing)
		{
			gameState = State.Playing;
		}
	}

	public void Unload()
	{
		monsters.Dispose();
	}

}
// TODO: game scenes/stages
// - monster target hunting
//   - hunt N monsters, .e.g. hunt 5 草
//   - split monster to several monsters by each SentKanji character on death
//   - alternate between VocabKanji and SentKanji
//   - show target kanji and count on UI
//   - with reps
//   - on successful hunt, end current level
//     - show card details

//   - X add pseudo monster texts
//     X replace characters with random kanji

// - non-target monsters take less damage

// - target monsters with card.type == new
//   -  change VocabKanji to SentKanji
//   -  change VocabDef to SentEng
//   - split monster to several monsters by each SentKanji character on death
//   - make it boss-like, larger and more health and damage
//     - less monsters for less noise



// - monster  (re)spawning
// - pickables (sword, health, bomb)

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


// TODO: I may need to remove the anki dependency
// and add the card contents as static asset
// that will defnitely make it easier to share publicly