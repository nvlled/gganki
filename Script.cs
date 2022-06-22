
using Love;
using System;
using gganki_love;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AwaitableCoroutine;
using MyNihongo.KanaDetector.Extensions;
using Love.Misc;
using static gganki_love.RandomContainer;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

public class Script : View
{
    List<View> subScripts = new List<View> { };

    public SharedState state;

    MonsterGroup monsters;

    StartScreen startScreen;
    HuntingGame game;

    View currentView;


    public Script(SharedState state)
    {
        this.state = state;
        subScripts = new List<View> { };

        game = new HuntingGame(state);
        startScreen = new StartScreen(state);

        currentView = startScreen;
    }


    public void Load()
    {

        currentView.Load();


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
        currentView.Unload();
        GamepadHandler.OnAxis -= OnGamepadAxis;
        GamepadHandler.OnPress -= OnGamepadPress;

        state.windowEntity.ClearComponents();
    }

    public void Update()
    {

        GAxis.left.Update();
        GAxis.right.Update();
        GAxis.right.buildMomentum = Gamepad.IsDown(GamepadButton.LeftShoulder)
                      || Keyboard.IsDown(KeyConstant.LShift);

        currentView.Update();
    }

    public void Draw()
    {
        currentView.Draw();
    }


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
    public enum State { OnHand, Throwing, Returning, Slash, Thrust, Spinning, Whirlwind, Barrage }

    public class SlashData
    {
        public Vector2 startDir = Vector2.UnitX;
        public Vector2 dir = Vector2.UnitX;
        public float radians = 0;
        public float steps = 0;
        public int sign;
    }
    public class ThrustingData
    {
        public Vector2 dir = Vector2.UnitX;
        public float maxSteps = 0;
        public float steps = 0;
        public float shift = 0;
    }
    public class SpinningData
    {
        public float maxSteps = 0;
        public float steps = 0;
        public Vector2 dir;
        public int sign;
    }
    public class WhirlwindData
    {
        public float chargeTime = 0;
        public float minSteps = 2;
        public float maxSteps = 4;
        public float steps = 0;
        public float radius = 0;
        public float maxRadius = 0;
        public bool charging;
        public Color color;
        public float colorStep;
        public float scale;
        public float manaCost = 10.0f;
    }
    public class ThrowingData
    {
        public const float maxSteps = 25;
        public const float haltDelay = 2;

        public float steps = 0;

        public float haltElapsed = 0;
    }
    public class ReturningData
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

    float lastAction = Love.Timer.GetTime();

    public State logicState = State.OnHand;
    public SlashData slashing = new SlashData();
    public ThrustingData thrusting = new ThrustingData();
    public SpinningData spinning = new SpinningData();
    public WhirlwindData whirlwind = new WhirlwindData();
    public ThrowingData throwing = new ThrowingData();
    public ReturningData returning = new ReturningData();

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

    public void Thrust()
    {
        if (holder == null)
        {
            return;
        }
        if (logicState != State.OnHand && logicState != State.Slash)
        {
            return;
        }

        logicState = State.Thrust;
        thrusting.steps = 0;
        thrusting.shift = 0;
        thrusting.maxSteps = Random.Shared.Next(120, 180);
        thrusting.dir = Vector2.Normalize(entity.pos - holder.pos);
    }

    public void Slash()
    {
        if (holder == null)
        {
            return;
        }
        if (logicState != State.OnHand && logicState != State.Slash)
        {
            return;
        }

        var v = entity.pos - holder.pos;
        var radians = (100 + Random.Shared.Next(40)) * MathF.PI / 180;
        logicState = State.Slash;
        slashing.sign = Xt.MathF.RandomSign();
        slashing.radians = radians + (90 + Random.Shared.Next(40)) * MathF.PI / 180;
        slashing.steps = 0;
        slashing.startDir = Vector2.Normalize(v);
        slashing.dir = Polar.Rotate(-slashing.sign * radians, slashing.startDir);


        var oldPos = GetHandlePos();
        RotateAt(slashing.dir);
        SetHandlePosition(oldPos);
    }
    public void Spin()
    {
        logicState = State.Spinning;
        spinning.steps = 0;
        spinning.maxSteps = 20;
        spinning.sign = Xt.MathF.RandomSign();
        spinning.dir = Vector2.Normalize(entity.pos - holder.pos);
    }

    public void DoAction1()
    {
        if (logicState == State.Throwing)
        {
            HaltThrow();
            return;
        }

        var now = Love.Timer.GetTime();
        if (now - lastAction < 0.25f)
        {
            return;
        }
        lastAction = now;

        switch (Random.Shared.Next(1, 6))
        {
            case 1:
            case 2:
            case 3: Slash(); break;
            case 4: Thrust(); break;
            case 5: Spin(); break;
        }

        //switch (logicState)
        //{
        //	case State.OnHand: Throw(); break;
        //	case State.Throwing: HaltThrow(); break;
        //}
    }

    public void DoAction2()
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

    public void ChargeWhirldwind()
    {
        if (logicState == State.Whirlwind)
        {
            return;
        }
        logicState = State.Whirlwind;
        whirlwind.charging = true;
        whirlwind.chargeTime = 0;
        whirlwind.colorStep = -1;
        whirlwind.steps = 0;

        whirlwind.color = Color.White;
    }

    public void ReleaseWhirlwind()
    {
        if (logicState != State.Whirlwind)
        {
            return;
        }
        var t = MathF.Max(whirlwind.chargeTime, 6);
        logicState = State.Whirlwind;
        whirlwind.scale = entity.scale;
        whirlwind.charging = false;
        //whirlwind.steps = t * 10;
        //whirlwind.radius = 0;
        //whirlwind.maxRadius = t;
        whirlwind.steps = MathF.Max(whirlwind.minSteps, whirlwind.steps);
        whirlwind.color = new Color(255, 255, 255, 255);
        whirlwind.colorStep = 1;
        entity.radianAngle = 0;
        entity.color = Color.White;
        entity.scale = 2.9f;
    }

