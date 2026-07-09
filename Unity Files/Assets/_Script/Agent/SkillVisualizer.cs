using UnityEngine;

// --- 1. 시각화 규약 ---
public interface ISkillVisualizer
{
    void DrawCone(Vector2 position, Vector2 direction, float radius, float angle, float duration, Color color);
    void DrawCircle(Vector2 position, float radius, float duration, Color color);
    void DrawLine(Vector2 start, Vector2 end, float duration, Color color);
}

// --- 2. 에디터 테스트용 구현체 (메쉬 및 라인 생성) ---
public class EditorSkillVisualizer : ISkillVisualizer
{
    public void DrawCone(Vector2 position, Vector2 direction, float radius, float angle, float duration, Color color)
    {
        VisualEffectHelper.CreateShape(position, direction, radius, angle, duration, color);
    }

    public void DrawCircle(Vector2 position, float radius, float duration, Color color)
    {
        // 360도 부채꼴 = 원
        VisualEffectHelper.CreateShape(position, Vector2.right, radius, 360f, duration, color);
    }

    public void DrawLine(Vector2 start, Vector2 end, float duration, Color color)
    {
        VisualEffectHelper.CreateLine(start, end, duration, color);
    }
}

// --- 3. ML-Agents 학습 빌드용 구현체 (아무것도 하지 않음 = GC 발생 0) ---
public class NullSkillVisualizer : ISkillVisualizer
{
    public void DrawCone(Vector2 pos, Vector2 dir, float rad, float ang, float dur, Color col) { }
    public void DrawCircle(Vector2 pos, float rad, float dur, Color col) { }
    public void DrawLine(Vector2 start, Vector2 end, float dur, Color col) { }
}