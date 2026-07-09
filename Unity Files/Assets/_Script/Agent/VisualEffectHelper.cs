using UnityEngine;

public static class VisualEffectHelper
{
    // 부채꼴 및 원 생성 (각도가 360이면 원)
    public static void CreateShape(Vector2 position, Vector2 direction, float radius, float angle, float duration, Color color)
    {
        GameObject go = new GameObject("VFX_Shape");
        go.transform.position = position;

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
        {
            meshRenderer.material = new Material(defaultShader);
            meshRenderer.material.color = color;
        }

        Mesh mesh = new Mesh();
        int segments = Mathf.Max(10, Mathf.RoundToInt(30 * (angle / 360f)));

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float startAngle = -angle / 2f;
        float angleStep = angle / segments;
        float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (currentAngle + startAngle + (angleStep * i)) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;

        Object.Destroy(go, duration);
    }

    // 사슬 포획 등 선형 시각화용
    public static void CreateLine(Vector2 start, Vector2 end, float duration, Color color)
    {
        GameObject go = new GameObject("VFX_Line");
        LineRenderer lr = go.AddComponent<LineRenderer>();

        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null) lr.material = new Material(defaultShader);

        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.sortingOrder = 10;

        Object.Destroy(go, duration);
    }
}