    public bool UpdateWhirlwind()
    {
        if (whirlwind.charging)
        {
            var color = whirlwind.color;
            var n = whirlwind.colorStep * 0.07f;
            if (color.Rf + n >= 1)
            {
                color.Rf = 1;
                whirlwind.colorStep = -1;
            }
            else if (color.Rf + n <= 0)
            {
                color.Rf = 0;
                whirlwind.colorStep = 1;
            }
            else
            {
                color.Rf += n;
            }
            whirlwind.color = color;
            entity.color = whirlwind.color;
            whirlwind.steps += 0.1f;
            whirlwind.steps = MathF.Min(whirlwind.steps, whirlwind.maxSteps);
            PointAt(Vector2.Normalize(entity.pos - holder.pos));
        }
        else if (whirlwind.steps > 0)
        {
            whirlwind.steps -= 0.03f;
            entity.radianAngle += whirlwind.steps;
        }
        else
        {
            logicState = State.OnHand;
            entity.scale = whirlwind.scale;
        }

        UpdateOnHand();
        return false;
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

    public bool UpdateThrusting()
    {
        var step = 12;
        thrusting.steps += step;
        if (thrusting.steps < thrusting.maxSteps / 2)
        {
            thrusting.shift += step;
        }
        else if (thrusting.steps > thrusting.maxSteps / 2)
        {
            logicState = State.OnHand;
        }
        else
        {
            thrusting.shift -= step;
        }
        UpdateOnHand(thrusting.dir * thrusting.shift);
        return true;
    }
    public bool UpdateSlashing()
    {
        var step = (MathF.PI / 180) * 25;
        slashing.steps += step;
        if (slashing.steps < slashing.radians)
        {
            slashing.dir = Polar.Rotate(slashing.sign * step, slashing.dir);
        }
        else if (slashing.steps > slashing.radians * 0.75f)
        {
            slashing.dir = slashing.startDir;
            logicState = State.OnHand;
        }
        else
        {
            slashing.dir = Polar.Rotate(-slashing.sign * step / 3, slashing.dir);

        }

        var oldPos = GetHandlePos() + slashing.startDir * slashing.steps * 2 * -slashing.sign;
        RotateAt(slashing.dir);
        SetHandlePosition(oldPos);
        UpdateOnHand();

        //logicState = State.OnHand;
        return true;
    }

    public bool UpdateSpinning()
    {
        spinning.steps++;
        if (spinning.steps > spinning.maxSteps)
        {
            logicState = State.OnHand;
            return false;
        }

        var oldPos = GetHandlePos();
        entity.radianAngle += spinning.sign * 40 * MathF.PI / 180;
        if (entity.radianAngle > MathF.PI)
        {
            entity.radianAngle = 0;
        }
        entity.pos = holder.pos + spinning.dir * entity.rect.Width * spinning.steps / spinning.maxSteps;

        return false;
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
            velocity *= 0;
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

    public bool UpdateOnHand(Vector2? shift = null)
    {
        if (holder is null) { return false; }

        var offset = new Vector2(
            holder.flipX * holdOffset.X * holder.rect.Width / 2,
            holder.flipY * holdOffset.Y * holder.rect.Height / 2
        );
        SetHandlePosition(holder.pos + offset + shift.GetValueOrDefault(Vector2.Zero));
        return true;
    }


    public void Update()
    {
        _ = logicState switch
        {
            State.OnHand => UpdateOnHand(),
            State.Thrust => UpdateThrusting(),
            State.Slash => UpdateSlashing(),
            State.Whirlwind => UpdateWhirlwind(),
            State.Spinning => UpdateSpinning(),
            State.Throwing => UpdateThrowing(),
            State.Returning => UpdateReturning(),
            _ => false,
        };

        entity.Update();
        var endPos = GetEndPos();
        if (logicState != State.OnHand)
        {
            velocity += (endPos - lastEndPos);
            velocity *= 0.5f;
        }
        lastEndPos = endPos;
    }

    public bool HasHit(Monster m)
    {
        if (logicState == State.OnHand)
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

    public bool IsWhirlwindAttacking()
    {
        return logicState == State.Whirlwind && !whirlwind.charging;
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

            activeDir = new Vector2(axisX, 0) + Vector2.UnitY * activeDir;
            activeDir = new Vector2(0, axisY) + Vector2.UnitX * activeDir;
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
                    activeDir = w;
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
                activeDir = new Vector2(axisX, axisY);
            }
        }



        var prevPassiveDir = passiveDir;
        if (axisX != 0 || axisY != 0)
        {
            passiveDir = new Vector2(axisX, 0) + Vector2.UnitY * passiveDir;
            passiveDir = new Vector2(0, axisY) + Vector2.UnitX * passiveDir;
        }
        passiveDir.Normalize();

        /*
		*/
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


public interface IEntity : IPos
{
    public Entity entity { get; set; }
    public new Vector2 pos { get { return entity.pos; } set { entity.pos = value; } }
    public float scale { get { return entity.scale; } set { entity.scale = value; } }
    public float radianAngle { get { return entity.radianAngle; } set { entity.radianAngle = value; } }
    public RectangleF rect { get { return entity.rect; } }

    public void Update() { entity.Update(); }
    public void Draw() { entity.Draw(); }
}

public class Consumable : IEntity
{
    public Entity entity { get; set; }
    public Vector2 pos { get { return entity.pos; } set { entity.pos = value; } }

    public float healthGain = 0;

    public Consumable(int tileID)
    {
        entity = Entity.Create(tileID);
        healthGain = Random.Shared.Next(10, 30);
    }

    public static Consumable CreateRandom()
    {
        return new Consumable(TileID.foods.GetRandom());
    }
}

public class ItemGroup
{

    PartitionedList<IEntity> foods = new PartitionedList<IEntity>(SharedState.self.worldTileSize);

    World? world;
    public HuntingGame game;

    public ItemGroup(HuntingGame g)
    {
        game = g;
        this.world = game.world;
    }


    public IEntity SpawnRandom()
    {
        var item = Consumable.CreateRandom();
        item.pos = world != null ? Xt.Vector2.Random(world.Width, world.Height) : Xt.Vector2.Random();
        foods.Add(item);
        return item;
    }

    public void SpawnAt(Vector2 pos)
    {
        var item = Consumable.CreateRandom();
        item.pos = pos + Xt.Vector2.RandomDir() * Rand.Next(50, 70);
        foods.Add(item);
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
        foods.Clear();
    }


    public IEnumerable<IEntity> Iterate()
    {
        return foods.Iterate();
    }

    public IEnumerable<IEntity> GetItemsAt(RectangleF r)
    {
        return foods.GetItemsAt(
            new Vector2(r.Left, r.Top),
            new Vector2(r.Left, r.Bottom),
            new Vector2(r.Right, r.Top),
            new Vector2(r.Right, r.Bottom)
        );
    }

    public IEnumerable<IEntity> GetItemsAt(params Vector2[] pos)
    {
        return foods.GetItemsAt(pos);
    }

    public void Update()
    {
        foreach (var item in foods.Iterate())
        {
            item.Update();
        }

    }

    public void Draw()
    {
        foreach (var item in foods.Iterate())
        {
            item.Draw();
        }
    }

    public void Dispose()
    {
        foods.Dispose();
    }

    public void Remove(IEntity item)
    {
        foods.Remove(item);
    }
}

public class MonsterGroup
{
    public event Action<Monster> OnKill = (m) => { };
    public event Action<Monster> OnHit = (m) => { };


    PartitionedList<Monster> monsters = new PartitionedList<Monster>(SharedState.self.worldTileSize);

    Entity? defaultTarget;
    World? world;
    public HuntingGame game;

    public MonsterGroup(HuntingGame g)
    {
        game = g;
        this.world = game.world;
    }

    public MonsterGroup(HuntingGame g, Entity target) : this(g)
    {
        defaultTarget = target;
    }

    private void OnMonsterKill(Monster m)
    {
        OnKill(m);
    }

    private void OnMonsterHit(Monster m)
    {
        OnHit(m);
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
    public void RegisterOnHit(Action<Monster> fn)
    {
        OnHit -= fn;
        OnHit += fn;
    }
    public void RegisterOnKill(Action<Monster> fn)
    {
        OnKill -= fn;
        OnKill += fn;
    }

    public void SpawnFromCards(List<CardInfo> cards)
    {
        var font = SharedState.self.fontAsian;
        foreach (var card in cards)
        {
            var text = card?.GetField("VocabKanji") ?? "";
            var audioFilename = card?.GetField("VocabAudio") ?? "";
            var mon = SpawnMonster(text);

            mon.card = card;
            mon.audioFilename = audioFilename;
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
        monsters.Dispose();
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

    public void RemoveByText(string text)
    {
        //Monster? toRemove;
        foreach (var m in monsters.Iterate())
        {
            if (m.text == text && m.logicState.ID != Monster.State.Dead)
            {
                //toRemove = m;
                Remove(m);
                //break;
            }
        }
    }
}

/*
class Monster;

void Update() {
	logicState.Update();
}

*/

public interface IMonsterState
{
    Monster.State ID { get; }
    void Update();
}
public struct Attacked : IMonsterState
{
    public Monster.State ID { get; init; } = Monster.State.Attacked;
    public float elapsed = 0;
    public float numBlinks = 0;
    public Vector2 dir = Vector2.Zero;
    public IMonsterState? prevState = null;

    Monster monster;
    public Attacked(Monster m) { monster = m; }
    public void Update()
    {
        var m = monster;
        if (m.logicState.ID == Monster.State.Attacked)
        {
            if (m.attacked.numBlinks > 1)
            {
                //m.approaching.pause = 0;
                m.entity.color = Color.White;
                m.logicState = m.attacked.prevState ?? m.approaching;
                if (m.logicState.ID == Monster.State.Attacked)
                {
                    m.logicState = m.approaching;
                }
            }
            else if (m.attacked.elapsed == 0 || m.attacked.elapsed > .25f)
            {
                var entityColor = (m.attacked.numBlinks * 10) % 2 == 0 ? Color.Red : Color.White;
                m.attacked.elapsed = 0;
                m.attacked.numBlinks += 1.0f;
                m.entity.color = entityColor;
            }
            m.pos += -m.attacked.dir * 1.5f;
            m.attacked.elapsed += Love.Timer.GetDelta();
        }

    }
}

public struct Approaching : IMonsterState
{
    public Monster.State ID { get; init; } = Monster.State.Approaching;
    public Vector2 dir = Vector2.Zero;
    public float pause = 0;

    public Vector2 diversionPoint = Vector2.Zero;
    public int steps = 0;

    Monster monster;
    public Approaching(Monster m) { monster = m; }

    public void Update()
    {
        var m = monster;
        if (m.target == null)
        {
            return;
        }

        if (m.approaching.pause > 0)
        {
            m.approaching.pause -= Love.Timer.GetDelta();
            return;
        }

        var world = m.group.game.world;
        var targetVec = m.target.pos - m.pos;
        if (targetVec.Length() > 500)
        {
            var point = m.approaching.diversionPoint;
            if (m.approaching.steps % 200 == 0 || Vector2.Distance(point, m.pos) < 10)
            {
                var n = Mathf.Random(800, 1900) * Xt.MathF.RandomSign();
                var diversionPoint = m.target.pos + Vector2.Normalize(Vector2.Rotate(targetVec, 90)) * n;
                m.approaching.diversionPoint = world.RestrictPosition(diversionPoint);
            }
            m.approaching.steps++;
            if (m.approaching.steps > 1000)
            {
                m.approaching.steps = 0;
            }

            m.pos += Vector2.Normalize(point - m.pos) * m.entity.speed * 1.2f;
        }
        else
        {
            if (!DivertFromCollision())
            {
                m.pos += Vector2.Normalize(targetVec) * m.entity.speed;
            }
        }


    }
    public bool DivertFromCollision()
    {
        var self = monster;
        if (self.group == null)
        {
            return false;
        }

        var distanceFromTarget = Vector2.Distance(self.pos, (self.target?.pos) ?? Vector2.Zero);
        var hasCollision = false;

        if (distanceFromTarget > 900)
        {
            return false;
        }

        foreach (var m in self.group.GetMonstersAt(self.rect))
        {
            if (m == self)
            {
                continue;
            }
            if (!self.HasMonstersAround(m))
            {
                return false;
            }

            var v = m.pos - self.pos;
            if (v.Length() == 0)
            {
                v.X = (0.1f + Random.Shared.NextSingle()) * Xt.MathF.RandomSign();
                v.Y = (0.1f + Random.Shared.NextSingle()) * Xt.MathF.RandomSign();
            }
            var dir = Vector2.Normalize(v);
            self.pos += -dir * self.entity.speed * 0.31f;
            hasCollision = true;
        }

        return hasCollision;
    }
}

public struct Fleeing : IMonsterState
{
    public Monster.State ID { get; init; } = Monster.State.Fleeing;
    public float time = 0;
    public Vector2 dir = Vector2.Zero;
    Monster monster;
    public Fleeing(Monster m) { monster = m; }

    public void Update()
    {
        var m = monster;
        m.fleeing.time -= Love.Timer.GetDelta();
        m.pos += m.fleeing.dir * m.entity.speed * 1.2f;
        if (m.fleeing.time <= 0)
        {
            m.logicState = m.approaching;
            m.fleeing.time = 0;
        }

    }
}
public struct Dead : IMonsterState
{
    public Monster.State ID { get; init; } = Monster.State.Dead;
    public float elapsed = 0;
    Monster monster;
    public Dead(Monster m) { monster = m; }

    public void Update()
    {

    }
}
public struct Exploring : IMonsterState
{
    Monster monster;
    public Monster.State ID { get; init; } = Monster.State.Exploring;
    public Exploring(Monster m) { monster = m; }

    Vector2 dir = Xt.Vector2.RandomDir();
    public float steps = 0;
    public float speed = 100;
    public float maxSpeed = 2;

    public void Update()
    {
        if (steps <= 0)
        {
            steps = Random.Shared.Next(100, 350);
            speed = Random.Shared.NextSingle() * maxSpeed;
            dir = Xt.Vector2.RandomDir();
        }
        monster.pos += dir * speed;
        steps -= 1;
    }
}

public struct Idle : IMonsterState
{
    Monster monster;
    public Monster.State ID { get; init; } = Monster.State.Idle;
    public Idle(Monster m) { monster = m; }

    public void Update() { }
}



public class Monster : IPos
{
    [Flags]
    public enum Flags
    {
        None = 0,
        SubTarget = 1,
    }

    public event Action<Monster> OnMonsterKill = (e) => { };
    public event Action<Monster> OnMonsterHit = (e) => { };

    public enum State { Exploring, Fleeing, Approaching, Attacked, Dead, Idle }
    public Entity entity;
    public Entity? target;
    public string text;
    public Color? textColor;
    public string audioFilename;

    public TextEntity textObject;

    public MonsterGroup? group;

    public Attacked attacked;
    public Approaching approaching;
    public Fleeing fleeing;
    public Exploring exploring;
    public Dead dead;
    public IMonsterState logicState;

    public CardInfo? card;

    int seed = Random.Shared.Next(0, 100);

    public float health = 100;
    public float defense = 1;
    public Flags flags = 0;

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
        this.textObject = new TextEntity(text, font, (int)(entity.rect.Width * 7));

        InitState();
    }

    public Monster()
    {
        var font = Graphics.GetFont();
        this.entity = Entity.Create(0);
        this.textObject = new TextEntity("", font);
        InitState();
    }
    private void InitState()
    {
        attacked = new Attacked(this);
        approaching = new Approaching(this);
        fleeing = new Fleeing(this);
        dead = new Dead(this);
        exploring = new Exploring(this);
        logicState = approaching;
    }

    public bool IsPaused()
    {
        return logicState.ID != State.Approaching || approaching.pause > 0;
    }

    public void Pause(float seconds)
    {
        if (logicState.ID == State.Approaching)
        {
            approaching.pause = seconds;
        }
    }


    public bool HasMonstersAround(Monster m)
    {
        return Vector2.Distance(pos, m.pos) < rect.DiagonalLength() * 2.0f;
    }

    public void Update()
    {
        entity.Update();
        logicState.Update();

        if (logicState.ID != State.Dead && health <= 0)
        {
            if (textObject.scale > 0.35f)
            {
                textObject.scale = 0.35f;
            }
            textObject.SetColor(Color.Gray);
            logicState = dead;
            OnMonsterKill(this);
        }
    }

    public void Draw()
    {
        entity.Draw();
        if ((flags & Monster.Flags.SubTarget) != 0)
        {
            //Graphics.SetColor(Color.Blue);
            //Graphics.Rectangle(DrawMode.Line, entity.rect);
        }

        //Graphics.Rectangle(DrawMode.Line, entity.rect);


        var t = textObject;
        var w = t.GetWidth();
        var h = t.GetHeight();
        var x = entity.rect.Center.X - w / 2;
        var y = entity.rect.Top - h * 1.4f;

        t.pos = new Vector2(x, y);

        Graphics.SetColor(IsAlive() ? Color.White : Color.WhiteSmoke);
        Graphics.SetFont(SharedState.self.fontAsian);


        Graphics.SetColor(textObject.bgColor);
        Graphics.Polygon(
            DrawMode.Fill,
            new Vector2(rect.Center.X - 30, textObject.rect.Bottom),
            new Vector2(rect.Center.X, rect.Top),
            new Vector2(rect.Center.X + 30, textObject.rect.Bottom)
        );
        Graphics.SetColor(Color.Gray);
        Graphics.Polygon(
            DrawMode.Line,
            new Vector2(rect.Center.X - 30, textObject.rect.Bottom),
            new Vector2(rect.Center.X, rect.Top),
            new Vector2(rect.Center.X + 30, textObject.rect.Bottom)
        );

        Graphics.SetColor(textColor ?? entity.color);
        textObject.Draw();

        Graphics.SetColor(entity.color);
        if (logicState.ID != State.Dead)
        {
            var healthH = 5;
            Graphics.SetColor(Color.Teal);
            Graphics.Rectangle(DrawMode.Fill, entity.rect.Left, entity.rect.Bottom, (entity.rect.Width * health) / 100, healthH);
            Graphics.SetColor(Color.SkyBlue);
            Graphics.Rectangle(DrawMode.Line, entity.rect.Left, entity.rect.Bottom, entity.rect.Width, healthH);
        }

    }
    public bool CanDamage()
    {
        return logicState.ID != State.Dead;
    }

    public void Flee(float seconds = 2)
    {
        logicState = fleeing;
        fleeing.time += seconds;
        if (target != null)
        {
            var angle = Random.Shared.Next(-45, 45);
            fleeing.dir = Polar.Rotate(angle * MathF.PI / 180, Vector2.Normalize(pos - target.pos));
        }
    }

    public bool CanHit()
    {
        return !(logicState.ID == State.Attacked || logicState.ID == State.Dead || logicState.ID == State.Fleeing);
    }

    public void Hit(Vector2? weapon = null)
    {
        if (!CanHit())
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

        var isWhirlwindAttack = (flags & Monster.Flags.SubTarget) == 0
                           && (weapon?.IsWhirlwindAttacking() ?? false);

        if (isWhirlwindAttack && weapon?.holder != null)
        {
            attackDir = Vector2.Normalize(weapon.holder.pos - pos);
            approaching.pause = 5;
        }
        else if (attackPos.HasValue)
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
        var n = weapon.logicState == WeaponEntity.State.Whirlwind ? 5 : 1;

        Attack(attackDir * n, damage);

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
                entity.radianAngle = Xt.MathF.RandomSign() * MathF.PI / 4;

                return;
            }
        }

        attacked.prevState = logicState;
        logicState = attacked;
        attacked.numBlinks = 0;
        attacked.elapsed = 0;
        attacked.dir = attackDir;

    }

    public void Dispose()
    {
        //textObject.Clear();
        //textObject.Dispose();
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    public Monster Clone()
    {
        var m = new Monster(this.entity.tileID, this.text, textObject.font);
        m.pos = pos;
        //m.entity.rect = rect;
        m.logicState = logicState;
        m.speed = speed;
        m.group = group;
        m.card = card;
        m.target = target;
        m.health = 100;
        m.logicState = new Idle();

        return m;
    }

    public float GetTextSize(Font? font = null)
    {
        return textObject.GetWidth();
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
    public float mana = 0;
    public float damageElapse = 0;
    public float defence = 8.5f;

    HuntingGame game;

    public Player(HuntingGame g)
    {
        game = g;

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
            entity.pos += entity.speed * game.input.GetMotion();
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
    public void PerformMeleeAttack()
    {
        sword.DoAction1();
    }
    public void PerformLongAttack()
    {
        sword.DoAction2();
    }

    public void PerformDash()
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
            health -= damage / defence;
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

        entity.dir = game.input.GetMotion();
        entity.FaceDirectionX(game.input.GetMotion2());
        entity.Update();

        var world = game.world;
        world.RestrictPosition(entity);
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

        entity.color = damageElapse > 0
                     ? Color.Red
                     : sword.logicState == WeaponEntity.State.Whirlwind
                     ? sword.entity.color
                     : Color.White;
        entity.Draw();
        sword.Draw();

        //Graphics.Rectangle(DrawMode.Line, rect);
        //Graphics.Circle(DrawMode.Line, entity.pos, 10);

    }

    public void DrawInterface()
    {
        var healthW = 300;
        var x = 20f;
        Graphics.SetColor(Color.Red);
        Graphics.Rectangle(DrawMode.Fill, x, 20, (healthW * health) / 100, 30);
        Graphics.SetColor(Color.Orange);
        Graphics.Rectangle(DrawMode.Line, x, 20, healthW, 30);

        x = Graphics.GetWidth() - healthW * 1.1f;
        Graphics.SetColor(Color.Blue);
        Graphics.Rectangle(DrawMode.Fill, x, 20, (healthW * mana) / 100, 30);
        Graphics.SetColor(Color.Teal);
        Graphics.Rectangle(DrawMode.Line, x, 20, healthW, 30);
    }

    public void DoCharge1()
    {
        if (mana > sword.whirlwind.manaCost * sword.whirlwind.minSteps)
        {
            sword.ChargeWhirldwind();
        }
    }
    public void ReleaseCharge1()
    {
        mana -= sword.whirlwind.manaCost * sword.whirlwind.steps;
        mana = MathF.Max(mana, 0);
        sword.ReleaseWhirlwind();
    }

    public void AddHealth(float healthGain)
    {
        health = MathF.Min(health + healthGain, 100);
    }
}

public class Camera
{
    public float zoom = 1.10f;

    public Vector2 pos;
    public RectangleF innerRect;
    public Font font = Graphics.NewFont(18);
    public HuntingGame game;

    public Camera(HuntingGame g)
    {
        game = g;
        var center = SharedState.self.center;
        var width = Graphics.GetWidth() * 0.25f;
        var height = Graphics.GetHeight() * 0.25f;
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

    public void ZoomIn()
    {
        zoom += 0.01f;
    }

    public void ZoomOut()
    {
        zoom -= 0.01f;
    }
}



public class World
{
    public Vector2 size = new Vector2(3000, 3000);
    Vector2 origin = new Vector2(0, 0);
    int tileSize = SharedState.self.worldTileSize;
    Color gridColor = new Color(80, 80, 80, 255);

    Canvas gridCanvas;
    Player player;
    HuntingGame game;

    public Vector2 Center
    {
        get
        {
            return origin + new Vector2(size.X / 2, size.Y / 2);
        }
    }

    public float Width { get { return size.X; } }
    public float Height { get { return size.Y; } }

    public World(HuntingGame g)
    {
        game = g;
        this.player = game.player;

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

    public Vector2 RestrictPosition(Vector2 pos)
    {
        var margin = 500;
        var start = origin - Vector2.One * margin;
        var end = size + Vector2.One * margin;

        if (pos.X < start.X)
        {
            pos.X = start.X;
        }
        else if (pos.X > end.X)
        {
            pos.X = end.X;
        }
        if (pos.Y < start.Y)
        {
            pos.Y = start.Y;
        }
        else if (pos.Y > end.Y)
        {
            pos.Y = end.Y;
        }
        return pos;
    }

    public void RestrictPosition(Entity entity)
    {
        var r = entity.rect;
        if (r.Left < origin.X)
        {
            r.Left = origin.X;
        }
        else if (r.Right > size.X)
        {
            r.Right = size.X;
        }
        if (r.Top < origin.Y)
        {
            r.Top = origin.Y;
        }
        else if (r.Bottom > size.Y)
        {
            r.Bottom = size.Y;
        }

        entity.pos = r.Center;
        entity.rect = r;
    }
}

enum WeaponState
{
    OnHand,
    Attacking,
    Returning
}

public class FX
{

    public static Moonshine.Godsray swordRay = new Moonshine.Godsray();
    public static Moonshine.Godsray godsray = new Moonshine.Godsray();
    public static Moonshine screenEffects;
    public static Moonshine swordEffect;

    static FX()
    {

        var scanlines = new Moonshine.Scanlines();
        //scanlines.Opacity = 0.5f;
        //scanlines.Width = 0.8f;
        scanlines.Thickness = 0.2f;

        godsray.Density = 0.2f;
        screenEffects = Moonshine.China(godsray)
            //.Next(Moonshine.CRT.Default)
            //.Next(Moonshine.Glow.Default)
            //.Next(scanlines)
            ;
        godsray.Enable = false;

        swordEffect = Moonshine.China(swordRay);
    }
}

public class HuntingGame : View
{

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
        public TextProgress textProgress = new TextProgress();

        public void AddSubTarget(Monster monster)
        {
            monster.flags = Monster.Flags.SubTarget;
            subTargets.Add(monster);
        }
        public void SetSubTargets(IEnumerable<Monster> monsters)
        {
            subIndex = 0;
            subTargets.Clear();
            subTargets.AddRange(monsters);

            if (subTargets.Count() > 0)
            {
                foreach (var m in subTargets)
                {
                    m.flags = Monster.Flags.SubTarget;
                }
            }
        }
        public void NextTarget()
        {
            subIndex++;
        }

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
    public Player player;
    public Camera cam;
    public World world;
    public MonsterGroup monsterGroup;
    public ItemGroup itemGroup;

    ComponentRegistry components = new ComponentRegistry();
    Corunner runner = new Corunner();

    List<CardInfo> cards = new List<CardInfo>();

    public State gameState = State.Playing;
    public Gameover gameover = new Gameover();
    public Playing playing = new Playing();
    public Clear clear = new Clear();
    public float elapsed = 0;
    public List<CardInfo> allCards = new List<CardInfo>();
    public int cardIndex = 0;

    public SimpleMenu gameMenu = new SimpleMenu(new[] { "new game", "options", "exit" });

    public GameInput input = new();


    CoroutineControl ctrl;

    //public List<Monster> killedMonsters = new List<Monster>();

    public HuntingGame(SharedState state)
    {
        this.state = state;

        cam = new Camera(this);
        player = new Player(this);
        world = new World(this);
        monsterGroup = new MonsterGroup(this);
        itemGroup = new ItemGroup(this);


        var deckName = state.lastDeckName ?? state.deckNames.Keys.First();
        allCards = state.deckCards?[deckName]?.ToList() ?? new List<CardInfo>();

        gameMenu.align = PosAlign.StartX;
        //gameMenu.SetPosition(new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2));
        gameMenu.SetPosition(new Vector2(50, Graphics.GetHeight() / 2));

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

    public void RemoveUnwantedTags(CardInfo card, string field)
    {
        if (card.HasField(field))
        {
            var text = card.fields?[field]?.value;
            if (text != null)
            {
                text = Regex.Replace(text, @"\<.*?\>", "");
                text = Regex.Replace(text, @"&nbsp;", " ");
                card.fields[field].value = text;
            }
        }
    }
    public void RemoveUnwantedText(CardInfo card, string field)
    {
        if (card.HasField(field))
        {
            var text = card.fields?[field]?.value;
            if (text != null)
            {
                text = Regex.Replace(text, @"\[.*?\]", "");
                text = Regex.Replace(text, @"\<.*?\>", "");
                text = Regex.Replace(text, @"&nbsp;", " ");
                card.fields[field].value = text;
            }
        }
    }
    public void RemoveUnwantedText(CardInfo card)
    {
        RemoveUnwantedText(card, "VocabKanji");
        RemoveUnwantedText(card, "SentKanji");
        RemoveUnwantedText(card, "SentEng");
        RemoveUnwantedTags(card, "SentFurigana");
        RemoveUnwantedTags(card, "VocabFurigana");
    }

    public List<CardInfo> CreateCards()
    {
        var numText = 30;
        var deckName = state.lastDeckName ?? state.deckNames.Keys.First();
        var cards = state.deckCards?[deckName]?.ToList() ?? new List<CardInfo>();

        //cards.Sort((a, b) => Random.Shared.Next(-1, 2));

        cards = cards.Take(numText).ToList();

        foreach (var card in cards)
        {
            RemoveUnwantedText(card, "VocabKanji");
            RemoveUnwantedText(card, "SentKanji");
            RemoveUnwantedText(card, "SentEng");
        }

        return cards;
    }

    public async Coroutine<Monster> GetMonsterKill(CoroutineControl ctrl)
    {
        Monster? monster = null;
        var onKill = (Monster m) =>
        {
            monster = m;
        };

        monsterGroup.RegisterOnKill(onKill);
        using var _ = Defer.Run(() =>
        {
            Console.WriteLine("*** remove on kill");
            monsterGroup.OnKill -= onKill;
        });
        while (monster == null)
        {
            await ctrl.Yield();
        }

        return monster;
    }

    public async Coroutine<Monster> GetHuntedKill(CoroutineControl ctrl)
    {
        while (true)
        {
            var m = await GetMonsterKill(ctrl);
            if (playing.subIndex >= playing.subTargets.Count())
            {
                playing.subIndex = 0;
            }
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

    public void SetPlayingCard(string vocab)
    {
        var card = allCards.Where(c => c.GetVocab() == vocab).FirstOrDefault(cards[0]);
        if (!cards.Contains(card))
        {
            cards.Add(card);
        }
        RemoveUnwantedText(card, "VocabKanji");
        playing.card = card;
    }

    public void InitGame(bool tryAgain = false)
    {
        playing.subIndex = 0;

        gameState = State.Playing;
        player.entity.pos = world.Center;
        player.health = 100;
        cam.CenterAt(player.pos);

        if (!tryAgain)
        {

            playing.card = allCards[cardIndex++];
            RemoveUnwantedText(playing.card);
            if (!cards.Contains(playing.card))
            {
                cards.Add(playing.card);
            }
            AnkiAudioPlayer.LoadCardAudios(cards);
        }

        Console.WriteLine("==============================");
        Console.WriteLine("current card");
        Console.WriteLine(playing.card);

        //SetPlayingCard("脱ぐ");

        //Console.WriteLine("current card: `{0}`, {1}", playing.card.GetVocab(), playing.card.cardId);

        monsterGroup.Clear();
        monsterGroup.RegisterOnKill(OnMonsterKill);
        monsterGroup.RegisterOnHit(OnDefaultMonsterHit);

        itemGroup.Clear();

        //monsterGroup = MonsterGroup.CreateFromCards(cards);


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


    public async Coroutine GameoverScript(CoroutineControl ctrl)
    {
        gameover.opacity = 0;

        using var _1 = components.AddUpdate(UpdateGameover);
        using var _2 = components.AddDraw(DrawGameover);
        components.RemoveUpdate(UpdatePlayerAction);
        components.RemoveUpdate(UpdateMonsterCollision);

        await input.PressAny(ctrl, ActionType.ButtonA, ActionType.ButtonB);
        //await ctrl.AwaitAnyKey();
    }

    public async Coroutine StartGameScript()
    {
        cam = new Camera(this);
        player = new Player(this);
        world = new World(this);
        cards = CreateCards();

        monsterGroup.Dispose();
        monsterGroup = new MonsterGroup(this, player.entity);

        ctrl = new CoroutineControl();
        var tryAgain = false;
        var skipChooseAnswer = false;
        while (true)
        {
            InitGame(tryAgain);

            components.RemoveDraw(DrawClear);

            using var _1 = components.AddUpdate(UpdatePlayerAction);
            using var _2 = components.AddUpdate(UpdateMonsterCollision);
            using var _3 = components.AddUpdate(UpdateMonsters);
            using var _4 = components.AddDraw(DrawInterface);

            gameState = State.Playing;
            var startHunt = true;

            if (!tryAgain && !skipChooseAnswer)
            {
                var correct = await StartChooseAnswerScript(ctrl);
                if (correct)
                {
                    startHunt = false;
                    gameState = State.Clear;
                    _ = AnkiConnect.AnswerCard(playing.card.cardId, AnkiButton.Good);
                }
                else
                {
                    _ = AnkiConnect.AnswerCard(playing.card.cardId, AnkiButton.Again);
                }
            }

            if (startHunt)
            {
                var gameScript = StartHuntScript(ctrl);
                await ctrl.While(() => gameState == State.Playing);
                try
                {
                    Console.WriteLine("cancelling");
                    ctrl.Cancel(gameScript);
                }
                catch (Exception e)
                {
                    // ignore
                    Console.WriteLine($"huh: ${e.Message}");
                }

                if (!gameScript.IsCompleted)
                {
                    await gameScript;
                }

                if (gameState == State.Clear)
                {
                    _ = AnkiConnect.AnswerCard(playing.card.cardId, AnkiButton.Good);
                }
            }

            ctrl = new CoroutineControl();

            if (gameState == State.Clear)
            {
                tryAgain = false;
                components.AddDraw(DrawClear);
                //await ctrl.AwaitAnyKey();
                await input.PressAny(ctrl, ActionType.ButtonA, ActionType.ButtonB);
            }
            else if (gameState == State.Gameover)
            {
                await GameoverScript(ctrl);
                tryAgain = true;
            }
        }
    }

    void ClearSubTargets()
    {
        playing.hunts = 0;
        playing.maxHunts = 1;
        foreach (var m in playing.subTargets)
        {
            monsterGroup.Remove(m);
            playing.target.pos = m.pos;
        }
        playing.subTargets.Clear();
    }

    public async Coroutine<bool> StartChooseAnswerScript(CoroutineControl ctrl)
    {
        Color messageColor = Color.White;
        Color bgColor = new Color(20, 20, 20, 200);
        string message = "";
        Monster? killed = null;
        HashSet<Monster> questionMonsters = new HashSet<Monster>();
        HashSet<Monster> highligtedMonsters = new HashSet<Monster>();

        var onHit = (Monster m) =>
        {
            var damage = Random.Shared.Next(40, 60);
            if (highligtedMonsters.Contains(m))
            {

                AnkiAudioPlayer.Play(m.audioFilename);
                m.Attack(Vector2.Zero, damage);
            }
            else if (questionMonsters.Contains(m))
            {
                if (questionMonsters.Contains(m))
                {
                    AnkiAudioPlayer.Play(m.audioFilename);
                    foreach (var m2 in questionMonsters)
                    {
                        m2.Attack(Vector2.Zero, damage);
                    }
                }
            }
            else
            {
                m.Attack(Vector2.Zero, damage);
            }
        };
        var onKill = (Monster m) =>
        {
            killed = m;
            AnkiAudioPlayer.Play(m.audioFilename);
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

        monsterGroup.RegisterOnHit(onHit);
        monsterGroup.RegisterOnKill(onKill);
        monsterGroup.OnHit -= OnDefaultMonsterHit;
        components.RemoveDraw(DrawInterface);
        components.AddDraw(draw);

        using var _ = Defer.Run(() =>
        {
            monsterGroup.OnHit -= onHit;
            monsterGroup.OnKill -= onKill;
            monsterGroup.RegisterOnHit(OnDefaultMonsterHit);
            components.RemoveDraw(draw);
        });

        var card = playing.card;
        var choices = CreateChoices(card, 7);
        var (vocab, example) = CreateQuestion(card);

        questionMonsters.Add(vocab);
        highligtedMonsters.Add(vocab);
        vocab.defense = 2;
        if (example != null)
        {
            highligtedMonsters.Add(example);
            example.defense = 2;
            var __ = example.textObject.TypeWrite(ctrl, Color.Aqua, 10).AndThen(async () =>
            {
                await Coroutine.DelayCount(120);
                var (i, j) = example.text.FindSubstringIndex(vocab.text);
                if (i > 0 && j < example.text.Length - 1)
                {
                    example.textObject.SetColor(Color.Yellow, i, j);
                }
            });
        }

        foreach (var m in choices)
        {
            m.exploring.maxSpeed = 0.5f;
            m.exploring.speed = 0.1f;
            m.exploring.steps = 100;
            m.logicState = m.exploring;
        }

        // TODO: refetch cards on new game
        // TODO: disable damage on choose answer 

        // TODO: game menus

        // TODO: disable audio play when hitting english texts
        // TODO: await confirm (mouseclick, enter key/escape, gpad letter buttons)

        // TODO: show particle effect
        //   particles.Add(tileIDs)
        //   litters.Add(bloodIDs)

        // TODO: example boss
        //       shoots stuffs
        //       other monsters walks outside of grid
        //       focus camera on big guy and play text
        //       pick up sword with the matching text

        // TODO: random dungeon generation
        //   canvas layers (floor, walls, entities, interface)

        // TODO: general codebase clean up

        // TODO: clean up interface, create release build
        //       move game outside script file

        // TODO: start actual day-to-day testing

        // TODO: turn-based mode: monster only move when the player moves
        // TODO: skirmish

        // TODO: refactoring
        //       decouple events 

        // TODO: visual improvements
        //       shaders, particles, camera movements



        // TODO: in-game zoom in/out
        // set camera on question
        // pan to player 
        // await camera.panTo()
        // await camera.zoomIn()
        // await camera.zoomOut()

        await ctrl.While(() => killed == null);

        var correct = killed.text == playing.card.GetField("VocabDef");
        if (correct)
        {
            message = "/ okay";
            messageColor = Color.Green;
            // TODO: await Play to finish
        }
        else
        {
            message = "x nope";
            messageColor = Color.Red;
        }

        var audio = card.GetField("SentAudio") ?? card.GetField("VocabAudio");
        //await Coroutine.AwaitTask(AnkiAudioPlayer.PlayWait(audio));
        await ctrl.AwaitTask(AnkiAudioPlayer.PlayWait(audio));
        await ctrl.Sleep(1);

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
        var pos = player.pos + -player.entity.rect.DiagonalLength() * 4 * Vector2.UnitY;
        foreach (var m in choices)
        {
            m.pos = pos + polar.ToVector();
            m.target = null;
            polar.angle += angleStep;
        }

        return choices;
    }

    (Monster, Monster?) CreateQuestion(CardInfo card)
    {
        var vocab = card.GetVocab() ?? "";
        var example = card.GetExample() ?? "";

        var qpos = player.pos + Vector2.UnitY * player.entity.rect.DiagonalLength() * 1.5f;

        var monVocab = monsterGroup.SpawnMonster(vocab);
        monVocab.audioFilename = card.GetField("VocabAudio");
        monVocab.pos = qpos + Vector2.One * monVocab.rect.Height * 2.0f;
        monVocab.target = null;

        Monster? monExample = null;
        if (!string.IsNullOrEmpty(example))
        {
            monExample = monsterGroup.SpawnMonster(example);
            monExample.audioFilename = card.GetField("SentAudio");
            monExample.pos = qpos;
            monExample.target = null;

            monExample.textObject.autoScale = false;
            monExample.textObject.scale = 1;

            var (i, j) = example.FindSubstringIndex(vocab);
            if (i > 0 && j < example.Length - 1)
            {
                monExample.textObject.SetColor(Color.Yellow, i, j);
            }
        }
        else
        {
            monVocab.pos = qpos;
        }

        return (monVocab, monExample);



        /*
                Monster? bottom = null;

                if (vocab != v)
                {
                    monsterGroup.Remove(monExample);
                    monExample = monsterGroup.SpawnMonster(v, monExample.entity.tileID);
                    monExample.audioFilename = card.GetField("VocabAudio");
                    monExample.pos = qpos;
                    monExample.target = null;
                    monExample.textColor = Color.Yellow;

                    bottom = monsterGroup.SpawnMonster(vocab);
                }

                var pre = string.IsNullOrEmpty(a.Trim()) ? null : monsterGroup.SpawnMonster(a);
                var post = string.IsNullOrEmpty(b.Trim()) ? null : monsterGroup.SpawnMonster(b);

                if (pre != null) { pre.audioFilename = card.GetField("SentAudio"); }
                if (post != null) { post.audioFilename = card.GetField("SentAudio"); }
                if (bottom != null) { bottom.audioFilename = card.GetField("VocabAudio"); }

                var fontAsian = SharedState.self.fontAsian;
                if (pre != null)
                {
                    pre.pos = qpos - Vector2.UnitX * (mid.GetTextSize(fontAsian) / 2 + pre.GetTextSize(fontAsian) / 2);
                    pre.target = null;
                }


                if (post != null)
                {
                    post.pos = qpos + Vector2.UnitX * (mid.GetTextSize(fontAsian) / 2 + post.GetTextSize(fontAsian) / 2);
                    post.target = null;
                }

                if (bottom != null)
                {
                    bottom.pos = qpos + Vector2.UnitY * fontAsian.GetHeight() * 2;
                    bottom.target = null;
                    bottom.entity.color = Color.Gold;
                }
        */


    }

    public async Coroutine StartHuntScript(CoroutineControl ctrl)
    {

        ClearSubTargets();
        monsterGroup.Clear();
        monsterGroup.SpawnFromCards(cards);
        components.AddDraw(DrawInterface);

        var text = playing.card.GetVocab() ?? "";
        foreach (var m in monsterGroup.Iterate())
        {
            //if (m.card.HasExample())
            //{
            //	playing.target = m;
            //	playing.card = m.card;
            //}
            if (m.card == playing.card)
            {
                playing.target = m;
                break;
            }
        }
        var target = playing.target;

        //await ReadyGameScript(ctrl);

        await StartVocabScript(ctrl);

        if (target.text.Length > 1)
        {
            await StartVocabPartsScript(ctrl);
            await MergeDeadMonsters(ctrl, player.pos);
        }


        if (playing.card.HasExample())
        {
            await StartExampleScript(ctrl);
            await StartExamplePartsScript(ctrl);
        }

        await MergeDeadMonsters(ctrl, player.pos);
        gameState = State.Clear;

        //Console.WriteLine("hanging up");
        //await Xt.Coroutine.Abyss();
    }
    public async Coroutine MergeDeadMonsters(CoroutineControl ctrl, Vector2 mergePos)
    {
        List<Monster> monsters = new();
        foreach (var m in monsterGroup.Iterate())
        {
            if (!m.IsAlive())
            {
                monsters.Add(m);
            }
        }
        if (monsters.Count() == 0)
        {
            return;
        }

        //components.RemoveUpdate(UpdateMonsters);
        //var mergePos = monsters[^1];
        var done = false;
        using var _ = components.AddUpdate(() =>
        {
            var doneCount = 0;
            foreach (var m in monsters)
            {
                var v = mergePos - m.pos;
                if (v.Length() <= 10)
                {
                    doneCount++;
                }
                if (v.Length() == 0)
                {
                    v = Xt.Vector2.RandomDir();
                }
                m.pos += Vector2.Normalize(v) * 10;
            }
            if (doneCount == monsters.Count())
            {
                done = true;
            }
        });

        await ctrl.While(() => !done);
        await ctrl.Sleep(1f);

        foreach (var m in monsters)
        {
            monsterGroup.Remove(m);
        }
    }

    public async Coroutine StartVocabPartsScript(CoroutineControl ctrl)
    {
        playing.state = HuntState.VocabParts;

        var target = playing.target;
        var pos = target.pos;
        if (playing.subTargets.Count() > 0)
        {
            pos = playing.subTargets[^1].pos;
        }

        monsterGroup.Remove(target);

        ClearSubTargets();
        AddSubTargetsBy(target, "VocabKanji");
        playing.textProgress.SetText(target.text);
        playing.maxHunts = 2;

        foreach (var m in playing.subTargets)
        {
            m.pos = pos;
        }

        monsterGroup.RegisterOnHit(OnTargetHit);
        monsterGroup.OnHit -= OnDefaultMonsterHit;
        using var _ = Defer.Run(() =>
        {
            monsterGroup.RegisterOnHit(OnDefaultMonsterHit);
            monsterGroup.OnHit -= OnTargetHit;
        });

        while (true)
        {
            Vector2 lastPos = Vector2.Zero;
            while (playing.subIndex < playing.subTargets.Count())
            {
                var hunted = await GetHuntedKill(ctrl);
                playing.textProgress.NextTextItem();
                playing.NextTarget();
                lastPos = hunted.pos;
            }
            DropLoots(lastPos);
            playing.subIndex = 0;
            playing.hunts++;
            if (playing.hunts >= playing.maxHunts)
            {
                break;
            }
            playing.subTargets.Clear();
            AddSubTargetsBy(target, "VocabKanji");
        }
    }

    public async Coroutine StartExampleScript(CoroutineControl ctrl)
    {
        ClearSubTargets();
        AddExampleMonster(playing.target);
        playing.textProgress.SetWholeText(playing.card.GetExample() ?? "");

        while (true)
        {
            var hunted = await GetHuntedKill(ctrl);
            playing.hunts++;
            if (playing.hunts >= playing.maxHunts)
            {
                break;
            }
            DropLoots(hunted.pos);

            playing.subTargets.Clear();
            AddExampleMonster(playing.target);
        }
    }
    public async Coroutine StartExamplePartsScript(CoroutineControl ctrl)
    {
        ClearSubTargets();
        AddSubTargetsBy(playing.target, "SentKanji");
        playing.textProgress.SetText(playing.card.GetExample() ?? "");

        monsterGroup.RegisterOnHit(OnTargetHit);
        monsterGroup.OnHit -= OnDefaultMonsterHit;
        using var _ = Defer.Run(() =>
        {
            monsterGroup.RegisterOnHit(OnDefaultMonsterHit);
            monsterGroup.OnHit -= OnTargetHit;
        });

        while (playing.subIndex < playing.subTargets.Count())
        {
            var hunted = await GetHuntedKill(ctrl);
            playing.textProgress.NextTextItem();
            playing.NextTarget();

            //playing.subTargets.Clear();
            //AddExampleMonster(playing.target);
        }
    }

    public async Coroutine StartVocabScript(CoroutineControl ctrl)
    {
        playing.state = HuntState.Vocab;
        var target = playing.target;
        var skipVocabParts = target.text.Length <= 1;
        playing.textProgress.SetWholeText(target.text);
        playing.AddSubTarget(target);
        playing.maxHunts = 2;

        if (skipVocabParts)
        {
            playing.maxHunts += 1;
        }
        if (!playing.card.HasExample())
        {
            playing.maxHunts += 2;
        }

        while (true)
        {

            var hunted = await GetHuntedKill(ctrl);
            playing.hunts++;
            if (playing.hunts >= playing.maxHunts)
            {
                break;
            }
            DropLoots(hunted.pos);

            var font = SharedState.self.fontAsian;
            //var newMonster = new Monster(TileID.RandomMonsterID(), hunted.text.ToString(), font);
            var newMonster = monsterGroup.SpawnMonster(hunted.text);
            newMonster.target = hunted.target;
            newMonster.card = hunted.card;
            newMonster.audioFilename = hunted.audioFilename;
            newMonster.Flee(1);

            playing.subTargets.Clear();
            playing.AddSubTarget(newMonster);

        }

    }

    public async Coroutine ReadyGameScript(CoroutineControl ctrl)
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

        using var _1 = components.AddComponent(scriptFn);
        using var _2 = components.TemporaryRemoveUpdate(UpdateMonsters);

        AnkiAudioPlayer.Play(card.GetField("VocabAudio"));

        while (countdown > 0)
        {
            countdown--;
            await ctrl.Sleep(1.0f);
        }
    }

    public void AddExampleMonster(Monster m)
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
        newMonster.defense = 3;
        newMonster.Flee(3);

        playing.textProgress.SetText(text);
        playing.AddSubTarget(newMonster);
        monsterGroup.Add(newMonster);
    }

    public void AddSubTargetsBy(Monster m, string fieldName)
    {
        var text = m.card?.GetField(fieldName) ?? m.text;
        var subMonsters = new List<Monster>();
        var font = SharedState.self.fontAsian;
        var audioFilename = m.card?.GetField(fieldName == "SentKanji" ? "SentAudio" : "VocabAudio") ?? m.text;
        var subTexts = JP.SplitText(text);

        playing.subIndex = 0;
        playing.textProgress.SetText(text);

        foreach (var e in subTexts)
        {
            monsterGroup.RemoveByText(e.text);
        }

        foreach (var e in subTexts)
        {
            if (e.type != JP.Type.KanaOrKanji)
            {
                continue;
            }
            var newMonster = monsterGroup.SpawnMonster(e.text);
            newMonster.pos = m.pos;
            newMonster.target = m.target;
            newMonster.card = m.card;
            newMonster.audioFilename = audioFilename;
            newMonster.defense = 0.5f;
            newMonster.Flee(3);

            subMonsters.Add(newMonster);
        }
        playing.SetSubTargets(subMonsters);
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
        FX.screenEffects.Draw(() =>
        {
            cam.StartDraw();
            {
                world.Draw();
                itemGroup.Draw();
                player.Draw();
                monsterGroup.Draw();
                DrawTargets();
            }
            cam.EndDraw();

            components.Draw();

            if (Keyboard.IsDown(KeyConstant.Tab))
            {
                DrawClear();
            }

        });

        gameMenu.Draw();


        //Graphics.SetColor(Color.Red);
        //Graphics.Rectangle(DrawMode.Line, testText.rect);
        //Graphics.Circle(DrawMode.Fill, p, 10);

        //var s = new ColoredString[]{
        //    new("line1\n", Color.Red),
        //    new("line2\n", Color.Orange),
        //};
        //Graphics.SetColor(Color.White);
        //Graphics.Printf(new ColoredStringArray(s), 50, 80, 300, AlignMode.Center);
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
            /*
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
			*/

            if ((playing?.target?.text?.Count() ?? 0) > 0)
            {
                playing.textProgress.Draw();

                if (playing.maxHunts > 1)
                {
                    var font = SharedState.self.fontMedium;
                    var pos = playing.textProgress.pos;
                    Graphics.SetFont(font);
                    Graphics.Printf(
                        string.Format("{0}/{1}", playing.hunts, playing.maxHunts),
                        pos.X, pos.Y - font.GetHeight(), Graphics.GetWidth(), AlignMode.Center
                    );
                }
            }
        }

        {
            var font = SharedState.self.fontMedium;
            var pos = new Vector2(Graphics.GetWidth() / 2, font.GetHeight() / 2);
            var card = playing.card;
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

    public void DrawClear()
    {
        clear.gpr.ResetLine();
        var card = playing.card;
        var maxWidth = Graphics.GetWidth() * 3f / 4f;

        Graphics.SetColor(clear.bgColor);
        Graphics.Rectangle(DrawMode.Fill, clear.rect);
        Graphics.SetColor(Color.White);

        clear.gpr.font = SharedState.self.fontSmall;
        clear.gpr.Print("id={0}", card.cardId);

        clear.gpr.font = SharedState.self.fontRegular;
        clear.gpr.Print("Elapsed: {0} seconds", MathF.Floor(elapsed));
        clear.gpr.font = SharedState.self.fontAsian;
        clear.gpr.Printf($"{card.GetField("VocabFurigana")} / {card.GetField("VocabDef")}", maxWidth, AlignMode.Left);


        if (card.HasExample())
        {
            clear.gpr.font = SharedState.self.fontSmall;
            clear.gpr.Print(" ");
            clear.gpr.font = SharedState.self.fontAsian;
            clear.gpr.Printf(card.GetField("SentFurigana") ?? "", maxWidth);
            clear.gpr.font = SharedState.self.fontSmall;
            clear.gpr.Print(" ");
            clear.gpr.font = SharedState.self.fontMedium;
            clear.gpr.Printf(card.GetField("SentEng") ?? "", maxWidth);
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

    public void DropLoots(Vector2 pos)
    {
        for (var i = 0; i < Rand.Next(1, 2); i++)
        {
            itemGroup.SpawnAt(pos);
        }
    }

    public void OnMonsterKill(Monster m)
    {
        Console.WriteLine("default monster kill");
        //DropLoots(m.pos);
    }

    public void OnTargetHit(Monster m)
    {
        var canAttack = true;
        var isTarget = false;
        foreach (var (t, i) in playing.subTargets.WithIndex())
        {
            if (t.text == m.text)
            {
                canAttack = i == playing.subIndex;
                isTarget = true;
                break;
            }
        }

        var r = Random.Shared;
        var (damage, manaGain) = !canAttack ? (0, 0)
               : !isTarget
               ? (r.Next(20, 30), r.Next(1, 2))
               : (r.Next(30, 60), r.Next(3, 5));

        m.Attack(player.sword, damage);
        AnkiAudioPlayer.Play(m.audioFilename);
        UpdateMana(manaGain);
    }

    public void StartBackgroundCoroutine(Func<Coroutine> fn)
    {
        runner.Create(fn);
    }

    public void OnDefaultMonsterHit(Monster m)
    {
        var targetMonster = playing.subIndex >= 0 && playing.subIndex < playing.subTargets.Count()
        ? playing.subTargets[playing.subIndex] : null;
        var isTarget = m.text == targetMonster?.text;
        var isSubTarget = (m.flags & Monster.Flags.SubTarget) != 0;
        var r = Random.Shared;
        var (damage, manaGain) = !isTarget ? (r.Next(20, 30), r.Next(1, 2)) : (r.Next(30, 60), r.Next(3, 5));

        if (!(isSubTarget && player.sword.IsWhirlwindAttacking()))
        {
            m.Attack(player.sword, damage);
            var filename = m.audioFilename;
            AnkiAudioPlayer.Play(filename);
            UpdateMana(manaGain);
        }

        if (m.health < 50 && m.health > 0 && !m.textObject.typewriting && m.text != m.card.GetExample())
        {
            StartBackgroundCoroutine(async () =>
            {
                // TODO: set example text
                var text = m.textObject.text == m.card.GetVocabDef() ? m.card.GetVocab() : m.card.GetVocabDef();
                if (text != null)
                {
                    m.textObject.SetText(text);
                    await m.textObject.TypeWrite(ctrl, Color.Red, 3);
                }
            });
        }
        else if (!m.IsAlive())
        {
            m.textObject.SetText(m.text);
        }
    }

    public void OnItemPickup(IEntity item)
    {
        if (item is Consumable food)
        {
            itemGroup.Remove(item);
            player.AddHealth(food.healthGain);
        }
    }

    public void UpdateMana(float gain)
    {

        if (player.mana < 100)
        {
            player.mana += gain;
            player.mana = MathF.Min(player.mana, 100);
        }
    }



    public (bool charging, bool released) GetPlayerCharge()
    {
        var charge = input.GetChargeTime(ActionType.ButtonA);
        var release = input.GetReleaseChargeTime(ActionType.ButtonA);
        return (
            charge >= 1,
            release >= 1
        );
    }


    public void UpdatePlayerAction()
    {
        player.Update();

        // TODO: use GameInput

        var (charging, released) = GetPlayerCharge();
        if (released)
        {
            player.ReleaseCharge1();

        }
        else if (charging)
        {
            player.DoCharge1();
        }

        if (input.IsPressed(ActionType.ButtonA))
        {
            player.PerformMeleeAttack();
        }
        else if (input.IsPressed(ActionType.ButtonB))
        {
            player.PerformLongAttack();
        }
        else if (input.IsPressed(ActionType.ButtonX))
        {
            player.PerformDash();
        }

        if (player.health <= 0)
        {
            gameState = State.Gameover;
        }
        else
        {
            foreach (var item in itemGroup.GetItemsAt(player.rect))
            {
                if (item.rect.IntersectsWith(player.rect))
                {
                    OnItemPickup(item);
                }
            }
        }
        itemGroup.Update();
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
                //if (m.health <= 0! && IsTarget(m))
                //{
                //    m.health = 1;
                //}
            }
        }

        foreach (var m in monsterGroup.GetMonstersAt(player.pos))
        {
            if (player.entity.CollidesWith(m.entity) && m.CanDamage())
            {
                player.Hit(player.entity.scale / m.entity.scale);
            }
        }


        //UpdateTargets();

    }
    public void UpdateMonsters()
    {
        monsterGroup.Update();
    }

    Coroutine coKillSubTargets;
    public async Coroutine KillSubTargets()
    {
        foreach (var m in playing.subTargets)
        {
            m.Attack(null, 100);
            await Xt.Coroutine.Sleep(0.55f);
        }
    }

    public void Update()
    {
        if (gameState == State.Playing)
        {
            elapsed += Love.Timer.GetDelta();
        }

        cam.Update();
        cam.RestrictWithin(player.pos);

        if (!Keyboard.IsDown(KeyConstant.Tab))
        {
            components.Update();
            runner.Update();
        }

        if (Keyboard.IsPressed(KeyConstant.F3))
        {
            //coKillSubTargets?.TryCancel();
            coKillSubTargets = runner.Create(KillSubTargets);
        }
        if (Keyboard.IsDown(KeyConstant.Equals))
        {
            cam.ZoomIn();
        }
        if (Keyboard.IsDown(KeyConstant.Minus))
        {

            cam.ZoomOut();
        }

        // TODO: use gameInput
        /*
        foreach (var actionType in Enum.GetValues(typeof(GameInput.ActionType)).Cast<GameInput.ActionType>())
        {
            if (gameInput.IsPressed(actionType))
            {
                Console.WriteLine("pressed {0}", actionType);
            }
        }
        var dir = gameInput.GetMotion();
        if (dir.Length() > 0)
        {
            Console.WriteLine("motion {0}", dir);
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
        input.Dispose();
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




public class TextProgress
{
    public Vector2 pos = Vector2.Zero;
    Vector4 doneColor = Xt.Vector4.FromColor(Color.Gray);
    Vector4 activeColor = Xt.Vector4.FromColor(Color.LightGreen);
    Vector4 nextColor = Xt.Vector4.FromColor(Color.White);

    int index;
    ColoredStringArray coloredText = new ColoredStringArray();
    JP.Type[] types = new JP.Type[0];
    Font font = Graphics.GetFont();

    public void SetText(string text)
    {
        var splittedText = JP.SplitText(text);
        var types = new JP.Type[splittedText.Length];
        var texts = new ColoredString[splittedText.Length];

        foreach (var (e, i) in splittedText.WithIndex())
        {
            types[i] = e.type;
            texts[i] = new ColoredString(e.text, nextColor);
        }

        var state = SharedState.self;
        this.coloredText = new ColoredStringArray(texts);
        this.types = types;
        this.font = JP.HasJapanese(text) ? state.fontAsian : state.fontRegular;
        this.pos = new Vector2(0, Graphics.GetHeight() - font.GetHeight() * 1.2f);

        index = -1;
        NextTextItem();
    }
    public void SetWholeText(string text)
    {
        var state = SharedState.self;
        this.coloredText = new ColoredStringArray(new ColoredString(text, activeColor));
        this.types = new[] { JP.Type.KanaOrKanji };
        this.font = JP.HasJapanese(text) ? state.fontAsian : state.fontRegular;
        this.pos = new Vector2(0, Graphics.GetHeight() - font.GetHeight() * 1.2f);

        index = -1;
        NextTextItem();
    }

    public void NextTextItem()
    {
        if (types.Length == 0 || index >= types.Length)
        {
            return;
        }

        while (true)
        {
            index++;
            if (index >= types.Length)
            {
                break;
            }
            if (types[index] == JP.Type.KanaOrKanji)
            {
                break;
            }
        }

        if (index < types.Length)
        {
            coloredText.colors[index] = activeColor;
        }

        for (var i = 0; i < index; i++)
        {
            coloredText.colors[i] = doneColor;
        }
    }

    public void Draw()
    {
        if (types.Length == 0)
        {
            return;
        }
        Graphics.SetFont(font);
        Graphics.SetColor(Color.White);
        Graphics.Printf(coloredText, pos.X, pos.Y, Graphics.GetWidth(), AlignMode.Center);
    }

}


public partial class TextEntity
{

    public RectangleF rect = new RectangleF();
    Vector2 _pos;
    PosAlign _align;
    float _scale = 1;
    public Vector2 pos
    {
        get { return _pos; }
        set
        {
            _pos = value;
            rect.Location = _pos;
        }
    }
    public PosAlign align
    {
        get { return _align; }
        set { _align = value; UpdateRect(); }
    }

    public float scale
    {
        get { return _scale; }
        set { _scale = value; UpdateRect(); }
    }

    public string text;
    public Font font;

    ColoredString[] coloredText;
    Color color = Color.White;
    float textWidth;
    float textHeight;
    public int margin = 20;

    public Color bgColor = new Color(20, 20, 20, 200);
    public Color borderColor = Color.Gray;
    public Color textColor = Color.White;

    string[] lines;
    int maxWidth;

    public bool typewriting = false;

    public bool autoScale = true;

    public AlignMode textAlign = AlignMode.Left;
    public bool fillMaxWidth = false;

    // TODO: setting the lineHeight other than 1
    // seems to be broken. Not sure if this is a bug.
    public TextEntity(string text, Font font, int maxWidth = -1)
    {
        this.maxWidth = maxWidth;
        this.font = font;
        //font = Graphics.NewFont(fontSize);
        //text = font.GetWrap()

        UpdateText(text);
        UpdateRect();
        SetText(text, Color.White);
    }

    public void UpdateText(string text)
    {
        if (autoScale)
        {
            var n = 4.0f;
            if (text.Length > n)
            {
                scale = MathF.Max(1 - (text.Length - n) / 40, 0.46f);
            }
        }
        this.text = text;

        lines = new string[0];
        if (maxWidth > 0)
        {
            var t = font.GetWrap(text, maxWidth / scale);
            textWidth = (float)t.Item1;
            lines = t.Item2;
        }
        else
        {
            lines = text.Split('\n').ToArray();
            foreach (var line in lines)
            {
                textWidth = Math.Max(textWidth, font.GetWidth(line));
            }
        }
        float numLines = lines.Length;
        float fontHeight = font.GetHeight();
        textHeight = numLines * (fontHeight * font.GetLineHeight());

        text = string.Join('\n', lines);

    }

    public void UpdateRect()
    {
        var p = pos;
        var w = fillMaxWidth ? maxWidth : textWidth;

        rect.Width = (w + margin) * scale;
        rect.Height = (textHeight + margin) * scale;

        p.X -= rect.Width / 2;
        p.Y -= rect.Height / 2;

        if ((align & PosAlign.StartX) != 0)
        {
            p.X += rect.Width / 2;
        }
        else if ((align & PosAlign.EndX) != 0)
        {
            p.X -= rect.Width / 2;
        }

        if ((align & PosAlign.StartY) != 0)
        {
            p.Y += rect.Height / 2;
        }
        else if ((align & PosAlign.EndY) != 0)
        {
            p.Y -= rect.Height / 2;
        }

        pos = p;
        rect.Location = p;
    }

    public void SetColor(Color color, int startIndex = 0, int? endIndexOpt = null)
    {
        int endIndex = endIndexOpt.GetValueOrDefault(text.Length - 1);
        for (var i = startIndex; i <= Math.Min(endIndex, text.Length - 1); i++)
        {
            var c = coloredText[i];
            coloredText[i] = new ColoredString(c.text, color);
        }
    }

    public void SetText(string text, Color? colorOpt = null)
    {
        var color = colorOpt.GetValueOrDefault(textColor);

        coloredText = new ColoredString[text.Length];
        foreach (var (ch, i) in text.WithIndex())
        {
            coloredText[i] = new(ch.ToString(), color);
        }
        UpdateText(text);
        UpdateRect();
    }



    public async Coroutine TypeWrite(CoroutineControl ctrl, Color tempColor, int speed = 5)
    {
        if (typewriting)
        {
            return;
        }
        typewriting = true;
        using var _ = Defer.Run(() => typewriting = false);

        int i;
        for (i = 0; i < coloredText.Length; i++)
        {
            coloredText[i] = new(coloredText[i].text, Color.Transparent);
        }
        for (i = 1; i < coloredText.Length; i++)
        {
            if (i >= coloredText.Length)
            {
                break;
            }
            coloredText[i - 1] = new(coloredText[i - 1].text, textColor);
            coloredText[i - 0] = new(coloredText[i - 0].text, tempColor);
            await ctrl.DelayCount(speed);
        }
        if (i > 0 && i <= coloredText.Length)
        {
            coloredText[i - 1] = new(coloredText[i - 1].text, textColor);
        }
    }

    public void Update()
    {

    }

    public void Draw()
    {
        Graphics.SetColor(bgColor);
        Graphics.Rectangle(DrawMode.Fill, rect.X, rect.Y, rect.Width, rect.Height);
        Graphics.SetColor(borderColor);
        Graphics.Rectangle(DrawMode.Line, rect.X, rect.Y, rect.Width, rect.Height);

        Graphics.Push();
        Graphics.Translate(pos.X, pos.Y);
        Graphics.Scale(scale);

        Graphics.SetFont(font);
        Graphics.SetColor(Color.White);
        Graphics.Printf(new ColoredStringArray(coloredText), margin / 2, margin / 2, fillMaxWidth ? maxWidth : textWidth, textAlign);
        Graphics.Pop();
    }

    public float GetWidth()
    {
        return rect.Width;
    }

    public float GetHeight()
    {
        return rect.Height;
    }
}

/*
public class ColoredStringBuilder
{
    List<Color> colors;
    List<string> texts;

    public void SetColor(Color color)
    {

    }

    // builder.SetColor(red)
    //        .AddText(s1).
    //        .SetColor(blue)
    //        .NewText(s2)
}
*/


public class SimpleMenu : View, IDisposable
{
    Font font;
    public float Width;
    public float Height;
    Vector2 pos;
    public PosAlign align = PosAlign.EndX | PosAlign.EndY;
    string[] choices;
    List<TextEntity> items = new();
    HashSet<int> disabledIndices = new();
    int index = -1;

    int _margin;
    Color _borderColor;
    Color _bgColor;
    Color _normalColor = Color.White;
    Color _selectedColor = Color.Red;
    public Color borderColor
    {
        get { return _borderColor; }
        set { _borderColor = value; UpdateColors(); }
    }
    public Color bgColor
    {
        get { return _bgColor; }
        set { _bgColor = value; UpdateColors(); }
    }
    public Color normalColor
    {
        get { return _normalColor; }
        set { _normalColor = value; UpdateColors(); }
    }
    public Color selectedColor
    {
        get { return _selectedColor; }
        set { _selectedColor = value; UpdateColors(); }
    }

    public int margin
    {
        get { return _margin; }
        set
        {
            _margin = value;
            foreach (var item in items) item.margin = value;
            UpdateSize();
        }
    }

    public SimpleMenu(string[] choices, int fontSize = 24, int maxWidth = -1)
    {
        this.choices = choices;
        font = Graphics.NewFont(fontSize);
        if (maxWidth < 0)
        {
            maxWidth = choices.Max(a => font.GetWidth(a));
        }

        foreach (var s in choices)
        {
            var c = new TextEntity(s, font, maxWidth);
            c.autoScale = false;
            c.scale = 1.2f;
            c.align = PosAlign.StartX | PosAlign.StartY;
            c.bgColor = Color.Transparent;
            c.borderColor = Color.Transparent;
            c.textAlign = AlignMode.Center;
            c.fillMaxWidth = true;
            c.margin = 10;
            items.Add(c);
        }

        UpdateSize();
        SetPosition(Vector2.Zero);
        SelectItem(0);
    }

    public void EnableItem(int i) { disabledIndices.Remove(i); UpdateColors(); }
    public void DisableItem(int i) { disabledIndices.Add(i); UpdateColors(); }

    public void UpdateColors()
    {
        foreach (var (item, i) in items.WithIndex())
        {
            item.bgColor = _bgColor;
            item.borderColor = _borderColor;
            var color = disabledIndices.Contains(i) ? Color.Gray
                           : i == index ? _selectedColor
                           : _normalColor;
            item.SetColor(color);
        }
    }


    public void UpdateSize()
    {

        float y = pos.Y;
        this.Height = 0;
        this.Width = 0;
        foreach (var item in items)
        {
            item.UpdateRect();
            this.Height += item.rect.Height;
            this.Width = MathF.Max(this.Width, item.rect.Width);
            item.pos = new Vector2(pos.X, y);
            y += item.rect.Height;
        }
    }


    public void SelectItem(int newIndex)
    {
        if (index >= 0 && index < items.Count())
        {
            items[index].SetColor(_normalColor);
        }
        if (newIndex >= 0 && newIndex < items.Count())
        {
            items[newIndex].SetColor(
                disabledIndices.Contains(newIndex)
                ? Color.Gray : _selectedColor
            );
        }
        index = newIndex;
    }

    public string GetChoice()
    {
        return items[index].text;
    }

    public void PrevItem()
    {
        if (index > 0)
        {
            SelectItem(index - 1);
        }
    }

    public void NextItem()
    {
        if (index < items.Count() - 1)
        {
            SelectItem(index + 1);
        }
    }

    public void SetPosition(Vector2 p)
    {

        p.X -= Width / 2;
        p.Y -= Height / 2;

        if ((align & PosAlign.StartX) != 0)
        {
            p.X += Width / 2;
        }
        else if ((align & PosAlign.EndX) != 0)
        {
            p.X -= Width / 2;
        }

        if ((align & PosAlign.StartY) != 0)
        {
            p.Y += Height / 2;
        }
        else if ((align & PosAlign.EndY) != 0)
        {
            p.Y -= Height / 2;
        }

        pos = p;

        float y = p.Y;
        foreach (var c in items)
        {
            c.pos = new Vector2(p.X, y);
            c.UpdateRect();
            y += c.rect.Height;
        }
    }


    public void Show(CoroutineControl ctrl)
    {
        Callbacks.AddDraw(Draw);
        Callbacks.AddUpdate(Update);
    }

    public void Hide(CoroutineControl ctrl)
    {

        Callbacks.RemoveDraw(Draw);
        Callbacks.RemoveUpdate(Update);
    }

    // TODO:
    // Align { left, right, center}
    // PrintCenter(rect, str)
    // PrintLeft(rect, str)
    // PrintRight(rect, str)
    // Print(align, rect, str)
    // Draw(align, rect, textObj)
    // or use a RectangleExtension

    public void Draw()
    {
        foreach (var item in items)
        {
            item.Draw();
        }
    }

    public void Update()
    {
    }

    public void Dispose()
    {
        Callbacks.RemoveDraw(Draw);
        Callbacks.RemoveUpdate(Update);
    }

    public record Choice(int index, string text) { }

    public void SetSelectedColor(Color red)
    {
        throw new NotImplementedException();
    }
}

public class LoadView : View
{
    string DeckName { get; set; } = "";
    public DeckNames DeckNames { get; set; } = new();

    public List<CardInfo> NewCards { get; set; } = new();
    public List<CardInfo> DueCards { get; set; } = new();


    public void Draw()
    {
    }

    public void Update()
    {
    }

    public async Task<DeckNames> LoadDecks(string deckName)
    {
        var resp = (await AnkiConnect.FetchDecks()).Unwrap();
        DeckNames = resp;
        return resp ?? new DeckNames();
    }

    public async Task<IEnumerable<CardInfo>> LoadCards(string deckName)
    {

        var resp = await Task.WhenAll(
            AnkiConnect.FetchNewCards(deckName),
            AnkiConnect.FetchAvailableCards(deckName)
        );
        var newCards = resp[0].Unwrap();
        var dueCards = resp[1].Unwrap();

        foreach (var card in newCards)
        {
            card.IsNew = true;
        }

        NewCards = newCards.ToList();
        DueCards = dueCards.ToList();

        return dueCards.Union(newCards);
    }
}

public class StartScreen : View
{
    SharedState state;
    public SimpleMenu gameMenu = new SimpleMenu(new[] { "start", "select deck", "options", "exit" });
    public GameInput input = new();
    string selectedDeck = "";

    CoroutineControl ctrl;
    Corunner runner;

    Scripter scripter;
    CardPreview testPreview = new CardPreview("北海道", new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2));

    public StartScreen(SharedState state)
    {
        this.state = state;
        ctrl = new();
        runner = new();

        gameMenu.selectedColor = Color.Blue;
        gameMenu.DisableItem(0);

        scripter = new Scripter(Script);
        scripter.Start();
    }

    public async Coroutine Script(CoroutineControl ctrl)
    {
        var n = 20;
        var pos = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);
        var dir = Xt.Vector2.RandomDir() * Random.Shared.Next(1, 2);
        scripter.components.AddUpdate(() =>
        {
            pos += dir * n / 10;
        });
        scripter.components.AddDraw(() =>
        {
            Graphics.SetColor(Color.Green);
            Graphics.Circle(DrawMode.Fill, pos, 30);
        });

        var c1 = new CoroutineControl();
        var c2 = new CoroutineControl();
        var h = new Dictionary<object, int>();
        h[c1.Cancel] = 1;
        h[c2.Cancel] = 2;
        h[c1.Cancel] = 3;
        Console.WriteLine(h[c1.Cancel]);
        Console.WriteLine(h[c2.Cancel]);

        while (true)
        {
            for (int i = 0; i < 20; i++)
            {
                await ctrl.DelayCount(1);
            }
            dir = Xt.Vector2.RandomDir() * Random.Shared.Next(1, 2);
            n = Random.Shared.Next(30, 60);
        }
    }

    public async Coroutine BgScript(CoroutineControl ctrl)
    {
        for (var i = 0; i < 10; i++)
        {
            Console.WriteLine($"bg {i}");
            await ctrl.DelayCount(20);
        }
    }

    public void Load()
    {
        gameMenu.align = PosAlign.StartX;
        gameMenu.SetPosition(new Vector2(gameMenu.Width / 2, Graphics.GetHeight() / 2));
        gameMenu.margin = 5;

        testPreview.runner = runner;
        testPreview.ctrl = ctrl;
        testPreview.Start();

        // TODO:
        /*
        var deckName = loader.LoadLastDeck();
        var decks = loader.LoadAvailableDecks();
        if (deckName is null && decks.Length > 0)
        {
            deckName = decks[0];
        }

        var cards = loader.FetchDueCards(deckName);

        */

        /*
        scripter.Run
        */
    }

    public void Unload()
    {
        input.Dispose();
    }

    public void Draw()
    {
        var leftBarWidth = gameMenu.Width * 2;
        Graphics.SetColor(new Color(20, 20, 20, 128));
        Graphics.Rectangle(DrawMode.Fill, 0, 0, Graphics.GetWidth(), Graphics.GetHeight());
        Graphics.SetColor(new Color(20, 20, 20, 220));
        Graphics.Rectangle(DrawMode.Fill, 0, 0, leftBarWidth, Graphics.GetHeight());
        gameMenu.Draw();

        var margin = 20;
        var deckInfoPos = new Vector2(leftBarWidth + margin, margin);
        Graphics.Print(selectedDeck, deckInfoPos.X, deckInfoPos.Y);
        Xt.Graphics.PrintVertical("testing", deckInfoPos + Vector2.UnitY * 100);

        testPreview.Draw();
        scripter.Draw();
    }

    public void Update()
    {
        if (input.IsPressed(ActionType.Up))
        {
            scripter.StartBackground(BgScript);
            gameMenu.PrevItem();
        }
        else if (input.IsPressed(ActionType.Down))
        {
            gameMenu.NextItem();
        }
        else if (input.IsPressed(ActionType.ButtonA))
        {
            PerformAction();
        }

        runner.Update();
        scripter.Update();
    }

    void PerformAction()
    {
        var choice = gameMenu.GetChoice();
        if (choice == "start")
        {
            Console.WriteLine("TODO");
        }
        else
        {
            Console.WriteLine($"not yet implemented: {choice}");
        }
    }

    public class CardPreview
    {
        public Corunner? runner;
        public CoroutineControl? ctrl;
        TextEntity textObj;
        Vector2 dir = Xt.Vector2.RandomDir();

        public CardPreview(string str, Vector2 pos)
        {
            var sb = new StringBuilder();
            foreach (var ch in str)
            {
                sb.Append(ch);
                sb.AppendLine();
            }
            textObj = new(sb.ToString(), SharedState.self.fontAsian);
            textObj.borderColor = Color.Transparent;
            textObj.bgColor = Color.Transparent;
            textObj.pos = pos;
            textObj.SetColor(Color.Transparent);
        }

        public void Start()
        {
            runner?.Create(StartScript);
        }
        public async Coroutine StartScript()
        {
            if (ctrl == null)
            {
                ctrl = new CoroutineControl();
            }

            var tempColor = Color.Gray;
            int i;
            for (i = 1; i < textObj.text.Length; i++)
            {
                textObj.SetColor(Color.White, i - 1, i - 1);
                textObj.SetColor(tempColor, i, i);
                await ctrl.DelayCount(5);
            }
            await ctrl.DelayCount(150);
            for (i = 1; i < textObj.text.Length; i++)
            {
                textObj.SetColor(Color.Transparent, i - 1, i - 1);
                textObj.SetColor(tempColor, i, i);
                await ctrl.DelayCount(5);
            }
        }

        public void Draw()
        {
            textObj.Draw();
            textObj.pos += dir;
            dir *= 0.98f;
        }

        public void Update()
        {

        }
    }
}

public class Scripter
{
    public CoroutineControl ctrl = new();
    public CoroutineRunner runner = new CoroutineRunner();
    public ComponentRegistry components = new ComponentRegistry();

    Func<CoroutineControl, Coroutine> mainScript;
    Coroutine? mainCoroutine;
    Queue<(Func<CoroutineControl, Coroutine>, object)> bgScripts = new();
    Dictionary<object, CoroutineControl> runningBgScripts = new();

    public Scripter(Func<CoroutineControl, Coroutine> mainScript)
    {
        this.mainScript = mainScript;
    }

    public void Start()
    {
        mainCoroutine = runner.Create(() => mainScript(ctrl));
        runner.Create(async () =>
        {
            while (true)
            {
                (Func<CoroutineControl, Coroutine>, object) pair;

                while (bgScripts.TryDequeue(out pair))
                {
                    var ctrl = new CoroutineControl();
                    var (fn, id) = pair;

                    runningBgScripts[id] = ctrl;
                    fn?.Invoke(ctrl);
                }

                await ctrl.DelayCount(1);
            }
        });
    }

    public void Update()
    {
        runner.Update();
        components.Update();
    }
    public void Draw()
    {
        components.Draw();
    }

    public void StartBackground(Func<CoroutineControl, Coroutine> bgScript, object? id = null)
    {
        id ??= bgScript;
        CoroutineControl c;
        if (runningBgScripts.TryGetValue(id, out c))
        {
            c.Cancel();
        }

        bgScripts.Enqueue((bgScript, id));
    }

    public void StopBgScripts()
    {
        foreach (var ctrl in runningBgScripts.Values)
        {
            ctrl.Cancel();
        }
    }
}

public class CardLoader
{
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
}
