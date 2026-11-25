using Godot;
using System;

public partial class Camera : Camera3D
{
	[Export] public float FollowSpeed = 6f;
	[Export] public float LookSensitivity = 0.002f;
	[Export] public float ZoomStep = 0.15f;
	[Export] public float MinZoom = 0.5f;
	[Export] public float MaxZoom = 4f;

	private Node3D _ship;
	private Transform3D _offset;
	private Basis _basis;
	private Vector2 _look;
	private float _zoom = 1f;
	private float _targetZoom = 1f;

	public override void _Ready()
	{
		_ship = GetParent<Node3D>();
		_offset = Transform;
		_basis = _ship.GlobalBasis;
		TopLevel = true;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Right))
		{
			_look -= motion.Relative * LookSensitivity;
			_look = new Vector2(Mathf.Clamp(_look.X, -Mathf.Pi, Mathf.Pi), Mathf.Clamp(_look.Y, -1.4f, 1.4f));
		}
		else if (e is InputEventMouseButton { Pressed: true } button)
		{
			if (button.ButtonIndex == MouseButton.WheelUp)
			{
				_targetZoom = Mathf.Max(_targetZoom - ZoomStep, MinZoom);
			}
			else if (button.ButtonIndex == MouseButton.WheelDown)
			{
				_targetZoom = Mathf.Min(_targetZoom + ZoomStep, MaxZoom);	
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float weight = 1f - Mathf.Exp(-FollowSpeed * (float)delta);
		
		_basis = _basis.Slerp(_ship.GlobalBasis, weight).Orthonormalized();
		_zoom = Mathf.Lerp(_zoom, _targetZoom, weight);
		if (!Input.IsMouseButtonPressed(MouseButton.Right))
		{
			_look = _look.Lerp(Vector2.Zero, weight);
		}
			

		Basis basis = _basis * new Basis(Vector3.Up, _look.X) * new Basis(Vector3.Right, _look.Y);
		GlobalTransform = new Transform3D(basis * _offset.Basis, _ship.GlobalPosition + basis * _offset.Origin * _zoom);
	}
}
