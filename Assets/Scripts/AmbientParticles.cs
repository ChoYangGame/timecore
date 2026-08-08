using UnityEngine;

/// <summary>
/// 시대별 배경 입자. 원시=불티, 중세=먼지, 현대=스파크, 미래=데이터 조각.
///
/// **이 프로젝트에서 유일하게 상시 도는 이펙트**라 프레임에 직접 얹힌다. 그래서
/// (1) EffectSystem 풀을 쓰지 않고 자기 풀을 따로 갖는다 — 배경 입자가 전투 이펙트 조각을
/// 밀어내면 적이 죽어도 조각이 안 튄다. (2) 개수를 24개로 낮게 고정한다.
/// (3) 카메라 자식으로 두고 화면 밖으로 나가면 반대편으로 돌린다 — 월드 좌표로 뿌리면
/// 카메라가 따라 움직일 때 재배치가 눈에 띈다.
///
/// 알파를 0.3 근처로 낮게 둔다. 배경 입자가 또렷하면 예고 표식·탄과 눈이 헷갈린다 —
/// 분위기용이지 정보가 아니다.
///
/// 씬에 배치하지 않는다. 씬 로드 후 스스로 만들어진다(씬 파일을 건드리지 않으려는 것).
/// 부착 대상: 없음 (런타임 자동 생성)
/// </summary>
[DisallowMultipleComponent]
public class AmbientParticles : MonoBehaviour
{
    private struct Look
    {
        public Color color;
        public Vector2 drift;      // 기본 흐름 방향·속도 (유닛/초)
        public float jitter;       // 입자마다 속도를 흔드는 폭
        public float size;
        public float alpha;
        public float blinkSpeed;   // 0이면 깜빡이지 않는다
        public float sway;         // 좌우로 흔들리는 폭
    }

    private const int Count = 24;
    private const int Order = -900;   // 배경(-1000) 위, 감속 지대(-500) 아래

    private static AmbientParticles _instance;

    private Camera _camera;
    private EraManager _eraManager;

    private Transform[] _tf;
    private SpriteRenderer[] _sr;
    private Vector2[] _velocity;
    private float[] _phase;
    private float[] _sizeScale;

    private Look _look;
    private int _lastEra = -1;
    private float _spriteW = 1f;

    /// <summary>실측용. 지금 그려지고 있는 배경 입자 수.</summary>
    public static int ActiveCount => _instance != null ? Count : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject go = new GameObject("AmbientParticles");
        go.transform.SetParent(cam.transform, false);

