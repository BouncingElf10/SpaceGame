using Godot;

public partial class Player : CharacterBody3D
{
	public const float Drag = 0.9999f;
	public const float MovementSpeed = 5f;
	public const float TurnSpeed = 1f;

	public override void _PhysicsProcess(double delta)
	{
		float step = TurnSpeed * (float)delta;

		RotateObjectLocal(Vector3.Up, Input.GetAxis("yaw_right", "yaw_left") * step);
		RotateObjectLocal(Vector3.Right, Input.GetAxis("pitch_down", "pitch_up") * step);
		RotateObjectLocal(Vector3.Back, Input.GetAxis("roll_right", "roll_left") * step);
		Basis = Basis.Orthonormalized();

		Vector3 thrust = new Vector3
		(
			Input.GetAxis("move_left", "move_right"),
			Input.GetAxis("move_down", "move_up"),
			Input.GetAxis("move_forward", "move_back")
		);
		
		Velocity = Velocity * Drag + Basis * thrust.LimitLength() * MovementSpeed * (float) delta;
		MoveAndSlide();
	}
}
