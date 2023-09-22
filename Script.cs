global using static gganki_love.DebugUtils;
global using static gganki_love.VectorUtils;

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

    CardLoader cardLoader = new();
    StartScreen startScreen;
    HuntingGame game;

    View currentView;


    public Script(SharedState state)
    {
        this.state = state;
        subScripts = new List<View> { };

        game = new HuntingGame(state, cardLoader);
        startScreen = new StartScreen(state, cardLoader);
        startScreen.autoStart = false;
        startScreen.OnStart += async () =>
        {
            game.Load();
            await LoadCards();
            currentView = game;
        };

        currentView = startScreen;
    }

    public async Task LoadCards()
    {
        var selectedDeck = (await cardLoader.RestoreSavedState()).lastDeckName;
        var countTask = selectedDeck == null ? Task.FromResult(0) : cardLoader.CountLearnedNewCardsToday(selectedDeck);
        state.lastDeckName = selectedDeck;

        var deckTask = cardLoader.LoadDecks();
        Task<IEnumerable<CardInfo>>? cardTask = null;
        if (selectedDeck != null)
        {
            cardTask = cardLoader.LoadCards(selectedDeck);
        }

        try
        {
            await (cardTask == null ? Task.WhenAll(deckTask, countTask) : Task.WhenAll(deckTask, countTask, cardTask));

            state.deckNames = deckTask.Result;
            if (selectedDeck != null && cardTask != null)
            {

                //state.deckCards[selectedDeck] = cardLoader.DueCards.ToArray();
                var count = Config.newCardsPerDay - cardLoader.LearnedNewCards;
                var newCards = count < 0 ? new CardInfo[0] : cardLoader.NewCards.ToArray()[0..count];
                PrintVar("new cards in deck {0}", newCards.Count());

                var allCards = new List<CardInfo>();
                allCards.AddRange(cardLoader.DueCards);
                allCards.AddRange(newCards);

                state.deckCards[selectedDeck] = allCards.ToArray();
            }
        }
        catch (Exception e)
        {
            PrintVar(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }

    public void Load()
    {

        _ = LoadCards();
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
        game.Unload();
        startScreen.Unload();
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
    public string text;
    public Font? font;

    public Consumable(int tileID)
    {
        entity = Entity.Create(tileID);
        healthGain = Random.Shared.Next(10, 30);
    }

    public void Draw()
    {
        entity.Draw();
        if (font != null) Graphics.SetFont(font);
        var w = (font ?? Graphics.GetFont()).GetWidth(text);
        Graphics.SetColor(Color.DarkCyan);
        Graphics.Print(text, pos.X - w / 2 - 2, pos.Y - 2);
        Graphics.SetColor(Color.White);
        Graphics.Print(text, pos.X - w / 2, pos.Y);
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

    public Consumable SpawnAt(Vector2 pos)
    {
        var item = Consumable.CreateRandom();
        item.pos = pos + Xt.Vector2.RandomDir() * Rand.Next(-50, 50);
        foods.Add(item);
        return item;
    }



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
            foods.Move(item);
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

    public Monster SpawnMonster(string text, int? tileIDArg = null)
    {
        var tileID = tileIDArg.GetValueOrDefault(TileID.RandomMonsterID());
        //var (text, audio) = card.GetContents(type);
        var font = JP.HasJapanese(text) ? SharedState.self.fontAsian : SharedState.self.fontRegular;
        var mon = new Monster(tileID, text ?? "", font);

        mon.pos = world != null ? Xt.Vector2.Random(world.Width, world.Height) : Xt.Vector2.Random();
        mon.group = this;
        mon.target = defaultTarget;

        monsters.Add(mon);

        return mon;
    }


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
        monsters.Clear();
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
    public bool hidden = false;

    public int textDir = Random.Shared.NextSingle() < 0.5f ? 1 : -1;


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

    float lastChangeDir = Love.Timer.GetTime();
    float textOffset = 0;
    public void Update()
    {
        var prevX = entity.pos.X;
        var prevPos = entity.pos;
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
        }

        var xStep = entity.pos.X - prevX;
        textOffset = !IsAlive() ? 0
                   : target?.pos != null ? Xt.MathF.Clamp(Vector2.Normalize(target.pos - entity.pos).X * 50, -50, 50)
                   : Xt.MathF.Clamp(textOffset + xStep * 0.5f, -35, 35);

        var now = Love.Timer.GetTime();
        if (now - lastChangeDir > 3)
        {
            bool changedDir = entity.FaceDirectionX(entity.pos - prevPos);
            if (changedDir)
            {
                lastChangeDir = now;
            }
        }

        var t = textObject;
        var w = t.GetWidth();
        var h = t.GetHeight();
        var x = entity.rect.Center.X - w / 2;
        var y = entity.rect.Top - h - entity.rect.Height / 2;

        t.pos = new Vector2(x + textOffset, y);
    }

    public void Draw()
    {
        if (hidden)
        {
            return;
        }

        entity.Draw();
        if ((flags & Monster.Flags.SubTarget) != 0)
        {
            //Graphics.SetColor(Color.Blue);
            //Graphics.Rectangle(DrawMode.Line, entity.rect);
        }

        //Graphics.Rectangle(DrawMode.Line, entity.rect);


        Graphics.SetColor(IsAlive() ? Color.White : Color.WhiteSmoke);
        Graphics.SetFont(SharedState.self.fontAsian);

        Graphics.SetColor(textColor ?? entity.color);
        textObject.Draw();

        var a = MathF.Min(textObject.rect.Width / 4, 20);

        var p1 = new Vector2(textObject.rect.Center.X - a, textObject.rect.Bottom);
        var p2 = new Vector2(textObject.rect.Center.X + a, textObject.rect.Bottom);
        Graphics.SetColor(textObject.bgColor);
        Graphics.Polygon(
            DrawMode.Fill,
            p1,
            new Vector2(rect.Center.X, rect.Top),
            p2
        );
        Graphics.SetColor(Color.Gray);
        Graphics.Polygon(
            DrawMode.Line,
            new Vector2(p1.X - 1, p1.Y),
            new Vector2(rect.Center.X, rect.Top),
            new Vector2(p2.X + 1, p2.Y)
        );
        var c = textObject.bgColor;
        c.Af = 1;
        Graphics.SetColor(c);
        Graphics.Line(p1, p2);




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
        if (!IsAlive())
        {
            return false;
        }
        return !(logicState.ID == State.Attacked || logicState.ID == State.Dead || logicState.ID == State.Fleeing);
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
        sword.PointAt(Vector2.Normalize(game.input.GetMotion2()));
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

    public bool Contains(Vector2 pos)
    {
        var start = origin - Vector2.One;
        var end = size + Vector2.One;
        if (pos.X < start.X
         || pos.X > end.X
         || pos.Y < start.Y
         || pos.Y > end.Y)
        {
            return false;
        }

        return true;
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

public class EventManager
{
    public delegate void MonsterHitFn(Monster m, Player attacker);
    public delegate void MonsterKillFn(Monster m, Player attacker);
    public delegate void PlayerHitFn(Player player, Monster attacker);
    public delegate void ItemPickupFn(IEntity item, Player pickuper);
    public delegate void EventFn(object eventArg);

    event MonsterHitFn OnMonsterHit = (m, p) => { };
    event MonsterKillFn OnMonsterKill = (m, p) => { };
    event PlayerHitFn OnPlayerHit = (p, m) => { };
    event ItemPickupFn OnItemPickup = (i, p) => { };
    event EventFn OnEvent = (e) => { };


    public void AddOnEvent(EventFn fn)
    {
        OnEvent -= fn;
        OnEvent += fn;
    }
    public void RemoveOnEvent(EventFn fn)
    {
        OnEvent -= fn;
    }

    public void AddOnMonsterHit(MonsterHitFn fn)
    {
        OnMonsterHit -= fn;
        OnMonsterHit += fn;
    }
    public void RemoveOnMonsterHit(MonsterHitFn fn)
    {
        OnMonsterHit -= fn;
    }
    public void AddOnMonsterKill(MonsterKillFn fn)
    {
        OnMonsterKill -= fn;
        OnMonsterKill += fn;
    }
    public void RemoveOnMonsterKill(MonsterKillFn fn)
    {
        OnMonsterKill -= fn;
    }

    public void AddOnPlayerHit(PlayerHitFn fn)
    {
        OnPlayerHit -= fn;
        OnPlayerHit += fn;
    }
    public void RemoveOnPlayerHit(PlayerHitFn fn)
    {
        OnPlayerHit -= fn;
    }
    public void AddOnItemPickup(ItemPickupFn fn)
    {
        OnItemPickup -= fn;
        OnItemPickup += fn;
    }
    public void RemoveOnItemPickup(ItemPickupFn fn)
    {
        OnItemPickup -= fn;
    }

    public void EmitMonsterHit(Monster m, Player attacker)
    {
        OnMonsterHit(m, attacker);
    }
    public void EmitMonsterKill(Monster m, Player attacker)
    {
        OnMonsterKill(m, attacker);
    }
    public void EmitPlayerHit(Player player, Monster attacker)
    {
        OnPlayerHit(player, attacker);
    }

    public void EmitItemPickup(IEntity item, Player pickuper)
    {
        OnItemPickup(item, pickuper);
    }
    public void EmitEvent(object arg)
    {
        OnEvent(arg);
    }

    public async Coroutine<T?> WaitEvent<T>(CoroutineControl ctrl)
    {
        T? x = default(T);
        var done = false;
        EventFn handler = e =>
        {
            if (e is T t)
            {
                x = t;
                done = true;
            }
        };
        AddOnEvent(handler);
        using (Defer.Run(() => RemoveOnEvent(handler)))
        {
            while (!done) await ctrl.Yield();
            return x;
        }
    }
}

public class HuntingGame : View
{

    public enum State { Initializing, Playing, Clear, Gameover }
    public enum HuntState { Init, Vocab, VocabParts, Example, ExampleParts, Clear }

    public class Playing
    {
        public int kills = 0;
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
    Scripter scripter;


    public State gameState = State.Playing;
    public Gameover gameover = new Gameover();
    public Playing playing = new Playing();
    public Clear clear = new Clear();



    public float playTime = 0;
    public int cardIndex = 0;
    public List<CardInfo> allCards = new List<CardInfo>();
    List<CardInfo> cards = new List<CardInfo>();

    public EventManager eventManager = new();

    public GameInput input = new();

    public TextTimer textTimer;

    CardLoader cardLoader;

    public HuntingGame(SharedState state, CardLoader cardLoader)
    {
        this.state = state;
        this.cardLoader = cardLoader;

        cam = new Camera(this);
        player = new Player(this);
        world = new World(this);
        monsterGroup = new MonsterGroup(this);
        itemGroup = new ItemGroup(this);
        scripter = new Scripter(StartGameScript);

        textTimer = new TextTimer();
        textTimer.text.pos = new Vector2(Graphics.GetWidth(), Graphics.GetHeight());
        textTimer.text.align = PosAlign.EndX | PosAlign.EndY;

        components.AddComponent(textTimer);
    }

    public void Load()
    {
        var deckName = state.lastDeckName ?? state.deckNames.Keys.First();
        allCards = state.deckCards?[deckName]?.ToList() ?? new List<CardInfo>();

        foreach (var card in allCards)
        {
            RemoveUnwantedText(card);
        }

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
        var numText = 10;
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
        EventManager.MonsterKillFn onKill = (m, _) =>
        {
            monster = m;
        };

        eventManager.AddOnMonsterKill(onKill);
        using var _ = Defer.Run(() => eventManager.RemoveOnMonsterKill(onKill));

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
        playing.card = card;
    }

    SortedSet<CardInfo> skippedCards = new SortedSet<CardInfo>(Comparer<CardInfo>.Create((a, b) =>
    {
        // put new cards at the end
        if (a.type == 0)
        {
            return 1;
        }
        if (b.type == 0)
        {
            return 0;
        }
        return (int)(a.due - b.due);
    }));
    public CardInfo? GetSkippedDueCard(SortedSet<CardInfo> set)
    {
        CardInfo? selectedCard = null;
        foreach (var card in set)
        {
            if (card == null)
            {
                return null;
            }
            if (card.IsDue())
            {
                selectedCard = card;
                break;
            }
        }

        if (selectedCard != null)
        {
            set.Remove(selectedCard);
        }
        return selectedCard;
    }

    public CardInfo? GetNextCard()
    {
        CardInfo? card = GetSkippedDueCard(skippedCards);

        if (card != null)
        {
            return card;
        }


        while (cardIndex < allCards.Count())
        {
            card = allCards[cardIndex++];
            if (card.IsDue())
            {
                return card;
            }
            skippedCards.Add(card);
            Console.WriteLine("skipped card: {0}, {1}", card.cardId, card.GetVocab());
        }

        return null;
    }

    public void InitGame(bool tryAgain = false)
    {
        if (cardIndex >= allCards.Count() - 1)
        {
            return;
        }

        playing.subIndex = 0;

        gameState = State.Playing;
        // TODO: player.reset()
        player.entity.pos = world.Center;
        player.health = 100;
        playing.maxHunts = 0;
        cam.CenterAt(player.pos);


        if (!tryAgain)
        {
            playing.kills = 0;
            //textTimer.Stop();

            var card = GetNextCard();
            if (card == null)
            {
                Console.WriteLine("no cards available");
                return;
            }
            playing.card = card;

            if (!cards.Contains(playing.card))
            {
                cards.Add(playing.card);
                cards.RemoveAt(0);
            }
            AnkiAudioPlayer.LoadCardAudios(cards);
        }

        Console.WriteLine("==============================");
        Console.WriteLine("current card");
        Console.WriteLine(playing.card);

        //SetPlayingCard("脱ぐ");

        monsterGroup.Clear();
        itemGroup.Clear();

        eventManager.AddOnMonsterKill(OnDefaultMonsterKill);
        eventManager.AddOnMonsterHit(OnDefaultMonsterHit);
        eventManager.AddOnPlayerHit(OnDefaultPlayerHit);
        eventManager.AddOnItemPickup(OnDefaultItemPickup);
    }


    public void StartGame()
    {
        scripter.Start();
    }


    public async Coroutine GameoverScript(CoroutineControl ctrl)
    {
        gameover.opacity = 0;

        using var _1 = components.AddUpdate(UpdateGameover);
        using var _2 = components.AddDraw(DrawGameover);

        await input.PressAny(ctrl, ActionType.ButtonA, ActionType.ButtonB);
        //await ctrl.AwaitAnyKey();
    }
    public async Coroutine UpdateCardDetails(CoroutineControl ctrl, bool correctAnswer)
    {
        await Coroutine.AwaitTask(
            AnkiConnect.AnswerCard(playing.card.cardId, correctAnswer ? AnkiButton.Good : AnkiButton.Again)
        );

        var resp = (await Coroutine.AwaitTask(AnkiConnect.FetchCardInfo(playing.card.cardId))).Unwrap();
        if (resp.Length > 0)
        {
            var card = resp[0];
            playing.card.due = card.due;
            playing.card.interval = card.interval;
            playing.card.queue = card.queue;
            playing.card.reps = card.reps;
            playing.card.factor = card.factor;
            playing.card.mod = card.mod;
        }
        if (!correctAnswer)
        {
            skippedCards.Remove(playing.card);
            skippedCards.Add(playing.card);
        }

    }

    public async Coroutine StartGameScript(CoroutineControl ctrl)
    {
        cam = new Camera(this);
        player = new Player(this);
        world = new World(this);
        cards = CreateCards();

        monsterGroup.Dispose();
        monsterGroup = new MonsterGroup(this, player.entity);

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
                }

                scripter.StartBackground(async ctrl => await UpdateCardDetails(ctrl, correct));
            }


            if (startHunt)
            {
                if (tryAgain && textTimer.IsDone())
                {
                    textTimer.StartCountDown(15);
                }
                else if (!tryAgain)
                {
                    textTimer.StartCountDown(75);
                }


                var subCtrl = new CoroutineControl();
                var gameScript = StartHuntScript(subCtrl);

                //await subCtrl.While(() => gameState == State.Playing);
                while (gameState == State.Playing)
                {
                    await subCtrl.Yield();
                    if (textTimer.IsDone())
                    {
                        break;
                    }
                }

                try
                {
                    Console.WriteLine("cancelling");
                    subCtrl.Cancel();
                }
                catch (Exception e)
                {
                    // ignore
                }

                if (gameState == State.Clear)
                {
                    _ = AnkiConnect.AnswerCard(playing.card.cardId, AnkiButton.Good);
                }

                //if (textTimer.IsDone() && playing.kills > 2)
                //{
                //    gameState = State.Clear;
                //}
            }

            components.RemoveUpdate(UpdatePlayerAction);
            components.RemoveUpdate(UpdateMonsterCollision);

            if (gameState == State.Gameover)
            {
                await GameoverScript(ctrl);
                tryAgain = true;
            }
            else
            {
                tryAgain = false;
                components.AddDraw(DrawClear);
                await input.PressAny(ctrl, ActionType.ButtonA, ActionType.ButtonB);

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


    enum ChooseState
    {
        Start = 0,
        FindItems = 1,
        HuntMonster = 2,
    }

    public async Coroutine<bool> StartChooseAnswerScript(CoroutineControl ctrl)
    {
        playing.textProgress.SetText("");

        Color messageColor = Color.White;
        Color bgColor = new Color(20, 20, 20, 200);
        string message = "";
        Monster? killed = null;
        HashSet<Monster> questionMonsters = new HashSet<Monster>();
        var items = new List<Consumable>();
        var prevPos = player.pos;
        var chooseState = ChooseState.FindItems;
        var numLetters = 0;
        var card = playing.card;
        var choices = CreateChoices(card, 7);
        var (vocab, example) = CreateQuestion(card);

        foreach (var c in choices)
        {
            c.hidden = true;
        }


        EventManager.MonsterHitFn onHit = (m, _) =>
        {
            var damage = Random.Shared.Next(40, 60);
            m.Attack(Vector2.Zero, damage);

            if (m.IsAlive() && questionMonsters.Contains(m))
            {
                AnkiAudioPlayer.Play(m.audioFilename);
            }
        };

        EventManager.MonsterKillFn onKill = (m, _) => killed = m;

        var update = () =>
        {
            prevPos = player.pos;
            foreach (var e in items)
            {
                e.pos += e.entity.dir;
            }
            if (chooseState == ChooseState.FindItems)
            {

                foreach (var e in items)
                {
                    var d = Vector2.Distance(e.pos, world.Center);
                    if (MathF.Floor(Love.Timer.GetTime()) % 5 == 0
                       && d < world.size.X / 2
                       && d > 100)
                    {
                        e.entity.dir = Vector2.Rotate(e.entity.dir, Random.Shared.Next(-45, 45));
                    }
                    if (d > Graphics.GetWidth() / 2)
                    {
                        e.entity.dir = Vector2.Normalize(world.Center - e.pos) * e.entity.dir.Length();
                    }
                }
            }


        };

        var draw = () =>
        {
            if (message != "")
            {
                var pos = SharedState.self.centerBottom - Vector2.UnitY * 100;
                //Graphics.SetColor(bgColor);
                //Xt.Graphics.PrintlnRect(DrawMode.Fill, message, SharedState.self.fontRegular);
                Graphics.SetColor(messageColor);
                Xt.Graphics.PrintPos = pos;
                Xt.Graphics.Println(message, SharedState.self.fontMedium);
            }
        };
        EventManager.ItemPickupFn onItemPickup = (IEntity item, Player player) =>
        {
            var hasMoved = player.pos != prevPos;
            if (!(item is Consumable food) || !hasMoved)
            {
                return;
            }
            var vocab = playing.card.GetVocab() ?? "";

            itemGroup.Remove(item);
            if (!vocab.Contains(food.text))
            {
                player.Hit(40);
            }
            else
            {
                numLetters--;
                if (numLetters <= 0)
                {
                    eventManager.EmitEvent(ChooseState.FindItems);

                }
                else
                {
                    message = $"Find matching letters ({numLetters})";
                }
                //player.AddHealth(food.healthGain);
            }
        };

        eventManager.AddOnMonsterHit(onHit);
        eventManager.AddOnMonsterKill(onKill);
        eventManager.AddOnItemPickup(onItemPickup);
        eventManager.RemoveOnPlayerHit(OnDefaultPlayerHit);
        eventManager.RemoveOnMonsterHit(OnDefaultMonsterHit);
        eventManager.RemoveOnMonsterKill(IncrementMonsterKillCount);
        eventManager.RemoveOnItemPickup(OnDefaultItemPickup);

        //components.RemoveDraw(DrawInterface);
        components.AddDraw(draw);
        components.AddUpdate(update);

        using var _ = Defer.Run(() =>
        {
            eventManager.RemoveOnMonsterHit(onHit);
            eventManager.RemoveOnMonsterKill(onKill);
            eventManager.AddOnMonsterHit(OnDefaultMonsterHit);
            eventManager.AddOnPlayerHit(OnDefaultPlayerHit);
            eventManager.AddOnMonsterKill(IncrementMonsterKillCount);
            eventManager.AddOnItemPickup(OnDefaultItemPickup);
            eventManager.RemoveOnItemPickup(onItemPickup);
            components.RemoveDraw(draw);
            components.RemoveUpdate(update);
        });


        var texts = cards.GetRandomItems(2).Select(c => c.GetVocab() ?? "").ToList();
        var numVocab = 2;
        var vocabParts = vocab.text.Where(c => JP.IsKanaOrKanji(c) || char.IsLetterOrDigit(c)).ToArray();
        for (var i = 0; i < 2; i++)
        {
            texts.Add(vocab.text);
        }

        foreach (var text in texts)
        {
            foreach (var ch in text)
            {
                if (!JP.IsKanaOrKanji(ch) && !char.IsLetterOrDigit(ch))
                {
                    continue;
                }
                var item = itemGroup.SpawnAt(vocab.pos + Vector2.UnitY * 20);
                item.text = ch.ToString();
                item.font = state.fontRegular;
                item.entity.dir = Xt.Vector2.RandomDir() * Random.Shared.Next(2, 3);
                items.Add(item);
            }
        }

        numLetters = numVocab * vocabParts.Length;
        message = $"Find matching letters ({numLetters})";

        // TODO: show cards learned/reviewed while playing
        // TODO: show message when there is no card available

        // TODO: use ebisu spacing algorithm
        // so yeah, I'm dropping anki dependency in the end
        // or at least, for the AJT kanji transition deck
        // it'll make the game easier to package and 
        // let other people try it out
        // ebisu algo does seem simpler compared to
        // to supermemo derived algos,
        // and would probably work better for my use case
        // but
        // I've already spent a little too long
        // on this project
        // my interest in continuing has admittedly died bit
        // It wasn't entirely a useless endeavor,
        // I learned lots of things,
        // particularly the do's and don'ts when making a game.
        // At least for the next game project,
        // I would have better idea how to structure larger games.
        // I should probably write them down sooner.
        // On the positive side, I guess I did somehow manage
        // to achieve my goal. That is, a more efficient
        // means of learning from flashcards.
        // The cards I had difficulty learning before,
        // now learn them easier or faster. And I learn
        // the difficult cards easier as well.
        // So yeah, at least the concept has potential.
        // Anyway, for now, I will freeze the project
        // from any feature/visual changes, and ocassionally
        // do some tiny bug fixes and some minor tweaking
        // on the game parameters.
        // It's good enough for now.
        // While in testing phase, I'm going to
        // do other projects now. Hooray!??11
        // Yeah, working on one project for a long time
        // isn't very fun.
        // I'm considering what to do next,
        // but more importantly, I should start finding
        // work. The stress of running out of money
        // is starting to take a toll on me.
        // Unfortunately, I still need to make
        // some new one or two tiny projects
        // when applying.
        // So the plan then would be to create
        // simple projects in different tech stacks.
        // I'm thinking of using tauri, monogame,
        // and ebiten.
        // By simple, I mean something that can be done 
        // in a day or two, and does one thing well.
        // Also, I was considering of creating a
        // lua-based static site generator,
        // but I should focus on making
        // a more presentable personal site/resume instead. 
        // I'd be a lot more chill if my mother wasn't
        // occasionally gaslighting me and asking
        // me where's the money.
        // But yeah, fuck it, how is it my sole
        // responsibility. I have 5 more other siblings
        // to share my burden.
        // At worst case, I end up going outside
        // and start looking for local work,
        // and that doesn't seem so bad.


        // TODO: add time elapsed when submitting answer to anki

        // TODO: add some walking monsters on start screen

        // TODO: in-game menu

        // TODO: example boss
        //       shoots stuffs
        //       - other monsters walks outside of grid
        //       - focus camera on big guy and play text
        //       - pick up sword with the matching text
        //       - show kanji damage effect per hit

        // TODO: general codebase clean up
        //       fix all warnings

        // TODO: clean up interface, create release build
        //       move game outside script file

        // TODO: start actual day-to-day testing

        // TODO: add basic floors walls, decorations (random biomes)
        //       canvas layers (floor, walls, entities, interface)


        // TODO: turn-based mode: monster only move when the player moves
        // TODO: skirmish

        // TODO: refactoring
        //       decouple events 

        // TODO: handle failed HTTP requests

        // TODO: visual improvements
        //       shaders, particles, camera movements
        //       - particles.Add(tileIDs)
        //       - litters.Add(bloodIDs)

        // TODO: random dungeon generation

        // TODO: add other game types
        //       - search for pieces in maze

        // So... making this into a general purpose
        // anki interface would take more work, which
        // involves making sure that the platform
        // is supported, as well as the anki
        // and the plugins are compatible with each user. 
        // So the plan would be just to release
        // it as a demo with some pre-installed
        // decks installed.
        // I'll add some contact info and
        // future plans in the game, and
        // see if there are any interest for this kind of stuffs.
        // If none, well, it works well as a portfolio
        // addition.
        // While play testing and waiting for feedback,
        // I should move on to next projects.
        // I've been thinking of porting the voxel editor
        // to monogame. Monogame does have (basic?) 3D support.
        // More importantly, I should start doing
        // smaller, more focused projects with well-defined
        // scopes and does one thing well.
        // It's either for utility or amusement.
        // Then I start applying for work.

        // TODO: in-game zoom in/out
        // set camera on question
        // pan to player 
        // await camera.panTo()
        // await camera.zoomIn()
        // await camera.zoomOut()

        player.pos = vocab.pos - Vector2.UnitX * player.rect.Width * 2;
        prevPos = player.pos;

        questionMonsters.Add(vocab);
        vocab.defense = 2;
        if (example != null)
        {
            questionMonsters.Add(example);
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

        await Coroutine.WaitAny(
                eventManager.WaitEvent<ChooseState>(ctrl),
                ctrl.While(() => killed == null)
        );

        message = "Hunt monster with correct answer";
        chooseState = ChooseState.HuntMonster;
        foreach (var e in items)
        {
            e.entity.dir = Vector2.Normalize(e.entity.dir) * 10;
        }
        foreach (var c in choices)
        {
            c.hidden = false;
        }

        foreach (var m in choices)
        {
            m.exploring.maxSpeed = 0.5f;
            m.exploring.speed = 0.1f;
            m.exploring.steps = 100;
            m.logicState = m.exploring;
        }


        await ctrl.While(() => killed == null);
        itemGroup.Clear();

        eventManager.RemoveOnMonsterHit(onHit);
        eventManager.RemoveOnMonsterKill(onKill);

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


        await ctrl.AwaitTask(AnkiAudioPlayer.PlayWait(card.GetField("VocabAudio")));
        await ctrl.Sleep(0.3f);
        await ctrl.AwaitTask(AnkiAudioPlayer.PlayWait(card.GetField("SentAudio")));
        await ctrl.Sleep(0.1f);

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
        choices.Shuffle();

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
        monVocab.pos = qpos + Vector2.One * monVocab.rect.Height * 2.7f;
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
        eventManager.AddOnPlayerHit(OnDefaultPlayerHit);
        eventManager.AddOnMonsterKill(IncrementMonsterKillCount);

        var text = playing.card.GetVocab() ?? "";
        foreach (var m in monsterGroup.Iterate())
        {
            if (m.card == playing.card)
            {
                playing.target = m;
                break;
            }
        }
        var target = playing.target;

        await ReadyGameScript(ctrl);

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

        eventManager.AddOnMonsterHit(OnSubTargetHit);
        eventManager.RemoveOnMonsterHit(OnDefaultMonsterHit);
        using var _ = Defer.Run(() =>
        {
            eventManager.RemoveOnMonsterHit(OnSubTargetHit);
            eventManager.AddOnMonsterHit(OnDefaultMonsterHit);
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

        eventManager.AddOnMonsterHit(OnSubTargetHit);
        eventManager.RemoveOnMonsterHit(OnDefaultMonsterHit);
        using var _ = Defer.Run(() =>
        {
            eventManager.AddOnMonsterHit(OnDefaultMonsterHit);
            eventManager.RemoveOnMonsterHit(OnSubTargetHit);
        });

        while (playing.subIndex < playing.subTargets.Count())
        {
            var hunted = await GetHuntedKill(ctrl);
            playing.textProgress.NextTextItem();
            playing.NextTarget();
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
        var countdown = 3;

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

        /*{
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
        */
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
        clear.gpr.Print("Elapsed: {0} seconds", MathF.Floor(playTime));
        clear.gpr.font = SharedState.self.fontAsian;
        clear.gpr.Printf($"{card.GetField("VocabFurigana")} / {card.GetField("VocabDef")}", maxWidth, AlignMode.Left);


        if (card.HasExample())
        {
            clear.gpr.font = SharedState.self.fontSmall;
            clear.gpr.Print(" ");
            clear.gpr.font = SharedState.self.fontAsian;
            clear.gpr.Printf(card.GetField("SentFurigana") ?? "", maxWidth);
            clear.gpr.font = SharedState.self.fontSmall;
            clear.gpr.font = SharedState.self.fontMedium;
            clear.gpr.Print(" ");
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

    public void OnDefaultPlayerHit(Player player, Monster m)
    {
        player.Hit(player.entity.scale / m.entity.scale);
    }

    public void OnDefaultMonsterKill(Monster m, Player attacker)
    {
        Console.WriteLine("default monster kill");
        //DropLoots(m.pos);
    }
    public void IncrementMonsterKillCount(Monster m, Player attacker)
    {
        if (gameState == State.Playing)
        {
            playing.kills++;
        }
    }

    public void OnSubTargetHit(Monster m, Player attacker)
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

    public void OnDefaultMonsterHit(Monster m, Player attacker)
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
            scripter.StartBackground(async (ctrl) =>
            {
                //var text = m.textObject.text == m.card.GetVocabDef() ? m.card.GetVocab() : m.card.GetVocabDef();
                var text = m.text == m.card.GetVocab()
                         ? m.card.GetVocabDef()
                         : m.text == m.card.GetExample()
                         ? m.card.GetField("SetEng")
                         : m.text;

                if (m.textObject.text == text)
                {
                    return;
                }

                if (text != null)
                {
                    m.textObject.SetText(text);
                    await m.textObject.TypeWrite(ctrl, Color.Red, 3);
                }
                await ctrl.Sleep(5);

                m.textObject.SetText(m.text);
                await m.textObject.TypeWrite(ctrl, Color.Red, 3);
            });
        }
        else if (!m.IsAlive())
        {
            m.textObject.SetText(m.text);
        }
    }

    public void OnDefaultItemPickup(IEntity item, Player pickuper)
    {
        if (item is Consumable food)
        {
            itemGroup.Remove(item);
            pickuper.AddHealth(food.healthGain);
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
        foreach (var item in itemGroup.GetItemsAt(player.rect))
        {
            if (item.rect.IntersectsWith(player.rect))
            {
                eventManager.EmitItemPickup(item, player);
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

                if (m.CanHit())
                {
                    eventManager.EmitMonsterHit(m, player);
                }


                if (!m.IsAlive())
                {
                    eventManager.EmitMonsterKill(m, player);
                }

            }
        }

        foreach (var m in monsterGroup.GetMonstersAt(player.pos))
        {
            if (player.entity.CollidesWith(m.entity) && m.CanDamage())
            {
                //player.Hit(player.entity.scale / m.entity.scale);
                eventManager.EmitPlayerHit(player, m);
            }
        }
    }

    public void UpdateMonsters()
    {
        monsterGroup.Update();
    }

    public async Coroutine KillSubTargets(CoroutineControl ctrl)
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
            playTime += Love.Timer.GetDelta();
        }

        cam.Update();
        cam.RestrictWithin(player.pos);

        if (!Keyboard.IsDown(KeyConstant.Tab))
        {
            components.Update();
            scripter.Update();
        }

        if (Keyboard.IsPressed(KeyConstant.F3))
        {
            scripter.StartBackground(KillSubTargets);
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
        scripter.Cancel();
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

public class TextTimer : IComponent
{
    event Action OnTimeUp = () => { };
    public TextEntity text;
    float startSeconds;
    float endSeconds;
    float step = 0;
    float elapsed = 0;
    bool running = false;
    public TextTimer()
    {
        text = new("00:00", SharedState.self.fontSmall);
    }

    public void StartCountDown(float numSeconds)
    {
        elapsed = 0;
        endSeconds = 0;
        startSeconds = numSeconds;
        step = Math.Sign(endSeconds - startSeconds);
        running = true;
        UpdateText();
    }

    private void UpdateText()
    {
        var span = TimeSpan.FromSeconds(Math.Abs(endSeconds - startSeconds));
        text.SetText($"{span.Minutes.ToString("D2")}:{span.Seconds.ToString("D2")}");
    }

    public void Update()
    {
        if (!running)
        {
            return;
        }

        elapsed += Love.Timer.GetDelta();
        if (elapsed >= 1)
        {
            elapsed = 0;
            startSeconds += step;

            if ((step > 0 && startSeconds >= endSeconds) ||
                (step < 0 && startSeconds <= endSeconds) ||
                 step == 0)
            {
                running = false;
                OnTimeUp();
            }

            UpdateText();
        }
    }

    public void Draw()
    {
        text.Draw();
    }

    public async Coroutine WaitTimeUp(CoroutineControl ctrl)
    {
        var done = false;
        var fn = () => { done = true; };

        OnTimeUp += fn;
        using (Defer.Run(() => OnTimeUp -= fn))
        {
            while (!done) await ctrl.Yield();
        }

    }

    public void RemoveOnTimeUp(Action fn) => OnTimeUp -= fn;
    public void AddOnTimeUp(Action fn)
    {
        OnTimeUp -= fn;
        OnTimeUp += fn;
    }

    public bool IsDone()
    {
        return !running;
    }
}

public class TextEntity
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

        this.text = string.Join('\n', lines);

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

        //pos = p;
        rect.Location = p;
    }

    public void SetColor(Color color, int startIndex = 0, int? endIndexOpt = null)
    {
        int endIndex = endIndexOpt.GetValueOrDefault(coloredText.Count() - 1);
        for (var i = startIndex; i <= Math.Min(endIndex, coloredText.Count() - 1); i++)
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
        Graphics.Translate(rect.X, rect.Y);
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


public class StartScreen : View
{
    public event Action OnStart = () => { };

    SharedState state;
    public SimpleMenu gameMenu = new SimpleMenu(new[] { "start", "select deck", "options", "exit" });
    public GameInput input = new();
    string selectedDeck = "";

    Scripter scripter;
    LoadingIcon loadingIcon = new();
    RectangleF sideRect;
    RectangleF mainRect;

    CardLoader cardLoader;
    CardPreview testPreview = new CardPreview("北海道", new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2));
    public bool autoStart;

    public StartScreen(SharedState state, CardLoader cardLoader)
    {
        this.state = state;
        this.cardLoader = cardLoader;
        loadingIcon.pos = new Vector2(state.center);

        input.sensitivity = 0.8f;

        //gameMenu.selectedColor = Color.Blue;
        //gameMenu.DisableItem(2);
        //gameMenu.DisableItem(1);

        scripter = new Scripter(Script);
        scripter.Start();

        sideRect = new RectangleF(0, 0, 300f, Graphics.GetHeight());
        mainRect = new RectangleF(sideRect.Width, 0, Graphics.GetWidth() - sideRect.Width, Graphics.GetHeight());
        loadingIcon.pos = mainRect.Center;

        gameMenu.align = PosAlign.StartX;
        gameMenu.SetPosition(new Vector2((sideRect.Width - gameMenu.Width) / 2, Graphics.GetHeight() / 2));
        gameMenu.margin = 5;
    }

    public async Coroutine Script(CoroutineControl ctrl)
    {
        var done = false;

        while (!done)
        {
            while (cardLoader.DueCards.Count() == 0)
            {
                await ctrl.DelayCount(1);
            }

            var previews = new HashSet<CardPreview>();
            var cos = new HashSet<Coroutine>();
            var rect = mainRect.SplitY(2, 0);
            rect.Inflate(0, -120);

            var cards = cardLoader.DueCards.GetRandomItems(10);
            var numColumns = cards.Count();
            var itemWidth = rect.Width / numColumns;
            var columns = (0..(cards.Count() - 1)).ToArray();
            var columnIndex = 0;
            columns.Shuffle();

            foreach (var card in cards)
            {
                var pos = new Vector2(
                    rect.X + columns[columnIndex++] * itemWidth + itemWidth / 2,
                    rect.Y + Random.Shared.Next(0, (int)rect.Height)
                );

                CardPreview cardPreview = new CardPreview(card.GetVocab(), pos);
                previews.Add(cardPreview);

                scripter.components.AddDraw(cardPreview.Draw);
                scripter.components.AddUpdate(cardPreview.Update);

                cos.Add(cardPreview.Animate(ctrl));
                await ctrl.DelayCount(60);
            }

            await ctrl.DelayCount(120);
        }
    }

    public async Task LoadCards()
    {
        var retries = 0;
        while (true)
        {
            try
            {
                selectedDeck = await Run(cardLoader) ?? "";
                break;
            }
            catch (System.IO.IOException)
            {
                if (retries++ > 10)
                {
                    throw;
                }
                await Task.Delay(retries * 250);
            }
        }
        static async Task<string?> Run(CardLoader cardLoader)
        {

            var savedStateTask = cardLoader.RestoreSavedState();
            var decksTask = cardLoader.LoadDecks();

            await Task.WhenAll(savedStateTask, decksTask);
            var selectedDeck = (await savedStateTask)?.lastDeckName ?? "";
            var decks = await decksTask;

            if (selectedDeck is null && decks.Count() > 0)
            {
                selectedDeck = decks.Keys.FirstOrDefault("");
            }

            if (!string.IsNullOrEmpty(selectedDeck))
            {
                _ = cardLoader.CountLearnedNewCardsToday(selectedDeck);
                await cardLoader.LoadCards(selectedDeck);
            }
            return selectedDeck;
        }
    }

    public void Load()
    {
        scripter.StartBackground(async (ctrl) =>
        {
            loadingIcon.Enable = true;
            await ctrl.AwaitTask(LoadCards());
            Console.WriteLine("done loading {0}", selectedDeck);
            await ctrl.DelayCount(10);
            loadingIcon.Enable = false;

            if (autoStart)
            {
                OnStart();
            }
        });
    }

    public void CreateCardPreviews()
    {

    }

    public void Unload()
    {
        input.Dispose();
        scripter.Cancel();
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

        scripter.Draw();
        loadingIcon.Draw();
    }

    public void Update()
    {
        if (input.IsPressed(ActionType.Up))
        {
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

        loadingIcon.Update();
        scripter.Update();
    }

    void PerformAction()
    {
        var choice = gameMenu.GetChoice();
        if (choice == "start")
        {
            OnStart();
        }
        else if (choice == "exit")
        {
            Love.Event.Quit();
        }
        else
        {
            Console.WriteLine($"not yet implemented: {choice}");
        }
    }

    public class CardPreview
    {
        TextEntity textObj;
        Vector2 dir = Vector2.UnitY * Xt.MathF.RandomSign();

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

        public async Coroutine Animate(CoroutineControl ctrl)
        {
            textObj.SetColor(Color.Transparent);
            var tempColor = Color.Gray;
            int i;
            for (i = 1; i < textObj.text.Length; i++)
            {
                textObj.SetColor(Color.White, i - 1, i - 1);
                textObj.SetColor(tempColor, i, i);
                await ctrl.DelayCount(5);
            }
            await ctrl.DelayCount(200);
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
        }

        public void Update()
        {
            textObj.pos += dir;
            dir *= 0.98f;
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

    public void Cancel()
    {
        ctrl.Cancel();
        foreach (var ctrl in runningBgScripts.Values)
        {
            ctrl.Cancel();
        }
    }
}

public class CardLoader
{
    public DeckNames DeckNames { get; set; } = new();

    public List<CardInfo> NewCards { get; set; } = new();
    public List<CardInfo> DueCards { get; set; } = new();
    public int LearnedNewCards { get; set; } = 0;

    public bool IsLoadingDecks { get; set; }
    public bool IsLoadingCards { get; set; }

    Task<DeckNames>? loadDecksTask;
    Task<IEnumerable<CardInfo>>? loadCardsTask;

    public async Task<DeckNames> LoadDecks()
    {
        if (loadDecksTask != null)
        {
            return await loadDecksTask;
        }

        loadDecksTask = Task.Run(async () =>
        {
            IsLoadingDecks = true;
            using var _ = Defer.Run(() => IsLoadingDecks = false);

            var resp = (await AnkiConnect.FetchDecks()).Unwrap();
            DeckNames = resp;

            return resp ?? new DeckNames();
        });

        var result = await loadDecksTask;
        loadDecksTask = null;

        return result;
    }

    public async Task<IEnumerable<CardInfo>> LoadCards(string deckName)
    {
        if (loadCardsTask != null)
        {
            return await loadCardsTask;
        }

        loadCardsTask = Task.Run(async () =>
        {
            var cardCountLimit = 100;
            IsLoadingCards = true;
            using var _ = Defer.Run(() => IsLoadingDecks = false);

            var resp = await Task.WhenAll(
                AnkiConnect.FetchNewCards(deckName, cardCountLimit),
                AnkiConnect.FetchAvailableCards(deckName, cardCountLimit)
            );
            var newCards = resp[0].Unwrap();
            var dueCards = resp[1].Unwrap();
            //Console.WriteLine("new cards: {0}", newCards.Length);
            Console.WriteLine("due cards: {0}", dueCards.Length);

            foreach (var card in newCards)
            {
                card.IsNew = true;
            }

            NewCards = newCards.ToList();
            DueCards = dueCards.ToList();

            return dueCards.Union(newCards);
        });

        var result = await loadCardsTask;
        loadCardsTask = null;
        return result;
    }
    public async Task<int> CountLearnedNewCardsToday(string deckName)
    {
        var count = (await AnkiConnect.CountLearnedNewCardsToday(deckName)).Unwrap();
        LearnedNewCards = count;
        Console.WriteLine("learned new cards {0}", count);
        return count;
    }

    public async Task WriteSaveState(SavedState save)
    {
        // disabled because "Select deck is not implemented"
        //var contents = JsonSerializer.Serialize(save);
        //await System.IO.File.WriteAllTextAsync(Config.savedStateFilename, contents);
    }

    public async Task<SavedState> RestoreSavedState()
    {
        // disabled because "Select deck is not implemented"
        //try
        //{
        //    var contents = await System.IO.File.ReadAllTextAsync(Config.savedStateFilename);
        //    var savedState = JsonSerializer.Deserialize<SavedState>(contents);
        //    return savedState ?? new SavedState();
        //}
        //catch (JsonException) { }
        //catch (System.IO.FileNotFoundException) { }

        return new SavedState { lastDeckName = "AJT Kanji Transition TSC" };
    }
}


public class LoadingIcon
{
    public Vector2 pos;
    public float radians1;
    public float radians2;
    public float stepSize = 0.01f;
    public float radius = 50;
    public float counter = 100;
    public Color color = Color.White;

    public bool Enable { get; set; }

    public void Update()
    {
        if (!Enable)
        {
            return;
        }

        radians1 += stepSize;
        radians2 += stepSize * Random.Shared.NextSingle();
        if (counter-- <= 0)
        {
            stepSize = Random.Shared.NextSingle() / 2 * Xt.MathF.RandomSign();
            counter = 100;
        }
    }

    public void Draw()
    {
        if (!Enable)
        {
            return;
        }

        Graphics.Push();
        Graphics.Translate(pos.X, pos.Y);
        Graphics.Rotate(radians2);
        Graphics.SetColor(color);
        Graphics.Circle(DrawMode.Line, 0, 0, radius - counter / 5);
        Graphics.Circle(DrawMode.Line, 0, 0, radius);
        Graphics.Rotate(radians1);
        Graphics.Rectangle(DrawMode.Line, -radius, -radius, radius * 2, radius * 2);
        Graphics.Rotate(radians2);
        Graphics.Rectangle(DrawMode.Line, -radius / 2, -radius / 2, radius, radius);
        Graphics.Pop();

        //Graphics.SetFont(SharedState.self.fontSmall);
        //Graphics.Print("Loading", pos.X - radius, pos.Y);
    }
}