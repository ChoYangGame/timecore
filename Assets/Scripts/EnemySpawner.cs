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

    [Tooltip("화면 모서리 바깥으로 더 밀어낼 여유 거리")]
    [SerializeField] private float spawnMargin = 2f;

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

        Enemy spawned = Instantiate(enemyPrefab, RandomPointOutsideView(), Quaternion.identity);
        _alive.Add(spawned);
        OnEnemySpawned?.Invoke(spawned);
    }

    /// <summary>카메라를 감싸는 외접원 위의 랜덤한 점. 항상 화면 밖이 보장된다.</summary>
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
