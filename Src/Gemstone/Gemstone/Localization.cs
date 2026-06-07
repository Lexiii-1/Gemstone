using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using UnityEngine;
using System.Threading.Tasks;

namespace Gemstone.Gemstone
{
    public static class Localization
    {
        private static readonly Dictionary<string, string> _translationCache = new Dictionary<string, string>();
        private static readonly HashSet<string> _pendingRequests = new HashSet<string>();
        private static readonly HttpClient _httpClient = new HttpClient();

        private static int _lastLanguageId = -1;

        // 1 english
        // 2 spanish
        // 3 german
        // 4 russian
        // 5 polish
        public static int CurrentLanguage => ModConfig.instance.Language != null ? ModConfig.instance.Language.Value : 1;

        public static string Get(string key)
        {
            int langId = CurrentLanguage;
            if (langId == 1) return key;

            if (langId != _lastLanguageId)
            {
                _translationCache.Clear();
                _pendingRequests.Clear();
                _lastLanguageId = langId;
            }

            if (_translationCache.TryGetValue(key, out string translated))
            {
                return translated;
            }

            if (!_pendingRequests.Contains(key))
            {
                _ = TranslateAsync(key, langId);
            }

            return "...";
        }

        private static async Task TranslateAsync(string key, int langId)
        {
            _pendingRequests.Add(key);

            try
            {
                string targetCode = GetLanguageCode(langId);

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl={targetCode}&dt=t&q={HttpUtility.UrlEncode(key)}";

                string response = await _httpClient.GetStringAsync(url);
                string translatedText = ParseGoogleResponse(response);

                if (!string.IsNullOrEmpty(translatedText))
                {
                    if (langId == CurrentLanguage)
                    {
                        _translationCache[key] = translatedText;

                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            Main.instance.RefreshMenu();
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"translation failed for {key}: {e.Message}");
            }
            finally
            {
                _pendingRequests.Remove(key);
            }
        }

        private static string GetLanguageCode(int id)
        {
            return id switch
            {
                2 => "es", // spanish
                3 => "de", // german
                4 => "ru", // russian
                5 => "pl", // polish
                _ => "en"  // english
            };
        }

        private static string ParseGoogleResponse(string json)
        {
            try
            {
                int start = json.IndexOf("\"") + 1;
                int end = json.IndexOf("\"", start);

                if (start > 0 && end > start)
                {
                    return json.Substring(start, end - start);
                }
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
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static UnityMainThreadDispatcher _instance;

        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<UnityMainThreadDispatcher>();
                if (_instance == null)
                {
                    var obj = new GameObject("MainThreadDispatcher");
                    _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }

        public void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        public void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }
    }
}