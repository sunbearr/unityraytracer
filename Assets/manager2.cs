using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingMaster2 : MonoBehaviour
{
    [SerializeField] bool useShaderInSceneView;
    public Shader RayTracingShader2;
    Material rayTracingMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture target)
    {
        if (Camera.current.name != "SceneCamera" || useShaderInSceneView) {
            ShaderHelper.InitMaterial(RayTracingShader2, ref rayTracingMaterial);
            UpdateCameraParams(Camera.current);
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
}