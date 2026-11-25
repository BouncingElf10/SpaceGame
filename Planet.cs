using Godot;
using System;

public partial class Planet : Node3D
{
	public const float GravityConstant = 3f;
	
	public override void _Ready()
	{
		
	}
	
	public override void _Process(double delta)
	{
		
	}

	public const float AtmosphereHeight = 0.3f;
	public float Radius => Scale.X * 0.5f;

	public bool IsInAtmosphere(Vector3 playerPos)
	{
		return GetAtmosphereEffect(playerPos) > 0;
	}
	
	public float GetAltitude(Vector3 playerPos)
	{
		return Position.DistanceTo(playerPos) - Radius;
	}
	
	public float GetAtmosphereEffect(Vector3 playerPos)
	{
		float altitude = GetAltitude(playerPos);
		return Math.Clamp(1 - altitude / (Radius * AtmosphereHeight), 0, 1);
	}

	public float GetGravityEffect(Vector3 playerPos)
	{
		float radius = Scale.X * ((Area3D) GetNode("Gravity Area")).Scale.X;
		float distance = Position.DistanceTo(playerPos);
		return Math.Clamp(1 - distance / radius, 0, 1);
	}

	public Vector3 GetGravityVector(Vector3 playerPos)
	{
		return -GetUp(playerPos) * GetGravityEffect(playerPos) * GravityConstant;
	}

	public Vector3 GetUp(Vector3 playerPos)
	{
		return -(Position - playerPos).Normalized();
	}
	
	public Vector3 GetRight(Vector3 playerPos)
	{
		return Vector3.Forward.Cross(GetUp(playerPos)).Normalized();
	}

	public Vector3 GetBack(Vector3 playerPos)
	{
		return -GetUp(playerPos).Cross(GetRight(playerPos)).Normalized();
	}
}
