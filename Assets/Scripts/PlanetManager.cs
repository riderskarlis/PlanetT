using System.Collections.Generic;
using UnityEngine;

public class PlanetManager : MonoBehaviour
{
    [Header("Solar System Setup")]
    public Transform sun;
    public GameObject planetPrefab;
    public int planetCount = 5;

    [Header("Orbit Spacing Settings")]
    public float initialOrbitDistance = 150f;
    public float minSpacing = 100f;
    public float maxSpacing = 200f;

    [Header("Planetary Material (Standard Vertex Color Shader)")]
    public Material planetMaterial;

    [Header("Resource Prefabs")]
    public GameObject clayPrefab;
    public GameObject coalPrefab;
    public GameObject ironPrefab;

    private static readonly string[] PlanetNames = {
        "Aurelia", "Xylos", "Zion", "Pandora", "Zephyr", "Vectera", "Kryx", "Akila", 
        "Solaria", "Triton", "Osiris", "Arrakis", "Caladan", "Naboo", "Tatooine", "Hoth", "Endor",
        "Chronos", "Theron", "Aether", "Volcanus", "Geras", "Nova", "Helios"
    };

    private static readonly string[] PlanetDescriptions = {
        "A lush, green world abundant with exotic flora and rich mineral resources.",
        "A scorched, volcanic desert locked in a close orbit with its host star.",
        "An icy, frozen wasteland enveloped in thick nitrogen glaciers and snow storms.",
        "An alien ocean world with vast violet oceans and bio-luminescent coral reefs.",
        "A barren, dust-swept planet rich in radioactive heavy metals and iron ore.",
        "A temperate paradise with majestic mountain ranges, rolling grasslands, and deep blue seas.",
        "A radioactive rock-world characterized by constant lightning storms and volcanic activity.",
        "A swampy, high-humidity sphere covered in prehistoric fern forests and sulfur bogs.",
        "A golden sand-world with massive dunes, dried riverbeds, and ancient ruins.",
        "A dense, metallic planet covered in crystalline structures and high-value silicon veins."
    };

    void Start()
    {
        // Initialize random seed from game setup screen
        int seed = GameSetupData.GetParsedSeed();
        Random.InitState(seed);

        // Get planet count from setup
        planetCount = GameSetupData.planetCount;

        // Find the Sun in the scene if not set
        if (sun == null)
        {
            GameObject sunGo = GameObject.Find("Sun");
            if (sunGo != null)
            {
                sun = sunGo.transform;
            }
            else
            {
                // Fallback: search for any light or object at origin
                GameObject originObj = new GameObject("Sun_Fallback");
                originObj.transform.position = Vector3.zero;
                sun = originObj.transform;
            }
        }

        SpawnPlanets();
    }

