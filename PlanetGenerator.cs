using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlanetGenerator : StaticBody3D
{	
	private const float Radius = 1.0f;
	private const int Subdivisions = 8;

	public override void _Ready()
	{
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		GenerateIcosphere(st, Radius);
		GetParent<Planet>().SetRadius(Radius);

		st.GenerateNormals();
		GetNode<MeshInstance3D>("MeshInstance3D").Mesh = st.Commit();
		
		GetNode<CollisionShape3D>("CollisionShape3D").Scale = new Vector3(Radius * 2, Radius * 2, Radius * 2);
	}

	private static void GenerateIcosphere(SurfaceTool st, float radius)
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

		for (int s = 0; s < Subdivisions; s++)
		{
			var next = new List<int>();
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
			}

			faces = next;
		}

		for (int i = 0; i < faces.Count; i += 3)
		{
			st.AddVertex(v[faces[i]] * radius);
			st.AddVertex(v[faces[i + 2]] * radius);
			st.AddVertex(v[faces[i + 1]] * radius);
		}
	}
}
