using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Audio feedback for the demo and the product. All clips are generated at runtime
    /// (no audio assets to import), so the demo sounds good on a fresh import. Swap
    /// <see cref="ClipProvider"/> for your own AudioClips in production.
    /// </summary>
    public class AudioFeedback : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip clickClip;
        private AudioClip craftClip;
        private AudioClip equipClip;
        private AudioClip failClip;

        /// <summary>Override with your own clips (Phase 1 polish → real sounds).</summary>
        public AudioClip ClickClip, CraftClip, EquipClip, FailClip;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        public void PlayClick() => Play(ClickClip != null ? ClickClip : (clickClip = BuildTone(0.07f, 640f, 0.18f)));
        public void PlayCraftComplete() => Play(CraftClip != null ? CraftClip : (craftClip = BuildChord(0.35f, new[] { 523.25f, 659.25f, 783.99f }, 0.28f)));
        public void PlayEquip() => Play(EquipClip != null ? EquipClip : (equipClip = BuildTone(0.16f, 880f, 0.22f)));
        public void PlayFail() => Play(FailClip != null ? FailClip : (failClip = BuildTone(0.3f, 160f, 0.25f)));

        private void Play(AudioClip clip)
        {
            if (clip == null || source == null) return;
            source.PlayOneShot(clip);
        }

        /// <summary>Simple sine tone with a decaying envelope.</summary>
        public static AudioClip BuildTone(float duration, float frequency, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, (int)(duration * sampleRate));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }
            var clip = AudioClip.Create("tone_" + frequency, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Arpeggio-ish chord (success jingle).</summary>
        public static AudioClip BuildChord(float duration, float[] frequencies, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, (int)(duration * sampleRate));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)samples);
                float v = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                    v += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t);
                data[i] = v / frequencies.Length * envelope * volume;
            }
            var clip = AudioClip.Create("chord", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
