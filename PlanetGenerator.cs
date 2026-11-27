using Godot;
using System.Collections.Concurrent;

public partial class PlanetGenerator : StaticBody3D
{
	public const float Radius = 1.0f;
	public const float Bumpiness = 0.08f;
	public const int GridQuads = 32;
	public const int MaxDepth = 18;

	private const int UploadsPerFrame = 4;
	private const int ShapesPerFrame = 2;

	private static readonly FastNoiseLite Noise = new() { Frequency = 10.5f, FractalType = FastNoiseLite.FractalTypeEnum.None };

	public readonly ConcurrentQueue<QuadNode> Built = new();
	public readonly ConcurrentQueue<QuadNode> BuiltShapes = new();

	private readonly QuadNode[] _roots = new QuadNode[6];
	private Player _player;

	public override void _Ready()
	{
		GetParent<Planet>().SetRadius(Radius);
		_player = (Player) GetTree().Root.FindChild("Player", true, false);

		for (int face = 0; face < _roots.Length; face++)
		{
			_roots[face] = new QuadNode(this, face, 0, 0, 0);	
		}
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < UploadsPerFrame && Built.TryDequeue(out var node); i++)
		{
			node.Upload();
		}

		for (int i = 0; i < ShapesPerFrame && BuiltShapes.TryDequeue(out var node); i++)
		{
			node.UploadShape();
		}

		var camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Vector3 local = ToLocal(camera.GlobalPosition);
		Vector3 player = ToLocal(_player.GlobalPosition);
		
		foreach (var root in _roots)
		{
			root.Update(local, player, true);
		}
	}

	public static float Height(Vector3 dir)
	{
		return Noise.GetNoise3Dv(dir) * Bumpiness;
	}
}
