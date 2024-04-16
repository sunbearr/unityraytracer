using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingMaster2 : MonoBehaviour
{
    [SerializeField] bool useShaderInSceneView;
    public Shader RayTracingShader2;
    Material rayTracingMaterial;

    // Buffers
	ComputeBuffer sphereBuffer;

    private void OnRenderImage(RenderTexture source, RenderTexture target)
    {
        if (Camera.current.name != "SceneCamera" || useShaderInSceneView) {
            ShaderHelper.InitMaterial(RayTracingShader2, ref rayTracingMaterial);
            UpdateCameraParams(Camera.current);
            CreateSpheres();
            Graphics.Blit(null, target, rayTracingMaterial);
        }
        else {
            Graphics.Blit(source, target);
        }
    }

    void UpdateCameraParams(Camera cam) 
    { 
        float planeHeight = cam.nearClipPlane * Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
    }

    void CreateSpheres()
    {
        ShaderHelper.Release(sphereBuffer);

        // create sphere object lists from the spheres placed in the scene view.
        RayTracedSphere[] sphereObjects = FindObjectsOfType<RayTracedSphere>();
        Sphere[] spheres = new Sphere[sphereObjects.Length];

        for (int i = 0; i < sphereObjects.Length; i++) {

            // for each scene view sphere, populate with sphere data
            spheres[i] = new Sphere()
            {
                position = sphereObjects[i].transform.position,
                radius = sphereObjects[i].transform.localScale.x * 0.5f,
                material = sphereObjects[i].material
            };
        }

        // Create buffer for sphere data and send to shader
        ShaderHelper.CreateStructuredBuffer(ref sphereBuffer, spheres);
        rayTracingMaterial.SetBuffer("Spheres", sphereBuffer);
        rayTracingMaterial.SetInt("NumSpheres", sphereObjects.Length);

    }

    void OnDisable()
	{
		ShaderHelper.Release(sphereBuffer);
    }
}