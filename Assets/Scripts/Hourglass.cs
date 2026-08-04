using System;
using UnityEngine;

/// <summary>
/// 원시 보스 처치 후 아레나 중앙에 등장하는 모래시계. 플레이어가 닿으면 다음 시대로 전환된다.
/// 부착 대상: Hourglass 프리팹 (SpriteRenderer + CircleCollider2D isTrigger = true)
/// </summary>
[DisallowMultipleComponent]
public class Hourglass : MonoBehaviour
{
    public event Action OnCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        OnCollected?.Invoke();
        Destroy(gameObject);
    }
}
