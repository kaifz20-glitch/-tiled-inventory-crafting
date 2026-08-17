using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Resolves the built-in UI font without requiring TextMeshPro essentials or any
    /// imported assets. Works on Unity 2022 LTS (Arial.ttf) and Unity 6
    /// (LegacyRuntime.ttf). Override <see cref="Default"/> to use your own font.
    /// </summary>
    public static class Fonts
    {
        private static Font cached;

        public static Font Default
        {
            get
            {
                if (cached != null) return cached;
                cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cached == null) cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return cached;
            }
        }
    }
}
