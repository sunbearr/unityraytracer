using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;

    private RenderTexture _target;

    public Texture SkyboxTexture;
    
    public int MaxBounces = 8;

    private Camera _camera;

    // progressive sampling vars
    private uint _currentSample = 0;
    private Material _addMaterial;

// camera speed
     public float speed = 0.1f;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        RayTracingShader.SetFloat("Resolution", _target.width);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        if (_addMaterial == null) {
            _addMaterial = new Material(Shader.Find("Hidden/AccumulateShader"));
        }

        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample++;
 
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RayTracingShader.SetInt("MaxBounces", MaxBounces);
    }
    
    // control camera
    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            speed *= 2;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed /= 2;
        }

        //movement
        if (Input.GetKey("w"))
        {
            _camera.transform.position += _camera.transform.forward * speed * 0.1f;
        }

        if (Input.GetKey("s"))
        {
            _camera.transform.position -= _camera.transform.forward * speed * 0.1f;
        }

        if (Input.GetKey("a"))
        {
            _camera.transform.position -= _camera.transform.right * speed * 0.1f;
        }

        if (Input.GetKey("d"))
        {
            _camera.transform.position += _camera.transform.right * speed * 0.1f;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            _camera.transform.position += Vector3.up * speed * 0.1f;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            _camera.transform.position += Vector3.down * speed * 0.1f;
        }

        //Rotation
        if (Input.GetKey("k"))
        {
            _camera.transform.eulerAngles += Vector3.right * speed;
        }

        if (Input.GetKey("i"))
        {
            _camera.transform.eulerAngles += Vector3.left * speed;
        }

        if (Input.GetKey("l"))
        {
            _camera.transform.eulerAngles += Vector3.up * speed;
        }

        if (Input.GetKey("j"))
        {
            _camera.transform.eulerAngles += Vector3.down * speed;
        }
    }
}