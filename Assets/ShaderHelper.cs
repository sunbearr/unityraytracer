using UnityEngine;

public static class ShaderHelper
{
    	public static void InitMaterial(Shader shader, ref Material mat)
	{
		if (mat == null || (mat.shader != shader && shader != null))
		{
			if (shader == null)
			{
				shader = Shader.Find("Unlit/Texture");
			}

			mat = new Material(shader);
		}
	}

}