    public void SpawnPlanets()
    {
        float currentOrbitDistance = initialOrbitDistance;

        for (int i = 0; i < planetCount; i++)
        {
            // Calculate orbital ring distance
            currentOrbitDistance += Random.Range(minSpacing, maxSpacing);

            // Calculate random starting angle
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnPos = new Vector3(
                Mathf.Cos(angle) * currentOrbitDistance,
                0f,
                Mathf.Sin(angle) * currentOrbitDistance
            );

            // Instantiate/Create Planet
            GameObject planetObj;
            if (planetPrefab != null)
            {
                planetObj = Instantiate(planetPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                planetObj = new GameObject("ProceduralPlanet");
                planetObj.transform.position = spawnPos;
            }

            // Ensure planet has the LowPolyPlanet component
            LowPolyPlanet planet = planetObj.GetComponent<LowPolyPlanet>();
            if (planet == null)
            {
                planet = planetObj.AddComponent<LowPolyPlanet>();
            }

            // Set Material if specified (useful if creating dynamically)
            MeshRenderer renderer = planetObj.GetComponent<MeshRenderer>();
            if (renderer != null && planetMaterial != null)
            {
                renderer.sharedMaterial = planetMaterial;
            }
            else if (renderer != null && renderer.sharedMaterial == null)
            {
                // Fallback to custom shader that supports vertex colors and GPU eclipse shadows
                renderer.sharedMaterial = new Material(Shader.Find("Custom/LowPolyPlanet"));
            }

            // Randomize planet properties
            RandomizePlanet(planet);

            // Configure planet orbit
            PlanetOrbit orbit = planetObj.GetComponent<PlanetOrbit>();
            if (orbit == null)
            {
                orbit = planetObj.AddComponent<PlanetOrbit>();
            }
            orbit.sun = sun;

            planetObj.name = planet.planetName;

            // Generate the mesh and collider now so raycasting works
            planet.GeneratePlanet();

            // Spawn Clay, Coal, and Iron resources
            SpawnResourcesOnPlanet(planetObj, planet);
        }
    }

    private void SpawnResourcesOnPlanet(GameObject planetObj, LowPolyPlanet planet)
    {
        // Spawn 1 to 7 nodes of each resource type on the planet surface
        SpawnResourcePrefab(planetObj, planet, clayPrefab, "Clay", Random.Range(1, 8));
        SpawnResourcePrefab(planetObj, planet, coalPrefab, "Coal", Random.Range(1, 8));
        SpawnResourcePrefab(planetObj, planet, ironPrefab, "Iron", Random.Range(1, 8));
    }

    private void SpawnResourcePrefab(GameObject planetObj, LowPolyPlanet planet, GameObject prefab, string resourceName, int count)
    {
        if (prefab == null) return;

        int spawnedCount = 0;
        int attempts = 0;
        
        // Loop up to 150 times to give a bit more breathing room on mostly-water planets
        while (spawnedCount < count && attempts < 150)
        {
            attempts++;
            Vector3 randomDir = Random.onUnitSphere;
            float maxRayDist = planet.radius * 4f;
            Vector3 rayOrigin = planetObj.transform.position + randomDir * (planet.radius * 3f);
            Ray ray = new Ray(rayOrigin, -randomDir);
            
            // Ignore trigger colliders (like atmosphere triggers or selection rings) when raycasting
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // Retrieve target planet component in parent (handles child mesh colliders if present)
                LowPolyPlanet hitPlanet = hit.collider.GetComponent<LowPolyPlanet>();
                if (hitPlanet == null)
                {
                    hitPlanet = hit.collider.GetComponentInParent<LowPolyPlanet>();
                }

                if (hitPlanet == planet)
                {
                    // Ensure the node spawns on land (not in the ocean)
                    if (!planet.IsWater(hit.point))
                    {
                        GameObject resource = Instantiate(prefab, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                        
                        // Attach as child so it moves with the planet
                        resource.transform.SetParent(planetObj.transform);
                        resource.name = resourceName;
                        
                        spawnedCount++;
                    }
                }
            }
        }
    }

    private void RandomizePlanet(LowPolyPlanet planet)
    {
        // 1. Name & Description
        string randName = PlanetNames[Random.Range(0, PlanetNames.Length)];
        // Add a suffix if there are duplicates to make it feel unique
        if (Random.value > 0.5f)
        {
            string[] suffixes = { "Prime", "II", "III", "IV", "V", "B", "C", "X", "Delta", "Alpha" };
            randName += " " + suffixes[Random.Range(0, suffixes.Length)];
        }
        planet.planetName = randName;
        planet.planetDescription = PlanetDescriptions[Random.Range(0, PlanetDescriptions.Length)];

        // 2. Geometry
        planet.radius = Random.Range(20f, 60f);
        planet.subdivisions = 4; // Always 4 subdivisions
        planet.heightMultiplier = Random.Range(5f, 20f);

        // 3. Climate Influences
        planet.humidityInfluence = Random.Range(0.1f, 0.25f);
        planet.temperatureInfluence = Random.Range(0.1f, 0.25f);

        // 4. Biome Height Levels
        planet.waterLevel = Random.Range(0.2f, 0.45f);
        planet.sandLevel = planet.waterLevel + Random.Range(0.02f, 0.08f);
        planet.grassLevel = planet.sandLevel + Random.Range(0.1f, 0.25f);
        planet.snowLevel = planet.grassLevel + Random.Range(0.1f, 0.2f);

        // 5. Biome Colors
        planet.waterColor = GenerateRandomWaterColor();
        planet.sandColor = GenerateRandomSandColor();
        planet.grassColor = GenerateRandomGrassColor();
        planet.rockColor = GenerateRandomRockColor();
        planet.snowColor = Color.white; // Snow is typically white
    }

    private Color GenerateRandomWaterColor()
    {
        // Beautiful water shades: deep blue, teal, aqua, purple/alien
        int type = Random.Range(0, 4);
        switch (type)
        {
            case 0: return new Color(Random.Range(0.05f, 0.15f), Random.Range(0.2f, 0.4f), Random.Range(0.6f, 0.85f)); // Classic Blue
            case 1: return new Color(Random.Range(0.05f, 0.15f), Random.Range(0.5f, 0.75f), Random.Range(0.5f, 0.7f)); // Teal/Greenish
            case 2: return new Color(Random.Range(0.3f, 0.45f), Random.Range(0.1f, 0.25f), Random.Range(0.6f, 0.8f)); // Violet/Alien
            default: return new Color(Random.Range(0.05f, 0.12f), Random.Range(0.1f, 0.25f), Random.Range(0.45f, 0.65f)); // Dark ocean
        }
    }

    private Color GenerateRandomSandColor()
    {
        // Sand shades: golden yellow, beige, reddish desert, greyish
        int type = Random.Range(0, 3);
        switch (type)
        {
            case 0: return new Color(Random.Range(0.75f, 0.88f), Random.Range(0.65f, 0.78f), Random.Range(0.35f, 0.48f)); // Golden
            case 1: return new Color(Random.Range(0.8f, 0.9f), Random.Range(0.55f, 0.65f), Random.Range(0.4f, 0.5f)); // Red/Orange
            default: return new Color(Random.Range(0.68f, 0.78f), Random.Range(0.63f, 0.73f), Random.Range(0.53f, 0.63f)); // Beige
        }
    }

    private Color GenerateRandomGrassColor()
    {
        // Grass shades: vibrant green, toxic lime, autumn orange, alien blue
        int type = Random.Range(0, 4);
        switch (type)
        {
            case 0: return new Color(Random.Range(0.15f, 0.35f), Random.Range(0.55f, 0.75f), Random.Range(0.15f, 0.35f)); // Green
            case 1: return new Color(Random.Range(0.6f, 0.8f), Random.Range(0.45f, 0.65f), Random.Range(0.1f, 0.2f)); // Orange/Fall
            case 2: return new Color(Random.Range(0.1f, 0.25f), Random.Range(0.45f, 0.65f), Random.Range(0.6f, 0.85f)); // Blue/Teal
            default: return new Color(Random.Range(0.45f, 0.65f), Random.Range(0.7f, 0.85f), Random.Range(0.1f, 0.25f)); // Lime/Toxic
        }
    }

    private Color GenerateRandomRockColor()
    {
        // Rock shades: dark grey, brownish, terracotta red, obsidian black
        int type = Random.Range(0, 3);
        switch (type)
        {
            case 0: return new Color(Random.Range(0.35f, 0.45f), Random.Range(0.35f, 0.45f), Random.Range(0.35f, 0.45f)); // Dark grey
            case 1: return new Color(Random.Range(0.45f, 0.58f), Random.Range(0.35f, 0.45f), Random.Range(0.25f, 0.35f)); // Brown
            default: return new Color(Random.Range(0.55f, 0.7f), Random.Range(0.25f, 0.35f), Random.Range(0.2f, 0.3f)); // Terracotta
        }
    }
}
