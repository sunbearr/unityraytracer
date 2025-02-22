using System.Collections.Generic;
using UnityEngine;
public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public Light DirectionalLight;

    public float speed = 0.1f;

    [Header("Spheres")]
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;
    public int SphereSeed = 1235446;

    private Camera _camera;
    private float _lastFieldOfView;
    private RenderTexture _target;
     private RenderTexture _converged;
    private Material _addMaterial;
    private uint _currentSample = 0;
    private ComputeBuffer _sphereBuffer;
    private List<Transform> _transformsToWatch = new List<Transform>();
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        
        public Vector3 specular;
        

        public float smoothness;
        public Vector3 emission;
    }
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _transformsToWatch.Add(transform);
        _transformsToWatch.Add(DirectionalLight.transform);
    }
    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }
    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }
    private void Update()
    {
        if (_camera.fieldOfView != _lastFieldOfView)
        {
            _currentSample = 0;
            _lastFieldOfView = _camera.fieldOfView;
        }
        foreach (Transform t in _transformsToWatch)
        {
            if (t.hasChanged)
            {
                _currentSample = 0;
                t.hasChanged = false;
            }
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
    private void SetUpScene()
    {

        List<Sphere> spheres = new List<Sphere>();
        // Add a number of random spheres
        for (int i = 0; i < 4; i++)
        {
            Sphere sphere = new Sphere();
            // Radius and radius
            sphere.radius = SphereRadius.x + i * (SphereRadius.y - SphereRadius.x);
            sphere.radius = 1 + i*2;
            sphere.position = new Vector3(0, sphere.radius, i*9);
            // Reject spheres that are intersecting others
            
            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = i % 2 !=0;
            sphere.albedo = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector4(0.8f, 0.8f, 0.8f) : new Vector4(0.04f, 0.04f, 0.04f);

            if (i==0) {
                sphere.position = new Vector3(3.0f, 1.0f, -3.0f);
                sphere.albedo = new Vector4(0.8f, 0.8f, 0.8f);
                sphere.specular = new Vector4(0.04f, 0.04f, 0.04f);
                sphere.smoothness = 0.0f;
            }

            if (i==1) {
                sphere.emission = new Vector3(10.0f, 10.0f, 10.0f);
                sphere.position = new Vector3(3.0f, 5.0f, 0.0f);
                sphere.radius = 0.5f;
            }

            if (i==2) {
                sphere.position = new Vector3(3.0f, 1.0f, 0.0f);
                sphere.albedo = Vector4.zero;
                sphere.specular = new Vector4(0.8f, 0.8f, 0.8f);
                sphere.smoothness = 0.5f;
                sphere.radius = 1.0f;
            }

            if (i==3) {
                sphere.position = new Vector3(3.0f, 1.0f, 3.0f);
                sphere.albedo = Vector4.zero;
                sphere.specular = new Vector4(0.8f, 0.8f, 0.8f);
                sphere.smoothness = 1.0f;
                sphere.radius = 1.0f;
            }

            // Add the sphere to the list
            spheres.Add(sphere);
            
            continue;
        }

        // Assign to compute buffer
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 56);
           // _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
            _sphereBuffer.SetData(spheres);
        }
    }
    private void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        if (_sphereBuffer != null)
            RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
        RayTracingShader.SetFloat("_Seed", Random.value);
    }
        private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
            {
                _target.Release();
                _converged.Release();
            }

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
            _converged = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _converged.enableRandomWrite = true;
            _converged.Create();

            // Reset sampling
            _currentSample = 0;
        }
    }
    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();
        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AccumulateShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, _converged, _addMaterial);
        Graphics.Blit(_converged, destination);
        _currentSample++;
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }
}