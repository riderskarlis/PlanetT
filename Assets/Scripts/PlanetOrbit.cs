using UnityEngine;

public class PlanetOrbit : MonoBehaviour
{
    public Transform sun;
    public float speed;
    public float spinSpeed;
    public float minSpeed = 5f;
    public float maxSpeed = 25f;
    public float minSpinSpeed = 10f;
    public float maxSpinSpeed = 60f;

    [Header("Orbit Rendering")]
    public bool renderOrbit = true;
    public int segments = 120;
    public float lineWidth = 0.15f;
    public Color orbitColor = new Color(1f, 1f, 1f, 0.25f);
    [Tooltip("The camera Y height at or above which the orbit line is fully visible")]
    public float fadeReferenceHeight = 120f;

    private LineRenderer lineRenderer;

    void Start()
    {
        speed = Random.Range(minSpeed, maxSpeed);
        spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);

        if (renderOrbit)
        {
            DrawOrbitLine();
        }
    }

    void Update()
    {
        // Orbit around sun
        Vector3 sunPos = sun != null ? sun.position : Vector3.zero;
        transform.RotateAround(sunPos, Vector3.up, speed * Time.deltaTime);
        
        // Spin on self
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

        // Fade out orbit lines when camera gets close to y:0
        if (renderOrbit && lineRenderer != null)
        {
            float camY = fadeReferenceHeight;
            if (Camera.main != null)
            {
                camY = Mathf.Abs(Camera.main.transform.position.y);
            }
            
            float alphaScale = Mathf.Clamp01(camY / fadeReferenceHeight);
            Color currentOrbitColor = orbitColor;
            currentOrbitColor.a = orbitColor.a * alphaScale;
            lineRenderer.startColor = currentOrbitColor;
            lineRenderer.endColor = currentOrbitColor;
        }
    }

    private void DrawOrbitLine()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;

        // Visual setup for a thin, clean line
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = orbitColor;
        lineRenderer.endColor = orbitColor;

        // Shadow & lighting setup
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        float radius = Vector3.Distance(transform.position, sun != null ? sun.position : Vector3.zero);
        Vector3 center = sun != null ? sun.position : Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(center.x + x, center.y, center.z + z));
        }
    }
}