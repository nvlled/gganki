
using Love;
using System;
using gganki_love;
using System.Collections.Generic;

enum WeaponState
{
	OnHand,
	Attacking,
	Returning
}


public class Script : View
{
	List<Monster> monsters = new List<Monster> { };
	List<View> subScripts = new List<View> { };

	System.Random rand = new System.Random();
	public SharedState state;

	WeaponEntity sword;

	GAxis axisLeft = new GAxis(GAxisSide.Left, 50) { pos = new Vector2(100, Graphics.GetHeight() - 100) };
	GAxis axisRight = new GAxis(GAxisSide.Right, 50) { pos = new Vector2(Graphics.GetWidth() - 100, Graphics.GetHeight() - 100) };

	Plotter plotter;


	public Script(SharedState state)
	{
		this.state = state;
		subScripts = new List<View> { };
	}
	public void Load()
	{
		state.player.pos = Vector2.Zero;
		//state.player.debug = false;

		var katana = Entity.Create(3059);
		sword = new WeaponEntity(katana, new Vector2(1, 0), new Vector2(0, 1));
		sword.entity.pos = state.center;
		sword.entity.debug = false;
		sword.AttachTo(state.player, new Vector2(-50, 0));


		for (var i = 0; i < 2; i++)
		{
			var mon = new Monster(TileID.RandomMonsterID(), "化物語", state.fontAsian);
			mon.pos = new Vector2(
				state.rand.Next(-300, Graphics.GetWidth() + 300),
				 -20
			);
			mon.target = state.player;
			monsters.Add(mon);
		}


		plotter = new Plotter(sword.entity, state);
		plotter.enabled = false;

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
		GamepadHandler.OnAxis -= OnGamepadAxis;
		GamepadHandler.OnPress -= OnGamepadPress;

		foreach (var m in monsters) m.Dispose();
	}

	float angle = 0.1f;
	public void Update()
	{
		UpdateSubScripts();

		//sword.enabled = axisRight.activeDir.Length() > 0;

		if (Gamepad.IsPressed(GamepadButton.A))
		{
			sword.DoAction();
		}

		foreach (var m in monsters)
		{
			m.Update();

			//var hit = m.rect.Contains(sword.GetEndPos()) || m.rect.Contains(sword.entity.pos);
			var hit = sword.HasHit(m);
			if (hit && sword.enabled)
			{
				m.Hit();
			}
		}

		axisLeft.Update();
		axisRight.Update();
		plotter.Update();

		sword.Update();
		sword.PointAt(axisRight.movingDir);
		axisRight.buildMomentum = Gamepad.IsDown(GamepadButton.LeftShoulder);

		state.player.pos += state.player.speed * axisLeft.activeDir;
	}

