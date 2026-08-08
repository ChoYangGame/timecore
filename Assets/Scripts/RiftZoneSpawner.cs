using UnityEngine;

/// <summary>
/// 시간 감속 지대(RiftZone)를 아레나에 배치한다.
///
/// 상시 스폰은 웨이브 중에 돌고, 보스전에는 주기를 늘려 계속 돈다. 시대 전환·타이틀 대기에서만 쉰다.
/// (EraHazardSpawner와 같은 규칙이다 — 보스전에 기믹을 통째로 끄니 아레나가 텅 비어 보였다.)
///
/// AnomalyDirector의 "전선 고정" 개입은 SpawnOnPlayer()를 호출한다.
/// 상시 스폰과 디렉터 개입을 분리해 둔 이유: 디렉터 조건이 한 번도 성립하지 않는 판에서도
/// 기믹 자체는 반드시 등장해야 하기 때문이다.
///
/// 부착 대상: RiftZoneSpawner (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class RiftZoneSpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RiftZone riftZonePrefab;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EraManager eraManager;
    [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾는다")]
    [SerializeField] private Transform player;

    /// <summary>미래 보스의 "시간 정지 영역" 기믹이 빌려 쓴다. EraHazardSpawner의 프리팹 공개와 같은 이유.</summary>
    public RiftZone ZonePrefab => riftZonePrefab;

    [Header("상시 스폰")]
    [Tooltip("시대가 시작되고 이 시간이 지나야 첫 지대가 뜬다. 시대 전환 직후 배너와 겹치지 않게 하는 여유")]
    [SerializeField] private float firstSpawnDelay = 12f;
    [SerializeField] private float spawnInterval = 15f;
    [Tooltip("동시에 존재할 수 있는 지대 수. 디렉터 개입분도 여기에 포함해 센다")]
    [SerializeField] private int maxConcurrent = 2;

    [Tooltip("보스전 동안 스폰 간격에 곱하는 값. 감속 지대는 보스 돌진과 겹치면 특히 위험해 더 뜸하게 둔다")]
    [SerializeField] private float bossPhaseIntervalMultiplier = 2f;

    [Header("배치")]
    [SerializeField] private Vector2 sizeRange = new Vector2(5f, 7f);
    [Tooltip("상시 스폰이 플레이어 발밑에 뜨지 않게 하는 최소 거리. 디렉터 개입은 이 제한을 받지 않는다")]
    [SerializeField] private float minPlayerDistance = 6f;
    [SerializeField] private int maxPlacementAttempts = 8;
    [Tooltip("아레나 경계에서 안쪽으로 더 띄울 여유. 지대 절반 크기와 함께 적용된다")]
    [SerializeField] private float edgePadding = 0.5f;

    [Header("외형")]
    [Tooltip("바닥에 깔린 시간 균열의 기본색. 적·플레이어와 색이 겹치지 않도록 어두운 보라를 쓴다")]
    [SerializeField] private Color baseColor = new Color(0.373f, 0.196f, 0.573f, 1f);
    [Tooltip("현재 시대의 보스 색을 이만큼 섞는다. 새 아트 없이 시대마다 다르게 보이게 하려는 것")]
    [Range(0f, 1f)][SerializeField] private float eraTintStrength = 0.3f;

    private float _timer;
    private bool _firstSpawned;
    private WaveManager _waveManager;
    private bool _lastBossActive;

    private bool BossPhase => _waveManager != null && _waveManager.BossActive;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 씬 참조를 새로 꽂으면 씬 편집이 필요하다(에디터 모달 위험). 런타임에 찾아 캐시한다.
        _waveManager = FindFirstObjectByType<WaveManager>();
    }

    private void Update()
    {
        if (!CanSpawn())
        {
            // 전환·타이틀 중에는 타이머를 되돌린다. 그 구간이 끝나자마자 지대가 튀어나오면
            // 플레이어가 상황을 읽을 틈이 없다.
            _timer = 0f;
            return;
        }

        // 웨이브 → 보스전 전환 순간에도 한 번 되돌려 여유를 준다.
        if (BossPhase != _lastBossActive)
        {
            _lastBossActive = BossPhase;
            _timer = 0f;
            _firstSpawned = false;
        }

        _timer += Time.deltaTime;

        float required = _firstSpawned
            ? spawnInterval * (BossPhase ? Mathf.Max(1f, bossPhaseIntervalMultiplier) : 1f)
            : firstSpawnDelay;
        if (_timer < required) return;

        _timer = 0f;

        if (RiftZone.ActiveCount >= maxConcurrent) return;

        if (TrySpawnRandom()) _firstSpawned = true;
    }

    private bool CanSpawn()
    {
        if (riftZonePrefab == null) return false;
        if (ArenaBounds.Instance == null) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
        if (eraManager != null && eraManager.IsTransitioning) return false;

        // 보스전에는 잡몹 스폰이 꺼지지만 지대는 계속 깔린다(간격만 늘어난다).
        if (BossPhase) return true;

        // 타이틀 대기 중에는 SpawningEnabled가 꺼져 있다.
        if (enemySpawner == null || !enemySpawner.SpawningEnabled) return false;
        return true;
    }

    /// <summary>아레나 안쪽 랜덤 위치. 플레이어와 충분히 떨어진 후보를 찾는다.</summary>
    private bool TrySpawnRandom()
    {
        float size = Random.Range(sizeRange.x, sizeRange.y);
        Rect area = PlaceableArea(size);
        if (area.width <= 0f || area.height <= 0f) return false;

        Vector2 chosen = RandomIn(area);
        for (int i = 0; i < maxPlacementAttempts; i++)
        {
            Vector2 candidate = RandomIn(area);
            if (IsFarFromPlayer(candidate))
            {
                chosen = candidate;
                break;
            }
        }

        Spawn(chosen, size);
        return true;
    }

    /// <summary>
    /// AnomalyDirector의 "전선 고정" 개입. 플레이어가 서 있는 자리에 지대를 깐다.
    /// 한 자리에서만 싸우던 플레이어가 느린 걸음으로 빠져나오게 만드는 것이 목적이라
    /// 상시 스폰과 달리 최소 거리 제한을 받지 않는다.
    /// </summary>
    public bool SpawnOnPlayer()
    {
        if (!CanSpawn() || player == null) return false;

        float size = sizeRange.y;
        Rect area = PlaceableArea(size);
        if (area.width <= 0f || area.height <= 0f) return false;

        // 아레나 밖으로 삐져나가지 않게 가장자리 근처에서는 안쪽으로 당긴다.
        Vector2 pos = player.position;
        pos.x = Mathf.Clamp(pos.x, area.xMin, area.xMax);
        pos.y = Mathf.Clamp(pos.y, area.yMin, area.yMax);

        Spawn(pos, size);
        return true;
    }

    private void Spawn(Vector2 position, float size)
    {
        RiftZone zone = Instantiate(riftZonePrefab, position, Quaternion.identity);
        zone.Configure(ZoneColor(), size);
    }

    /// <summary>지대 중심이 놓일 수 있는 범위. 사각형 전체가 걷는 영역 안에 들어오도록 절반만큼 줄인다.</summary>
    private Rect PlaceableArea(float size)
    {
        Rect r = ArenaBounds.Instance.Rect;
        float margin = size * 0.5f + edgePadding;
        return Rect.MinMaxRect(r.xMin + margin, r.yMin + margin, r.xMax - margin, r.yMax - margin);
    }

    private static Vector2 RandomIn(Rect area)
    {
        return new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
    }

    private bool IsFarFromPlayer(Vector2 point)
    {
        if (player == null) return true;
        return ((Vector2)player.position - point).sqrMagnitude >= minPlayerDistance * minPlayerDistance;
    }

    private Color ZoneColor()
    {
        EraManager.EraConfig cfg = eraManager != null ? eraManager.CurrentConfig : null;
        if (cfg == null) return baseColor;
        return Color.Lerp(baseColor, cfg.bossColor, eraTintStrength);
    }
}
