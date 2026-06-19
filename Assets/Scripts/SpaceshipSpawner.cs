using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpaceshipSpawner : MonoBehaviour
{
    public GameObject playerShipPrefab;
    public GameObject enemyShipPrefab;
    public float spawnRadius = 50f;

    public void SpawnPlayerShips(int count)
    {
        if (playerShipPrefab == null)
        {
            Debug.LogError("[SpaceshipSpawner] Player Ship Prefab is unassigned!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randCircle.x, 0f, randCircle.y);
            
            GameObject ship = Instantiate(playerShipPrefab, spawnPos, Quaternion.identity);
            SpaceshipController sc = ship.GetComponent<SpaceshipController>();
            if (sc != null)
            {
                sc.isEnemy = false;
                sc.health = sc.maxHealth;
            }
        }
        Debug.Log($"[SpaceshipSpawner] Spawned {count} player ships.");
    }

    public void SpawnEnemyShips(int count)
    {
        if (enemyShipPrefab == null)
        {
            Debug.LogError("[SpaceshipSpawner] Enemy Ship Prefab is unassigned!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randCircle.x, 0f, randCircle.y);

            GameObject ship = Instantiate(enemyShipPrefab, spawnPos, Quaternion.identity);
            SpaceshipController sc = ship.GetComponent<SpaceshipController>();
            if (sc != null)
            {
                sc.isEnemy = true;
                sc.aggressive = true; // Default enemy AI to aggressive
                sc.health = sc.maxHealth;
            }
        }
        Debug.Log($"[SpaceshipSpawner] Spawned {count} enemy ships.");
    }

    void Update()
    {
        // Keyboard shortcuts to spawn 10 player or enemy ships in play mode
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SpawnPlayerShips(10);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            SpawnEnemyShips(10);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpaceshipSpawner))]
public class SpaceshipSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpaceshipSpawner spawner = (SpaceshipSpawner)target;

        GUILayout.Space(15);
        GUILayout.Label("Spawn Player Ships", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn 10")) spawner.SpawnPlayerShips(10);
        if (GUILayout.Button("Spawn 20")) spawner.SpawnPlayerShips(20);
        if (GUILayout.Button("Spawn 50")) spawner.SpawnPlayerShips(50);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Spawn Enemy Ships", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn 10")) spawner.SpawnEnemyShips(10);
        if (GUILayout.Button("Spawn 20")) spawner.SpawnEnemyShips(20);
        if (GUILayout.Button("Spawn 50")) spawner.SpawnEnemyShips(50);
        GUILayout.EndHorizontal();
    }
}
#endif
