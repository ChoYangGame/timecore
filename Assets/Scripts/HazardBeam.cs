using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 예고 후 발사되는 직선 위험 구역. 시대별 패턴(원시 지면 균열 / 중세 화살 세례 /
/// 현대 십자 폭격 / 미래 레이저 격자)이 전부 이 하나를 각도·개수·굵기와 **스타일**만 바꿔 쓴다.
///
/// 처음에는 사각형 스프라이트 한 장에 색만 입혔는데, 그러면 넷 다 같은 막대라
/// 말 그대로 히트박스를 그려 놓은 것으로 보였다. 지금은 같은 내장 Square를
/// **여러 겹·여러 조각으로 쪼개서** 시대마다 다른 물건으로 읽히게 한다 —
/// 균열은 밝은 틈 위에 어두운 파편, 화살은 비스듬히 꽂히는 짧은 축, 폭격은 순차로 터지는 폭발,
/// 레이저는 넓은 글로우 + 흰 코어 + 끝단 캡. 새 스프라이트나 셰이더는 하나도 쓰지 않는다.
///
/// 조각은 Configure() 시점에 스타일이 필요한 개수만 만든다(레이저 5, 균열 9, 폭격 9, 화살 11).
/// 루트 SpriteRenderer는 스프라이트·정렬 순서의 템플릿으로만 쓰고 그리지 않는다 —
/// 루트에 스케일을 걸면 자식 조각이 전부 같이 늘어나기 때문이다.
///
/// 판정은 예전 그대로다. 콜라이더를 쓰지 않고 중심에서의 세로·가로 거리를 내적 2회로 직접 잰다.
/// 보이는 것이 화려해져도 맞는 범위는 예고 막대가 덮은 사각형 그대로여야 한다.
///
/// 부착 대상: HazardBeam 프리팹 (SpriteRenderer만. 콜라이더 없음)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class HazardBeam : MonoBehaviour
{
    public enum BeamStyle
    {
        /// <summary>원시 — 지면이 갈라진다. 밝은 틈 위에 어두운 파편이 어긋나게 얹힌다</summary>
        Fissure,
        /// <summary>중세 — 화살이 비스듬히 꽂힌다. 예고는 바닥 착탄 표시, 발사는 순차로 박히는 축</summary>
        Volley,
        /// <summary>현대 — 포격. 조준 사각형이 순서대로 터진다</summary>
        Bombardment,
        /// <summary>미래 — 레이저. 넓은 글로우 + 흰 코어 + 끝단 캡, 코어가 깜빡인다</summary>
        Laser,
    }

    public struct Spec
    {
        public Vector2 center;
        public float angleDeg;
        public float length;
        public float width;
        public Color color;
        public float warnDuration;
        public float fireDuration;
        public float playerDamage;
        public float enemyDamage;
        public BeamStyle style;
    }

    [Tooltip("예고 구간의 굵기 비율. 발사 순간 이 비율에서 1.0으로 튄다")]
    [SerializeField] private float warnWidthRatio = 0.22f;
    [SerializeField] private float warnAlpha = 0.35f;
    [SerializeField] private float fireAlpha = 0.85f;
    [Tooltip("발사가 끝난 뒤 사라지는 시간")]
    [SerializeField] private float fadeOutDuration = 0.35f;
    [Tooltip("예고 막대가 깜빡이는 속도. 정지한 반투명 막대는 배경으로 읽힌다")]
    [SerializeField] private float warnBlinkSpeed = 9f;

    /// <summary>감속 지대(-500)보다 위, 적·플레이어·탄(0)보다 아래.</summary>
    [SerializeField] private int sortingOrder = -400;

    private static readonly List<HazardBeam> ActiveBeams = new List<HazardBeam>();

    public static int ActiveCount => ActiveBeams.Count;

    /// <summary>지금 살아있는 빔들이 쓰고 있는 조각 renderer 총합. 저사양 프레임 실측용.</summary>
    public static int ActiveLayerCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < ActiveBeams.Count; i++)
                if (ActiveBeams[i] != null && ActiveBeams[i]._layers != null) n += ActiveBeams[i]._layers.Length;
            return n;
        }
    }

    private SpriteRenderer _template;
    private Spec _spec;
    private Vector2 _dir;
    private Vector2 _perp;
    private float _elapsed;
    private bool _configured;
    private bool _firedOnce;

    private readonly List<Health> _alreadyHit = new List<Health>();
    private Transform _player;

    // 조각. [0]은 항상 바탕 막대(예고선 겸 글로우), 나머지는 스타일마다 의미가 다르다.
    private Transform[] _layerTf;
    private SpriteRenderer[] _layers;
    private float _spriteW = 1f;
    private float _spriteH = 1f;

    // Configure 시점에 한 번 뽑아 고정하는 조각별 배치값. 매 프레임 다시 뽑으면 지직거린다.
    private float[] _segAlong;    // 빔 방향 위치 (-0.5 ~ 0.5 비율)
    private float[] _segPerp;     // 수직 방향 어긋남 (굵기 비율)
    private float[] _segLen;      // 길이 배율
    private float[] _segWid;      // 굵기 배율
    private float[] _segTilt;     // 추가 회전(도)
    private float[] _segDelay;    // 발사 구간 안에서의 등장 지연 비율 (0~1)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveBeams.Clear();

    /// <summary>시대 전환 암전 중 EraManager가 부른다.</summary>
    public static void ClearAll()
    {
        HazardBeam[] snapshot = ActiveBeams.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActiveBeams.Clear();
    }

    public void Configure(Spec spec)
    {
        _spec = spec;
        _configured = true;

        float rad = spec.angleDeg * Mathf.Deg2Rad;
        _dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        _perp = new Vector2(-_dir.y, _dir.x);

        // 루트는 스케일 1로 둔다. 여기에 스케일을 걸면 자식 조각이 전부 같이 늘어난다.
        transform.position = spec.center;
        transform.rotation = Quaternion.Euler(0f, 0f, spec.angleDeg);
        transform.localScale = Vector3.one;

        BuildLayers(ExtraLayerCount(spec.style) + 1);
        ApplyVisual();
    }

    private void Awake()
    {
        _template = GetComponent<SpriteRenderer>();

        // 루트는 템플릿으로만 쓴다. 실제로 보이는 건 전부 자식 조각이다.
        _template.enabled = false;

        if (_template.sprite != null)
        {
            Vector2 size = _template.sprite.bounds.size;
            if (size.x > 0.0001f) _spriteW = size.x;
            if (size.y > 0.0001f) _spriteH = size.y;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void OnEnable() => ActiveBeams.Add(this);

    private void OnDisable() => ActiveBeams.Remove(this);

    private static int ExtraLayerCount(BeamStyle style)
    {
        switch (style)
        {
            case BeamStyle.Laser: return 4;        // mid, core, cap x2
            case BeamStyle.Fissure: return 8;      // 파편
            case BeamStyle.Bombardment: return 8;  // 폭발
            case BeamStyle.Volley: return 10;      // 화살
            default: return 4;
        }
    }

    private void BuildLayers(int count)
    {
        _layerTf = new Transform[count];
        _layers = new SpriteRenderer[count];

        _segAlong = new float[count];
        _segPerp = new float[count];
        _segLen = new float[count];
        _segWid = new float[count];
        _segTilt = new float[count];
        _segDelay = new float[count];

        int extras = count - 1;

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Layer" + i);
            go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _template.sprite;
            // 뒤 조각일수록 위에 그린다. 레이저의 흰 코어가 글로우 위에 얹혀야 한다.
            sr.sortingOrder = sortingOrder + i;
            sr.enabled = false;

            _layerTf[i] = go.transform;
            _layers[i] = sr;

            if (i == 0) continue;

            int e = i - 1;
            // 조각을 빔 방향으로 균등하게 깔되 구간 안에서만 흔든다. 완전 랜덤이면 뭉친다.
            float slot = extras > 1 ? (e + 0.5f) / extras - 0.5f : 0f;
            _segAlong[i] = slot + Random.Range(-0.35f, 0.35f) / Mathf.Max(1, extras);
            _segDelay[i] = extras > 1 ? (float)e / (extras - 1) : 0f;

            switch (_spec.style)
            {
                case BeamStyle.Fissure:
                    // 파편을 가운데로 모으고 굵게 잡는다. 그래야 위아래로 밝은 바탕이 테두리처럼 남아
                    // "빛나는 틈 사이가 갈라졌다"로 읽힌다. 흩어 놓으면 그냥 얼룩덜룩한 막대가 된다.
                    _segPerp[i] = Random.Range(-0.14f, 0.14f);
                    // 1.0을 밑돌면 파편 사이가 벌어져 밝은 바탕이 큼직하게 드러난다 — 갈라진 틈이 아니라
                    // 얼룩진 띠로 보인다(실측). 반드시 겹치도록 1.0~1.5로 잡는다.
                    _segLen[i] = Random.Range(1f, 1.5f);
                    _segWid[i] = Random.Range(0.7f, 0.95f);
                    // 기울기를 크게 준다. ±8도로는 파편이 아니라 그냥 검은 사각형으로 보였다(실측).
                    _segTilt[i] = Random.Range(-15f, 15f);
                    break;

                case BeamStyle.Volley:
                    _segPerp[i] = Random.Range(-0.3f, 0.3f);
                    _segLen[i] = 1f;
                    _segWid[i] = 1f;
                    // 화살은 전부 같은 쪽에서 날아온다. 방향이 제각각이면 화살비로 안 읽힌다.
                    _segTilt[i] = 58f + Random.Range(-7f, 7f);
                    break;

                case BeamStyle.Bombardment:
                    _segPerp[i] = Random.Range(-0.18f, 0.18f);
                    _segLen[i] = 1f;
                    _segWid[i] = Random.Range(0.85f, 1.25f);
                    _segTilt[i] = Random.Range(0f, 45f);
                    break;

                default:   // Laser — 배치가 고정이라 흔들 것이 없다
                    _segAlong[i] = 0f;
                    _segLen[i] = 1f;
                    _segWid[i] = 1f;
                    break;
            }
        }
    }

    private void Update()
    {
        if (!_configured) return;

        _elapsed += Time.deltaTime;

        float endOfFire = _spec.warnDuration + _spec.fireDuration;
        if (_elapsed >= endOfFire + fadeOutDuration)
        {
            Destroy(gameObject);
            return;
        }

        if (_elapsed >= _spec.warnDuration && _elapsed < endOfFire)
        {
            // 발사로 넘어가는 첫 프레임에만 한 번 흔든다. 예고→발사의 "쿵"을 만드는 부분이다.
            if (!_firedOnce)
            {
                _firedOnce = true;
                PlayImpact();
            }
            ApplyDamage();
        }

        ApplyVisual();
    }

    /// <summary>발사 순간의 이펙트. 스타일마다 다른 소리를 내야 같은 막대로 안 읽힌다.</summary>
    private void PlayImpact()
    {
        Color bright = Color.Lerp(_spec.color, Color.white, 0.45f);
        Vector2 c = _spec.center;
        float half = _spec.length * 0.5f;

        switch (_spec.style)
        {
            case BeamStyle.Fissure:
                // 갈라진 틈에서 흙먼지가 위로 솟는다.
                CameraShake.Shake(0.45f, 0.18f);
                EffectSystem.Ring(c, bright, 10, 6f, 0.34f, 0.4f);
                EffectSystem.Spray(c + _dir * (half * 0.5f), _perp, _spec.color, 4, 60f, 5f, 0.3f, 0.4f);
                break;

            case BeamStyle.Volley:
                // 착탄. 흔들림은 약하게 — 화살은 지면을 흔들지 않는다.
                CameraShake.Shake(0.22f, 0.12f);
                EffectSystem.Spray(c, -_perp, bright, 5, 70f, 5f, 0.24f, 0.3f);
                EffectSystem.Spray(c - _dir * (half * 0.5f), -_perp, bright, 4, 70f, 5f, 0.24f, 0.3f);
                break;

            case BeamStyle.Bombardment:
                CameraShake.Shake(0.55f, 0.22f);
                EffectSystem.Ring(c, bright, 12, 9f, 0.36f, 0.35f);
                EffectSystem.Burst(c + _dir * (half * 0.45f), bright, 5, 7f, 0.32f, 0.35f);
                break;

            default:   // Laser — 흔들림보다 빛. 양 끝에서 링이 번진다.
                CameraShake.Shake(0.3f, 0.14f);
                EffectSystem.Ring(c + _dir * half, bright, 8, 8f, 0.26f, 0.3f);
                EffectSystem.Ring(c - _dir * half, bright, 8, 8f, 0.26f, 0.3f);
                break;
        }
    }

    /// <summary>발사 구간 동안 매 프레임 판정하되 같은 대상은 1회만 맞는다.</summary>
    private void ApplyDamage()
    {
        if (_spec.playerDamage > 0f && _player != null)
            TryHit(_player.GetComponent<Health>(), _spec.playerDamage);

        // 적 피해가 0이면 적을 훑지 않는다. 기본값은 0 — 레이저가 웨이브를 대신 치워버리면
        // 난이도 곡선이 통째로 무너진다.
        if (_spec.enemyDamage > 0f)
        {
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                TryHit(e.GetComponent<Health>(), _spec.enemyDamage);
        }
    }

    private void TryHit(Health target, float damage)
    {
        if (target == null || target.IsDead) return;
        if (!Contains(target.transform.position)) return;
        if (_alreadyHit.Contains(target)) return;

        _alreadyHit.Add(target);
        target.TakeDamage(damage);
    }

    /// <summary>회전한 막대 안인지. 예고 막대가 덮은 사각형과 정확히 같은 식이다.</summary>
    public bool Contains(Vector2 worldPos)
    {
        Vector2 d = worldPos - _spec.center;
        if (Mathf.Abs(Vector2.Dot(d, _dir)) > _spec.length * 0.5f) return false;
        if (Mathf.Abs(Vector2.Dot(d, _perp)) > _spec.width * 0.5f) return false;
        return true;
    }

    /// <summary>지금 발사 구간인지. 판정이 실제로 걸리는 구간을 밖에서 확인할 때 쓴다.</summary>
    public bool IsFiring => _configured
        && _elapsed >= _spec.warnDuration
        && _elapsed < _spec.warnDuration + _spec.fireDuration;

    // ────────────────────────────── 그리기 ──────────────────────────────

    private void ApplyVisual()
    {
        if (_layers == null || _layers.Length == 0) return;

        float endOfFire = _spec.warnDuration + _spec.fireDuration;

        if (_elapsed < _spec.warnDuration)
        {
            DrawWarn();
            return;
        }

        // 발사가 끝나면 통째로 알파만 빼면서 같은 그림을 유지한다.
        float fade = _elapsed < endOfFire
            ? 1f
            : Mathf.Max(0f, 1f - (_elapsed - endOfFire) / Mathf.Max(0.0001f, fadeOutDuration));

        float fireT = Mathf.Clamp01((_elapsed - _spec.warnDuration) / Mathf.Max(0.0001f, _spec.fireDuration));

        switch (_spec.style)
        {
            case BeamStyle.Fissure: DrawFissure(fireT, fade); break;
            case BeamStyle.Volley: DrawVolley(fireT, fade); break;
            case BeamStyle.Bombardment: DrawBombardment(fireT, fade); break;
            default: DrawLaser(fireT, fade); break;
        }
    }

    /// <summary>
    /// 예고. 스타일이 뭐든 "여기가 위험하다"가 1순위라, 덮을 사각형을 얇게 그대로 보여준다.
    /// 다만 예고 단계에서도 스타일이 보이도록 조각을 살짝 얹는다(레이저만 선 하나로 둔다).
    /// </summary>
    private void DrawWarn()
    {
        float blink = 0.775f + 0.225f * Mathf.Sin(_elapsed * warnBlinkSpeed);
        Color c = _spec.color;

        float bandWidth = _spec.style == BeamStyle.Laser
            ? _spec.width * 0.1f     // 레이저 예고는 실 같은 선 하나
            : _spec.width * warnWidthRatio;

        SetLayer(0, 0f, 0f, _spec.length, bandWidth, 0f,
            _spec.style == BeamStyle.Laser ? Color.Lerp(c, Color.white, 0.5f) : c,
            warnAlpha * blink * (_spec.style == BeamStyle.Laser ? 1.6f : 1f));

        // 착탄 예고 표시. 어디에 꽂히는지/터지는지를 미리 점으로 보여준다.
        bool marks = _spec.style == BeamStyle.Volley || _spec.style == BeamStyle.Bombardment;

        for (int i = 1; i < _layers.Length; i++)
        {
            if (!marks)
            {
                _layers[i].enabled = false;
                continue;
            }

            float mark = _spec.width * 0.34f;
            SetLayer(i, _segAlong[i], _segPerp[i], mark, mark, _segTilt[i] * 0.5f,
                c, warnAlpha * blink * 0.9f);
        }
    }

    /// <summary>원시 — 밝은 틈(바탕) 위에 어두운 파편을 어긋나게 얹어 갈라진 지면으로 읽히게 한다.</summary>
    private void DrawFissure(float fireT, float fade)
    {
        // 벌어지는 느낌: 파편이 처음 20% 동안 바깥으로 밀려난다.
        float open = Mathf.Min(1f, fireT / 0.2f);

        // 바탕은 "달궈진 틈". 시대 색 자체가 이미 밝아서 흰색을 섞을수록 채도만 빠진다 —
        // 0.35에서도 살구색 띠가 됐다(실측). 거의 원색으로 두고 파편의 어둠으로 대비를 만든다.
        SetLayer(0, 0f, 0f, _spec.length, _spec.width, 0f,
            Color.Lerp(_spec.color, Color.white, 0.12f), 0.95f * fade);

        for (int i = 1; i < _layers.Length; i++)
        {
            SetLayer(i,
                _segAlong[i],
                _segPerp[i] * open,
                _spec.length / (_layers.Length - 1) * _segLen[i],
                _spec.width * _segWid[i] * (0.35f + 0.65f * open),
                _segTilt[i],
                // 0.72면 흙바닥과 같은 갈색이 되어 안 보이고, 0.9면 새까만 블록이 된다. 0.8이 그 사이다(실측).
                Color.Lerp(_spec.color, Color.black, 0.8f),
                0.95f * fade);
        }
    }

    /// <summary>중세 — 짧은 축이 비스듬히, 빔을 따라 순서대로 꽂힌다.</summary>
    private void DrawVolley(float fireT, float fade)
    {
        // 바탕은 착탄 띠 정도로만 남긴다. 화살이 주인공이다.
        SetLayer(0, 0f, 0f, _spec.length, _spec.width * 0.5f, 0f,
            Color.Lerp(_spec.color, Color.black, 0.35f), fireAlpha * 0.45f * fade);

        for (int i = 1; i < _layers.Length; i++)
        {
            // 앞쪽 화살부터 꽂힌다. 전부 동시에 나오면 빗줄기가 아니라 벽으로 보인다.
            float appear = _segDelay[i] * 0.4f;
            float t = Mathf.Clamp01((fireT - appear) / 0.18f);

            if (t <= 0f)
            {
                _layers[i].enabled = false;
                continue;
            }

            // 꽂히는 순간 길게 튀었다가 제 길이로 앉는다.
            float stab = Mathf.Lerp(1.5f, 1f, t);

            SetLayer(i,
                _segAlong[i],
                _segPerp[i],
                // 판정 폭(width)보다 길면 화살이 띠 밖으로 삐져나온다. 위험을 과장하는 쪽이라
                // 억울한 죽음은 안 나지만, 너무 벌어지지 않게 1.0으로 잡는다.
                _spec.width * stab,
                _spec.width * 0.15f,
                _segTilt[i],
                Color.Lerp(_spec.color, Color.white, 0.35f),
                0.95f * fade);
        }
    }

    /// <summary>현대 — 조준 사각형이 빔을 따라 순서대로 터진다.</summary>
    private void DrawBombardment(float fireT, float fade)
    {
        SetLayer(0, 0f, 0f, _spec.length, _spec.width, 0f,
            Color.Lerp(_spec.color, Color.black, 0.25f), fireAlpha * 0.22f * fade);

        for (int i = 1; i < _layers.Length; i++)
        {
            float appear = _segDelay[i] * 0.5f;
            float t = (fireT - appear) / 0.35f;

            if (t <= 0f || t >= 1f)
            {
                _layers[i].enabled = false;
                continue;
            }

            // 거의 즉시 최대 크기까지 커진 뒤 마지막에 빠르게 꺼진다.
            // sqrt(t) + (1-t)로 두면 "작을 때 제일 진하고 클 때 제일 옅은" 역방향이라
            // 화면에는 크고 흐린 사각형만 남았다(실측). 터진 순간이 가장 진해야 한다.
            float grow = Mathf.Pow(t, 0.35f);
            // 1.35배는 판정 폭(1.8)보다 훨씬 커서 위험 범위를 과장했다. 띠와 비슷하게 맞춘다.
            float size = _spec.width * 1.05f * _segWid[i] * grow;

            SetLayer(i, _segAlong[i], _segPerp[i], size, size, _segTilt[i],
                Color.Lerp(_spec.color, Color.white, 0.12f),
                (1f - t * t * t) * fade);
        }
    }

    /// <summary>미래 — 넓은 글로우 + 중간층 + 흰 코어 + 양 끝 캡. 코어가 빠르게 깜빡인다.</summary>
    private void DrawLaser(float fireT, float fade)
    {
        // 처음 12%에 확 벌어졌다가 제 굵기로 앉는다. "발사됐다"는 순간을 만드는 부분.
        float punch = fireT < 0.12f ? Mathf.Lerp(1.6f, 1f, fireT / 0.12f) : 1f;
        float flicker = 0.85f + 0.15f * Mathf.Sin(_elapsed * 55f);

        // 알파는 실측으로 올린 값이다. 0.32/0.65로는 흙바닥·아스팔트 같은 중간 톤 배경 위에서
        // 글로우가 통째로 씻겨 나가 얇은 선 하나로만 보였다.

        // 0: 바깥 글로우
        SetLayer(0, 0f, 0f, _spec.length, _spec.width * punch, 0f,
            _spec.color, 0.5f * fade);

        // 1: 중간층
        SetLayer(1, 0f, 0f, _spec.length, _spec.width * 0.5f * punch, 0f,
            Color.Lerp(_spec.color, Color.white, 0.4f), 0.85f * fade);

        // 2: 흰 코어 — 이 한 겹이 "레이저"를 만든다
        SetLayer(2, 0f, 0f, _spec.length, _spec.width * 0.24f * punch * flicker, 0f,
            Color.Lerp(_spec.color, Color.white, 0.9f), fade);

        // 3, 4: 끝단 캡. along 0.5 = 길이의 절반이라 정확히 끝단에 앉는다.
        // 45도 돌려 마름모로 보이게 하면 같은 사각형인데도 캡으로 읽힌다.
        float cap = _spec.width * 0.85f * punch;
        Color capColor = Color.Lerp(_spec.color, Color.white, 0.7f);
        SetLayer(3, 0.5f, 0f, cap, cap, 45f, capColor, 0.8f * fade);
        SetLayer(4, -0.5f, 0f, cap, cap, 45f, capColor, 0.8f * fade);
    }

    /// <summary>
    /// 조각 하나 배치. along은 빔 길이 대비 비율(-0.5~0.5), perp는 굵기 대비 비율이다.
    /// 루트가 이미 angleDeg로 돌아 있으므로 여기서는 전부 로컬 좌표로 둔다.
    /// </summary>
    private void SetLayer(int index, float along, float perp,
        float lengthWorld, float widthWorld, float tiltDeg, Color color, float alpha)
    {
        if (_layers == null || index < 0 || index >= _layers.Length) return;

        Transform t = _layerTf[index];
        t.localPosition = new Vector3(along * _spec.length, perp * _spec.width, 0f);
        t.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
        t.localScale = new Vector3(
            Mathf.Max(0.0001f, lengthWorld) / _spriteW,
            Mathf.Max(0.0001f, widthWorld) / _spriteH,
            1f);

        color.a = Mathf.Clamp01(alpha);
        _layers[index].color = color;
        _layers[index].enabled = alpha > 0.003f;
    }
}
