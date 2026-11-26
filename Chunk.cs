using Godot;

public partial class Chunk : MeshInstance3D
{
	private static readonly StandardMaterial3D NearMaterial = new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex, AlbedoColor = Colors.Red };
	private static readonly StandardMaterial3D FarMaterial = new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex, AlbedoColor = Colors.White };

	private int _id;
	private CollisionShape3D _collisionShape3D;
	private Player _player;
	private Aabb _aabb;
	private float _nearDistance;

	public void SetId(int id)
	{
		_id = id;
	}

	public void SetCollision(CollisionShape3D collisionShape3D)
	{
		_collisionShape3D = collisionShape3D;
	}

	public override void _Ready()
	{
		_player = (Player) GetTree().Root.FindChild("Player", true, false);
		_aabb = Mesh.GetAabb();
		_nearDistance = (_aabb.Size.X + _aabb.Size.Y + _aabb.Size.Z) / 3 * 1.05f;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 playerLocal = ToLocal(_player.GlobalPosition);
		bool near = _aabb.HasPoint(playerLocal) || playerLocal.DistanceTo(_aabb.GetCenter()) < _nearDistance;

		_collisionShape3D.Disabled = !near;
		MaterialOverride = near ? NearMaterial : FarMaterial;
	}
}
