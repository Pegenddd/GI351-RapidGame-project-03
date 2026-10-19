using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpecialObjectConfig
    {
        [Tooltip("Prefab ของเสาพินบอลพิเศษ")]
        public GameObject prefab;
        
        [Tooltip("ดีเลย์ก่อนเริ่มเกิดชิ้นแรกของชนิดนี้ หลังจากมันพัง (วินาที)")]
        public float respawnDelay = 5.0f;
        
        [Tooltip("ระยะห่าง (วินาที) หากชนิดนี้พังพร้อมกันหลายชิ้น ชิ้นต่อไปจะรอเพิ่มกี่วินาที")]
        public float spawnInterval = 1.0f;
    }

    [Header("Prefabs & Timings (ตั้งเวลาแยกตามชนิดได้)")]
    public List<SpecialObjectConfig> specialObjects = new List<SpecialObjectConfig>();

    [Header("Spawn Configuration")]
    [Tooltip("จำนวนสูงสุดที่อนุญาตให้มีในฉาก 'ต่อ 1 ชนิด'")]
    public int maxPerType = 1;               
    public float checkInterval = 2.0f;       

    [Header("Spawn Points")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Safety Checks")]
    public float minDistanceFromPlayer = 3.5f; 
    public float minDistanceFromEnemy = 2.5f;  
    public float minDistanceFromOtherPinball = 2.0f; 

    private float timer = 0f;
    private Dictionary<SpecialPinballObject.ObjectType, int> queuePerType = new Dictionary<SpecialPinballObject.ObjectType, int>();

    private void Start()
    {
        CheckAndQueueSpawns(true);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckAndQueueSpawns(false);
        }
    }

    public void CheckAndQueueSpawns(bool immediate)
    {
        if (specialObjects == null || specialObjects.Count == 0) return;
        if (spawnPoints == null || spawnPoints.Count == 0) return;

        SpecialPinballObject[] existingObjects = FindObjectsByType<SpecialPinballObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        Dictionary<SpecialPinballObject.ObjectType, int> activeCounts = new Dictionary<SpecialPinballObject.ObjectType, int>();
        foreach (var obj in existingObjects)
        {
            if (obj != null)
            {
                if (!activeCounts.ContainsKey(obj.objectType)) activeCounts[obj.objectType] = 0;
                activeCounts[obj.objectType]++;
            }
        }

        // 🌟 วนลูปเช็กแต่ละชนิดว่าขาดไปกี่ชิ้นจากโควตา Max Per Type
        foreach (var config in specialObjects)
        {
            if (config.prefab != null)
            {
                var script = config.prefab.GetComponent<SpecialPinballObject>();
                if (script != null)
                {
                    var type = script.objectType;
                    int totalOfType = (activeCounts.ContainsKey(type) ? activeCounts[type] : 0) + 
                                      (queuePerType.ContainsKey(type) ? queuePerType[type] : 0);
                    
                    int neededOfThisType = maxPerType - totalOfType;

                    // สั่งคิวเกิดใหม่ตามจำนวนที่ขาด
                    for (int i = 0; i < neededOfThisType; i++)
                    {
                        if (!queuePerType.ContainsKey(type)) queuePerType[type] = 0;
                        
                        int queuePosition = queuePerType[type];
                        queuePerType[type]++;

                        float delay = immediate ? 0f : (config.respawnDelay + (queuePosition * config.spawnInterval));
                        StartCoroutine(SpawnSpecificObjectAfterDelay(delay, config.prefab, type));
                    }
                }
            }
        }
    }

    private IEnumerator SpawnSpecificObjectAfterDelay(float delay, GameObject prefab, SpecialPinballObject.ObjectType type)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        List<Transform> validSpawnPoints = new List<Transform>();
        foreach (var sp in spawnPoints)
        {
            if (sp != null && IsPositionSafe(sp.position))
            {
                validSpawnPoints.Add(sp);
            }
        }

        if (validSpawnPoints.Count > 0)
        {
            Transform selectedSpawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
            Vector3 spawnPos = selectedSpawnPoint.position;
            spawnPos.y = 0.6f;
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }

        if (queuePerType.ContainsKey(type))
        {
            queuePerType[type]--;
            if (queuePerType[type] < 0) queuePerType[type] = 0;
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