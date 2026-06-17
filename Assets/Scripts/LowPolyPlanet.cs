using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolyPlanet : MonoBehaviour
{
    public float radius = 50f;
    public int subdivisions = 3;

    public float waterLevel = 0.38f;
    public float sandLevel = 0.45f;
    public float grassLevel = 0.62f;
    public float snowLevel = 0.82f;

    public Color waterColor = new Color(0.1f, 0.3f, 0.6f);
    public Color sandColor = new Color(0.8f, 0.7f, 0.4f);
    public Color grassColor = new Color(0.2f, 0.6f, 0.2f);
    public Color rockColor = new Color(0.4f, 0.35f, 0.3f);
    public Color snowColor = Color.white;

    public float noiseScale = 0.08f;
    public float heightMultiplier = 8f;
    public float humidityScale = 0.05f;
    public float humidityInfluence = 0.08f;
    public float temperatureScale = 0.06f;
    public float temperatureInfluence = 0.12f;
    public float continentalnessScale = 0.04f;
    public float erosionScale = 0.03f;
    public float riverFlowScale = 0.025f;
    public float slopeScale = 0.04f;
    public float volcanicScale = 0.025f;
    public float biomeScale = 0.05f;
    
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    private TerrainType[] terrainTypes;
    
    public enum TerrainType { Water, Sand, Grass, Rock, Snow }
    
    void OnValidate()
    {
        GeneratePlanet();
    }

    void Start()
    {
        GeneratePlanet();
    }
    
    void GeneratePlanet()
    {
        if (mesh == null)
            mesh = new Mesh();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
            meshFilter.sharedMesh = mesh;

        CreateIcosahedron();
        Subdivide(subdivisions);
        ApplyNoiseAndColor();
        RecalculateMesh();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = mesh;
    }
    
    void CreateIcosahedron()
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        
        vertices = new Vector3[]
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0),
            new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t),
            new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1),
            new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = vertices[i].normalized * radius;
        
        triangles = new int[]
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };
    }
    
    void Subdivide(int levels)
    {
        for (int l = 0; l < levels; l++)
        {
            var newTriangles = new System.Collections.Generic.List<int>();
            var newVertices = new System.Collections.Generic.List<Vector3>(vertices);
            var midPointCache = new System.Collections.Generic.Dictionary<long, int>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];
                
                int a = GetMidPoint(v1, v2, newVertices, midPointCache);
                int b = GetMidPoint(v2, v3, newVertices, midPointCache);
                int c = GetMidPoint(v3, v1, newVertices, midPointCache);
                
                newTriangles.Add(v1); newTriangles.Add(a); newTriangles.Add(c);
                newTriangles.Add(v2); newTriangles.Add(b); newTriangles.Add(a);
                newTriangles.Add(v3); newTriangles.Add(c); newTriangles.Add(b);
                newTriangles.Add(a); newTriangles.Add(b); newTriangles.Add(c);
            }
            
            vertices = newVertices.ToArray();
            triangles = newTriangles.ToArray();
        }
    }
    
    int GetMidPoint(int p1, int p2, System.Collections.Generic.List<Vector3> verts, 
        System.Collections.Generic.Dictionary<long, int> cache)
    {
        long key = ((long)Mathf.Min(p1, p2) << 32) + Mathf.Max(p1, p2);
        if (cache.ContainsKey(key)) return cache[key];
        
        Vector3 mid = (verts[p1] + verts[p2]) * 0.5f;
        mid = mid.normalized * radius;
        verts.Add(mid);
        cache[key] = verts.Count - 1;
        return verts.Count - 1;
    }

    float FractalNoise(Vector3 point, float scale, float seed)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = scale;

        for (int octave = 0; octave < 3; octave++)
        {
            float x = point.x * frequency + seed + octave * 13f;
            float y = point.y * frequency + seed * 0.5f + octave * 29f;
            float z = point.z * frequency + seed * 1.5f + octave * 37f;

            float noise = Mathf.PerlinNoise(x + 100f, y + 200f) * 0.5f +
                Mathf.PerlinNoise(y + 300f, z + 400f) * 0.3f +
                Mathf.PerlinNoise(z + 500f, x + 600f) * 0.2f;

            value += noise * amplitude;
            frequency *= 2f;
            amplitude *= 0.5f;
        }

        return Mathf.Clamp01(value);
    }
    
    void ApplyNoiseAndColor()
    {
        colors = new Color[vertices.Length];
        terrainTypes = new TerrainType[vertices.Length];
        
        for (int i = 0; i < vertices.Length; i++)
        {
            float heightNoise = FractalNoise(vertices[i], noiseScale, 0f);
            float humidity = FractalNoise(vertices[i], humidityScale, 999f);
            float temperature = FractalNoise(vertices[i], temperatureScale, 1234f);
            float continentalness = FractalNoise(vertices[i], continentalnessScale, 4321f);
            float erosion = FractalNoise(vertices[i], erosionScale, 5678f);
            float riverFlow = FractalNoise(vertices[i], riverFlowScale, 6789f);
            float slopeNoise = FractalNoise(vertices[i], slopeScale, 7890f);
            float volcanicActivity = FractalNoise(vertices[i], volcanicScale, 8901f);
            float biomeNoise = FractalNoise(vertices[i], biomeScale, 9012f);
            float height = heightNoise * heightMultiplier;
            float normalizedHeight = Mathf.Clamp01(heightNoise);

            vertices[i] = vertices[i].normalized * (radius + height);

            Vector3 normal = vertices[i].normalized;
            float slope = Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(normal, Vector3.up)) + slopeNoise * 0.18f);

            float oceanThreshold = waterLevel + humidity * 0.04f - continentalness * 0.06f;
            float sandThreshold = sandLevel + (1f - humidity) * 0.02f + erosion * 0.02f;
            float grassThreshold = grassLevel - humidity * 0.03f + continentalness * 0.03f;
            float coldThreshold = snowLevel - temperatureInfluence * (1f - temperature) - slope * 0.04f;
            float riverMask = (riverFlow > 0.72f && normalizedHeight < grassThreshold && normalizedHeight > waterLevel + 0.01f) ? 1f : 0f;

            TerrainType type;
            Color color;
            
            if (normalizedHeight < oceanThreshold || (riverMask > 0f && normalizedHeight < grassThreshold - 0.02f))
            {
                type = TerrainType.Water;
                color = Color.Lerp(waterColor, new Color(0.15f, 0.45f, 0.75f), humidity);
                color = Color.Lerp(color, new Color(0.1f, 0.55f, 0.8f), temperature * 0.15f);
            }
            else if (normalizedHeight < sandThreshold || (erosion > 0.78f && slope < 0.22f))
            {
                type = TerrainType.Sand;
                color = Color.Lerp(sandColor, rockColor, 0.2f + humidity * 0.1f + erosion * 0.15f);
                color = Color.Lerp(color, new Color(0.9f, 0.8f, 0.55f), temperature * 0.2f);
            }
            else if (normalizedHeight < grassThreshold || (biomeNoise > 0.58f && normalizedHeight < grassThreshold + 0.08f))
            {
                type = TerrainType.Grass;
                color = Color.Lerp(grassColor, new Color(0.15f, 0.5f, 0.2f), humidity);
                color = Color.Lerp(color, new Color(0.6f, 0.8f, 0.3f), temperature * 0.25f + biomeNoise * 0.1f);
            }
            else if (normalizedHeight < coldThreshold || (volcanicActivity > 0.78f && normalizedHeight > grassThreshold))
            {
                type = TerrainType.Rock;
                color = Color.Lerp(rockColor, snowColor, (normalizedHeight - grassThreshold) / (coldThreshold - grassThreshold));
                color = Color.Lerp(color, new Color(0.7f, 0.7f, 0.7f), temperature * 0.2f);
                if (volcanicActivity > 0.78f && normalizedHeight > grassThreshold)
                {
                    color = Color.Lerp(color, new Color(0.6f, 0.15f, 0.1f), volcanicActivity * 0.5f);
                }
            }
            else
            {
                type = TerrainType.Snow;
                color = Color.Lerp(snowColor, new Color(0.8f, 0.9f, 1f), temperature * 0.1f + biomeNoise * 0.05f);
            }
            
            terrainTypes[i] = type;
            colors[i] = color;
        }
    }
    
    void RecalculateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        OrbitalCameraController orbital = Camera.main.transform.parent.GetComponent<OrbitalCameraController>();
        if (orbital == null || !orbital.enabled) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
        {
            TerrainType terrain = GetTerrainAtPoint(hit.point);
            bool canBuild = CanBuildAt(hit.point);

            Debug.Log(
                $"[LowPolyPlanet] Clicked at {hit.point}\n" +
                $"  Terrain: {terrain}\n" +
                $"  CanBuild: {canBuild}"
            );
        }
    }
    
    public TerrainType GetTerrainAtPoint(Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        int closest = 0;
        float minDist = float.MaxValue;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            float d = Vector3.Distance(localPoint.normalized * radius, vertices[i]);
            if (d < minDist)
            {
                minDist = d;
                closest = i;
            }
        }
        
        return terrainTypes[closest];
    }
    
    public bool CanBuildAt(Vector3 worldPoint)
    {
        TerrainType type = GetTerrainAtPoint(worldPoint);
        return type == TerrainType.Grass || type == TerrainType.Sand || type == TerrainType.Rock;
    }
    
    public bool IsWater(Vector3 worldPoint)
    {
        return GetTerrainAtPoint(worldPoint) == TerrainType.Water;
    }
}