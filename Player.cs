using Godot;
using Godot.Collections;

public partial class Player : CharacterBody3D
{
	public const float Drag = 0.9999f;
	public const float MovementSpeed = 5f;
	public const float TurnSpeed = 1f;
	public const float MouseSensitivity = 0.0003f;
	public const float TurnResponse = 8f;

	private Vector2 _mouse;
	private Vector3 _turn;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured &&
		    !Input.IsMouseButtonPressed(MouseButton.Right))
		{
			_mouse += motion.Relative;
		}
		else if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (e.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float) delta;
		
		Array<Node> children = GetParent<Node3D>().GetChildren();
		Planet planet = GetClosestPlanet(children);
		
		if (planet == null)
		{
			GD.PrintErr("No planet found!: " + children );
			return;
		}
		
		Vector3 turn = new Vector3
		(
			Input.GetAxis("pitch_down", "pitch_up") * TurnSpeed - _mouse.Y * MouseSensitivity / dt,
			Input.GetAxis("yaw_right", "yaw_left") * TurnSpeed - _mouse.X * MouseSensitivity / dt,
			Input.GetAxis("roll_right", "roll_left") * TurnSpeed
		);
		_mouse = Vector2.Zero;
		_turn = _turn.Lerp(turn, 1f - Mathf.Exp(-TurnResponse * dt));

		RotateObjectLocal(planet.GetRight(Position), _turn.X * dt);
		RotateObjectLocal(planet.GetUp(Position), _turn.Y * dt);
		RotateObjectLocal(planet.GetBack(Position), _turn.Z * dt);
		Basis = Basis.Orthonormalized();

		Vector3 thrust = new Vector3
		(
			Input.GetAxis("move_left", "move_right"),
			Input.GetAxis("move_down", "move_up"),
			Input.GetAxis("move_forward", "move_back")
		);

		Velocity = Velocity * Drag + Basis * thrust.LimitLength() * MovementSpeed * (float) delta + planet.GetGravityVector(Position);
		MoveAndSlide();
	}

	private Planet GetClosestPlanet(Array<Node> children)
	{
		Planet closest = null;
		float closestDist = float.MaxValue;

		foreach (Node child in children)
		{
			if (child is Planet planet)
			{
				float distance = planet.GlobalPosition.DistanceSquaredTo(GlobalPosition);
				if (distance < closestDist)
				{
					closestDist = distance;
					closest = planet;
				}
			}
		}

		return closest;
	}
}
