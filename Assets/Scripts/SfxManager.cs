using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>효과음 종류. 배열 인덱스로 쓰이므로 순서를 바꾸지 말고 뒤에만 추가한다.</summary>
public enum SfxId
{
    UiClick,
    Shoot,
    BladeSwing,
    EnemyHit,
    EnemyDeath,
    PlayerHit,
    ExpPickup,
    Collect,
    LevelUp,
    Heal,
    BossSpawn,
    BossHit,
    BossDeath,
    EraShift,
    GameOver,
    GameClear,
}

/// <summary>효과음 호출 창구. 어디서든 <c>Sfx.Play(SfxId.Shoot)</c> 한 줄이면 된다.</summary>
public static class Sfx
{
    /// <summary>전체 효과음 볼륨. BGM(0.5)보다 살짝 낮게 둔다 — 타격음이 계속 나기 때문.</summary>
    public static float MasterVolume = 0.45f;

    public static void Play(SfxId id, float volumeScale = 1f) => SfxManager.PlayInternal(id, volumeScale);
}

/// <summary>
/// 효과음을 **코드에서 파형으로 합성**해 쓰는 시스템. 오디오 파일이 하나도 없다.
///
/// 왜 합성인가: 타깃이 WebGL이고 빌드 용량 = 로딩 시간이다. 무료 효과음 팩을 받으면
/// 16종에 수 MB가 붙고 라이선스 표기까지 따라온다. 사인/사각/톱니/노이즈를 섞어 만들면
/// **빌드 용량 증가 0바이트**, 라이선스도 우리 것이다. 대신 실사 녹음 질감은 포기한다
/// (이 게임은 레트로 아케이드 톤이라 오히려 맞는다).
///
/// 부착 대상: **없다.** RuntimeInitializeOnLoadMethod로 자기가 뜬다(DontDestroyOnLoad).
/// 씬 재시작(Restart)에도 살아남아 클립을 다시 굽지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class SfxManager : MonoBehaviour
{
    // 22050Hz 모노. 짧은 타격음에 44.1kHz는 낭비고, 메모리가 정확히 절반이 된다.
    private const int SampleRate = 22050;
    private const int VoiceCount = 10;

    private static SfxManager _instance;

    private AudioClip[] _clips;
    private AudioSource[] _voices;
    private int _nextVoice;
    private float[] _nextPlayTime;

    // 같은 소리가 연달아 터질 때의 최소 간격(초). 적 20마리가 한 프레임에 맞아도
    // 소리는 한 번만 난다 — 저사양 브라우저에서 보이스가 폭발하는 것을 막는다.
    private static readonly float[] MinInterval =
    {
        0.05f, // UiClick
        0.04f, // Shoot
        0.05f, // BladeSwing
        0.045f, // EnemyHit
        0.05f, // EnemyDeath
        0.12f, // PlayerHit
        0.045f, // ExpPickup
        0.10f, // Collect
        0.20f, // LevelUp
        0.20f, // Heal
        1.00f, // BossSpawn
        0.05f, // BossHit
        1.00f, // BossDeath
        0.50f, // EraShift
        1.00f, // GameOver
        1.00f, // GameClear
    };

    // 재생마다 피치를 이만큼(±비율) 흔든다. 반복되는 타격음이 기계처럼 들리지 않게 한다.
    private static readonly float[] PitchJitter =
    {
        0.03f, // UiClick
        0.06f, // Shoot
        0.07f, // BladeSwing
        0.10f, // EnemyHit
        0.08f, // EnemyDeath
        0.04f, // PlayerHit
        0.08f, // ExpPickup
        0.03f, // Collect
        0f,    // LevelUp
        0.03f, // Heal
        0f,    // BossSpawn
        0.06f, // BossHit
        0f,    // BossDeath
        0.03f, // EraShift
        0f,    // GameOver
        0f,    // GameClear
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("~SfxManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SfxManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        _clips = BuildAllClips();
        _nextPlayTime = new float[_clips.Length];

        _voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D. 카메라가 움직여도 거리 감쇠가 걸리면 안 된다.
            _voices[i] = src;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        HookScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (_instance == this) _instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => HookScene();

    /// <summary>
    /// 씬의 이벤트에 효과음을 건다. 여기서 처리할 수 있는 것은 게임플레이 스크립트를 건드리지 않는다 —
    /// 마감 직전이라 검증이 끝난 파일을 여는 횟수를 줄이는 편이 안전하다.
    /// 재시작(씬 재로드)마다 다시 걸어야 하므로 sceneLoaded에서 부른다.
    /// </summary>
    private void HookScene()
    {
        // 버튼 전체. 씬의 버튼은 전부 미리 배치돼 있고(증강 카드·직업 카드 포함) 런타임 생성이 없어
        // 한 번 훑으면 끝난다. 비활성 패널 안의 버튼까지 잡아야 해서 Include.
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            b.onClick.AddListener(PlayUiClick);
        }

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            Health playerHealth = playerGo.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.OnHit += HandlePlayerHit;
                playerHealth.OnDeath += HandlePlayerDeath;
            }
        }

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.OnLevelUp += HandleLevelUp;

        EraManager era = FindFirstObjectByType<EraManager>();
        if (era != null)
        {
            era.OnEraChanged += HandleEraChanged;
            era.OnGameClear += HandleGameClear;
        }
    }

    private void PlayUiClick() => Sfx.Play(SfxId.UiClick);
    private void HandlePlayerHit(float _) => Sfx.Play(SfxId.PlayerHit);
    private void HandlePlayerDeath(Health _) => Sfx.Play(SfxId.GameOver);
    private void HandleLevelUp(int _) => Sfx.Play(SfxId.LevelUp);
    private void HandleEraChanged(EraManager.Era _) => Sfx.Play(SfxId.EraShift);
    private void HandleGameClear() => Sfx.Play(SfxId.GameClear);

    /// <summary>합성된 클립을 꺼낸다. 파형을 실측(피크·무음 여부)할 때만 쓴다 — 게임 코드는 Sfx.Play를 쓴다.</summary>
    public static AudioClip GetClip(SfxId id)
    {
        if (_instance == null || _instance._clips == null) return null;

        int i = (int)id;
        return i >= 0 && i < _instance._clips.Length ? _instance._clips[i] : null;
    }

    internal static void PlayInternal(SfxId id, float volumeScale)
    {
        if (_instance == null) Bootstrap();
        if (_instance == null) return;

        _instance.PlayOn(id, volumeScale);
    }

    private void PlayOn(SfxId id, float volumeScale)
    {
        int i = (int)id;
        if (_clips == null || i < 0 || i >= _clips.Length || _clips[i] == null) return;

        // timeScale=0(타이틀·일시정지·증강 카드)에서도 눌린 버튼은 소리가 나야 한다.
        float now = Time.unscaledTime;
        if (now < _nextPlayTime[i]) return;
        _nextPlayTime[i] = now + MinInterval[i];

        AudioSource src = TakeVoice();
        float jitter = PitchJitter[i];
        src.pitch = jitter > 0f ? 1f + UnityEngine.Random.Range(-jitter, jitter) : 1f;
        src.PlayOneShot(_clips[i], Mathf.Clamp01(Sfx.MasterVolume * volumeScale));
    }

    /// <summary>노는 보이스를 준다. 전부 물려 있으면 가장 오래된 것을 뺏는다(라운드 로빈).</summary>
    private AudioSource TakeVoice()
    {
        for (int k = 0; k < VoiceCount; k++)
        {
            AudioSource s = _voices[(_nextVoice + k) % VoiceCount];
            if (!s.isPlaying)
            {
                _nextVoice = (_nextVoice + k + 1) % VoiceCount;
                return s;
            }
        }

        AudioSource stolen = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % VoiceCount;
        return stolen;
    }

    // ────────────────────────────────────────────────────────────────────
    // 합성기
    // ────────────────────────────────────────────────────────────────────

    private enum Wave { Sine, Square, Saw, Tri }

    private AudioClip[] BuildAllClips()
    {
        var clips = new AudioClip[16];

        // 버튼: 짧고 마른 "틱". 길면 연타할 때 지저분해진다.
        clips[(int)SfxId.UiClick] = Bake("sfx_ui", 0.06f, 0.45f, buf =>
        {
            Tone(buf, 0f, 0.055f, 1500f, 950f, 0.5f, Wave.Square, 3.5f);
            Noise(buf, 0f, 0.012f, 0.25f, 0.7f, 0.4f, 2f);
        });

        // 총: 위에서 아래로 떨어지는 "퓽". 0.5초마다 나므로 짧고 얇게.
        clips[(int)SfxId.Shoot] = Bake("sfx_shoot", 0.14f, 0.38f, buf =>
        {
            Tone(buf, 0f, 0.13f, 1150f, 260f, 0.5f, Wave.Saw, 2.5f);
            Tone(buf, 0f, 0.09f, 560f, 130f, 0.25f, Wave.Square, 3f);
            Noise(buf, 0f, 0.03f, 0.18f, 0.9f, 0.3f, 2f);
        });

        // 근접 휘두르기: 소리가 커졌다 사라지는 바람 소리. 톤이 아니라 필터 스윕이 본체다.
        clips[(int)SfxId.BladeSwing] = Bake("sfx_swing", 0.20f, 0.42f, buf =>
        {
            Noise(buf, 0f, 0.20f, 0.5f, 0.85f, 0.12f, 1.6f, 0.45f);
            Tone(buf, 0.02f, 0.14f, 420f, 900f, 0.10f, Wave.Tri, 2f);
        });

        // 적 피격: 아주 짧은 "탁". 초당 수십 번 나므로 여운을 남기면 안 된다.
        clips[(int)SfxId.EnemyHit] = Bake("sfx_enemyhit", 0.07f, 0.40f, buf =>
        {
            Noise(buf, 0f, 0.05f, 0.5f, 0.65f, 0.25f, 3f);
            Tone(buf, 0f, 0.06f, 300f, 150f, 0.45f, Wave.Square, 3.5f);
        });

        // 적 처치: 같은 타격이지만 아래로 무너지는 꼬리가 붙어 "죽었다"가 읽힌다.
        clips[(int)SfxId.EnemyDeath] = Bake("sfx_enemydeath", 0.28f, 0.50f, buf =>
        {
            Noise(buf, 0f, 0.26f, 0.55f, 0.6f, 0.06f, 2.2f);
            Tone(buf, 0f, 0.22f, 260f, 60f, 0.45f, Wave.Saw, 2f);
        });

        // 내가 맞음: 이 게임에서 가장 중요한 신호. 다른 소리에 묻히면 안 되므로
        // 가장 크고(0.95) 가장 낮고 거칠게 — 대역이 겹치는 소리가 없다.
        clips[(int)SfxId.PlayerHit] = Bake("sfx_playerhit", 0.35f, 0.95f, buf =>
        {
            Tone(buf, 0f, 0.32f, 200f, 78f, 0.6f, Wave.Square, 1.4f, 22f, 0.12f);
            Noise(buf, 0f, 0.16f, 0.35f, 0.45f, 0.08f, 2f);
            Tone(buf, 0f, 0.10f, 95f, 55f, 0.4f, Wave.Sine, 1.5f);
        });

        // 경험치: 위로 올라가는 밝은 "핑". 자주 나므로 가장 조용하다(0.35).
        clips[(int)SfxId.ExpPickup] = Bake("sfx_exp", 0.13f, 0.35f, buf =>
        {
            Tone(buf, 0f, 0.12f, 1046f, 1568f, 0.5f, Wave.Sine, 2.5f);
            Tone(buf, 0f, 0.08f, 2092f, 3136f, 0.15f, Wave.Sine, 3f);
        });

        // 모래시계: 경험치보다 확실히 큰 사건이라 3음 상행 아르페지오로 구분한다.
        clips[(int)SfxId.Collect] = Bake("sfx_collect", 0.40f, 0.70f, buf =>
        {
            Tone(buf, 0.00f, 0.14f, 784f, 784f, 0.4f, Wave.Sine, 2f);
            Tone(buf, 0.09f, 0.14f, 1046f, 1046f, 0.4f, Wave.Sine, 2f);
            Tone(buf, 0.18f, 0.22f, 1568f, 1568f, 0.45f, Wave.Sine, 1.8f);
            Tone(buf, 0.18f, 0.22f, 3136f, 3136f, 0.10f, Wave.Sine, 2.5f);
        });

        // 레벨업: 증강 카드가 뜨기 직전의 신호. 완전한 상행 3화음.
        clips[(int)SfxId.LevelUp] = Bake("sfx_levelup", 0.50f, 0.70f, buf =>
        {
            Tone(buf, 0.00f, 0.13f, 659f, 659f, 0.35f, Wave.Square, 2.5f);
            Tone(buf, 0.10f, 0.13f, 880f, 880f, 0.35f, Wave.Square, 2.5f);
            Tone(buf, 0.20f, 0.30f, 1319f, 1319f, 0.40f, Wave.Square, 1.8f);
            Tone(buf, 0.20f, 0.30f, 1319f, 1319f, 0.25f, Wave.Sine, 1.8f);
        });

        // 회복: 부드럽게 위로. 사각파를 쓰지 않는다 — 회복은 유일하게 '착한' 소리다.
        clips[(int)SfxId.Heal] = Bake("sfx_heal", 0.35f, 0.55f, buf =>
        {
            Tone(buf, 0f, 0.33f, 523f, 784f, 0.5f, Wave.Sine, 1.8f);
            Tone(buf, 0.08f, 0.25f, 784f, 1046f, 0.22f, Wave.Sine, 2f);
        });

        // 보스 등장: 아래로 깔리는 경보. 길고(1.3초) 저역이라 배너와 같이 나면 무게가 생긴다.
        clips[(int)SfxId.BossSpawn] = Bake("sfx_bossspawn", 1.30f, 0.90f, buf =>
        {
            Tone(buf, 0f, 1.30f, 62f, 44f, 0.55f, Wave.Sine, 0.8f);
            Tone(buf, 0.05f, 1.10f, 233f, 110f, 0.30f, Wave.Saw, 1.0f, 5.5f, 0.05f);
            Tone(buf, 0.05f, 1.10f, 117f, 55f, 0.25f, Wave.Square, 1.0f, 5.5f, 0.05f);
            Noise(buf, 0f, 1.25f, 0.22f, 0.05f, 0.02f, 1.2f, 0.35f);
        });

        // 보스 피격: 잡몹보다 낮고 둔탁하게. 같은 소리를 쓰면 보스를 때리는 감각이 안 산다.
        clips[(int)SfxId.BossHit] = Bake("sfx_bosshit", 0.11f, 0.45f, buf =>
        {
            Noise(buf, 0f, 0.09f, 0.45f, 0.4f, 0.12f, 2.5f);
            Tone(buf, 0f, 0.10f, 170f, 85f, 0.5f, Wave.Square, 2.5f);
        });

        // 보스 처치: 폭발 + 무너지는 저역. 판에서 가장 큰 보상 순간이다.
        clips[(int)SfxId.BossDeath] = Bake("sfx_bossdeath", 1.40f, 0.90f, buf =>
        {
            Noise(buf, 0f, 1.35f, 0.60f, 0.9f, 0.03f, 1.5f);
            Tone(buf, 0f, 0.90f, 320f, 42f, 0.45f, Wave.Saw, 1.6f);
            Tone(buf, 0f, 1.20f, 90f, 32f, 0.50f, Wave.Sine, 1.2f);
        });

        // 시대 전환: 위로 빨려 올라가는 시간 왜곡. 암전 한가운데서 난다.
        clips[(int)SfxId.EraShift] = Bake("sfx_erashift", 1.00f, 0.70f, buf =>
        {
            Noise(buf, 0f, 1.00f, 0.5f, 0.03f, 0.95f, 1.0f, 0.55f);
            Tone(buf, 0f, 0.95f, 180f, 1600f, 0.35f, Wave.Sine, 0.7f);
            Tone(buf, 0f, 0.95f, 90f, 800f, 0.20f, Wave.Tri, 0.7f);
        });

        // 게임 오버: 아래로 내려앉는 단3도. 유일하게 화음이 어두운 소리다.
        clips[(int)SfxId.GameOver] = Bake("sfx_gameover", 1.20f, 0.75f, buf =>
        {
            Tone(buf, 0.00f, 0.35f, 392f, 392f, 0.40f, Wave.Square, 2f);
            Tone(buf, 0.28f, 0.35f, 311f, 311f, 0.40f, Wave.Square, 2f);
            Tone(buf, 0.56f, 0.62f, 233f, 220f, 0.45f, Wave.Square, 1.2f);
            Tone(buf, 0.56f, 0.62f, 116f, 110f, 0.35f, Wave.Sine, 1.2f);
        });

        // 클리어: 상행 팡파르. 게임 오버와 정반대 방향으로 움직여야 한 번에 구분된다.
        clips[(int)SfxId.GameClear] = Bake("sfx_gameclear", 1.40f, 0.85f, buf =>
        {
            Tone(buf, 0.00f, 0.16f, 523f, 523f, 0.35f, Wave.Square, 2f);
            Tone(buf, 0.13f, 0.16f, 659f, 659f, 0.35f, Wave.Square, 2f);
            Tone(buf, 0.26f, 0.16f, 784f, 784f, 0.35f, Wave.Square, 2f);
            Tone(buf, 0.39f, 0.95f, 1046f, 1046f, 0.45f, Wave.Square, 1.0f, 6f, 0.01f);
            Tone(buf, 0.39f, 0.95f, 1568f, 1568f, 0.20f, Wave.Sine, 1.0f);
            Tone(buf, 0.39f, 0.95f, 523f, 523f, 0.25f, Wave.Sine, 1.0f);
        });

        return clips;
    }

    /// <summary>
    /// 버퍼를 만들어 fill을 돌리고, **최고점을 targetPeak에 맞춰 정규화**한 뒤 AudioClip으로 굽는다.
    /// 정규화가 핵심이다 — 오실레이터를 몇 개 겹치든 소리 크기는 targetPeak 하나로 정해진다.
    /// (합성 진폭을 손으로 맞추려 들면 소리마다 크기가 들쭉날쭉해진다.)
    /// </summary>
    private static AudioClip Bake(string name, float seconds, float targetPeak, Action<float[]> fill)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        float[] buf = new float[n];

        fill(buf);

        float peak = 0f;
        for (int i = 0; i < n; i++) { float a = buf[i] < 0f ? -buf[i] : buf[i]; if (a > peak) peak = a; }
        if (peak > 0.0001f)
        {
            float k = targetPeak / peak;
            for (int i = 0; i < n; i++) buf[i] *= k;
        }

        // 끝을 무조건 0으로 내린다. 마지막 샘플이 0이 아니면 재생 종료에서 "틱" 하고 튄다.
        int tail = Mathf.Min(Mathf.RoundToInt(0.004f * SampleRate), n);
        for (int i = 0; i < tail; i++) buf[n - 1 - i] *= (float)i / tail;

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(buf, 0);
        return clip;
    }

    /// <summary>
    /// 오실레이터 하나를 버퍼에 더한다. 주파수는 f0→f1로 선형 활공하고,
    /// 진폭은 (1-t)^curve로 떨어진다(curve가 클수록 짧고 딱딱한 타격음).
    /// vibHz/vibDepth를 주면 떨림이 붙는다 — 경보음 같은 질감에 쓴다.
    /// </summary>
    private static void Tone(float[] buf, float start, float dur, float f0, float f1,
                             float amp, Wave wave, float curve, float vibHz = 0f, float vibDepth = 0f)
    {
        int i0 = Mathf.RoundToInt(start * SampleRate);
        int n = Mathf.RoundToInt(dur * SampleRate);
        if (n <= 0) return;

        int attack = Mathf.Max(1, Mathf.Min(Mathf.RoundToInt(0.003f * SampleRate), n / 4));
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            int idx = i0 + i;
            if (idx < 0) continue;
            if (idx >= buf.Length) break;

            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            if (vibDepth > 0f) f *= 1f + vibDepth * Mathf.Sin(2f * Mathf.PI * vibHz * i / SampleRate);

            phase += f / SampleRate;
            while (phase >= 1f) phase -= 1f;

            float env = Mathf.Pow(1f - t, curve);
            if (i < attack) env *= (float)i / attack; // 시작 클릭 방지

            buf[idx] += Sample(wave, phase) * amp * env;
        }
    }

    /// <summary>
    /// 노이즈를 1극 저역통과로 걸러 더한다. lp0→lp1로 필터를 쓸어 "쉬익"(스윕)을 만든다.
    /// swell&gt;0이면 그 비율만큼 앞부분이 서서히 커진다(휘두르기·시대 전환용).
    /// </summary>
    private static void Noise(float[] buf, float start, float dur, float amp,
                              float lp0, float lp1, float curve, float swell = 0f)
    {
        int i0 = Mathf.RoundToInt(start * SampleRate);
        int n = Mathf.RoundToInt(dur * SampleRate);
        if (n <= 0) return;

        int attack = Mathf.Max(1, Mathf.Min(Mathf.RoundToInt(0.003f * SampleRate), n / 4));
        float y = 0f;

        for (int i = 0; i < n; i++)
        {
            int idx = i0 + i;
            if (idx < 0) continue;
            if (idx >= buf.Length) break;

            float t = (float)i / n;
            float a = Mathf.Clamp(Mathf.Lerp(lp0, lp1, t), 0.01f, 1f);
            float x = UnityEngine.Random.value * 2f - 1f;
            y += a * (x - y);

            // 필터가 셀수록(a가 작을수록) 진폭이 줄어드는 것을 되돌린다. 안 하면 저역 스윕이 사라진다.
            float comp = 1f / Mathf.Sqrt(a);

            float env = Mathf.Pow(1f - t, curve);
            if (swell > 0f) env *= Mathf.Min(1f, t / swell);
            else if (i < attack) env *= (float)i / attack;

            buf[idx] += y * comp * amp * env;
        }
    }

    private static float Sample(Wave wave, float p)
    {
        switch (wave)
        {
            case Wave.Sine: return Mathf.Sin(2f * Mathf.PI * p);
            case Wave.Square: return p < 0.5f ? 1f : -1f;
            case Wave.Saw: return 2f * p - 1f;
            default: return 4f * Mathf.Abs(p - 0.5f) - 1f; // Tri
        }
    }
}