        _instance = go.AddComponent<AmbientParticles>();
        _instance._camera = cam;
    }

    private void Start()
    {
        // EffectSystem.Awake가 끝난 뒤라야 스프라이트를 빌릴 수 있다.
        if (EffectSystem.PieceSprite == null)
        {
            enabled = false;
            return;
        }

        _eraManager = FindFirstObjectByType<EraManager>();
        ApplyEra(0);
        Build();
    }

    private void Build()
    {
        Sprite sprite = EffectSystem.PieceSprite;
        float w = sprite.bounds.size.x;
        if (w > 0.0001f) _spriteW = w;

        _tf = new Transform[Count];
        _sr = new SpriteRenderer[Count];
        _velocity = new Vector2[Count];
        _phase = new float[Count];
        _sizeScale = new float[Count];

        for (int i = 0; i < Count; i++)
        {
            GameObject go = new GameObject("Ambient" + i);
            go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Order;

            _tf[i] = go.transform;
            _sr[i] = sr;

            Respawn(i, true);
        }
    }

    private void Update()
    {
        if (_tf == null) return;

        int era = _eraManager != null ? (int)_eraManager.CurrentEra : 0;
        if (era != _lastEra) ApplyEra(era);

        float dt = Time.deltaTime;
        float halfH = _camera != null ? _camera.orthographicSize : 5f;
        float halfW = halfH * (_camera != null ? _camera.aspect : 1.777f);

        // 화면 밖으로 조금 나가야 되돌린다. 경계에서 바로 돌리면 사라지는 게 보인다.
        float marginX = halfW + 1f;
        float marginY = halfH + 1f;

        for (int i = 0; i < Count; i++)
        {
            _phase[i] += dt;

            Vector3 p = _tf[i].localPosition;
            float swayX = _look.sway > 0f ? Mathf.Sin(_phase[i] * 1.7f) * _look.sway : 0f;

            p.x += (_velocity[i].x + swayX) * dt;
            p.y += _velocity[i].y * dt;

            // 반대편으로 돌린다. 흐름 방향이 바뀌어도 같은 식으로 처리된다.
            if (p.x > marginX) p.x = -marginX;
            else if (p.x < -marginX) p.x = marginX;

            if (p.y > marginY) p.y = -marginY;
            else if (p.y < -marginY) p.y = marginY;

            p.z = 1f;   // 카메라 앞
            _tf[i].localPosition = p;

            if (_look.blinkSpeed > 0f)
            {
                Color c = _sr[i].color;
                // 0.6~1.0 사이. 0.35까지 떨어뜨렸더니 실측 알파가 0.12까지 내려가
                // 바닥 텍스처에 완전히 묻혔다. 깜빡임은 살리되 바닥을 올린다.
                c.a = _look.alpha * (0.8f + 0.2f * Mathf.Sin(_phase[i] * _look.blinkSpeed));
                _sr[i].color = c;
            }
        }
    }

    /// <summary>시대가 바뀌면 색·흐름·크기를 통째로 갈아끼운다. 입자를 다시 만들지는 않는다.</summary>
    private void ApplyEra(int era)
    {
        _lastEra = era;
        _look = LookFor(era);

        if (_tf == null) return;

        for (int i = 0; i < Count; i++) Respawn(i, false);
    }

    private void Respawn(int i, bool anywhere)
    {
        float halfH = _camera != null ? _camera.orthographicSize : 5f;
        float halfW = halfH * (_camera != null ? _camera.aspect : 1.777f);

        if (anywhere)
        {
            _tf[i].localPosition = new Vector3(
                Random.Range(-halfW, halfW), Random.Range(-halfH, halfH), 1f);
        }

        _phase[i] = Random.Range(0f, 10f);
        _sizeScale[i] = Random.Range(0.6f, 1.5f);

        _velocity[i] = new Vector2(
            _look.drift.x + Random.Range(-_look.jitter, _look.jitter),
            _look.drift.y + Random.Range(-_look.jitter, _look.jitter));

        float s = _look.size * _sizeScale[i];
        _tf[i].localScale = new Vector3(s / _spriteW, s / _spriteW, 1f);

        Color c = _look.color;
        c.a = _look.alpha;
        _sr[i].color = c;
    }

    /// <summary>
    /// 시대별 성격. 색은 시대 배경·적 색과 겹치지 않는 쪽으로 골랐다 —
    /// 배경 입자가 적이나 탄과 같은 색이면 눈이 계속 헛짚는다.
    /// </summary>
    private static Look LookFor(int era)
    {
        switch (era)
        {
            // 크기 0.07~0.10 / 알파 0.3에서는 화면에 아무것도 안 보였다(실측: 실제 알파 0.12, 6~11px).
            // 바닥 아트가 대비가 센 그림이라 배경 입자는 이 정도는 되어야 존재가 읽힌다.
            case 1:   // 중세 — 가라앉는 먼지. 느리고 조용하다
                return new Look
                {
                    color = new Color(0.88f, 0.85f, 0.78f),
                    drift = new Vector2(-0.35f, -0.42f), jitter = 0.18f,
                    size = 0.17f, alpha = 0.4f, blinkSpeed = 0f, sway = 0.12f,
                };

            case 2:   // 현대 — 비스듬히 튀는 스파크. 빠르고 명멸한다
                return new Look
                {
                    color = new Color(0.9f, 0.95f, 0.5f),
                    drift = new Vector2(1.5f, -1.1f), jitter = 0.5f,
                    size = 0.14f, alpha = 0.5f, blinkSpeed = 9f, sway = 0f,
                };

            case 3:   // 미래 — 위로 흐르는 데이터 조각. 플레이어(시안)와 겹치지 않게 옅은 청록
                return new Look
                {
                    color = new Color(0.5f, 0.88f, 0.98f),
                    drift = new Vector2(0.1f, 1.4f), jitter = 0.35f,
                    size = 0.18f, alpha = 0.48f, blinkSpeed = 6f, sway = 0.1f,
                };

            default:  // 원시 — 위로 떠오르는 불티. 흔들리며 올라간다
                return new Look
                {
                    color = new Color(1f, 0.62f, 0.28f),
                    drift = new Vector2(0.2f, 0.75f), jitter = 0.22f,
                    size = 0.18f, alpha = 0.5f, blinkSpeed = 3.5f, sway = 0.35f,
                };
        }
    }
}
