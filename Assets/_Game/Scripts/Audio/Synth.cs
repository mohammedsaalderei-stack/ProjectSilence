// Synth.cs - written with AI help (Claude, 25 Nov 2026), copied from the AI-made Silence demo. List it in READ THIS DOCTOR.txt.
using System;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------------------------
// Synth.cs
// Makes every sound effect FROM CODE (no audio files needed) with simple additive synthesis:
// add a few sine waves together and shape them with an envelope. This gives the game
// "distinct audio cues" (proposal Section 6) on day one. Replace them with real recordings
// from your audio teammate whenever you like - just pass a real AudioClip to Sfx.Play instead.
//
// It also composes a short lute-like song (Karplus-Strong plucked strings) for the field,
// so the Silence has music to take away.
// ---------------------------------------------------------------------------------------------
namespace ProjectSilence
{
    public static class Synth
    {
        public const int Rate = 44100;
        public const float FieldSongBpm = 90f;

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly System.Random noise = new System.Random(7);

        const float TwoPi = Mathf.PI * 2f;

        static float Sin(float hz, float t) => Mathf.Sin(TwoPi * hz * t);
        static float Noise() => (float)(noise.NextDouble() * 2.0 - 1.0);

        /// <summary>Quick attack then exponential decay.</summary>
        static float Env(float t, float attack, float decay) =>
            t < attack ? t / attack : Mathf.Exp(-(t - attack) / Mathf.Max(0.0001f, decay));

