using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        [Tooltip("Prefab ของศัตรูชนิดนี้")]
        public GameObject enemyPrefab;
        [Tooltip("น้ำหนักโอกาสสุ่มเกิด")]
        public float spawnWeight = 1f;
    }

    [Header("Enemy Pool")]
    public List<EnemySpawnData> enemyPrefabs = new List<EnemySpawnData>();

    [Header("Spawn Points")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Offset Settings")]
    [Tooltip("ปรับค่านี้ใน Inspector เพื่อยกตัวศัตรูขึ้นจากพื้น (แก้ปัญหาจมดิน)")]
    public float spawnHeightOffset = 1.5f;

    [Header("Balance & Scaling Settings")]
    [Tooltip("จำนวนศัตรูสูงสุดที่อนุญาตให้มีในฉากพร้อมกัน")]
    public int maxEnemiesInScene = 25;

    [Tooltip("อัตราการเกิดเริ่มต้น (กี่วินาทีต่อ 1 ตัว)")]
    public float baseSpawnInterval = 1.5f;

    [Tooltip("ความยากเพิ่มขึ้นตามเวลา (ยิ่งค่ามาก ยิ่งเกิดเร็วขึ้นเรื่อยๆ ทุกๆ นาที)")]
    public float difficultyMultiplierPerMinute = 0.15f;

    private float gameTimer = 0f;
    private float spawnTimer = 0f;

    private void Update()
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Count == 0) return;

        // จับเวลาเกมเดินหน้า
        gameTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        // คำนวณช่วงเวลาเกิดที่จะสั้นลงเรื่อยๆ ตามเวลาที่ผ่านไป
        float currentInterval = Mathf.Max(0.2f, baseSpawnInterval - (gameTimer / 60f) * difficultyMultiplierPerMinute);

        if (spawnTimer >= currentInterval)
        {
            spawnTimer = 0f;

            // เช็กจำนวนมอนสเตอร์ปัจจุบันในฉาก
            int currentEnemyCount = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

            if (currentEnemyCount < maxEnemiesInScene)
            {
                SpawnBalancedEnemy();
            }
        }
    }

    private void SpawnBalancedEnemy()
    {
        // 1. สุ่มเลือกจุดเกิด
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Count)];
        if (sp == null) return;

        // 2. สุ่มเลือกชนิดศัตรูตาม Weight
        EnemySpawnData selectedData = GetWeightedRandomEnemyData();
        if (selectedData == null || selectedData.enemyPrefab == null) return;

        // 3. สร้างศัตรูพร้อมบวกความสูงตามค่าที่ตั้งไว้ใน Inspector
        Vector3 spawnPos = sp.position + (Vector3.up * spawnHeightOffset);
        Instantiate(selectedData.enemyPrefab, spawnPos, sp.rotation);
    }

    private EnemySpawnData GetWeightedRandomEnemyData()
    {
        float totalWeight = 0f;
        foreach (var data in enemyPrefabs) { totalWeight += data.spawnWeight; }

        if (totalWeight <= 0f) return null;

        float rnd = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var data in enemyPrefabs)
        {
            currentSum += data.spawnWeight;
            if (rnd <= currentSum) return data;
        }
        return enemyPrefabs[0];
    }
}