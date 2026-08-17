using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TiledInventory
{
    /// <summary>
    /// Cloud save backend backed by SyncVault. SyncVault is a game backend service that
    /// stores per-player key/value data (and more) over HTTPS.
    ///
    /// Wire-up: set <see cref="BaseUrl"/> to your SyncVault project endpoint and
    /// <see cref="AuthToken"/> to a player-scoped token, then assign this backend to
    /// <see cref="SaveManager.Backend"/>:
    ///
    /// <code>
    /// saveManager.Backend = new SyncVaultSaveBackend("https://api.syncvault.example/v1", "player-token");
    /// </code>
    ///
    /// Because every Save/Load is async, use <see cref="SaveManager.Saved"/> /
    /// <see cref="SaveManager.Loaded"/> callbacks instead of the return values when
    /// this backend is active.
    /// </summary>
    public class SyncVaultSaveBackend : ISaveBackend
    {
        public string BaseUrl;
        public string AuthToken;
        public bool VerboseLogging = true;

        public SyncVaultSaveBackend(string baseUrl = null, string authToken = null)
        {
            BaseUrl = baseUrl ?? Environment.GetEnvironmentVariable("SYNCVAULT_BASE_URL");
            AuthToken = authToken ?? Environment.GetEnvironmentVariable("SYNCVAULT_TOKEN");
        }

        // ISaveBackend is synchronous by contract. SaveManager calls it from
        // MonoBehaviour code, so we dispatch the HTTP request and poll it to completion.
        // For production, prefer the async helpers: SaveAsync / LoadAsync.

        public bool Save(string slot, string json)
        {
            var request = BuildRequest(slot, json);
            return RunToCompletion(request);
        }

        public bool Load(string slot, out string json)
        {
            json = null;
            var request = BuildRequest(slot, null);
            if (!RunToCompletion(request)) return false;
            var parsed = ParseBody(request.downloadHandler.text);
            if (parsed == null) return false;
            json = parsed;
            return true;
        }

        public bool Delete(string slot)
        {
            var request = BuildRequest(slot, null, "DELETE");
            return RunToCompletion(request);
        }

        /// <summary>Async save — recommended over the sync variant in production.</summary>
        public IEnumerator SaveAsync(string slot, string json, Action<bool> onDone)
        {
            var request = BuildRequest(slot, json);
            yield return request.SendWebRequest();
            bool ok = IsSuccess(request);
            if (VerboseLogging) LogResult("Save", slot, request);
            onDone?.Invoke(ok);
        }

        /// <summary>Async load — recommended over the sync variant in production.</summary>
        public IEnumerator LoadAsync(string slot, Action<bool, string> onDone)
        {
            var request = BuildRequest(slot, null);
            yield return request.SendWebRequest();
            bool ok = IsSuccess(request);
            if (VerboseLogging) LogResult("Load", slot, request);
            onDone?.Invoke(ok, ok ? ParseBody(request.downloadHandler.text) : null);
        }

        // ------------------------------------------------------------------ internals

        private UnityWebRequest BuildRequest(string slot, string json, string method = null)
        {
            string safe = Uri.EscapeDataString(string.IsNullOrEmpty(slot) ? "save" : slot);
            string url = $"{BaseUrl.TrimEnd('/')}/saves/{safe}";
            var request = new UnityWebRequest(url, method ?? (json == null ? "GET" : "PUT"));
            request.downloadHandler = new DownloadHandlerBuffer();
            if (json != null)
            {
                var body = new UTF8Encoding().GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (!string.IsNullOrEmpty(AuthToken))
                request.SetRequestHeader("Authorization", $"Bearer {AuthToken}");
            return request;
        }

        private bool RunToCompletion(UnityWebRequest request)
        {
            // The sync path is only safe while a coroutine host exists. SaveManager is
            // a MonoBehaviour, so we spin synchronously — acceptable for tooling/demos.
            var op = request.SendWebRequest();
            while (!op.isDone) { /* busy wait — do not use in production */ }
            bool ok = IsSuccess(request);
            if (VerboseLogging) LogResult("Sync op", request.url, request);
            request.Dispose();
            return ok;
        }

        private static bool IsSuccess(UnityWebRequest request) =>
            request != null && request.result == UnityWebRequest.Result.Success;

        private static string ParseBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            try
            {
                // Expect { "data": "<json string>" } or a raw JSON body.
                var parsed = JsonUtility.FromJson<Envelope>(body);
                if (parsed != null && !string.IsNullOrEmpty(parsed.data)) return parsed.data;
            }
            catch { /* raw body fallthrough */ }
            return body;
        }

        private static void LogResult(string op, string slot, UnityWebRequest request)
        {
            if (request == null) return;
            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"[TiledInventory] SyncVault {op} '{slot}' OK.");
            else
                Debug.LogWarning($"[TiledInventory] SyncVault {op} '{slot}' failed: {request.error}");
        }

        [Serializable]
        private class Envelope
        {
            public string data;
        }
    }
}