        static AudioClip Make(string key, float seconds, Func<float, float> wave)
        {
            if (cache.TryGetValue(key, out AudioClip cached) && cached != null) return cached;
            int samples = Mathf.CeilToInt(seconds * Rate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = wave(i / (float)Rate);

            // Short fade-out so nothing clicks at the end.
            int fade = Mathf.Min(samples, 256);
            for (int i = 0; i < fade; i++) data[samples - 1 - i] *= i / (float)fade;

            AudioClip clip = AudioClip.Create(key, samples, 1, Rate, false);
            clip.SetData(data, 0);
            cache[key] = clip;
            return clip;
        }

        // ---- Musical notes ------------------------------------------------------------------
        /// <summary>A soft bell/celesta note. Used for on-beat hits (each hero has a note).</summary>
        public static AudioClip Bell(float hz, float decay = 0.6f) =>
            Make($"bell_{hz:F1}_{decay:F2}", decay * 3.2f, t =>
                Env(t, 0.004f, decay) * (Sin(hz, t) + 0.5f * Sin(hz * 2f, t) + 0.22f * Sin(hz * 3.01f, t) + 0.1f * Sin(hz * 4.2f, t)) * 0.42f);

        /// <summary>A big inharmonic church bell (Bell Shrines, the Lowbell).</summary>
        public static AudioClip ChurchBell(float hz = 196f) =>
            Make($"church_{hz:F0}", 4.5f, t =>
            {
                float s = 0.9f * Sin(hz * 0.5f, t) * Mathf.Exp(-t / 2.4f)
                        + 1.0f * Sin(hz, t) * Mathf.Exp(-t / 1.6f)
                        + 0.6f * Sin(hz * 1.183f, t) * Mathf.Exp(-t / 1.2f)
                        + 0.45f * Sin(hz * 1.506f, t) * Mathf.Exp(-t / 1.0f)
                        + 0.35f * Sin(hz * 2.0f, t) * Mathf.Exp(-t / 0.8f)
                        + 0.25f * Sin(hz * 2.514f, t) * Mathf.Exp(-t / 0.6f)
                        + 0.15f * Sin(hz * 3.011f, t) * Mathf.Exp(-t / 0.4f);
                return s * 0.22f * Mathf.Min(1f, t / 0.003f);
            });

        // ---- Feedback cues ------------------------------------------------------------------
        public static AudioClip Tick => Make("tick", 0.06f, t => Env(t, 0.001f, 0.012f) * Sin(1760f, t) * 0.55f);
        public static AudioClip TickAccent => Make("tickAccent", 0.08f, t => Env(t, 0.001f, 0.02f) * Sin(2349f, t) * 0.6f);

        public static AudioClip Miss => Make("miss", 0.4f, t =>
            Env(t, 0.002f, 0.12f) * (Sin(98f, t) + 0.8f * Sin(104f, t) + 0.25f * Noise()) * 0.4f);

        public static AudioClip Hit => Make("hit", 0.3f, t =>
            Env(t, 0.001f, 0.06f) * (0.6f * Noise() + 0.9f * Sin(60f + 90f * Mathf.Exp(-t * 25f), t)) * 0.5f);

        public static AudioClip Block => Make("block", 0.35f, t =>
            Env(t, 0.001f, 0.08f) * (Sin(880f, t) + 0.6f * Sin(1320f, t) + 0.3f * Noise()) * 0.3f);

        public static AudioClip Danger => Make("danger", 0.35f, t =>
            Env(t, 0.005f, 0.2f) * (Sin(1318.5f, t) + Sin(1396.9f, t)) * (0.6f + 0.4f * Sin(16f, t)) * 0.22f);

        /// <summary>Looping high tremolo used when you aim at an enemy that is about to attack.</summary>
        public static AudioClip TensionLoop => Make("tension", 2f, t =>
            (Sin(1320f, t) + Sin(1400f, t)) * (0.55f + 0.45f * Sin(8f, t)) * 0.3f);

        public static AudioClip Explosion
        {
            get
            {
                float lowpass = 0f;
                return Make("boom", 1.4f, t =>
                {
                    lowpass += (Noise() - lowpass) * 0.07f;
                    return Env(t, 0.002f, 0.38f) * (2.4f * lowpass + 0.7f * Sin(45f, t) * Mathf.Exp(-t * 4f)) * 0.7f;
                });
            }
        }

        public static AudioClip Step
        {
            get
            {
                float lowpass = 0f;
                return Make("step", 0.09f, t =>
                {
                    lowpass += (Noise() - lowpass) * 0.2f;
                    return Env(t, 0.002f, 0.02f) * lowpass * 0.6f;
                });
            }
        }

        public static AudioClip UiMove => Make("uiMove", 0.05f, t => Env(t, 0.001f, 0.015f) * Sin(1046.5f, t) * 0.3f);
        public static AudioClip UiConfirm => Make("uiConfirm", 0.25f, t =>
            Env(t, 0.002f, 0.08f) * (Sin(784f, t) + (t > 0.06f ? Sin(1175f, t) : 0f)) * 0.3f);
        public static AudioClip UiBack => Make("uiBack", 0.2f, t =>
            Env(t, 0.002f, 0.06f) * (t < 0.06f ? Sin(880f, t) : Sin(659f, t)) * 0.3f);

        /// <summary>Notes of a rising arpeggio, e.g. heal sparkle or level-up fanfare.</summary>
        public static AudioClip Arpeggio(string key, float[] notesHz, float spacing, float decay) =>
            Make(key, spacing * notesHz.Length + decay * 3f, t =>
            {
                float s = 0f;
                for (int i = 0; i < notesHz.Length; i++)
                {
                    float local = t - i * spacing;
                    if (local < 0f) break;
                    s += Env(local, 0.003f, decay) * (Sin(notesHz[i], local) + 0.4f * Sin(notesHz[i] * 2f, local));
                }
                return s * 0.28f;
            });

        public static AudioClip Heal => Arpeggio("heal", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.07f, 0.35f);
        public static AudioClip LevelUp => Arpeggio("levelup", new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f, 0.4f);
        public static AudioClip Victory => Arpeggio("victory", new[] { 261.63f, 329.63f, 392f, 523.25f, 659.25f, 783.99f }, 0.11f, 0.6f);
        public static AudioClip Defeat => Arpeggio("defeat", new[] { 392f, 349.23f, 311.13f, 261.63f }, 0.28f, 0.7f);

        /// <summary>The "sound being sucked away" swell when a battle starts.</summary>
        public static AudioClip BattleStart => Make("battleStart", 1.1f, t =>
        {
            float sweep = Mathf.Lerp(200f, 900f, t / 1.1f);
            return Mathf.Min(1f, t * 3f) * Mathf.Exp(-t * 1.4f) * (Sin(sweep, t) + 0.5f * Sin(sweep * 1.5f, t)) * 0.25f;
        });

        // ---- Procedural field song ------------------------------------------------------------
        /// <summary>
        /// Composes an 8-bar loop in D minor (Dm - Dm - C - C - Bb - Bb - C - A) with a plucked
        /// "lute" arpeggio, a bass note per bar and a few high bell notes. 90 BPM, about 21 seconds.
        /// </summary>
        public static AudioClip FieldSong()
        {
            if (cache.TryGetValue("fieldSong", out AudioClip cached) && cached != null) return cached;

            float beat = 60f / FieldSongBpm;
            int bars = 8;
            int samples = Mathf.RoundToInt(bars * 4 * beat * Rate);
            var data = new float[samples];

            // MIDI note numbers for each bar's chord (root, third, fifth, octave).
            int[][] chords =
            {
                new[] { 50, 53, 57, 62 }, new[] { 50, 53, 57, 62 },   // Dm
                new[] { 48, 52, 55, 60 }, new[] { 48, 52, 55, 60 },   // C
                new[] { 46, 50, 53, 58 }, new[] { 46, 50, 53, 58 },   // Bb
                new[] { 48, 52, 55, 60 }, new[] { 45, 49, 52, 57 },   // C, A
            };
            int[] pattern = { 0, 2, 3, 1, 3, 2, 1, 2 }; // which chord tone each eighth note plays
            int[] melody = { 74, 72, 69, 72, 70, 69, 67, 69 }; // a slow high line, one note per bar

            for (int bar = 0; bar < bars; bar++)
            {
                int[] chord = chords[bar];
                float barStart = bar * 4 * beat;
                AddPluck(data, Mathf.RoundToInt(barStart * Rate), Midi(chord[0] - 12), 0.55f, 2.6f);
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int note = chord[pattern[eighth]];
                    float time = barStart + eighth * beat * 0.5f;
                    AddPluck(data, Mathf.RoundToInt(time * Rate), Midi(note), eighth % 2 == 0 ? 0.32f : 0.24f, 1.4f);
                }
                AddBell(data, Mathf.RoundToInt((barStart + beat * 2f) * Rate), Midi(melody[bar]), 0.11f, 1.2f);
            }

            float peak = 0.0001f;
            foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < samples; i++) data[i] = data[i] / peak * 0.6f;

