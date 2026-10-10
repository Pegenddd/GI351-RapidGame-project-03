using System.Collections;
using UnityEngine;

public class PinballBumper : MonoBehaviour
{
    private Renderer bumperRenderer;
    private MaterialPropertyBlock propBlock;
    private Color originalColor;
    public Color hitColor = Color.yellow; // สีที่จะเปลี่ยนเวลาบอลชน

    private void Awake()
    {
        bumperRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        if (bumperRenderer != null)
        {
            originalColor = bumperRenderer.sharedMaterial.color;
        }
    }

    // ฟังก์ชันรับคำสั่งเปลี่ยนสีจากลูกบอล
    public void OnHitByBall()
    {
        StopAllCoroutines();
        StartCoroutine(FlashColorRoutine());
    }

    private IEnumerator FlashColorRoutine()
    {
        if (bumperRenderer != null)
        {
            propBlock.SetColor("_BaseColor", hitColor);
            propBlock.SetColor("_Color", hitColor);
            bumperRenderer.SetPropertyBlock(propBlock);

            yield return new WaitForSeconds(0.15f);

            propBlock.SetColor("_BaseColor", originalColor);
            propBlock.SetColor("_Color", originalColor);
            bumperRenderer.SetPropertyBlock(propBlock);
        }
    }
}