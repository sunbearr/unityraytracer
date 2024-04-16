using UnityEngine;

[System.Serializable]
public struct RayTracingMaterial
{
	public enum MaterialFlag
	{
		None,
		CheckerPattern,
		InvisibleLight
	}

	public Color colour;


	public void SetDefaultValues()
	{
		colour = Color.white;
    }
}