            AudioClip clip = AudioClip.Create("fieldSong", samples, 1, Rate, false);
            clip.SetData(data, 0);
            cache["fieldSong"] = clip;
            return clip;
        }

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        /// <summary>Karplus-Strong: a burst of noise in a delay line that averages itself = plucked string.</summary>
        static void AddPluck(float[] buffer, int start, float hz, float amplitude, float seconds)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / hz));
            var ring = new float[period];
            var random = new System.Random(start * 31 + period);
            for (int i = 0; i < period; i++) ring[i] = (float)(random.NextDouble() * 2.0 - 1.0);

            int length = Mathf.RoundToInt(seconds * Rate);
            int index = 0;
            for (int i = 0; i < length; i++)
            {
                float value = ring[index];
                int next = (index + 1) % period;
                ring[index] = (value + ring[next]) * 0.5f * 0.998f;
                buffer[(start + i) % buffer.Length] += value * amplitude;
                index = next;
            }
        }

        static void AddBell(float[] buffer, int start, float hz, float amplitude, float decay)
        {
            int length = Mathf.RoundToInt(decay * 4f * Rate);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float s = Env(t, 0.004f, decay) * (Sin(hz, t) + 0.35f * Sin(hz * 2.01f, t) + 0.12f * Sin(hz * 3.0f, t));
                buffer[(start + i) % buffer.Length] += s * amplitude;
            }
        }
    }
}
