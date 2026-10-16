using UnityEngine;
public class Icewall : MonoBehaviour
{
    [Header("Ice Wall Settings")]
    [Tooltip("ตัวคูณความเร็วเมื่อลูกบอลชน (เช่น 0.4 คือความเร็วลดลงเหลือ 40%)")]
    [Range(0.1f, 0.9f)]
    public float slowMultiplier = 0.4f;

    [Tooltip("ระยะเวลา (วินาที) ที่ศัตรูจะติดสโลว์เมื่อโดนลูกบอลน้ำแข็งนี้พุ่งชน")]
    public float enemySlowDuration = 2.0f;

    private void OnCollisionEnter(Collision collision)
    {
        ApplyIceToBall(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyIceToBall(other);
    }

    private void ApplyIceToBall(Collider hitCollider)
    {
        // เช็กว่าเป็น "ลูกบอล" หรือไม่
        BouncingBall ball = hitCollider.GetComponent<BouncingBall>() ?? hitCollider.GetComponentInParent<BouncingBall>();
        if (ball != null)
        {
            // สั่งให้บอลลดความเร็วและรับบัฟน้ำแข็ง
            ball.ApplyIceEffect(slowMultiplier, enemySlowDuration);
        }
    }
}