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
    private float[] _segLen;      // 길이. 균열은 월드 길이, 나머지는 배율
    private float[] _segWid;      // 굵기 배율
    private float[] _segTilt;     // 추가 회전(도)
    private float[] _segDelay;    // 발사 구간 안에서의 등장 지연 비율 (0~1)
    private bool[] _segFired;     // 이 마디가 처음 터진 순간에 이펙트를 1회만 내보내려는 것

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

        // 루트는 정렬 순서 템플릿으로만 쓴다. 실제로 보이는 건 전부 자식 조각이다.
        _template.enabled = false;

        // 조각 스프라이트는 FxTextures가 코드로 구운 것을 쓴다(전부 월드 bounds 1x1).
        // 프리팹의 내장 Square를 늘려 쓰던 동안에는 위아래 경계가 칼같이 서서
        // 빛이 아니라 히트박스 사각형으로 보였다.
        _spriteW = 1f;
        _spriteH = 1f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void OnEnable() => ActiveBeams.Add(this);

    private void OnDisable() => ActiveBeams.Remove(this);

    private static int ExtraLayerCount(BeamStyle style)
    {
        switch (style)
        {
            case BeamStyle.Laser: return 5;        // 글로우, 중간, 코어, 캡 x2 (+ 0번 헤일로)
            case BeamStyle.Fissure: return 8;      // 지그재그로 이어지는 균열 마디
            case BeamStyle.Bombardment: return 8;  // 순서대로 터지는 폭발
            case BeamStyle.Volley: return 5;       // 굵은 창. 10개 얇은 화살보다 "콱" 꽂힌다
            default: return 4;
        }
    }

    /// <summary>
    /// 조각 index가 쓸 모양. 층마다 다른 것을 쓰는 게 핵심이다 —
    /// 길게 늘이는 층은 세로로 부드럽게 빠지는 띠, 터지는 층은 원, 파편은 각진 사각형.
    /// index 0은 어느 스타일이든 "바탕 띠"라 항상 GlowBar다.
    /// </summary>
    private Sprite LayerShape(int index)
    {
        if (index == 0) return FxTextures.GlowBar;

        switch (_spec.style)
        {
            // 지면 파편은 각이 살아 있어야 갈라진 돌로 읽힌다.
            case BeamStyle.Fissure: return FxTextures.SoftSquare;

            // 창은 촉이 있어야 "찌른다"로 읽힌다. 막대로는 안 된다.
            case BeamStyle.Volley: return FxTextures.Spear;

            // 폭발은 둥글어야 한다. 사각형이 커졌다 작아지면 그냥 사각형이다.
            case BeamStyle.Bombardment: return FxTextures.Dot;

            // 레이저: 0~3은 헤일로/글로우/중간/코어(늘이는 층), 4~5는 끝단 캡(둥근 점).
            default: return index <= 3 ? FxTextures.GlowBar : FxTextures.Dot;
        }
    }

    /// <summary>
    /// 균열 마디를 지그재그로 **이어서** 배치한다.
    ///
    /// 예전에는 마디를 각자 랜덤 위치·랜덤 기울기로 흩뿌렸는데, 그러면 갈라진 틈이 아니라
    /// 얼룩덜룩한 막대로 보였다. 실제 균열은 한 줄로 이어지면서 좌우로 꺾인다.
    /// 그래서 빔 위에 마디+1개의 꼭짓점을 위아래로 번갈아 찍고, 마디 e가 꼭짓점 e와 e+1을 잇게 한다 —
    /// 각 마디의 길이와 기울기가 두 점 사이 거리·각도로 정해지므로 선이 끊기지 않는다.
    /// </summary>
    private void BuildFissureNodes(int extras)
    {
        if (extras < 1) return;

        // 꼭짓점의 수직 위치(굵기 비율). 위아래로 번갈아 꺾되 크기는 조금씩 다르게.
        float[] nodePerp = new float[extras + 1];
        for (int j = 0; j <= extras; j++)
        {
            float side = (j % 2 == 0) ? 1f : -1f;
            // 진폭이 크면 균열이 판정 띠 밖으로 크게 벗어난다. 0.34까지 흔드니 눈에 띄게 삐져나왔다(실측).
            nodePerp[j] = side * Random.Range(0.14f, 0.27f);
        }

        // 양 끝은 가운데로 모아 균열이 뾰족하게 시작·끝나게 한다.
        nodePerp[0] *= 0.25f;
        nodePerp[extras] *= 0.25f;

        for (int e = 0; e < extras; e++)
        {
            int i = e + 1;

            float alongA = -0.5f + (float)e / extras;
            float alongB = -0.5f + (float)(e + 1) / extras;

            // 월드 단위로 환산해 두 점 사이의 실제 길이·각도를 낸다.
            float dAlong = (alongB - alongA) * _spec.length;
            float dPerp = (nodePerp[e + 1] - nodePerp[e]) * _spec.width;

            _segAlong[i] = (alongA + alongB) * 0.5f;
            _segPerp[i] = (nodePerp[e] + nodePerp[e + 1]) * 0.5f;

            // 1.06을 곱해 마디끼리 살짝 겹치게 한다. 딱 맞추면 이음매에 틈이 보인다.
            _segLen[i] = Mathf.Sqrt(dAlong * dAlong + dPerp * dPerp) * 1.06f;
            _segTilt[i] = Mathf.Atan2(dPerp, dAlong) * Mathf.Rad2Deg;
            _segWid[i] = Random.Range(0.24f, 0.38f);
            _segDelay[i] = (float)e / Mathf.Max(1, extras - 1);
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
        _segFired = new bool[count];

        int extras = count - 1;

        // 균열은 마디끼리 이어져야 해서 배치를 따로 계산한다.
        if (_spec.style == BeamStyle.Fissure) BuildFissureNodes(extras);

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Layer" + i);
            go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LayerShape(i);
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
                    break;   // BuildFissureNodes가 이미 채웠다. 여기서 덮어쓰면 지그재그가 끊긴다

                case BeamStyle.Volley:
                    // 창은 전부 같은 쪽 위에서 내리꽂힌다. 방향이 제각각이면 찌르기로 안 읽힌다.
                    // 촉(스프라이트 +X)이 아래를 향하도록 음수 각도를 준다.
                    _segPerp[i] = 0.38f;
                    _segLen[i] = 1f;
                    _segWid[i] = 1f;
                    _segTilt[i] = -68f + Random.Range(-6f, 6f);
                    break;

                case BeamStyle.Bombardment:
                    // 위치를 흔들지 않는다. "끝에서부터 순서대로" 터지려면
                    // 인덱스 순서와 빔 위의 순서가 정확히 같아야 한다.
                    _segAlong[i] = slot;
                    _segPerp[i] = Random.Range(-0.12f, 0.12f);
                    _segLen[i] = 1f;
                    _segWid[i] = Random.Range(0.85f, 1.2f);
                    _segTilt[i] = 0f;
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
                // 땅이 쩍 갈라지는 첫 충격. 흙먼지는 마디가 열릴 때마다 따로 튄다(DrawFissure).
                CameraShake.Shake(0.5f, 0.2f);
                EffectSystem.Ring(c, bright, 1f, 5.5f, 0.4f);
                break;

            case BeamStyle.Volley:
                // 창이 박히는 순간. 흔들림은 짧고 날카롭게.
                CameraShake.Shake(0.35f, 0.1f);
                EffectSystem.Ring(c, bright, 0.8f, 3.4f, 0.25f);
                break;

            case BeamStyle.Bombardment:
                // 여기서 크게 터뜨리지 않는다. 연쇄가 주인공이라 첫 방이 세면 순서가 안 읽힌다.
                // 폭발 하나하나의 이펙트·흔들림은 DrawBombardment가 낸다.
                break;

            default:   // Laser — 흔들림보다 빛. 양 끝에서 링이 크게 번지고 화면이 한 번 밝아진다.
                CameraShake.Shake(0.4f, 0.16f);

                EffectSystem.Ring(c + _dir * half, bright, 0.6f, 4.5f, 0.32f);
                EffectSystem.Ring(c - _dir * half, bright, 0.6f, 4.5f, 0.32f);

                // 발사구 섬광. 링이 번지는 동안 끝단이 하얗게 남는다.
                EffectSystem.Linger(c + _dir * half, Color.Lerp(_spec.color, Color.white, 0.85f), _spec.width * 1.4f, 0.22f);
                EffectSystem.Linger(c - _dir * half, Color.Lerp(_spec.color, Color.white, 0.85f), _spec.width * 1.4f, 0.22f);

                // 화면 전체가 아주 짧게 밝아진다. 격자로 4줄이 겹쳐도 ScreenFlash가 센 것만 남긴다.
                ScreenFlash.Full(Color.Lerp(_spec.color, Color.white, 0.55f), 0.13f, 0.16f);
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

    /// <summary>
    /// 원시 — 땅이 갈라진다. 달궈진 바탕 위로 **한 줄로 이어진 지그재그 틈**이 벌어진다.
    /// 틈은 끝에서부터 순서대로 열리고, 열리는 마디마다 흙을 튀긴다.
    /// </summary>
    private void DrawFissure(float fireT, float fade)
    {
        // 바탕은 "달궈진 지면". 시대 색 자체가 이미 밝아서 흰색을 섞을수록 채도만 빠진다 —
        // 0.35에서도 살구색 띠가 됐다(실측). 거의 원색으로 두고 틈의 어둠으로 대비를 만든다.
        SetLayer(0, 0f, 0f, _spec.length, _spec.width, 0f,
            Color.Lerp(_spec.color, Color.white, 0.12f), fade);

        int extras = _layers.Length - 1;

        for (int i = 1; i < _layers.Length; i++)
        {
            // 마디가 앞에서부터 차례로 갈라진다. 전부 동시에 열면 "쩍" 하는 방향감이 사라진다.
            float openAt = _segDelay[i] * 0.45f;
            float open = Mathf.Clamp01((fireT - openAt) / 0.18f);

            if (open <= 0f)
            {
                _layers[i].enabled = false;
                continue;
            }

            // 갈라지는 순간 흙을 튀긴다. 마디마다 1회만.
            if (!_segFired[i])
            {
                _segFired[i] = true;
                Vector2 at = SegmentWorldPos(i);
                EffectSystem.Spray(at, _perp, Color.Lerp(_spec.color, Color.black, 0.45f),
                    3, 70f, 4.5f, 0.26f, 0.35f);
            }

            SetLayer(i,
                _segAlong[i],
                _segPerp[i] * open,
                _segLen[i],
                // 벌어지는 느낌: 처음엔 실금이었다가 제 굵기로 열린다.
                _spec.width * _segWid[i] * (0.2f + 0.8f * open),
                _segTilt[i],
                // 0.72면 흙바닥과 같은 갈색이라 안 보이고, 0.9면 새까만 블록이 된다. 0.85가 그 사이다.
                Color.Lerp(_spec.color, Color.black, 0.85f),
                fade);
        }
    }

    /// <summary>조각 i의 월드 좌표. 루트가 이미 회전해 있으므로 로컬 → 월드 변환 한 번이면 된다.</summary>
    private Vector2 SegmentWorldPos(int i)
    {
        return transform.TransformPoint(
            new Vector3(_segAlong[i] * _spec.length, _segPerp[i] * _spec.width, 0f));
    }

    /// <summary>
    /// 중세 — 굵은 창이 위에서 **콱** 내리꽂힌다.
    /// 얇은 화살 10개를 순차로 흘리던 것을 5자루로 줄이고, 거의 동시에 짧고 세게 박히게 바꿨다 —
    /// 개수가 많고 느리면 "빗줄기"가 되지 "찌르기"가 되지 않는다.
    /// </summary>
    private void DrawVolley(float fireT, float fade)
    {
        // 바탕은 착탄 띠 정도로만 남긴다. 창이 주인공이다.
        SetLayer(0, 0f, 0f, _spec.length, _spec.width * 0.55f, 0f,
            Color.Lerp(_spec.color, Color.black, 0.3f), fireAlpha * 0.4f * fade);

        for (int i = 1; i < _layers.Length; i++)
        {
            // 시간차를 아주 짧게(전체의 12%) 둔다. 완전 동시면 벽처럼 보이고, 길면 힘이 빠진다.
            float appear = _segDelay[i] * 0.12f;
            float t = Mathf.Clamp01((fireT - appear) / 0.09f);

            if (t <= 0f)
            {
                _layers[i].enabled = false;
                continue;
            }

            if (!_segFired[i])
            {
                _segFired[i] = true;
                // 촉이 박히는 지점에서 파편이 튄다.
                Vector2 tip = SegmentWorldPos(i) - _perp * (_spec.width * 0.5f);
                EffectSystem.Spray(tip, -_perp, Color.Lerp(_spec.color, Color.white, 0.5f),
                    4, 60f, 6f, 0.24f, 0.28f);
            }

            // 길게 뻗었다가 순식간에 제 길이로 박힌다. 이 낙차가 "콱"이다.
            float stab = Mathf.Lerp(1.8f, 1f, t);

            SetLayer(i,
                _segAlong[i],
                _segPerp[i],
                // 2.3배로 두니 창끝이 판정 띠 밖으로 1.5유닛 넘게 삐져나왔다(실측).
                // 위험을 과장하는 쪽이라 억울한 죽음은 안 나지만, 띠와 창의 관계가 안 읽힌다.
                _spec.width * 1.5f * stab,
                _spec.width * 0.3f,
                _segTilt[i],
                Color.Lerp(_spec.color, Color.white, 0.45f),
                fade);
        }
    }

    /// <summary>
    /// 현대 — 한쪽 끝에서부터 **펑, 펑, 펑** 순서대로 터진다.
    /// 예전에는 시간차가 전체의 50%뿐이고 폭발 하나가 35%나 살아 있어서 거의 전부 겹쳐 보였다.
    /// 지금은 시간차를 85%로 늘리고 폭발 수명을 22%로 줄여 연쇄가 눈으로 세어진다.
    /// </summary>
    private void DrawBombardment(float fireT, float fade)
    {
        // 연쇄 사이사이에도 "여기가 위험 구역"이 남아 있어야 한다. 0.2로는 거의 안 보였다(실측).
        SetLayer(0, 0f, 0f, _spec.length, _spec.width, 0f,
            Color.Lerp(_spec.color, Color.black, 0.25f), fireAlpha * 0.32f * fade);

        for (int i = 1; i < _layers.Length; i++)
        {
            float appear = _segDelay[i] * 0.85f;
            float t = (fireT - appear) / 0.22f;

            if (t <= 0f || t >= 1f)
            {
                _layers[i].enabled = false;
                continue;
            }

            // 터지는 순간 이펙트와 흔들림을 같이 낸다. 이게 "펑" 소리를 대신한다.
            if (!_segFired[i])
            {
                _segFired[i] = true;
                Vector2 at = SegmentWorldPos(i);
                EffectSystem.Burst(at, Color.Lerp(_spec.color, Color.white, 0.35f), 4, 6f, 0.26f, 0.28f);
                CameraShake.Shake(0.22f, 0.09f);
            }

            // 거의 즉시 최대 크기까지 커진 뒤 마지막에 빠르게 꺼진다.
            // sqrt(t) + (1-t)로 두면 "작을 때 제일 진하고 클 때 제일 옅은" 역방향이라
            // 화면에는 크고 흐린 사각형만 남았다(실측). 터진 순간이 가장 진해야 한다.
            float grow = Mathf.Pow(t, 0.35f);
            float size = _spec.width * 1.25f * _segWid[i] * grow;

            // 폭발은 순간적으로 하얗게 달아올라야 "펑"으로 읽힌다. 터진 직후 가장 희고 빠르게 원래 색으로.
            Color hot = Color.Lerp(Color.white, _spec.color, Mathf.Clamp01(t * 2.2f));

            SetLayer(i, _segAlong[i], _segPerp[i], size, size, _segTilt[i],
                hot, (1f - t * t * t) * fade);
        }
    }

    /// <summary>
    /// 미래 — 헤일로 + 글로우 + 중간층 + 면도날 같은 흰 코어 + 양 끝 캡. 네 겹을 쌓아 광량을 만든다.
    /// 한 겹만 밝게 해서는 "센 레이저"가 안 된다. 넓고 옅은 층이 뒤를 받쳐야 빛이 번지는 것처럼 보인다.
    /// </summary>
    private void DrawLaser(float fireT, float fade)
    {
        // 처음 8%에 확 벌어졌다가 제 굵기로 앉는다. 짧고 클수록 "쏘였다"가 강해진다.
        float punch = fireT < 0.08f ? Mathf.Lerp(2.2f, 1f, fireT / 0.08f) : 1f;

        // 코어는 두 주기로 떨어 전기적인 불안정함을 준다.
        float flicker = 0.82f + 0.12f * Mathf.Sin(_elapsed * 55f) + 0.06f * Mathf.Sin(_elapsed * 137f);

        // 0: 헤일로 — 판정보다 넓지만 거의 투명하다. 빛이 번지는 범위지 맞는 범위가 아니다.
        // 1.9배 / 0.2로 두니 격자 4줄의 헤일로가 겹쳐 바닥 전체가 보라색으로 물들었고,
        // 어디까지가 맞는 범위인지 읽히지 않았다(실측). 피하는 게임에서는 그게 제일 나쁘다.
        SetLayer(0, 0f, 0f, _spec.length, _spec.width * 1.55f * punch, 0f,
            _spec.color, 0.14f * fade);

        // 1: 바깥 글로우 (판정 폭과 같다)
        SetLayer(1, 0f, 0f, _spec.length, _spec.width * punch, 0f,
            _spec.color, 0.55f * fade);

        // 2: 중간층
        SetLayer(2, 0f, 0f, _spec.length, _spec.width * 0.45f * punch, 0f,
            Color.Lerp(_spec.color, Color.white, 0.5f), 0.9f * fade);

        // 3: 면도날 코어 — 얇을수록 날카로워 보인다. 0.24는 두꺼워서 그냥 밝은 띠였다.
        SetLayer(3, 0f, 0f, _spec.length, _spec.width * 0.15f * punch * flicker, 0f,
            Color.Lerp(_spec.color, Color.white, 0.95f), fade);

        // 4, 5: 끝단 캡(둥근 점). along ±0.5가 정확히 끝단이다.
        float cap = _spec.width * 1.1f * punch;
        Color capColor = Color.Lerp(_spec.color, Color.white, 0.75f);
        SetLayer(4, 0.5f, 0f, cap, cap, 0f, capColor, 0.85f * fade);
        SetLayer(5, -0.5f, 0f, cap, cap, 0f, capColor, 0.85f * fade);
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
