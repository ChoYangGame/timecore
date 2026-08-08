using UnityEngine;

/// <summary>
/// 화면 가장자리 붉은 번쩍임(피격) / 전체 번쩍임(보스 격파) 연출.
///
/// 씬에 배치하지 않는다. 처음 쓰일 때 카메라 자식으로 스스로 만들어진다 —
/// 씬 파일을 건드리지 않으려는 것이다(에디터 모달로 세션이 막힌 전례가 있다).
///
/// 그라데이션 텍스처가 없어서 가장자리 어둠을 **띠 3겹**으로 흉내낸다.
/// 안쪽으로 갈수록 두껍고 옅은 띠를 깔면 경계가 뭉개져 비네트처럼 읽힌다.
/// 스프라이트는 EffectSystem이 쓰는 내장 Square를 그대로 빌린다(새 에셋 0).
///
/// timeScale과 무관하게 돌아야 한다 — 히트스톱이 걸린 순간이 정확히 번쩍여야 하는 순간이다.
/// 부착 대상: 없음 (런타임 자동 생성)
/// </summary>
[DisallowMultipleComponent]
public class ScreenFlash : MonoBehaviour
{
    /// <summary>가장자리 한 변당 띠 수. 안쪽일수록 두껍고 옅다.</summary>
    private const int StepsPerEdge = 3;

    private static ScreenFlash _instance;

    private Camera _camera;

    private SpriteRenderer[] _edges;      // 4변 x StepsPerEdge
    private SpriteRenderer _full;

    private Color _edgeColor = Color.red;
    private float _edgePeak;
    private float _edgeLife;
    private float _edgeMaxLife = 1f;

    private Color _fullColor = Color.white;
    private float _fullPeak;
    private float _fullLife;
    private float _fullMaxLife = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    /// <summary>화면 가장자리가 물든다. 플레이어 피격처럼 "내가 맞았다"를 알릴 때 쓴다.</summary>
    public static void Edges(Color color, float peakAlpha = 0.5f, float duration = 0.32f)
    {
        ScreenFlash f = Ensure();
        if (f == null) return;

        // 이미 더 센 번쩍임이 돌고 있으면 끊지 않는다(CameraShake와 같은 규칙).
        if (f._edgeLife > 0f && peakAlpha <= f._edgePeak) return;

        f._edgeColor = color;
        f._edgePeak = Mathf.Clamp01(peakAlpha);
        f._edgeMaxLife = Mathf.Max(0.01f, duration);
        f._edgeLife = f._edgeMaxLife;
    }

    /// <summary>화면 전체가 물든다. 보스 격파처럼 한 판의 국면이 바뀔 때만 쓴다.</summary>
    public static void Full(Color color, float peakAlpha = 0.35f, float duration = 0.3f)
    {
        ScreenFlash f = Ensure();
        if (f == null) return;

        if (f._fullLife > 0f && peakAlpha <= f._fullPeak) return;

        f._fullColor = color;
        f._fullPeak = Mathf.Clamp01(peakAlpha);
        f._fullMaxLife = Mathf.Max(0.01f, duration);
        f._fullLife = f._fullMaxLife;
    }

    private static ScreenFlash Ensure()
    {
        if (_instance != null) return _instance;

        Camera cam = Camera.main;
        if (cam == null) return null;

        // EffectSystem이 아직 Awake 전이면 스프라이트가 없다. 그 프레임은 조용히 건너뛴다.
        if (EffectSystem.PieceSprite == null) return null;

        GameObject go = new GameObject("ScreenFlash");
        go.transform.SetParent(cam.transform, false);

        _instance = go.AddComponent<ScreenFlash>();
        _instance._camera = cam;
        _instance.Build();

        return _instance;
    }

