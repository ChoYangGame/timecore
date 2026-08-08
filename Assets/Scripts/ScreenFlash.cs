using UnityEngine;

/// <summary>
/// 화면 가장자리 붉은 번쩍임(피격) / 전체 번쩍임(보스 격파) 연출.
///
/// 씬에 배치하지 않는다. 처음 쓰일 때 카메라 자식으로 스스로 만들어진다 —
/// 씬 파일을 건드리지 않으려는 것이다(에디터 모달로 세션이 막힌 전례가 있다).
///
/// 가장자리 물듦은 `FxTextures.EdgeGradient` 한 장으로 그린다. 처음에는 그라데이션 텍스처가 없어
/// 두께·농도가 다른 **띠 3겹**으로 흉내냈는데, 겹친 띠의 경계가 그대로 보여 조잡했다.
/// 지금은 변마다 그라데이션 한 장을 바깥이 진하도록 회전시켜 붙인다(변당 3장 → 1장).
///
/// timeScale과 무관하게 돌아야 한다 — 히트스톱이 걸린 순간이 정확히 번쩍여야 하는 순간이다.
/// 부착 대상: 없음 (런타임 자동 생성)
/// </summary>
[DisallowMultipleComponent]
public class ScreenFlash : MonoBehaviour
{
    private static ScreenFlash _instance;

    private Camera _camera;

    private SpriteRenderer[] _edges;      // 아래·위·오른쪽·왼쪽 순서로 4장
    private SpriteRenderer _full;

    /// <summary>변마다 그라데이션의 진한 쪽(로컬 -Y)이 화면 바깥을 향하도록 돌리는 각도.</summary>
    private static readonly float[] EdgeAngle = { 0f, 180f, 90f, 270f };

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

        GameObject go = new GameObject("ScreenFlash");
        go.transform.SetParent(cam.transform, false);

        _instance = go.AddComponent<ScreenFlash>();
        _instance._camera = cam;
        _instance.Build();

        return _instance;
    }

    private void Build()
    {
        _edges = new SpriteRenderer[4];
        for (int i = 0; i < _edges.Length; i++)
            _edges[i] = MakeQuad(FxTextures.EdgeGradient, "Edge" + i, 32000);

        _full = MakeQuad(FxTextures.Solid, "Full", 32001);
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

        if (edgeAlive)
        {
            _edgeLife -= dt;
            float t = Mathf.Clamp01(_edgeLife / _edgeMaxLife);
            LayoutEdges(halfW, halfH, _edgePeak * t);
            if (_edgeLife <= 0f) HideEdges();
        }

        if (fullAlive)
        {
            _fullLife -= dt;
            float t = Mathf.Clamp01(_fullLife / _fullMaxLife);
            float a = _fullPeak * t;

            // FxTextures는 bounds가 1x1이라 월드 크기를 그대로 스케일에 넣으면 된다.
            _full.transform.localPosition = new Vector3(0f, 0f, 1f);
            _full.transform.localScale = new Vector3(halfW * 2.2f, halfH * 2.2f, 1f);

            Color c = _fullColor;
            c.a = a;
            _full.color = c;
            _full.enabled = a > 0.004f;
        }
    }

    /// <summary>
    /// 변마다 그라데이션 한 장. 스케일은 회전 전(로컬) 기준으로 넣는다 —
    /// 로컬 X가 변을 따라가는 길이, 로컬 Y가 안쪽으로 스며드는 두께다.
    /// 회전은 그 뒤에 적용되므로 세로 변에서도 같은 식이 그대로 쓰인다.
    /// </summary>
    private void LayoutEdges(float halfW, float halfH, float alpha)
    {
        // 가로변은 화면 높이의, 세로변은 화면 폭의 일정 비율만큼 스며든다.
        float thickH = halfH * 0.42f;
        float thickV = halfW * 0.30f;

        Color c = _edgeColor;
        c.a = alpha;

        for (int side = 0; side < 4; side++)
        {
            SpriteRenderer sr = _edges[side];
            bool horizontal = side < 2;

            float thickness = horizontal ? thickH : thickV;
            float length = horizontal ? halfW * 2.05f : halfH * 2.05f;

            // 0=아래, 1=위, 2=오른쪽, 3=왼쪽. 진한 쪽(로컬 -Y)이 바깥을 보도록 EdgeAngle로 돌린다.
            Vector3 pos;
            switch (side)
            {
                case 0: pos = new Vector3(0f, -halfH + thickness * 0.5f, 1f); break;
                case 1: pos = new Vector3(0f, halfH - thickness * 0.5f, 1f); break;
                case 2: pos = new Vector3(halfW - thickness * 0.5f, 0f, 1f); break;
                default: pos = new Vector3(-halfW + thickness * 0.5f, 0f, 1f); break;
            }

            sr.transform.localPosition = pos;
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, EdgeAngle[side]);
            sr.transform.localScale = new Vector3(length, thickness, 1f);
            sr.color = c;
            sr.enabled = alpha > 0.004f;
        }
    }

    private void HideEdges()
    {
        for (int i = 0; i < _edges.Length; i++) _edges[i].enabled = false;
    }
}
