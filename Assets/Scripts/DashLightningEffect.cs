using UnityEngine;

public class DashLightningEffect : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float lifetime = 0.2f;
    private float timer = 0f;

    public void Init(Vector3 startPos, Vector3 endPos, Color lightningColor)
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = 4;
        lineRenderer.useWorldSpace = true;

        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material lineMat = new Material(lineShader);
        if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", lightningColor);
        if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color", lightningColor);
        lineRenderer.material = lineMat;

        lineRenderer.startColor = lightningColor;
        lineRenderer.endColor = new Color(lightningColor.r, lightningColor.g, lightningColor.b, 0f);

        // สร้างเส้นหักเหแบบสายฟ้า (Jagged Line) ระหว่างจุดเริ่มต้นและจุดสิ้นสุด
        Vector3 mid1 = Vector3.Lerp(startPos, endPos, 0.33f) + Random.insideUnitSphere * 0.3f;
        Vector3 mid2 = Vector3.Lerp(startPos, endPos, 0.66f) + Random.insideUnitSphere * 0.3f;
        mid1.y = startPos.y;
        mid2.y = startPos.y;

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, mid1);
        lineRenderer.SetPosition(2, mid2);
        lineRenderer.SetPosition(3, endPos);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime && lineRenderer != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            Color c = lineRenderer.startColor;
            c.a = alpha;
            lineRenderer.startColor = c;
        }
    }
}