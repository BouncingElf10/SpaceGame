using Godot;
using Godot.Collections;
using System.Linq;

public class QuadNode
{
	private const float SplitRange = 3f;
	private const float MergeRange = 3.6f;
	private const float ColliderRange = 0.01f;
	private const int ColliderDepth = 7;
	private const float SkirtDepth = 0.1f;
	
	private static readonly int[][] Faces =
	{
		new[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 },
		new[] { -1, 0, 0, 0, 0, 1, 0, 1, 0 },
		new[] { 0, 1, 0, 0, 0, 1, 1, 0, 0 },
		new[] { 0, -1, 0, 1, 0, 0, 0, 0, 1 },
		new[] { 0, 0, 1, 1, 0, 0, 0, 1, 0 },
		new[] { 0, 0, -1, 0, 1, 0, 1, 0, 0 }
	};

	private static readonly StandardMaterial3D Material = new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex, AlbedoColor = Colors.White };

	private readonly PlanetGenerator _gen;
	private readonly int _face, _level, _x, _y;
	private readonly double _size;
	private readonly double _cx, _cy, _cz;
	private readonly Vector3 _center;

	private QuadNode[] _kids;
	private MeshInstance3D _mesh;
	private CollisionShape3D _collider;
	private bool _colliding;
	private volatile bool _dead;
	private long _task;
	private float _radius;

	private Vector3[] _verts, _normals, _faces;
	private int[] _indices;
	private int _gridIndexCount;

	public QuadNode(PlanetGenerator gen, int face, int level, int x, int y)
	{
		_gen = gen;
		_face = face;
		_level = level;
		_x = x;
		_y = y;
		_size = 2.0 / (1 << level);

		var c = Surface(-1 + (x + 0.5) * _size, -1 + (y + 0.5) * _size);
		_cx = c.X;
		_cy = c.Y;
		_cz = c.Z;
		_center = new Vector3((float) c.X, (float) c.Y, (float) c.Z);

		_task = WorkerThreadPool.AddTask(Callable.From(Build));
	}

	public bool HasMesh => _mesh != null;
	
	private (double X, double Y, double Z) Surface(double u, double v)
	{
		var b = Faces[_face];
		double x = b[0] + b[3] * u + b[6] * v;
		double y = b[1] + b[4] * u + b[7] * v;
		double z = b[2] + b[5] * u + b[8] * v;
		double inv = 1.0 / Mathf.Sqrt(x * x + y * y + z * z);
		x *= inv;
		y *= inv;
		z *= inv;

		double r = PlanetGenerator.Radius + PlanetGenerator.Height(new Vector3((float) x, (float) y, (float) z));
		return (x * r, y * r, z * r);
	}
	
	private void Build()
	{
		int n = PlanetGenerator.GridQuads, w = n + 1, m = n + 3;
		double step = _size / n, u0 = -1 + _x * _size, v0 = -1 + _y * _size;
		
		var grid = new Vector3[m * m];
		for (int j = 0; j < m; j++)
		{
			for (int i = 0; i < m; i++)
			{
				var p = Surface(u0 + (i - 1) * step, v0 + (j - 1) * step);
				grid[j * m + i] = new Vector3((float) (p.X - _cx), (float) (p.Y - _cy), (float) (p.Z - _cz));
			}
		}

		var verts = new Vector3[w * w + 4 * w];
		var normals = new Vector3[verts.Length];
		var indices = new int[n * n * 6 + 4 * n * 6];

		float radius = 0;
		for (int j = 0; j < w; j++)
		{
			for (int i = 0; i < w; i++)
			{
				int g = (j + 1) * m + i + 1;
				verts[j * w + i] = grid[g];
				normals[j * w + i] = (grid[g + 1] - grid[g - 1]).Cross(grid[g + m] - grid[g - m]).Normalized();
				radius = Mathf.Max(radius, grid[g].Length());
			}
		}

		int t = 0;
		for (int j = 0; j < n; j++)
		{
			for (int i = 0; i < n; i++)
			{
				int a = j * w + i, b = a + 1, c = a + w, d = c + 1;
				indices[t++] = a;
				indices[t++] = c;
				indices[t++] = b;
				indices[t++] = b;
				indices[t++] = c;
				indices[t++] = d;
			}
		}
		_gridIndexCount = t;
		
		float drop = (float) (_size * SkirtDepth);
		int s = w * w, prev = 0;
		for (int e = 0; e < 4; e++)
		{
			for (int k = 0; k < w; k++)
			{
				int src = e switch { 0 => k, 1 => k * w + n, 2 => n * w + n - k, _ => (n - k) * w };
				verts[s] = verts[src] - (verts[src] + _center).Normalized() * drop;
				normals[s] = normals[src];

				if (k > 0)
				{
					indices[t++] = prev;
					indices[t++] = src;
					indices[t++] = s;
					indices[t++] = prev;
					indices[t++] = s;
					indices[t++] = s - 1;
				}

				prev = src;
				s++;
			}
		}

		_verts = verts;
		_normals = normals;
		_indices = indices;
		_radius = radius;
		_gen.Built.Enqueue(this);
	}
	
	private void BuildShape()
	{
		var faces = new Vector3[_gridIndexCount];
		for (int i = 0; i < faces.Length; i++)
			faces[i] = _verts[_indices[i]] / (float) _size;

		_faces = faces;
		_gen.BuiltShapes.Enqueue(this);
	}

	public void Upload()
	{
		WorkerThreadPool.WaitForTaskCompletion(_task);
		if (_dead) return;

		var arrays = new Array();
		arrays.Resize((int) Mesh.ArrayType.Max);
		arrays[(int) Mesh.ArrayType.Vertex] = _verts;
		arrays[(int) Mesh.ArrayType.Normal] = _normals;
		arrays[(int) Mesh.ArrayType.Index] = _indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		_normals = null;
		if (_level != ColliderDepth)
		{
			_verts = null;
			_indices = null;
		}

		_mesh = new MeshInstance3D { Mesh = mesh, MaterialOverride = Material, Position = _center, Visible = false };
		_gen.AddChild(_mesh);
	}

	public void UploadShape()
	{
		WorkerThreadPool.WaitForTaskCompletion(_task);
		if (_dead) return;

		_collider = new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = _faces }, Position = _center, Scale = Vector3.One * (float) _size };
		_gen.AddChild(_collider);
		_faces = null;
		_colliding = false;
	}
	
	public void Update(Vector3 cam, Vector3 player, bool draw)
	{
		if (_mesh == null) return;

		float dist = Distance(cam);
		if (_kids == null && _level < PlanetGenerator.MaxDepth && dist < _size * SplitRange)
			Split();
		else if (_kids != null && dist > _size * MergeRange)
			Merge();

		bool covered = _kids != null && _kids.All(k => k.HasMesh);
		_mesh.Visible = draw && !covered;

		if (_kids != null)
		{
			foreach (var kid in _kids)
				kid.Update(cam, player, draw && covered);
		}

		if (_level == ColliderDepth)
			UpdateCollider(Distance(player));
	}

	private float Distance(Vector3 point) => Mathf.Max(0, point.DistanceTo(_center) - _radius);

	private void UpdateCollider(float dist)
	{
		if (dist < ColliderRange)
		{
			if (_collider == null && !_colliding)
			{
				_colliding = true;
				_task = WorkerThreadPool.AddTask(Callable.From(BuildShape));
			}
		}
		else if (_collider != null && dist > ColliderRange * 1.25)
		{
			_collider.QueueFree();
			_collider = null;
		}
	}

	private void Split()
	{
		_kids = new QuadNode[4];
		for (int i = 0; i < 4; i++)
			_kids[i] = new QuadNode(_gen, _face, _level + 1, _x * 2 + (i & 1), _y * 2 + (i >> 1));
	}

	private void Merge()
	{
		foreach (var kid in _kids)
			kid.Free();

		_kids = null;
	}

	private void Free()
	{
		_dead = true;
		_mesh?.QueueFree();
		_collider?.QueueFree();

		if (_kids == null) return;
		foreach (var kid in _kids)
			kid.Free();
	}
}