    private void Build()
    {
        Sprite sprite = EffectSystem.PieceSprite;

        _edges = new SpriteRenderer[4 * StepsPerEdge];
        for (int i = 0; i < _edges.Length; i++)
        {
            // 두꺼운(=옅은) 띠를 먼저, 얇고 진한 띠를 위에 올린다.
            _edges[i] = MakeQuad(sprite, "Edge" + i, 32000 + i % StepsPerEdge);
        }

        _full = MakeQuad(sprite, "Full", 32000 + StepsPerEdge);
    }

    private SpriteRenderer MakeQuad(Sprite sprite, string label, int order)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.enabled = false;

        return sr;
    }

    private void LateUpdate()
    {
        // 히트스톱(timeScale 0.x)이 걸려 있어도 번쩍임은 실제 시간으로 흘러야 한다.
        float dt = Time.unscaledDeltaTime;

        bool edgeAlive = _edgeLife > 0f;
        bool fullAlive = _fullLife > 0f;

        if (!edgeAlive && !fullAlive) return;

        // 브라우저 창 크기가 바뀌면 aspect가 달라진다. 살아있는 동안만 다시 잰다.
        float halfH = _camera != null ? _camera.orthographicSize : 5f;
        float halfW = halfH * (_camera != null ? _camera.aspect : 1.777f);
        float spriteW = EffectSystem.PieceSprite != null ? EffectSystem.PieceSprite.bounds.size.x : 1f;
        if (spriteW <= 0.0001f) spriteW = 1f;

        if (edgeAlive)
        {
            _edgeLife -= dt;
            float t = Mathf.Clamp01(_edgeLife / _edgeMaxLife);
            LayoutEdges(halfW, halfH, spriteW, _edgePeak * t);
            if (_edgeLife <= 0f) HideEdges();
        }

        if (fullAlive)
        {
            _fullLife -= dt;
            float t = Mathf.Clamp01(_fullLife / _fullMaxLife);
            float a = _fullPeak * t;

            _full.transform.localPosition = new Vector3(0f, 0f, 1f);
            _full.transform.localScale = new Vector3(halfW * 2.2f / spriteW, halfH * 2.2f / spriteW, 1f);

            Color c = _fullColor;
            c.a = a;
            _full.color = c;
            _full.enabled = a > 0.004f;
        }
    }

    private void LayoutEdges(float halfW, float halfH, float spriteW, float alpha)
    {
        // 안쪽으로 갈수록 두껍고 옅게. 셋이 겹치면서 경계가 뭉개진다.
        float[] thickness = { 0.30f, 0.18f, 0.09f };
        float[] weight = { 0.30f, 0.60f, 1f };

        for (int step = 0; step < StepsPerEdge; step++)
        {
            float th = halfH * thickness[step];
            float a = alpha * weight[step];

            Color c = _edgeColor;
            c.a = a;

            for (int side = 0; side < 4; side++)
            {
                SpriteRenderer sr = _edges[side * StepsPerEdge + step];

                bool horizontal = side < 2;
                float length = horizontal ? halfW * 2.2f : halfH * 2.2f;

                Vector3 pos;
                Vector3 scale;

                if (horizontal)
                {
                    // 0 = 위, 1 = 아래
                    float y = (side == 0 ? halfH : -halfH) + (side == 0 ? -th * 0.5f : th * 0.5f);
                    pos = new Vector3(0f, y, 1f);
                    scale = new Vector3(length / spriteW, th / spriteW, 1f);
                }
                else
                {
                    // 2 = 오른쪽, 3 = 왼쪽
                    float x = (side == 2 ? halfW : -halfW) + (side == 2 ? -th * 0.5f : th * 0.5f);
                    pos = new Vector3(x, 0f, 1f);
                    scale = new Vector3(th / spriteW, length / spriteW, 1f);
                }

                sr.transform.localPosition = pos;
                sr.transform.localScale = scale;
                sr.color = c;
                sr.enabled = a > 0.004f;
            }
        }
    }

    private void HideEdges()
    {
        for (int i = 0; i < _edges.Length; i++) _edges[i].enabled = false;
    }
}
