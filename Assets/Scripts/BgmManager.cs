using System.Collections;
using UnityEngine;

/// <summary>
/// 시대별 BGM을 재생한다. EraManager.OnEraChanged를 구독해 곡을 크로스페이드로 갈아끼운다.
///
/// 곡은 Assets/Resources/BGM/ 아래 파일 이름으로 찾는다(인덱스 = Era enum 값):
///   0 원시 bgm_primitive / 1 중세 bgm_medieval / 2 현대 bgm_modern / 3 미래 bgm_future
/// 인스펙터 참조 대신 Resources.Load를 쓰는 이유는 4곡 전체를 미리 메모리에 올리지 않고
/// 그 시대에 들어갈 때만 로드하고, 다 쓴 곡은 UnloadAsset으로 즉시 버리기 위해서다.
/// WebGL + 저사양 노트북이 타깃이라 상시 상주 오디오를 4개 두지 않는다.
///
/// 부착 대상: EraManager와 같은 GameObject (또는 아무 빈 GameObject — EraManager는 자동으로 찾는다)
/// </summary>
[DisallowMultipleComponent]
public class BgmManager : MonoBehaviour
{
    /// <summary>Era enum 순서와 같아야 한다. Resources/BGM 아래 확장자 없는 파일 이름.</summary>
    private static readonly string[] TrackNames =
    {
        "BGM/bgm_primitive", // 원시 — 돌의 첫 박동
        "BGM/bgm_medieval",  // 중세 — 십자검 성채
        "BGM/bgm_modern",    // 현대 — 통제 구역 이상
        "BGM/bgm_future",    // 미래 — 크로노 가디언
    };

    /// <summary>타이틀·결과 화면 쪽에서 재생 시점을 잡을 때 쓴다.</summary>
    public static BgmManager Instance { get; private set; }

    [SerializeField] private EraManager eraManager;

    [Tooltip("BGM 최종 볼륨. 효과음이 아직 없어 0.5로 두면 과하지 않다")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    [Tooltip("시대 전환 시 이전 곡↔새 곡 교차 시간(초). EraManager 암전(0.8초)과 비슷하게 둔다")]
    [SerializeField] private float crossfadeDuration = 1.0f;

    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;   // 지금 소리를 내고 있는 쪽
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        Instance = this;

        if (eraManager == null) eraManager = FindFirstObjectByType<EraManager>();

        _a = CreateSource();
        _b = CreateSource();
        _active = _a;
    }

    private void OnEnable()
    {
        if (eraManager != null) eraManager.OnEraChanged += HandleEraChanged;
    }

    private void OnDisable()
    {
        if (eraManager != null) eraManager.OnEraChanged -= HandleEraChanged;
    }

    private void Start()
    {
        // 타이틀 화면에서는 음악을 걸지 않는다. 아직 판이 시작되지 않았고,
        // 시대 BGM은 "그 시대를 플레이하는 동안"의 소리라 메뉴에서 먼저 새면 안 된다.
        // 실제 시작은 TitleController.StartRun()이 BeginRun()을 부르는 시점이다.
        TitleController title = FindFirstObjectByType<TitleController>();
        if (title != null && title.IsWaitingToStart) return;

        BeginRun();
    }

    /// <summary>판이 시작될 때(직업 선택까지 끝난 뒤) 현재 시대 곡을 건다. 두 번 불려도 안전하다.</summary>
    public void BeginRun()
    {
        int index = eraManager != null ? (int)eraManager.CurrentEra : 0;
        PlayTrack(index, fade: false);
    }

    /// <summary>결과 화면에서 음악을 걷어낸다. 페이드가 끝나면 곡을 메모리에서 내린다.</summary>
    public void FadeOutAndStop(float duration = 1.2f)
    {
        if (_active == null || !_active.isPlaying) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutRoutine(_active, duration));
    }

    private AudioSource CreateSource()
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.volume = 0f;
        src.spatialBlend = 0f; // 2D. 카메라가 플레이어를 따라다녀도 거리 감쇠가 걸리면 안 된다.
        // 증강 카드/일시정지에서 timeScale=0이 걸려도 음악은 계속 흘러야 한다.
        src.ignoreListenerPause = true;
        return src;
    }

    private void HandleEraChanged(EraManager.Era era) => PlayTrack((int)era, fade: true);

    private void PlayTrack(int index, bool fade)
    {
        if (index < 0 || index >= TrackNames.Length) return;

        AudioClip clip = Resources.Load<AudioClip>(TrackNames[index]);
        if (clip == null)
        {
            Debug.LogWarning($"[BgmManager] BGM을 찾지 못했다: Resources/{TrackNames[index]}");
            return;
        }

        if (_active.clip == clip && _active.isPlaying) return;

        AudioSource next = _active == _a ? _b : _a;
        AudioSource prev = _active;

        next.clip = clip;
        next.volume = fade ? 0f : volume;
        next.Play();
        _active = next;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);

        if (fade)
        {
            _fadeRoutine = StartCoroutine(CrossfadeRoutine(prev, next));
        }
        else
        {
            StopAndRelease(prev);
        }
    }

    private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to)
    {
        float fromStart = from.volume;
        float t = 0f;

        // timeScale=0(증강 카드)에서도 페이드가 멈추면 안 되므로 unscaled로 돈다.
        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / crossfadeDuration);
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, volume, k);
            yield return null;
        }

        from.volume = 0f;
        to.volume = volume;

        StopAndRelease(from);
        _fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(AudioSource src, float duration)
    {
        float start = src.volume;
        float t = 0f;

        // 결과 화면은 timeScale=0이다. 여기서 스케일 시간을 쓰면 페이드가 그대로 얼어붙는다.
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
            yield return null;
        }

        src.volume = 0f;
        StopAndRelease(src);
        _fadeRoutine = null;
    }

    /// <summary>다 쓴 소스를 멈추고 그 곡을 메모리에서 내린다. 시대는 되돌아오지 않으니 붙들 이유가 없다.</summary>
    private void StopAndRelease(AudioSource src)
    {
        if (src == null) return;

        src.Stop();

        AudioClip old = src.clip;
        src.clip = null;
        if (old != null && old != _active.clip) Resources.UnloadAsset(old);
    }

    /// <summary>인스펙터에서 볼륨을 만지면 재생 중에도 바로 반영된다.</summary>
    private void OnValidate()
    {
        if (Application.isPlaying && _active != null && _fadeRoutine == null) _active.volume = volume;
    }
}
