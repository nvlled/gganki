
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
	List<View> subScripts = new List<View> { };

	System.Random rand = new System.Random();
	public SharedState state;

	WeaponEntity sword;


	float acceleration = 0;

	GAxis axisLeft = new GAxis(GAxisSide.Left, 50) { pos = new Vector2(100, Graphics.GetHeight() - 100) };
	GAxis axisRight = new GAxis(GAxisSide.Right, 50) { pos = new Vector2(Graphics.GetWidth() - 100, Graphics.GetHeight() - 100) };

	Plotter plotter;

	public Script(SharedState state)
	{
		this.state = state;
		subScripts = new List<View>
		{
			//new TilePointSelector(this),
		};
	}

	public void OnGamepadAxis(Joystick _, GamepadAxis axis, float value)
	{
	}

	public Vector2 RotateByPoint(Vector2 point, Vector2 pivot, float angle)
	{
		var p = Vector2.RotateRadian(point - pivot, angle);
		return pivot + p;
	}

	public Vector2 MoveByRelativePos(Vector2 pos, Vector2 srcPos, Vector2 destPos)
	{
		var v = destPos - srcPos;
		return pos + v;
	}

	public void OnGamepadPress(Joystick _, GamepadButton button)
	{
		if (button == GamepadButton.A)
		{
			acceleration = 30;
		}
	}

	public void Load()
	{
		state.player.pos = Vector2.Zero;
		//state.player.debug = false;

		var katana = new Entity(state.atlasImage, 3059, "test");
		sword = new WeaponEntity(katana, new Vector2(1, 0), new Vector2(0, 1));
		sword.entity.pos = state.center;
		sword.entity.debug = false;


		plotter = new Plotter(sword.entity, state);
		plotter.enabled = false;

		GamepadHandler.OnAxis += OnGamepadAxis;
		GamepadHandler.OnPress += OnGamepadPress;
	}
	public void Unload()
	{
		GamepadHandler.OnAxis -= OnGamepadAxis;
		GamepadHandler.OnPress -= OnGamepadPress;
	}

	float angle = 0.1f;
	public void Update()
	{
		UpdateSubScripts();
		axisLeft.Update();
		axisRight.Update();
		plotter.Update();

		sword.Update();
		sword.PointAt(axisRight.passiveDir);

		if (Mouse.IsDown(0))
		{

			sword.SetHandlePosition(Mouse.GetPosition());
		}

		state.player.pos += state.player.speed * axisLeft.activeDir;
		sword.SetHandlePosition(state.player.pos + new Vector2(-50, 0));

		//sword.PointAt(axisLeft.passiveDir);
		//sword.entity.radianAngle = angle;
		//sword.entity.radianAngle = Polar.FromVector(axisLeft.passiveDir).angle;

		//weapon.radianAngle = Polar.FromVector(weapon.dir).angle + 3.92f;
		//weapon.radianAngle = Polar.FromVector(weapon.dir).angle;

		//weapon.pos += weaponMoveDir * 10;

		/*
		if (acceleration > 0)
		{
			weapon.pos += weapon.dir * acceleration;
			acceleration -= 1;
		}
		else
		{
			acceleration = 0;
		}
		*/



		//angle += -0.01f;
	}

	public void Draw()
	{
		DrawSubScripts();
		state.atlasImage?.StartDraw();
		sword.Draw();
		state.atlasImage?.EndDraw();

		plotter.Draw();
		axisLeft.Draw();
		axisRight.Draw();

		//var p = new Vector2(sword.entity.rect.Right, sword.entity.rect.Top);
		Graphics.SetColor(Color.Yellow);
		Graphics.Circle(DrawMode.Fill, sword.GetHandlePos(), 10);
		Graphics.SetColor(Color.Orange);
		Graphics.Circle(DrawMode.Fill, sword.GetNewCenterPos(sword.GetHandlePos()), 10);

		var r = sword.GetRotatedRay();
		var rect = new RectangleF(50, 50, 150, 150);
		var p = Vector2.Zero;
		r.Intersects(rect, out p);
		var tipDistance = (p - sword.GetEndPos()).Length();
		var hit = rect.Contains(sword.GetEndPos()) || rect.Contains(sword.entity.pos);
		Graphics.Rectangle(hit ? DrawMode.Fill : DrawMode.Line, rect);

		Graphics.SetColor(Color.Green);
		Graphics.Circle(DrawMode.Fill, p, 20);
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
		public Entity entity;
		Vector2 handlePoint;
		Vector2 endPoint;
		Vector2 tileDir;

		float shiftAngle;

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

		public void RotateAt(Vector2 dir)
		{
			entity.dir = dir;
			entity.radianAngle = shiftAngle + Polar.FromVector(dir).angle;
		}

		public void PointAt(Vector2 dir)
		{

			// TODO: sync weapon pos with player movement
			// TODO: create moving text objects
			// TODO: camera view for zooming and panning
			// weapon.setHandlePos(player.FromRelativePos(0.5, 0))

			var oldPos = GetHandlePos();
			entity.dir = dir;
			entity.radianAngle = shiftAngle + Polar.FromVector(dir).angle;
			SetHandlePosition(oldPos);
		}

		public Ray2D GetRotatedRay()
		{
			var start = GetHandlePos();
			//return new Ray2D(start, -GetRotatedDir());
			return new Ray2D(start, GetEndPos() - start);
		}


		public void Draw()
		{
			entity.Draw();
			var start = GetHandlePos();
			//var end = start + GetRotatedDir() * this.tileDir.Length();
			var end = GetEndPos();
			var r = GetRotatedRay();
			Graphics.SetColor(Color.Red);
			Graphics.Line(r.Original, r.Original + r.Direction);
		}

		public void Update()
		{
			entity.Update();
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

		public Vector2 pos;
		public float radius;

		public GAxis(GAxisSide side, float radius = 50)
		{
			this.radius = radius;
			this.side = side;
			this.pos = Vector2.Zero;
			this.activeDir = Vector2.Zero;
			this.passiveDir = Vector2.UnitY;
		}

		public void Update()
		{
			Joystick? gamepad = null;
			foreach (var js in Joystick.GetJoysticks())
			{
				if (js.IsConnected())
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

			if (axisX != 0 || axisY != 0)
			{
				passiveDir = new Vector2(axisX, 0) + Vector2.UnitY * passiveDir;
				passiveDir = new Vector2(0, axisY) + Vector2.UnitX * passiveDir;
			}
			passiveDir.Normalize();
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

			Graphics.SetLineWidth(2);
			Graphics.SetColor(Color.Blue);
			Graphics.Line(pos, pos + passiveDir * radius);
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



			// TODO: insert on rotate rect, point2
			if (Mouse.IsDown(0) && entity.rect.Contains(Mouse.GetPosition()))
			{
				point1 = Mouse.GetPosition();
				point2 = EntityUtil.RotateByPoint(point1, entity.rect.Center, entity.radianAngle);

				//point2 = Mouse.GetPosition();
				//point1 = EntityUtil.RotateByPoint(point2, entity.rect.Center, -entity.radianAngle);

				//point3 = EntityUtil.RotateByPoint(point2, entity.rect.Center, -entity.radianAngle);
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
		Entity entity;
		string text;

		public Monster()
		{

		}
	}
}