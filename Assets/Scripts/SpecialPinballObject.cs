using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialPinballObject : MonoBehaviour
{
    public enum ObjectType { OrbitPinball, ExplodePinball, SplitBallObject }

    [Header("Object Settings")]
    public ObjectType objectType;

    [Header("Orbit Settings")]
    public float orbitDuration = 5.0f;
    public float initialOrbitSpeed = 150f;
    public float maxOrbitSpeed = 500f;
    public float speedRampRate = 60f;

    [Header("Enemy Pull Settings (Continuous Pull)")]
    [Tooltip("รัศมีวงกลมในการค้นหาและดึงดูดศัตรู")]
    public float pullRadius = 12.0f;
    [Tooltip("ความเร็วในการดึงดูดศัตรูเข้าหาศูนย์กลางอย่างต่อเนื่องทุกเฟรม")]
    public float pullSpeed = 6.0f;
    [Tooltip("ระยะห่างจากศูนย์กลางเสาที่ศัตรูจะหยุดวิ่งเข้าหา (ป้องกันมอนจมเข้าไปในตัวเสา)")]
    public float stopDistance = 1.8f;

    [Header("Explode Settings")]
    public float explosionRadius = 5.0f;
    public float explosionDamage = 80f;
    [Tooltip("ติ๊กเลือก = ระเบิดเฉพาะตอนชนศัตรูที่มี Tag 'Enemy' เท่านั้น | ไม่ติ๊ก = ระเบิดทันทีเมื่อชนกับอะไรก็ตาม")]
    public bool explodeOnlyOnEnemy = false;

    [Header("Split Ball Settings")]
    public int splitCount = 2;
    public float splitDuration = 5.0f;

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated && objectType == ObjectType.OrbitPinball) return;

        BouncingBall ball = other.GetComponent<BouncingBall>() ?? other.GetComponentInParent<BouncingBall>();
        if (ball != null)
        {
            ActivateEffect(ball);
        }
    }

    public void ActivateEffect(BouncingBall ball)
    {
        switch (objectType)
        {
            /* Orbit Pinball */
            case ObjectType.OrbitPinball:
                isActivated = true;
                ball.ApplyOrbitEffect(transform, initialOrbitSpeed, maxOrbitSpeed, speedRampRate, orbitDuration);

                Collider triggerCol = GetComponent<Collider>();
                if (triggerCol != null) triggerCol.enabled = false;

                // 🌟 ส่ง ball เข้าไปใน Coroutine เพื่อให้เช็คสถานะได้
                StartCoroutine(ContinuousPullAndDestroyRoutine(ball));
                break;

            /* Explode Pinball */
            case ObjectType.ExplodePinball:
                ball.SetExplosiveState(true, explosionRadius, explosionDamage, explodeOnlyOnEnemy);
                Destroy(gameObject);
                break;

            /* Split Ball Object */
            case ObjectType.SplitBallObject:
                ball.SplitBall(splitCount, splitDuration);
                Destroy(gameObject);
                break;
        }
    }

    private IEnumerator ContinuousPullAndDestroyRoutine(BouncingBall activeBall)
    {
        float elapsed = 0f;
        // เก็บรายชื่อศัตรูที่กำลังโดนดึงเพื่อใช้คืนสถานะตอนจบ
        HashSet<EnemyBase> pulledEnemies = new HashSet<EnemyBase>();

        while (elapsed < orbitDuration)
        {
            // 🌟 เช็คว่าลูกบอลยังคงหมุนอยู่รอบเสานี้หรือไม่
            if (activeBall == null || !activeBall.isOrbiting || activeBall.orbitTargetTransform != transform)
            {
                // ถ้าบอลหลุดออกไปแล้ว ให้หยุดวงจรการดูดทันที (Break ออกจากลูป)
                break;
            }

            elapsed += Time.deltaTime;

            Collider[] colliders = Physics.OverlapSphere(transform.position, pullRadius);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Enemy") || (col.transform.parent != null && col.transform.parent.CompareTag("Enemy")))
                {
                    EnemyBase enemyBase = col.GetComponentInParent<EnemyBase>() ?? col.GetComponent<EnemyBase>();
                    if (enemyBase != null)
                    {
                        // สั่งล็อคไม่ให้ศัตรูเดินเองตาม AI ปกติ
                        enemyBase.SetPulledState(true);
                        pulledEnemies.Add(enemyBase);

                        Transform enemyTarget = enemyBase.transform;
                        float distanceToCenter = Vector3.Distance(enemyTarget.position, transform.position);

                        // ถ้ายังไกลกว่าระยะหยุด ให้ขยับเข้าหาด้วย pullSpeed ที่กำหนด
                        if (distanceToCenter > stopDistance)
                        {
                            enemyTarget.position = Vector3.MoveTowards(enemyTarget.position, transform.position, pullSpeed * Time.deltaTime);
                        }
                    }
                }
            }

            yield return null;
        }

        // เมื่อหมดเวลาดึงดูด หรือลูกบอลหลุดออกไปแล้ว คืนสถานะการเดินปกติให้ศัตรูทั้งหมดที่เคยโดนดึง
        foreach (var enemy in pulledEnemies)
        {
            if (enemy != null)
            {
                enemy.SetPulledState(false);
            }
        }

        // ทำลายเสาทิ้งเมื่อจบการทำงาน
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (objectType == ObjectType.OrbitPinball)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pullRadius);
        }
    }
}