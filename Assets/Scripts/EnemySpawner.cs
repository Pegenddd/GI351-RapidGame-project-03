using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies periodically across the arena.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public int maxEnemies = 8;
    public float spawnInterval = 2.5f;
    public float arenaHalfSize = 12f;
    public float minDistanceFromPlayer = 5f;

    private float timer = 0f;
    private Transform playerTransform;

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }

        // Initial batch of enemies
        for (int i = 0; i < 4; i++)
        {
            SpawnEnemy();
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            int currentEnemyCount = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    public GameObject SpawnEnemy()
    {
        Vector3 spawnPos = GetRandomSpawnPosition();

        // Create Enemy GameObject
        GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObj.name = "Enemy";
        enemyObj.transform.position = spawnPos;
        enemyObj.transform.localScale = new Vector3(1f, 1.2f, 1f);

        // Setup material
        Renderer rend = enemyObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rend.material.color = new Color(0.9f, 0.25f, 0.25f);
        }

        // Add Enemy Controller
        EnemyController ec = enemyObj.AddComponent<EnemyController>();

        // Enemy eye indicator to show facing direction
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        visor.transform.SetParent(enemyObj.transform, false);
        visor.transform.localPosition = new Vector3(0, 0.4f, 0.45f);
        visor.transform.localScale = new Vector3(0.6f, 0.25f, 0.3f);
        Destroy(visor.GetComponent<Collider>());
        Renderer visorRend = visor.GetComponent<Renderer>();
        if (visorRend != null)
        {
            visorRend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            visorRend.material.color = Color.black;
        }

        return enemyObj;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        for (int attempts = 0; attempts < 15; attempts++)
        {
            float x = Random.Range(-arenaHalfSize, arenaHalfSize);
            float z = Random.Range(-arenaHalfSize, arenaHalfSize);
            Vector3 pos = new Vector3(x, 1f, z);

            if (playerTransform != null)
            {
                if (Vector3.Distance(pos, playerTransform.position) < minDistanceFromPlayer)
                {
                    continue; // Too close to player, try again
                }
            }
            return pos;
        }
        return new Vector3(Random.Range(-8f, 8f), 1f, Random.Range(-8f, 8f));
    }
}
