using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 클리어 타임 랭킹. 같은 도메인의 /api/leaderboard 와 통신한다.
///
/// UnityWebRequest를 쓰는 이유: WebGL에서 통신할 수 있는 유일한 정식 수단이다.
/// 내부적으로 브라우저 fetch/XHR로 내려가므로 CLAUDE.md가 금지한
/// 소켓·System.Net 경로를 전혀 타지 않는다.
///
/// URL을 하드코딩하지 않고 Application.absoluteURL에서 오리진을 뽑는 이유:
/// 프리뷰 배포(timecore-xxxx.vercel.app)에서 열렸을 때 프로덕션 URL로 쏘면 교차 출처가 되어
/// CORS로 막힌다. 같은 오리진으로 보내면 어느 배포에서 열든 그대로 동작한다.
///
/// 씬에 배치하지 않는다. 첫 호출 때 자기 자신을 만든다 (Hitstop / ScreenFlash 와 같은 방식).
/// </summary>
[DisallowMultipleComponent]
public class Leaderboard : MonoBehaviour
{
    private const string Path = "/api/leaderboard";
    private const string NameKey = "timecore.playerName";

    /// <summary>에디터에서 돌릴 때 쓸 폴백. 빌드에서는 absoluteURL이 이긴다.</summary>
    private const string EditorFallbackOrigin = "https://timecore-chi.vercel.app";

    private const int TimeoutSeconds = 8;

    [Serializable]
    public class Entry
    {
        public string name;
        public long ms;
    }

    [Serializable]
    private class EntryList
    {
        public Entry[] entries;
    }

    [Serializable]
    private class SubmitBody
    {
        public string name;
        public long ms;
    }

    private static Leaderboard _instance;

    private static Leaderboard Instance
    {
        get
        {
            if (_instance != null) return _instance;

            var go = new GameObject("~Leaderboard");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Leaderboard>();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    /// <summary>
    /// 마지막으로 쓴 이름. 판을 다시 할 때마다 다시 치게 하면 귀찮다.
    /// WebGL이라 저장은 PlayerPrefs만 쓸 수 있다 (System.IO 금지).
    /// </summary>
    public static string PlayerName
    {
        get => PlayerPrefs.GetString(NameKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(NameKey, Sanitize(value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 서버 규칙(A-Z 0-9, 1~8자)에 맞춰 다듬는다. 서버도 같은 검사를 하지만
    /// 여기서 걸러야 사용자가 입력하는 동안 바로 보인다.
    /// 한글을 못 받는 것은 폰트 서브셋 때문이다 — 자세한 사정은 docs/FONT_CHARS.md.
    /// </summary>
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new System.Text.StringBuilder(8);
        foreach (char c in raw.ToUpperInvariant())
        {
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
            if (sb.Length >= 8) break;
        }
        return sb.ToString();
    }

    /// <summary>ms → "12:34.5". 랭킹 표와 결과 화면이 같은 형식을 쓰게 한다.</summary>
    public static string Format(long ms)
    {
        if (ms < 0) ms = 0;
        long totalTenths = ms / 100;
        long minutes = totalTenths / 600;
        long seconds = (totalTenths / 10) % 60;
        long tenths = totalTenths % 10;
        return $"{minutes:00}:{seconds:00}.{tenths}";
    }

    /// <summary>상위 10개를 받아온다. 실패하면 null을 넘긴다 — 랭킹이 죽어도 게임은 돌아야 한다.</summary>
    public static void Fetch(Action<Entry[]> onDone)
    {
        Instance.StartCoroutine(Instance.FetchRoutine(onDone));
    }

    /// <summary>기록을 올리고 갱신된 상위 10개를 받는다. 실패하면 null.</summary>
    public static void Submit(string playerName, long ms, Action<Entry[]> onDone)
    {
        Instance.StartCoroutine(Instance.SubmitRoutine(Sanitize(playerName), ms, onDone));
    }

    private IEnumerator FetchRoutine(Action<Entry[]> onDone)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(Url()))
        {
            req.timeout = TimeoutSeconds;
            yield return req.SendWebRequest();
            onDone?.Invoke(Parse(req));
        }
    }

    private IEnumerator SubmitRoutine(string playerName, long ms, Action<Entry[]> onDone)
    {
        string json = JsonUtility.ToJson(new SubmitBody { name = playerName, ms = ms });

        // UnityWebRequest.Post(string, string)은 폼 인코딩이라 JSON이 깨진다.
        // 반드시 raw 업로드 핸들러로 붙여야 한다.
        using (var req = new UnityWebRequest(Url(), UnityWebRequest.kHttpVerbPOST))
        {
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(payload);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = TimeoutSeconds;

            yield return req.SendWebRequest();
            onDone?.Invoke(Parse(req));
        }
    }

    private static Entry[] Parse(UnityWebRequest req)
    {
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Leaderboard] {req.responseCode} {req.error} — 랭킹 없이 진행한다.");
            return null;
        }

        try
        {
            EntryList list = JsonUtility.FromJson<EntryList>(req.downloadHandler.text);
            return list != null && list.entries != null ? list.entries : Array.Empty<Entry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Leaderboard] 응답 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static string Url() => Origin() + Path;

    /// <summary>
    /// 페이지가 열린 오리진. "https://host" 까지만 남긴다.
    /// 에디터에서는 absoluteURL이 비어 있어 프로덕션으로 폴백한다.
    /// </summary>
    private static string Origin()
    {
        string abs = Application.absoluteURL;
        if (string.IsNullOrEmpty(abs)) return EditorFallbackOrigin;

        int schemeEnd = abs.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0) return EditorFallbackOrigin;

        int slash = abs.IndexOf('/', schemeEnd + 3);
        return slash < 0 ? abs : abs.Substring(0, slash);
    }
}
