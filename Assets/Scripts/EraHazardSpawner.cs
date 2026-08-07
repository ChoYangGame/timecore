using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 시대별 회피 패턴을 주기적으로 발동한다. 패턴은 전부 HazardBeam(예고 후 발사되는 직선) 하나를
/// 배치·개수·굵기·예고시간만 바꿔 조합한 것이다 — 시대마다 다른 코드를 쓰면 D-3에 4배로 터진다.
///
/// 원시 지면 균열(대각 1줄, 예고 길게) / 중세 화살 세례(평행 3줄) /
/// 현대 십자 폭격(직교 2줄) / 미래 레이저 격자(가로세로 4줄, 예고 짧게)
/// 뒤 시대일수록 줄이 많고 예고가 짧아 회피 난이도가 올라간다.
///
/// 웨이브 중에만 돈다. 보스전·시대 전환·타이틀 대기에서는 쉰다 — 보스 패턴 위에 레이저까지
/// 겹치면 피할 수 없는 죽음이 나온다. RiftZoneSpawner와 같은 신호(EnemySpawner.SpawningEnabled)를 쓴다.
///
/// 부착 대상: EraHazardSpawner (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class EraHazardSpawner : MonoBehaviour
{
    public enum Arrangement
    {
        /// <summary>무작위 각도 1줄씩. 아무 방향에서나 온다</summary>
        RandomLines,
        /// <summary>한 축을 골라 나란히. 한쪽으로 몰아 피하게 만든다</summary>
        Parallel,
        /// <summary>직교 2줄. 교차점을 피해야 한다</summary>
        Cross,
        /// <summary>가로·세로를 반씩. 격자 사이 빈칸으로 들어가야 한다</summary>
        Grid,
    }

    [Serializable]
    public class PatternConfig
    {
        public string label = "지면 균열";
        public Arrangement arrangement = Arrangement.RandomLines;

        [Tooltip("한 번에 나오는 줄 수")]
        public int beamCount = 1;

        [Tooltip("예고 시간(초). 짧을수록 어렵다")]
        public float warnDuration = 1.6f;

        [Tooltip("판정이 걸려 있는 시간(초)")]
        public float fireDuration = 0.45f;

        [Tooltip("줄 굵기(월드 유닛). 플레이어가 약 1유닛이다")]
        public float beamWidth = 2.2f;

        [Tooltip("맞았을 때 플레이어가 받는 피해")]
        public float playerDamage = 14f;

        [Tooltip("적이 받는 피해. 0이면 적은 훑지도 않는다 — 기본값 0")]
        public float enemyDamage = 0f;

        [Tooltip("패턴 사이 간격(초)")]
        public float interval = 12f;

        [Tooltip("줄과 줄 사이 최소 간격(월드 유닛). 붙어 나오면 피할 틈이 없다")]
        public float minGap = 3f;

        [Header("추적 장판 — 0이면 이 시대에는 안 나온다")]
        public int homingCount = 0;
        public float homingInterval = 16f;
        [Tooltip("따라다니는 시간. 이 동안 계속 움직여야 한다")]
        public float homingTrackDuration = 1.5f;
        [Tooltip("굳은 뒤 터지기까지. 반대로 꺾을 시간을 준다")]
        public float homingLockDuration = 0.7f;
        public float homingFireDuration = 0.35f;
        public float homingSize = 3.4f;
        [Tooltip("따라오는 속도. 플레이어 기본 이동속도는 6이다 — 이보다 느려야 떼어낼 수 있다")]
        public float homingTrackSpeed = 3.6f;
        public float homingDamage = 15f;

        [Header("분출구 — 0이면 이 시대에는 안 나온다")]
        public int ventCount = 0;
        public float ventInterval = 18f;
        public float ventWarnDuration = 1.2f;
        [Tooltip("분출이 지속되는 시간")]
        public float ventLifetime = 6f;
        public float ventBurstInterval = 1.5f;
        [Tooltip("한 번에 뿜는 탄 수. 저사양 브라우저에서는 이 값이 곧 프레임이다")]
        public int ventBurstCount = 6;
        [Tooltip("분출마다 각도를 이만큼 돌린다. 같은 틈에 계속 서 있지 못하게 한다")]
        public float ventSpinPerBurst = 12f;
        public float ventSize = 1.6f;
    }

    [Header("참조")]
    [SerializeField] private HazardBeam beamPrefab;
    [SerializeField] private HomingHazard homingPrefab;
    [SerializeField] private RiftVent ventPrefab;
    [Tooltip("분출구가 뿜는 탄. 보스 충격파와 같은 프리팹을 재사용한다")]
    [SerializeField] private BossProjectile ventProjectilePrefab;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EraManager eraManager;

    [Header("시대별 패턴 — 배열 인덱스 = EraManager.Era enum 값")]
    [SerializeField]
    private PatternConfig[] patterns =
    {
        // 시대가 갈수록 종류가 하나씩 늘어난다. 원시는 직선 하나만, 미래는 셋 다 나온다.
        new PatternConfig
        {
            label = "지면 균열",
            arrangement = Arrangement.RandomLines,
            beamCount = 1, warnDuration = 1.6f, fireDuration = 0.5f,
            beamWidth = 2.4f, playerDamage = 12f, interval = 13f,
            homingCount = 0,
            ventCount = 0,
        },
        new PatternConfig
        {
            label = "화살 세례",
            arrangement = Arrangement.Parallel,
            beamCount = 3, warnDuration = 1.4f, fireDuration = 0.45f,
            beamWidth = 1.7f, playerDamage = 14f, interval = 12f,
            homingCount = 1, homingInterval = 17f, homingDamage = 14f,
            ventCount = 0,
        },
        new PatternConfig
        {
            label = "십자 폭격",
            arrangement = Arrangement.Cross,
            beamCount = 2, warnDuration = 1.2f, fireDuration = 0.45f,
            beamWidth = 1.9f, playerDamage = 16f, interval = 11f,
            homingCount = 1, homingInterval = 15f, homingDamage = 16f,
            ventCount = 1, ventInterval = 19f, ventBurstCount = 6, ventLifetime = 6f,
        },
        new PatternConfig
        {
            label = "레이저 격자",
            arrangement = Arrangement.Grid,
            beamCount = 4, warnDuration = 1f, fireDuration = 0.4f,
            beamWidth = 1.5f, playerDamage = 18f, interval = 10f,
            homingCount = 2, homingInterval = 14f, homingDamage = 18f,
            ventCount = 2, ventInterval = 17f, ventBurstCount = 6, ventLifetime = 7f,
        },
    };

    [Header("공통")]
    [Tooltip("시대가 시작되고 이 시간이 지나야 첫 패턴이 나온다. 배너·전환과 겹치지 않게 하는 여유")]
    [SerializeField] private float firstDelay = 8f;
    [Tooltip("한 패턴 안에서 줄마다 주는 시간차(초). 0이면 전부 동시에 나온다")]
    [SerializeField] private float beamStagger = 0.12f;

    // 세 종류가 서로 다른 주기로 돈다. 한 타이머로 묶으면 매번 셋이 동시에 터져 화면을 읽을 수 없다.
    private float _beamTimer, _homingTimer, _ventTimer;
    private bool _beamStarted, _homingStarted, _ventStarted;
    private EraManager.Era _lastEra;
    private bool _eraKnown;

    private void Update()
    {
        if (!CanFire())
        {
            // 보스전·전환 중에는 타이머를 되돌린다. 그 구간이 끝나자마자 예고가 튀어나오면
            // 플레이어가 화면을 읽을 틈이 없다.
            ResetTimers();
            return;
        }

        // 시대가 바뀌면 첫 패턴까지 다시 여유를 준다.
        EraManager.Era era = eraManager != null ? eraManager.CurrentEra : EraManager.Era.Primitive;
        if (!_eraKnown) { _lastEra = era; _eraKnown = true; }
        else if (era != _lastEra)
        {
            _lastEra = era;
            ResetTimers();
        }

        PatternConfig cfg = CurrentPattern();
        if (cfg == null) return;

        float dt = Time.deltaTime;

        // --- 직선 패턴 ---
        _beamTimer += dt;
        if (_beamTimer >= (_beamStarted ? cfg.interval : firstDelay))
        {
            _beamTimer = 0f;
            _beamStarted = true;
            StartCoroutine(FirePattern(cfg));
        }

        // --- 추적 장판 ---
        if (cfg.homingCount > 0 && homingPrefab != null)
        {
            _homingTimer += dt;
            // 직선과 같은 순간에 겹치지 않도록 첫 발동을 절반만큼 밀어 둔다.
            float need = _homingStarted ? cfg.homingInterval : firstDelay + cfg.homingInterval * 0.5f;
            if (_homingTimer >= need)
            {
                _homingTimer = 0f;
                _homingStarted = true;
                SpawnHoming(cfg);
            }
        }

        // --- 분출구 ---
        if (cfg.ventCount > 0 && ventPrefab != null)
        {
            _ventTimer += dt;
            float need = _ventStarted ? cfg.ventInterval : firstDelay + cfg.ventInterval * 0.75f;
            if (_ventTimer >= need)
            {
                _ventTimer = 0f;
                _ventStarted = true;
                SpawnVents(cfg);
            }
        }
    }

    private void ResetTimers()
    {
        _beamTimer = _homingTimer = _ventTimer = 0f;
        _beamStarted = _homingStarted = _ventStarted = false;
    }

    /// <summary>추적 장판은 플레이어에게서 조금 떨어진 곳에서 시작해 따라온다.</summary>
    private void SpawnHoming(PatternConfig cfg)
    {
        Rect arena = ArenaBounds.Instance.Rect;
        Color color = BeamColor();

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        Vector2 playerPos = playerGO != null ? (Vector2)playerGO.transform.position : arena.center;

        for (int i = 0; i < cfg.homingCount; i++)
        {
            // 발밑에서 바로 시작하면 추적 구간이 무의미하다. 조금 떨어진 곳에서 걸어온다.
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(5f, 8f);
            Vector2 start = ArenaBounds.Instance.Clamp(playerPos + offset);

            HomingHazard h = Instantiate(homingPrefab, start, Quaternion.identity);
            h.Configure(new HomingHazard.Spec
            {
                startPosition = start,
                size = cfg.homingSize,
                trackDuration = cfg.homingTrackDuration,
                lockDuration = cfg.homingLockDuration,
                fireDuration = cfg.homingFireDuration,
                trackSpeed = cfg.homingTrackSpeed,
                playerDamage = cfg.homingDamage,
                color = color,
            });
        }
    }

    /// <summary>분출구는 플레이어와 떨어진 곳에 세운다. 발밑에 서면 첫 분출을 피할 수 없다.</summary>
    private void SpawnVents(PatternConfig cfg)
    {
        Rect arena = ArenaBounds.Instance.Rect;
        Color color = BeamColor();

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        Vector2 playerPos = playerGO != null ? (Vector2)playerGO.transform.position : arena.center;

        for (int i = 0; i < cfg.ventCount; i++)
        {
            Vector2 pos = arena.center;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 candidate = new Vector2(
                    UnityEngine.Random.Range(arena.xMin + 1f, arena.xMax - 1f),
                    UnityEngine.Random.Range(arena.yMin + 1f, arena.yMax - 1f));

                pos = candidate;
                if ((candidate - playerPos).sqrMagnitude >= 25f) break;   // 5유닛 이상
            }

            RiftVent v = Instantiate(ventPrefab, pos, Quaternion.identity);
            v.Configure(new RiftVent.Spec
            {
                position = pos,
                size = cfg.ventSize,
                warnDuration = cfg.ventWarnDuration,
                lifetime = cfg.ventLifetime,
                burstInterval = cfg.ventBurstInterval,
                burstCount = cfg.ventBurstCount,
                spinPerBurst = cfg.ventSpinPerBurst,
                color = color,
            }, ventProjectilePrefab);
        }
    }

    private bool CanFire()
    {
        if (beamPrefab == null) return false;
        if (ArenaBounds.Instance == null) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
        if (eraManager != null && eraManager.IsTransitioning) return false;
        if (enemySpawner == null || !enemySpawner.SpawningEnabled) return false;
        return true;
    }

    private PatternConfig CurrentPattern()
    {
        if (patterns == null || patterns.Length == 0) return null;
        int index = eraManager != null ? (int)eraManager.CurrentEra : 0;
        return patterns[Mathf.Clamp(index, 0, patterns.Length - 1)];
    }

    private IEnumerator FirePattern(PatternConfig cfg)
    {
        Rect arena = ArenaBounds.Instance.Rect;
        Color color = BeamColor();

        // 대각선 길이로 잡으면 어떤 각도로 놓아도 아레나를 완전히 가로지른다.
        float length = Mathf.Sqrt(arena.width * arena.width + arena.height * arena.height);

        int count = Mathf.Max(1, cfg.beamCount);
        bool verticalFirst = UnityEngine.Random.value < 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle;
            Vector2 center;
            ResolvePlacement(cfg, arena, i, count, verticalFirst, out angle, out center);

            HazardBeam beam = Instantiate(beamPrefab, center, Quaternion.identity);
            beam.Configure(new HazardBeam.Spec
            {
                center = center,
                angleDeg = angle,
                length = length,
                width = cfg.beamWidth,
                color = color,
                warnDuration = cfg.warnDuration,
                fireDuration = cfg.fireDuration,
                playerDamage = cfg.playerDamage,
                enemyDamage = cfg.enemyDamage,
            });

            if (beamStagger > 0f && i < count - 1) yield return new WaitForSeconds(beamStagger);
        }
    }

    /// <summary>
    /// 배치별로 각도와 중심을 정한다. 평행·격자는 줄이 겹치지 않도록 축을 따라 균등하게 나눠 놓고
    /// 그 안에서만 흔든다 — 완전 무작위로 두면 두 줄이 붙어 나와 피할 틈이 사라진다.
    /// </summary>
    private void ResolvePlacement(PatternConfig cfg, Rect arena, int index, int count, bool verticalFirst,
        out float angle, out Vector2 center)
    {
        switch (cfg.arrangement)
        {
            case Arrangement.Parallel:
            {
                bool vertical = verticalFirst;
                angle = vertical ? 90f : 0f;
                center = SlotCenter(arena, vertical, index, count, cfg.minGap);
                return;
            }

            case Arrangement.Cross:
            {
                bool vertical = (index % 2 == 0) == verticalFirst;
                angle = vertical ? 90f : 0f;
                center = SlotCenter(arena, vertical, index / 2, Mathf.Max(1, count / 2), cfg.minGap);
                return;
            }

            case Arrangement.Grid:
            {
                int half = Mathf.Max(1, count / 2);
                bool vertical = index < half ? verticalFirst : !verticalFirst;
                int slot = index < half ? index : index - half;
                angle = vertical ? 90f : 0f;
                center = SlotCenter(arena, vertical, slot, half, cfg.minGap);
                return;
            }

            default:
            {
                angle = UnityEngine.Random.Range(0f, 180f);
                center = new Vector2(
                    UnityEngine.Random.Range(arena.xMin, arena.xMax),
                    UnityEngine.Random.Range(arena.yMin, arena.yMax));
                return;
            }
        }
    }

    /// <summary>축을 count개 구간으로 나눈 뒤 index번째 구간 안의 랜덤한 위치.</summary>
    private static Vector2 SlotCenter(Rect arena, bool vertical, int index, int count, float minGap)
    {
        // 세로 줄(angle 90)은 x축을 따라 나뉘고, 가로 줄은 y축을 따라 나뉜다.
        float min = vertical ? arena.xMin : arena.yMin;
        float max = vertical ? arena.xMax : arena.yMax;

        float span = (max - min) / Mathf.Max(1, count);
        float slotMin = min + span * index;
        float slotMax = slotMin + span;

        // 구간 경계에 딱 붙지 않게 안쪽으로 조금 당긴다. 구간이 좁으면 그냥 가운데를 쓴다.
        float pad = Mathf.Min(minGap * 0.5f, span * 0.25f);
        float value = span > pad * 2f
            ? UnityEngine.Random.Range(slotMin + pad, slotMax - pad)
            : (slotMin + slotMax) * 0.5f;

        return vertical
            ? new Vector2(value, arena.center.y)
            : new Vector2(arena.center.x, value);
    }

    /// <summary>시대 보스 색을 그대로 쓴다. 새 아트 없이 시대마다 다른 색으로 읽히게 하려는 것.</summary>
    private Color BeamColor()
    {
        EraManager.EraConfig cfg = eraManager != null ? eraManager.CurrentConfig : null;
        return cfg != null ? cfg.bossColor : new Color(0.95f, 0.35f, 0.25f, 1f);
    }
}
