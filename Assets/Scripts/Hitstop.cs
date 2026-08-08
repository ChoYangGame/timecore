using UnityEngine;

/// <summary>
/// 히트스톱. 정해진 실제 시간 동안 timeScale을 떨어뜨렸다가 되돌린다.
/// 맞은 순간 한 박자 멎었다 풀리면 같은 데미지도 훨씬 세게 느껴진다 — 새 에셋 0, 코드 한 장.
///
/// 씬에 배치하지 않는다. 처음 쓰일 때 스스로 만들어진다(씬 파일을 건드리지 않으려는 것).
///
/// **timeScale은 이 게임에서 여러 곳이 만진다** — 증강 카드가 0으로 멈추고, 게임오버도 멈춘다.
/// 그래서 되돌릴 때 "내가 넣어둔 값이 아직 그대로일 때만" 되돌린다. 그 사이 누가 0으로 바꿨다면
/// 손대지 않고 물러난다. 안 그러면 카드가 떠 있는데 게임이 다시 흐른다.
/// 같은 이유로 이미 timeScale이 1이 아니면 히트스톱을 걸지 않는다.
///
/// 부착 대상: 없음 (런타임 자동 생성)
/// </summary>
[DisallowMultipleComponent]
public class Hitstop : MonoBehaviour
{
    private static Hitstop _instance;

    private float _remaining;
    private float _appliedScale;
    private bool _active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    /// <summary>
    /// duration(실제 시간) 동안 timeScale을 scale로 낮춘다.
    /// scale 0은 완전 정지, 0.2 정도면 슬로우모션으로 읽힌다.
    /// </summary>
    public static void Do(float duration, float scale = 0f)
    {
        if (duration <= 0f) return;

        // 이미 멈춰 있는 판(증강 카드·게임오버)에는 끼어들지 않는다.
        if (!Mathf.Approximately(Time.timeScale, 1f) && !IsOurs()) return;

        Hitstop h = Ensure();
        if (h == null) return;

        // 더 긴 히트스톱이 돌고 있으면 짧은 것이 끊지 않는다.
        if (h._active && duration <= h._remaining) return;

        h._remaining = duration;
        h._appliedScale = Mathf.Clamp01(scale);
        h._active = true;

        Time.timeScale = h._appliedScale;
    }

    private static bool IsOurs()
    {
        return _instance != null && _instance._active
            && Mathf.Approximately(Time.timeScale, _instance._appliedScale);
    }

    private static Hitstop Ensure()
    {
        if (_instance != null) return _instance;

        GameObject go = new GameObject("Hitstop");
        _instance = go.AddComponent<Hitstop>();
        return _instance;
    }

    private void Update()
    {
        if (!_active) return;

        // timeScale이 0일 수 있으니 반드시 unscaled로 센다.
        _remaining -= Time.unscaledDeltaTime;
        if (_remaining > 0f) return;

        _active = false;

        // 내가 넣은 값이 그대로일 때만 되돌린다. 그 사이 카드가 떴다면 0인 채로 두고 물러난다.
        if (Mathf.Approximately(Time.timeScale, _appliedScale)) Time.timeScale = 1f;
    }
}
