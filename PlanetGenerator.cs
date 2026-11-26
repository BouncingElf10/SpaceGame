using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlanetGenerator : StaticBody3D
{	
	private const float Radius = 1.0f;
	private const int Subdivisions = 8;
	private const float Bumpiness = 0.08f;
	private const int ChunkLevel = 2;

	private readonly FastNoiseLite _noise = new FastNoiseLite() { Frequency = 10.5f, FractalType = FastNoiseLite.FractalTypeEnum.None };

	public override void _Ready()
	{
		var start = Time.GetUnixTimeFromSystem();
		
		var chunks = GenerateIcosphere(Radius);
		GetParent<Planet>().SetRadius(Radius);

		foreach (var (id, st) in chunks)
		{
			st.GenerateNormals();
			var mesh = st.Commit();
			
			var chunk = new Chunk { Name = "Chunk" + id, Mesh = mesh };
			var collisions = new CollisionShape3D { Name = "Collision" + id, Shape = mesh.CreateTrimeshShape() };
			
			chunk.SetId(id);
			chunk.SetCollision(collisions);
			
			AddChild(chunk);
			AddChild(collisions);
		}
		
		var timeTook = Time.GetUnixTimeFromSystem() - start;
		GD.Print("Took " + timeTook + "s");
	}
	
	private float Height(Vector3 dir)
	{
		return _noise.GetNoise3Dv(dir) * Bumpiness;
	}

	private Dictionary<int, SurfaceTool> GenerateIcosphere(float radius)
	{
		float t = (1f + Mathf.Sqrt(5f)) / 2f;

		var v = new List<Vector3>
		{
			new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
			new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
			new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
		};

		int[] f =
		{
			0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
			1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
			3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
			4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
		};

		for (int i = 0; i < v.Count; i++)
			v[i] = v[i].Normalized();

		var faces = f.ToList();
		var faceChunk = new List<int>(new int[faces.Count / 3]);
		int chunkCount = 1;

		for (int s = 0; s < Subdivisions; s++)
		{
			var next = new List<int>();
			var nextChunk = new List<int>();
			var cache = new Dictionary<(int, int), int>();

			int Mid(int a, int b)
			{
				var key = (Math.Min(a, b), Math.Max(a, b));
				if (cache.TryGetValue(key, out int index)) return index;

				index = v.Count;
				v.Add((v[a] + v[b]).Normalized());
				cache[key] = index;
				return index;
			}

			for (int i = 0; i < faces.Count; i += 3)
			{
				int a = faces[i], b = faces[i + 1], c = faces[i + 2];
				int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);

				next.AddRange(new[] {
					a, ab, ca, b, bc, ab,
					c, ca, bc, ab, bc, ca
				});

				for (int k = 0; k < 4; k++)
				{
					nextChunk.Add(s == ChunkLevel - 1 ? chunkCount++ : faceChunk[i / 3]);
				}
			}

			faces = next;
			faceChunk = nextChunk;
		}

		Vector3 Lump(Vector3 p) => p * (radius + Height(p));

		var chunks = new Dictionary<int, SurfaceTool>();

		for (int i = 0; i < faces.Count; i += 3)
		{
			if (!chunks.TryGetValue(faceChunk[i / 3], out var st))
			{
				st = new SurfaceTool();
				st.Begin(Mesh.PrimitiveType.Triangles);
				chunks[faceChunk[i / 3]] = st;
			}

			st.AddVertex(Lump(v[faces[i]]));
			st.AddVertex(Lump(v[faces[i + 2]]));
			st.AddVertex(Lump(v[faces[i + 1]]));
		}

		return chunks;
	}
}
