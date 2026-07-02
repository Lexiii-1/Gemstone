using System.Web;
using UnityEngine;

namespace Gemstone.Gemstone;

public static class Localization
{
    private static readonly Dictionary<string, string> TranslationCache = new();
    private static readonly HashSet<string>            PendingRequests  = [];
    private static readonly HttpClient                 HttpClient       = new();

    private static int lastLanguageId = -1;

    private static int CurrentLanguage => ModConfig.instance.Language.Value;

    public static string Get(string key)
    {
        int langId = CurrentLanguage;

        if (langId == 1) return key;

        if (langId != lastLanguageId)
        {
            TranslationCache.Clear();
            PendingRequests.Clear();
            lastLanguageId = langId;
        }

        if (TranslationCache.TryGetValue(key, out string translated))
            return translated;

        if (!PendingRequests.Contains(key))
            _ = TranslateAsync(key, langId);

        return "...";
    }

    private static async Task TranslateAsync(string key, int langId)
    {
        if (langId == 1) return;

        PendingRequests.Add(key);

        try
        {
            string targetCode = GetLanguageCode(langId);

            string url =
                    $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl={targetCode}&dt=t&q={HttpUtility.UrlEncode(key)}";

            string response       = await HttpClient.GetStringAsync(url);
            string translatedText = ParseGoogleResponse(response);

            if (!string.IsNullOrEmpty(translatedText))
                if (langId == CurrentLanguage)
                {
                    TranslationCache[key] = translatedText;

                    UnityMainThreadDispatcher.Instance().Enqueue(() => { Main.instance.RefreshMenu(); });
                }
        }
        catch (Exception e)
        {
            Debug.LogError($"translation failed for {key}: {e.Message}");
        }
        finally
        {
            PendingRequests.Remove(key);
        }
    }

    private static string GetLanguageCode(int id)
    {
        return id switch
               {
                       2     => "es",
                       3     => "de",
                       4     => "ru",
                       5     => "pl",
                       var _ => "en",
               };
    }

    private static string? ParseGoogleResponse(string json)
    {
        try
        {
            int start = json.IndexOf("\"", StringComparison.Ordinal) + 1;
            int end   = json.IndexOf("\"", start, StringComparison.Ordinal);

            if (start > 0 && end > start)
                return json.Substring(start, end - start);

            return null;
        }
        catch
        {
            return null;
        }
    }
}

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action>              ExecutionQueue = new();
    private static          UnityMainThreadDispatcher? instance;

    public void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
                ExecutionQueue.Dequeue().Invoke();
        }
    }

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance != null)
            return instance;

        instance = FindObjectOfType<UnityMainThreadDispatcher>();

        if (instance != null)
            return instance;

        GameObject obj = new("MainThreadDispatcher");
        instance = obj.AddComponent<UnityMainThreadDispatcher>();
        DontDestroyOnLoad(obj);

        return instance;
    }

    public void Enqueue(Action action)
    {
        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(action);
        }
    }
}