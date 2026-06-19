using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Outward offset distance along the face normal to prevent Z-fighting (clipping) with the planet surface")]
    public float surfaceOffset = 0.25f;
    public Color canBuildColor = Color.green;
    public Color cannotBuildColor = Color.red;
    [Tooltip("The width of the highlighted triangle outline")]
    public float lineWidth = 0.25f;

    private LineRenderer lineRenderer;
    private OrbitalCameraController orbitalCamera;
    private bool buildModeActive = false;

    void Start()
    {
        // Get or add a LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Configure LineRenderer to draw a neat flat unlit overlay
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        
        // Find default sprites material so it renders in a solid flat color
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            lineRenderer.material = new Material(spriteShader);
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        }
        
        lineRenderer.positionCount = 0;

        // Cache the camera controller reference
        orbitalCamera = FindObjectOfType<OrbitalCameraController>();
    }

    void Update()
    {
        // Stop build system completely if the game is paused
        if (Time.timeScale == 0f)
        {
            ClearHighlight();
            return;
        }

        // 1. Toggle Build Mode with 'B' key (only allowed when in orbital camera mode)
        if (orbitalCamera != null && orbitalCamera.enabled)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                buildModeActive = !buildModeActive;
                if (!buildModeActive)
                {
                    ClearHighlight();
                }
                Debug.Log($"[BuildSystem] Build mode: {(buildModeActive ? "ENABLED" : "DISABLED")}");
            }
        }
        else
        {
            // If camera is no longer in orbital mode, automatically force build mode off
            if (buildModeActive)
            {
                buildModeActive = false;
                ClearHighlight();
                Debug.Log("[BuildSystem] Left orbital camera mode. Build mode DISABLED.");
            }
        }

        // 2. Perform build mode raycasting and highlighting
        if (buildModeActive)
        {
            PerformBuildRaycast();
        }
    }

    private struct CentroidAngle
    {
        public Vector3 worldPosition;
        public Vector3 localPosition;
        public float angle;
    }

    private void PerformBuildRaycast()
    {
        if (Camera.main == null)
        {
            ClearHighlight();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            LowPolyPlanet planet = hit.collider.GetComponent<LowPolyPlanet>();
            if (planet != null)
            {
                HighlightHexagon(planet, hit);
                return;
            }
        }

        ClearHighlight();
    }

    private void HighlightHexagon(LowPolyPlanet planet, RaycastHit hit)
    {
        Vector3[] planetVertices = planet.Vertices;
        int[] planetTriangles = planet.Triangles;

        if (planetVertices == null || planetTriangles == null || hit.triangleIndex < 0 || (hit.triangleIndex * 3 + 2) >= planetTriangles.Length)
        {
            ClearHighlight();
            return;
        }

        // 1. Find the vertex of the hit triangle closest to the hit point
        int i0 = planetTriangles[hit.triangleIndex * 3];
        int i1 = planetTriangles[hit.triangleIndex * 3 + 1];
        int i2 = planetTriangles[hit.triangleIndex * 3 + 2];

        Vector3 localHitPoint = planet.transform.InverseTransformPoint(hit.point);

        int closestVertexIndex = i0;
        float minDistSq = Vector3.SqrMagnitude(planetVertices[i0] - localHitPoint);

        float d1Sq = Vector3.SqrMagnitude(planetVertices[i1] - localHitPoint);
        if (d1Sq < minDistSq)
        {
            minDistSq = d1Sq;
            closestVertexIndex = i1;
        }

        float d2Sq = Vector3.SqrMagnitude(planetVertices[i2] - localHitPoint);
        if (d2Sq < minDistSq)
        {
            minDistSq = d2Sq;
            closestVertexIndex = i2;
        }

        // 2. Find all triangles sharing this vertex and calculate their centroids
        System.Collections.Generic.List<CentroidAngle> centroids = new System.Collections.Generic.List<CentroidAngle>();
        Vector3 cellCenterLocal = planetVertices[closestVertexIndex];
        Vector3 normal = cellCenterLocal.normalized;

        // Tangents for 2D angle calculation on the plane tangent to the sphere at this vertex
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = Vector3.Cross(normal, Vector3.right);
        }
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        for (int t = 0; t < planetTriangles.Length; t += 3)
        {
            int v0 = planetTriangles[t];
            int v1 = planetTriangles[t + 1];
            int v2 = planetTriangles[t + 2];

            if (v0 == closestVertexIndex || v1 == closestVertexIndex || v2 == closestVertexIndex)
            {
                // Calculate centroid in local coordinates
                Vector3 centroidLocal = (planetVertices[v0] + planetVertices[v1] + planetVertices[v2]) / 3f;
                Vector3 centroidWorld = planet.transform.TransformPoint(centroidLocal);

                // Project relative vector to tangent plane
                Vector3 rel = centroidLocal - cellCenterLocal;
                float x = Vector3.Dot(rel, tangent);
                float y = Vector3.Dot(rel, bitangent);
                float angle = Mathf.Atan2(y, x);

                centroids.Add(new CentroidAngle
                {
                    worldPosition = centroidWorld,
                    localPosition = centroidLocal,
                    angle = angle
                });
            }
        }

        if (centroids.Count < 3)
        {
            ClearHighlight();
            return;
        }

        // 3. Sort centroids around the vertex by their angle to form a neat closed ring
        centroids.Sort((a, b) => a.angle.CompareTo(b.angle));

        // 4. Draw the outline with LineRenderer (adding a loop point at the end)
        int pointCount = centroids.Count;
        lineRenderer.positionCount = pointCount + 1;

        for (int i = 0; i < pointCount; i++)
        {
            // Calculate a slight offset along the local surface normal at each centroid to prevent Z-fighting
            Vector3 centroidNormal = centroids[i].localPosition.normalized;
            Vector3 worldNormal = planet.transform.TransformDirection(centroidNormal);
            Vector3 offset = worldNormal * surfaceOffset;

            lineRenderer.SetPosition(i, centroids[i].worldPosition + offset);
        }

        // Set the loop closing position
        Vector3 loopNormal = centroids[0].localPosition.normalized;
        Vector3 loopWorldNormal = planet.transform.TransformDirection(loopNormal);
        Vector3 loopOffset = loopWorldNormal * surfaceOffset;
        lineRenderer.SetPosition(pointCount, centroids[0].worldPosition + loopOffset);

        // 5. Update build status color based on closest vertex's terrain type
        bool canBuild = false;
        if (planet.TerrainTypes != null && closestVertexIndex < planet.TerrainTypes.Length)
        {
            LowPolyPlanet.TerrainType type = planet.TerrainTypes[closestVertexIndex];
            canBuild = type == LowPolyPlanet.TerrainType.Grass ||
                       type == LowPolyPlanet.TerrainType.Sand ||
                       type == LowPolyPlanet.TerrainType.Rock;
        }

        Color targetColor = canBuild ? canBuildColor : cannotBuildColor;
        lineRenderer.startColor = targetColor;
        lineRenderer.endColor = targetColor;
        
        // Update width in case it is changed dynamically in inspector
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    private void ClearHighlight()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
}