	public void Draw()
	{
		DrawSubScripts();

		foreach (var m in monsters) m.Draw();

		state.atlasImage?.StartDraw();
		sword.Draw();
		state.atlasImage?.EndDraw();

		plotter.Draw();
		axisLeft.Draw();
		axisRight.Draw();

		//var p = new Vector2(sword.entity.rect.Right, sword.entity.rect.Top);
		//Graphics.SetColor(Color.Yellow);
		//Graphics.Circle(DrawMode.Fill, sword.GetHandlePos(), 10);
		//Graphics.SetColor(Color.Orange);
		//Graphics.Circle(DrawMode.Fill, sword.GetNewCenterPos(sword.GetHandlePos()), 10);

		//var r = sword.GetRotatedRay();
		//var rect = new RectangleF(50, 50, 150, 150);
		//var p = Vector2.Zero;
		//r.Intersects(rect, out p);
		//var tipDistance = (p - sword.GetEndPos()).Length();
		//var hit = rect.Contains(sword.GetEndPos()) || rect.Contains(sword.entity.pos);
		//Graphics.Rectangle(hit ? DrawMode.Fill : DrawMode.Line, rect);

		//Graphics.SetColor(Color.Green);
		//Graphics.Circle(DrawMode.Fill, p, 20);
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

		public Entity holder;
		public Vector2 holdOffset;

		Vector2 handlePoint;
		Vector2 endPoint;
		Vector2 tileDir;

		float shiftAngle;
		public bool enabled = true;

		Vector2 lastEndPos = new Vector2();
		Vector2 velocity = new Vector2();


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
			else
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
			SetHandlePosition(holder.pos + holdOffset);
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
			if (!m.rect.Contains(GetEndPos()) && !m.rect.Contains(entity.pos))
			{
				return false;
			}
			return velocity.Length() > 20;
		}
	}

	public enum GAxisSide
	{
		Left,
		Right
	}
	public class GAxis
	{
		GAxisSide side;
		public Vector2 activeDir;
		public Vector2 passiveDir;
		public Vector2 movingDir;

		float momentum;
		public bool buildMomentum = false;

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

			momentum += dir;
			momentum *= 0.99f;

			if (pointing > 0.5f)
			{
				momentum = 0;
			}


			movingDir = movingDir + activeDir * 0.5f;
			movingDir.Normalize();

			if (buildMomentum)
			{
				movingDir = Polar.Rotate(momentum / 5, movingDir);
			}

		}

		public void Draw()
		{
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

	// TODO:
	// entity.FlipViewX()
	// entity.FlipViewY()
	// entity.rect rename to drawRect
	// entity.drawRotation
	// entity.hitbox
	// entity.moveDir
	// entity.pos
	// entity.vel
	// entity.accel


	public class Monster
	{
		enum State { Exploring, Fleeing, Approaching, Attacked }
		private struct Attacked
		{
			public float elapsed;
			public float numBlinks;
			public Vector2 dir;
		}


		Entity entity;
		public Entity target;
		string text;

		Text textObject;

		State logicState = State.Approaching;

		Attacked attackedData;


		public Vector2 pos
		{
			get { return entity.pos; }
			set { entity.pos = value; }
		}
		public RectangleF rect
		{
			get { return entity.rect; }
		}

		public Monster(int tileID, string text, Font? font = null)
		{
			entity = Entity.Create(tileID);
			this.text = text;

			font ??= Graphics.GetFont();
			this.textObject = Graphics.NewText(font, text);
		}

		public void UpdateAttacked()
		{
			if (logicState == State.Attacked)
			{
				if (attackedData.numBlinks > 2)
				{
					logicState = State.Approaching;
					entity.color = Color.White;
				}
				else if (attackedData.elapsed == 0 || attackedData.elapsed > .30f)
				{
					var entityColor = (attackedData.numBlinks * 10) % 2 == 0 ? Color.Red : Color.White;
					attackedData.elapsed = 0;
					attackedData.numBlinks += 1f;
					entity.color = entityColor;
				}
				pos += -attackedData.dir * 1.5f;
				attackedData.elapsed += Love.Timer.GetDelta();
			}

		}

		public void UpdateApproach()
		{
			if (target != null)
			{
				var dir = target.pos - pos;
				if (dir.Length() > 150)
				{
					entity.pos += Vector2.Normalize(dir) * entity.speed;
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
				UpdateAttacked();
		}
		public void Draw()
		{
			entity.Draw();

			Graphics.Push();

			var t = textObject;
			var w = t.GetWidth();
			var h = t.GetHeight();
			var x = entity.rect.Center.X - w / 2;
			var y = entity.rect.Top - h / 2;
			var r = new RectangleF(x, y, w, h);
			Graphics.SetFont(SharedState.instance.fontAsian);
			Graphics.SetColor(Color.Yellow);
			//Graphics.Rectangle(DrawMode.Line, r);
			Graphics.SetColor(entity.color);
			//Graphics.Rectangle(DrawMode.Line, rect);
			Graphics.Draw(textObject, x, y);
			//Graphics.Print("くさ草", pos.X, pos.Y);
			//Graphics.SetFont(null);
			Graphics.Pop();


		}

		public void Hit()
		{
			if (logicState != State.Attacked)
			{

				logicState = State.Attacked;
				attackedData.numBlinks = 0;
				attackedData.elapsed = 0;
				attackedData.dir = Vector2.Normalize(target.pos - pos);


				// uhh, attacking by rotating the 
				// joystick in full circle motions isn't as fun as I though
				// it might even incur an injury if I do
				// it for prolonged period of time
				// just throwing the weapon seems more fun

				// TODO: player dash movement
				// TODO: camera view for zooming and panning
				// weapon.setHandlePos(player.FromRelativePos(0.5, 0))
				// TODO: implement other monster logic states

				// TODO: z-order, sort by y
				// TODO: vary monster run speed
				// TODO: monsters should avoid overstepping each other

				// TODO: add one more weapon (change with B button)
			}
		}

		public void Dispose()
		{
			textObject.Clear();
			textObject.Dispose();
		}
	}


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