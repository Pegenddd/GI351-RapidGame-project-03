using System.Collections.Generic;
using UnityEngine;

public class SpecialObjectSpawner : MonoBehaviour
{
    [Header("Prefabs ทั้ง 3 ชนิด")]
    public GameObject[] objectPrefabs; // 0: Orbit, 1: Explode, 2: Split

    [Header("Spawn Configuration")]
    public int totalSpawnCount = 5;          // จำนวนรวมที่ต้องการให้คงอยู่ในแมพตลอดเวลา
    public int maxPerType = 2;               // ห้ามมี Object ชนิดเดียวกันเกินกี่อัน
    public float checkInterval = 2.0f;       // เช็ครักษาจำนวนทุกๆ กี่วินาที

    [Header("Spawn Points")]
    [Tooltip("ลาก GameObjects ที่ใช้เป็นจุดเกิด (Spawn Points) มาใส่ในนี้")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Safety Checks")]
    public float minDistanceFromPlayer = 3.5f; // ห้ามเกิดใกล้ผู้เล่นเกินระยะนี้
    public float minDistanceFromEnemy = 2.5f;  // ห้ามเกิดทับหรือใกล้ศัตรูเกินระยะนี้
    public float minDistanceFromOtherPinball = 2.0f; // ห้ามเกิดใกล้พินบอลด้วยกันเองเกินระยะนี้

    private float timer = 0f;

    private void Start()
    {
        SpawnMissingObjects();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            SpawnMissingObjects();
        }
    }

    public void SpawnMissingObjects()
    {
        if (objectPrefabs == null || objectPrefabs.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Count == 0) return;

        SpecialPinballObject[] existingObjects = FindObjectsByType<SpecialPinballObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int currentTotal = existingObjects.Length;

        if (currentTotal >= totalSpawnCount) return;

        // 🌟 1. นับจำนวนแยกตามชนิด (Type) จาก objectType โดยตรงให้ชัวร์
        Dictionary<SpecialPinballObject.ObjectType, int> typeCounts = new Dictionary<SpecialPinballObject.ObjectType, int>();

        // กำหนดค่าเริ่มต้นให้ครบทุก Type เป็น 0
        foreach (SpecialPinballObject.ObjectType typeVal in System.Enum.GetValues(typeof(SpecialPinballObject.ObjectType)))
        {
            typeCounts[typeVal] = 0;
        }

        // นับจำนวนที่มีอยู่จริงในฉากตอนนี้
        foreach (var obj in existingObjects)
        {
            if (obj != null)
            {
                if (typeCounts.ContainsKey(obj.objectType))
                {
                    typeCounts[obj.objectType]++;
                }
            }
        }

        int neededCount = totalSpawnCount - currentTotal;

        while (neededCount > 0)
        {
            // 2. ค้นหาเฉพาะจุดเกิดที่ว่างและปลอดภัย
            List<Transform> validSpawnPoints = new List<Transform>();
            foreach (var sp in spawnPoints)
            {
                if (sp != null && IsPositionSafe(sp.position))
                {
                    validSpawnPoints.Add(sp);
                }
            }

            if (validSpawnPoints.Count == 0) break;

            // 🌟 3. กรองเอาเฉพาะ "ชนิดของพินบอล" ที่ยังมีจำนวนไม่เกิน maxPerType จริงๆ เท่านั้น
            List<int> availableTypeIndices = new List<int>();
            for (int i = 0; i < objectPrefabs.Length; i++)
            {
                if (objectPrefabs[i] != null)
                {
                    // ดึง Component จาก Prefab มาเช็ก Enum Type ของมันตรงๆ เลย
                    SpecialPinballObject pinballScript = objectPrefabs[i].GetComponent<SpecialPinballObject>();
                    if (pinballScript != null)
                    {
                        SpecialPinballObject.ObjectType prefabType = pinballScript.objectType;

                        // ถ้าชนิดนี้ยังมีจำนวนในฉากไม่ถึง maxPerType อนุญาตให้สุ่มเกิดได้
                        if (typeCounts.ContainsKey(prefabType) && typeCounts[prefabType] < maxPerType)
                        {
                            availableTypeIndices.Add(i);
                        }
                    }
                }
            }

            // ถ้าทุกชนิดเกินโควตา maxPerType หมดแล้ว ให้หยุดทันที
            if (availableTypeIndices.Count == 0) break;

            // สุ่มเลือก Index จากกลุ่มชนิดที่ยังไม่เต็มโควตาเท่านั้น
            int chosenPrefabIndex = availableTypeIndices[Random.Range(0, availableTypeIndices.Count)];

            // สุ่มเลือกจุดเกิดที่ปลอดภัย
            Transform selectedSpawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
            Vector3 spawnPos = selectedSpawnPoint.position;
            spawnPos.y = 0.6f;

            // สร้างพินบอล
            GameObject spawnedObj = Instantiate(objectPrefabs[chosenPrefabIndex], spawnPos, Quaternion.identity);

            // อัปเดตจำนวนนับในดิกชันนารีทันที เพื่อไม่ให้ลูปถัดไปสุ่มเกินอีก
            SpecialPinballObject spawnedScript = spawnedObj.GetComponent<SpecialPinballObject>();
            if (spawnedScript != null && typeCounts.ContainsKey(spawnedScript.objectType))
            {
                typeCounts[spawnedScript.objectType]++;
            }

            neededCount--;
        }
    }

    private bool IsPositionSafe(Vector3 pos)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            if (Vector3.Distance(pos, player.transform.position) < minDistanceFromPlayer)
                return false;
        }

        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (Vector3.Distance(pos, enemy.transform.position) < minDistanceFromEnemy)
                return false;
        }

        SpecialPinballObject[] existingObjects = FindObjectsByType<SpecialPinballObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pinball in existingObjects)
        {
            if (pinball != null && Vector3.Distance(pos, pinball.transform.position) < minDistanceFromOtherPinball)
            {
                return false;
            }
        }

        return true;
    }
}