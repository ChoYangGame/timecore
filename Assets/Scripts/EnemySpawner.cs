using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라 바깥 원형 둘레의 랜덤한 지점에 적을 스폰한다.
/// 오브젝트 풀링은 아직 넣지 않는다. maxAlive 로만 개체 수를 막는다.
/// 부착 대상: Spawner (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private float spawnInterval = 1.1f;
    [SerializeField] private int maxAlive = 40;

    [Tooltip("화면 모서리 바깥으로 더 밀어낼 여유 거리 (ArenaBounds가 없을 때의 폴백에서만 쓰인다)")]
    [SerializeField] private float spawnMargin = 2f;

    [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾는다")]
    [SerializeField] private Transform player;
    [Tooltip("아레나 가장자리 스폰 시 플레이어와 최소 이 거리 이상 떨어지도록 시도한다")]
    [SerializeField] private float minPlayerDistance = 4f;
    [Tooltip("아레나 가장자리 지점을 뽑을 때 '카메라 뷰 밖 + 플레이어와 충분히 멂' 조건을 만족할 때까지 재시도하는 횟수")]
    [SerializeField] private int maxEdgeAttempts = 6;

    [Header("필드 안 스폰")]
    [Tooltip("켜면 가장자리가 아니라 아레나 안쪽에서 예고 표식과 함께 나타난다. 끄면 예전처럼 가장자리 스폰")]
    [SerializeField] private bool spawnInsideField = true;
    [Tooltip("적이 나타나기 전에 뜨는 예고 표식. 비워두면 필드 안 스폰이 꺼진 것처럼 동작한다")]
    [SerializeField] private SpawnPortal portalPrefab;
    [Tooltip("표식이 떠 있는 시간(초). 이게 곧 플레이어가 피할 시간이다")]
    [SerializeField] private float portalWarnDuration = 0.7f;
    [SerializeField] private float portalSize = 1.4f;
    [Tooltip("필드 안 스폰은 코앞에 뜰 수 있으므로 가장자리 스폰보다 여유를 크게 잡는다")]
    [SerializeField] private float insideMinPlayerDistance = 6f;
    [Tooltip("아레나 경계에서 안쪽으로 띄울 여유")]
    [SerializeField] private float insideEdgePadding = 1.2f;
    [Tooltip("표식 색을 현재 시대 적 색으로 맞추는 데만 쓴다. 비워두면 프리팹 색으로 폴백한다")]
    [SerializeField] private EraManager eraManager;

    public float SpawnInterval
    {
        get => spawnInterval;
        set => spawnInterval = value;
    }

    public int MaxAlive
    {
        get => maxAlive;
        set => maxAlive = value;
    }

    public int AliveCount => _alive.Count;

    /// <summary>보스 등장 시 WaveManager 가 false 로 꺼서 일반 스폰을 중단시킨다.</summary>
    public bool SpawningEnabled { get; set; } = true;

    /// <summary>시대 전환 시 EraManager가 호출: 이후 스폰되는 적의 프리팹을 교체한다.</summary>
    public void SetEnemyPrefab(Enemy prefab)
    {
        enemyPrefab = prefab;
    }

    /// <summary>스폰 직후(Awake 완료 후) 호출된다. 웨이브 난이도 스케일링을 붙이는 지점.</summary>
    public event System.Action<Enemy> OnEnemySpawned;

    private readonly List<Enemy> _alive = new List<Enemy>();

    // 아직 열리지 않은 예고 표식. maxAlive 판정에 함께 세지 않으면 예고가 겹치는 동안
    // 상한을 넘겨 스폰해 놓고 한꺼번에 터진다.
    private readonly List<SpawnPortal> _pending = new List<SpawnPortal>();

    private Camera _cam;
    private float _timer;

    private void Awake()
    {
        _cam = Camera.main;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (!SpawningEnabled) return;
        if (enemyPrefab == null) return;
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        _timer += Time.deltaTime;
        if (_timer < spawnInterval) return;
        _timer = 0f;

        RemoveDestroyed();
        if (_alive.Count + _pending.Count >= maxAlive) return;

        if (UseInsideSpawn())
        {
            SpawnInsideWithPortal();
            return;
        }

        Enemy spawned = Instantiate(enemyPrefab, GetArenaEdgeSpawnPoint(), Quaternion.identity);
        _alive.Add(spawned);
        OnEnemySpawned?.Invoke(spawned);
    }

    private bool UseInsideSpawn()
    {
        return spawnInsideField && portalPrefab != null && ArenaBounds.Instance != null;
    }

    /// <summary>
    /// 아레나 안쪽에 예고 표식을 띄우고, 표식이 열릴 때 그 자리에 적을 만든다.
    /// 적 등록(_alive)과 OnEnemySpawned는 실제로 나타나는 시점에 한다 —
    /// 웨이브 스케일링·시대 색은 그 순간의 값으로 걸려야 한다.
    /// </summary>
    private void SpawnInsideWithPortal()
    {
        Enemy prefabAtRequest = enemyPrefab;
        Vector3 pos = GetInFieldSpawnPoint();

        SpawnPortal portal = Instantiate(portalPrefab, pos, Quaternion.identity);
        _pending.Add(portal);

        portal.Configure(PortalColor(), portalSize, portalWarnDuration, () =>
        {
            // 표식이 떠 있는 동안 시대가 바뀌었을 수 있다. 그때는 EraManager가 표식을 지우므로
            // 이 콜백 자체가 불리지 않지만, 프리팹은 요청 시점 값을 그대로 쓴다.
            if (prefabAtRequest == null) return;

            Enemy spawned = Instantiate(prefabAtRequest, pos, Quaternion.identity);
            _alive.Add(spawned);
            OnEnemySpawned?.Invoke(spawned);
        });
    }

    /// <summary>
    /// AnomalyDirector의 난입 적도 같은 표식을 쓰게 하는 진입점.
    /// 난입 적은 maxAlive에 잡히지 않으므로 _alive에 넣지 않고 호출자가 뒷정리를 맡는다.
    /// 표식 프리팹이 없으면 false를 돌려주고, 호출자는 예전처럼 즉시 스폰하면 된다.
    /// </summary>
    public bool SpawnWithPortal(Enemy prefab, Vector3 position, Color markerColor, System.Action<Enemy> onSpawned)
    {
        if (prefab == null || portalPrefab == null) return false;

        SpawnPortal portal = Instantiate(portalPrefab, position, Quaternion.identity);
        portal.Configure(markerColor, portalSize, portalWarnDuration, () =>
        {
            Enemy spawned = Instantiate(prefab, position, Quaternion.identity);
            onSpawned?.Invoke(spawned);
        });
        return true;
    }

    /// <summary>
    /// 아레나 안쪽의 랜덤한 지점. 플레이어와 insideMinPlayerDistance 이상 떨어진 후보를 찾는다.
    /// 못 찾으면 마지막 후보를 쓴다 — 예고 시간이 있으니 붙어 떠도 피할 수 있다.
    /// </summary>
    public Vector3 GetInFieldSpawnPoint()
    {
        if (ArenaBounds.Instance == null) return GetArenaEdgeSpawnPoint();

        Rect r = ArenaBounds.Instance.Rect;
        float pad = insideEdgePadding;
        float xMin = r.xMin + pad, xMax = r.xMax - pad;
        float yMin = r.yMin + pad, yMax = r.yMax - pad;
        if (xMin >= xMax || yMin >= yMax) return r.center;

        Vector2 chosen = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));

        for (int i = 0; i < maxEdgeAttempts; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
            chosen = candidate;

            if (player == null) break;
            if (((Vector2)player.position - candidate).sqrMagnitude
                >= insideMinPlayerDistance * insideMinPlayerDistance) break;
        }

        return chosen;
    }

    /// <summary>
    /// 표식 색은 이 시대 적 색을 따른다 — 무엇이 나올지 색으로 미리 읽히게 하려는 것.
    /// 프리팹 색을 쓰면 안 된다. 현대·미래가 중세와 같은 프리팹을 공유하고 색은 EraConfig가 덮으므로
    /// 프리팹에서 뽑으면 뒤 두 시대의 표식이 중세 색으로 나온다.
    /// </summary>
    private Color PortalColor()
    {
        EraManager.EraConfig cfg = eraManager != null ? eraManager.CurrentConfig : null;
        if (cfg != null)
        {
            Color c = cfg.enemyColor;
            c.a = 1f;
            return c;
        }

        var sr = enemyPrefab != null ? enemyPrefab.GetComponent<SpriteRenderer>() : null;
        Color fallback = sr != null ? sr.color : Color.white;
        fallback.a = 1f;
        return fallback;
    }

    /// <summary>
    /// 아레나 가장자리의 랜덤한 지점. 카메라 뷰 밖 + 플레이어와 minPlayerDistance 이상 떨어진 후보를
    /// maxEdgeAttempts회까지 찾아본다. AnomalyDirector의 난입 스폰도 이 메서드를 그대로 쓴다.
    /// ArenaBounds가 없으면(씬에 배치 안 된 경우) 기존 카메라 외접원 방식으로 폴백한다.
    /// </summary>
    public Vector3 GetArenaEdgeSpawnPoint()
    {
        if (ArenaBounds.Instance == null) return RandomPointOutsideView();

        Vector3 fallback = ArenaBounds.Instance.RandomPointOnEdge();
        for (int i = 0; i < maxEdgeAttempts; i++)
        {
            Vector2 candidate = ArenaBounds.Instance.RandomPointOnEdge();
            bool farFromPlayer = IsFarFromPlayer(candidate);

            if (farFromPlayer)
            {
                fallback = candidate;
                if (_cam == null || !IsInsideCameraView(candidate)) return candidate;
            }
        }
        return fallback;
    }

    private bool IsFarFromPlayer(Vector2 point)
    {
        if (player == null) return true;
        return ((Vector2)player.position - point).sqrMagnitude >= minPlayerDistance * minPlayerDistance;
    }

    private bool IsInsideCameraView(Vector2 point)
    {
        float halfHeight = _cam.orthographicSize;
        float halfWidth = halfHeight * _cam.aspect;
        Vector3 center = _cam.transform.position;

        return Mathf.Abs(point.x - center.x) <= halfWidth && Mathf.Abs(point.y - center.y) <= halfHeight;
    }

    /// <summary>카메라를 감싸는 외접원 위의 랜덤한 점. ArenaBounds가 없을 때만 쓰이는 폴백.</summary>
    private Vector3 RandomPointOutsideView()
    {
        float halfHeight = _cam.orthographicSize;
        float halfWidth = halfHeight * _cam.aspect;
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + spawnMargin;

        float angle = Random.value * Mathf.PI * 2f;
        Vector3 center = _cam.transform.position;

        return new Vector3(
            center.x + Mathf.Cos(angle) * radius,
            center.y + Mathf.Sin(angle) * radius,
            0f);
    }

    private void RemoveDestroyed()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null) _alive.RemoveAt(i);
        }

        // 열려서 사라진 표식과, 시대 전환으로 지워진 표식을 함께 걷어낸다.
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i] == null) _pending.RemoveAt(i);
        }
    }
}
