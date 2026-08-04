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
        if (_alive.Count >= maxAlive) return;

        Enemy spawned = Instantiate(enemyPrefab, GetArenaEdgeSpawnPoint(), Quaternion.identity);
        _alive.Add(spawned);
        OnEnemySpawned?.Invoke(spawned);
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
    }
}
