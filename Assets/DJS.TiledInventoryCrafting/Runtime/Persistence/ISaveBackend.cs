using System;
using System.IO;
using UnityEngine;

namespace TiledInventory
{
    /// <summary>
    /// Where save blobs live. Swap implementations freely:
    /// <see cref="LocalFileSaveBackend"/> for single-player, or a cloud backend
    /// (see SyncVaultSaveBackend) for cross-device / multiplayer persistence.
    /// </summary>
    public interface ISaveBackend
    {
        /// <summary>Persist a JSON blob under a named slot. Returns success.</summary>
        bool Save(string slot, string json);

        /// <summary>Load a JSON blob from a named slot. Returns false when missing or corrupt.</summary>
        bool Load(string slot, out string json);

        /// <summary>Remove a slot. Returns success.</summary>
        bool Delete(string slot);
    }

    /// <summary>Writes saves to Application.persistentDataPath — works on every platform.</summary>
    public class LocalFileSaveBackend : ISaveBackend
    {
        public static string DefaultDirectory =>
            Path.Combine(Application.persistentDataPath, "TiledInventory", "saves");

        private readonly string directory;

        public LocalFileSaveBackend(string directory = null)
        {
            this.directory = directory ?? DefaultDirectory;
        }

        private string PathFor(string slot)
        {
            string safe = string.IsNullOrEmpty(slot) ? "save" : slot;
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            return Path.Combine(directory, safe + ".json");
        }

        public bool Save(string slot, string json)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(PathFor(slot), json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TiledInventory] Save failed for slot '{slot}': {e.Message}");
                return false;
            }
        }

        public bool Load(string slot, out string json)
        {
            json = null;
            try
            {
                string path = PathFor(slot);
                if (!File.Exists(path)) return false;
                json = File.ReadAllText(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TiledInventory] Load failed for slot '{slot}': {e.Message}");
                return false;
            }
        }

        public bool Delete(string slot)
        {
            try
            {
                string path = PathFor(slot);
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TiledInventory] Delete failed for slot '{slot}': {e.Message}");
                return false;
            }
        }
    }
}
