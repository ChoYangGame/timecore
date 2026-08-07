using UnityEngine;

/// <summary>
/// 회복 코어를 배치한다.
///
/// 상시 스폰은 기본으로 꺼져 있다(ambientInterval = 0). 감속 지대·레이저·추적 장판·분출구가
/// 이미 돌고 있어 다섯 번째가 상시로 끼면 화면을 읽을 수 없기 때문이고,
/// 무엇보다 **디렉터가 위기 판정에서만 깔아줄 때** "죽을 것 같을 때만 나타나는 구원"이라는
/// 성격이 뚜렷해지기 때문이다. 켜고 싶으면 인스펙터에서 간격만 넣으면 된다.
///
/// 부착 대상: RecoveryCoreSpawner (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class RecoveryCoreSpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RecoveryCore corePrefab;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EraManager eraManager;
    [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾는다")]
    [SerializeField] private Transform player;

    [Header("배치")]
    [SerializeField] private float captureRadius = 2.2f;
    [SerializeField] private float healAmount = 35f;
    [Tooltip("플레이어에게서 이 거리 근처에 깐다. 발밑에 깔면 확보가 공짜가 된다")]
    [SerializeField] private float spawnDistance = 5.5f;
    [SerializeField] private int maxConcurrent = 1;

    [Header("상시 스폰 (0이면 끔 — 기본은 디렉터 전용)")]
    [SerializeField] private float ambientInterval = 0f;

    private float _timer;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (ambientInterval <= 0f) return;
        if (!CanSpawn()) { _timer = 0f; return; }

        _timer += Time.deltaTime;
        if (_timer < ambientInterval) return;

        _timer = 0f;
        Spawn();
    }

    private bool CanSpawn()
    {
        if (corePrefab == null) return false;
        if (ArenaBounds.Instance == null) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
        if (eraManager != null && eraManager.IsTransitioning) return false;
        if (enemySpawner == null || !enemySpawner.SpawningEnabled) return false;
        return RecoveryCore.ActiveCount < maxConcurrent;
    }

    /// <summary>
    /// AnomalyDirector의 "생존 위기" 개입이 호출한다.
    /// 배치에 실패하면 false — 디렉터는 그때 배너도 쿨다운도 소모하지 않는다.
    /// </summary>
    public bool Spawn()
    {
        if (!CanSpawn()) return false;

        Rect arena = ArenaBounds.Instance.Rect;
        Vector2 from = player != null ? (Vector2)player.position : arena.center;

        // 아레나 안쪽으로 코어 전체가 들어오도록 반지름만큼 여유를 두고 자른다.
        float margin = captureRadius;
        Rect area = Rect.MinMaxRect(
            arena.xMin + margin, arena.yMin + margin,
            arena.xMax - margin, arena.yMax - margin);
        if (area.width <= 0f || area.height <= 0f) return false;

        Vector2 pos = area.center;
        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 candidate = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDistance;
            candidate.x = Mathf.Clamp(candidate.x, area.xMin, area.xMax);
            candidate.y = Mathf.Clamp(candidate.y, area.yMin, area.yMax);

            pos = candidate;
            // 잘려서 플레이어 발밑으로 붙어버린 후보는 버린다.
            if ((candidate - from).sqrMagnitude >= spawnDistance * spawnDistance * 0.35f) break;
        }

        RecoveryCore core = Instantiate(corePrefab, pos, Quaternion.identity);
        core.Configure(captureRadius, healAmount);
        return true;
    }
}
