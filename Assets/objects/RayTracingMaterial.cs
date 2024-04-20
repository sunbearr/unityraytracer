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
	public Color emissionColour;

	public float emissionStrength;
	public void SetDefaultValues()
	{
		colour = Color.white;
		emissionColour = Color.white;
		emissionStrength = 0;
    }
}