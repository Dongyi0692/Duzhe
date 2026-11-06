using System;
using System.Collections.Generic;
using Duzhe.Chapters;
using UnityEngine;

namespace Duzhe.River
{
    /// One planned whirlpool wave. Lane indices are 0..2 mapping to x -1..+1
    /// lane spacing. All randomness flows through the chapter's river seed so
    /// a retry reproduces the exact same waves (learnable), while a replay of
    /// the chapter gets a fresh seed.
    [Serializable]
    public class RiverWave
    {
        public float arrivalTime;
        public int safeLane;
        public int[] bannedLanes = new int[0];
        /// Whirlpool x position per banned lane (includes visual-only jitter).
        public float[] whirlXs = new float[0];
        public float[] whirlVelocitiesX = new float[0];
    }

    /// Deterministic wave planning + reachability checks, shared by the
    /// runtime spawner and the editor seed validator (spec section 7:
    /// every wave keeps a reachable safe route, neighbouring safe lanes move
    /// at most one lane, whirlpools are visible early enough).
    public static class RiverRules
    {
        public const int LaneCount = 3;
        public const float ReactionTimeSeconds = 0.5f;
        /// Reference camera half height (kept in sync with GameBootstrap).
        public const float CameraHalfHeight = 5.625f;

        public static float LaneX(int lane, float laneSpacing)
        {
            return (lane - 1) * laneSpacing;
        }

        /// Seconds from a whirlpool first entering the visible band (camera
        /// top edge at boatY + cameraYOffset + half height) until it reaches
        /// the boat line. THE single geometry contract shared by the runtime
        /// and the validator - spawnLeadSeconds only decides WHERE a whirlpool
        /// is created (which must be outside the visible band), never when
        /// the player may start reacting.
        public static float VisibleWindow(RiverSettings s)
        {
            return (s.cameraYOffset + CameraHalfHeight) / s.forwardSpeed;
        }

        public static List<RiverWave> PlanWaves(int seed, RiverSettings s)
        {
            System.Random rng = new System.Random(seed);
            List<RiverWave> waves = new List<RiverWave>();
            int safeLane = 1; // start centred
            foreach (float arrival in s.waveArrivalTimes)
            {
                // Safe-lane random walk: stay or move one lane at most.
                int step = rng.Next(3) - 1;
                safeLane = Mathf.Clamp(safeLane + step, 0, LaneCount - 1);

                // Banned lanes are picked from the non-safe lanes.
                List<int> candidates = new List<int>();
                for (int l = 0; l < LaneCount; l++)
                {
                    if (l != safeLane) candidates.Add(l);
                }
                int banCount = Mathf.Min(s.maxBannedLanesPerWave, candidates.Count);
                // Fisher-Yates with the seeded rng.
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    int tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
                }

                RiverWave wave = new RiverWave();
                wave.arrivalTime = arrival;
                wave.safeLane = safeLane;
                wave.bannedLanes = new int[banCount];
                wave.whirlXs = new float[banCount];
                wave.whirlVelocitiesX = new float[banCount];
                for (int i = 0; i < banCount; i++)
                {
                    int lane = candidates[i];
                    wave.bannedLanes[i] = lane;
                    double jitter = (rng.NextDouble() * 2.0 - 1.0) * s.whirlJitterX;
                    wave.whirlXs[i] = LaneX(lane, s.laneSpacing) + (float)jitter;
                    wave.whirlVelocitiesX[i] = s.whirlDriftSpeed * (rng.Next(2) == 0 ? -1f : 1f);
                }
                waves.Add(wave);
            }
            return waves;
        }

        /// Simulates a boat that follows the safe lanes from x=0 (with a
        /// reaction delay per wave) and returns a human-readable failure
        /// reason, or null when the plan is fully reachable.
        public static string ValidatePlan(List<RiverWave> waves, RiverSettings s)
        {
            if (waves == null || waves.Count == 0) return "no waves planned";
            float prevSafe = 1;
            float boatX = LaneX(1, s.laneSpacing); // boats start centred
            for (int i = 0; i < waves.Count; i++)
            {
                RiverWave w = waves[i];

                // Wave sanity.
                if (w.safeLane < 0 || w.safeLane >= LaneCount) return "wave " + i + ": safeLane out of range";
                for (int b = 0; b < w.bannedLanes.Length; b++)
                {
                    if (w.bannedLanes[b] == w.safeLane) return "wave " + i + ": bans the safe lane";
                }

                // Neighbouring safe lanes move at most one lane.
                if (Mathf.Abs(w.safeLane - prevSafe) > 1)
                {
                    return "wave " + i + ": safe lane jumps " + prevSafe + "->" + w.safeLane;
                }

                // Time window: the boat (parked on the previous safe lane)
                // must reach this wave's safe lane before arrival, after a
                // reaction delay - and before the wave's whirlpools reach the
                // boat line, whirlpools may never spawn on top of the boat.
                float targetX = LaneX(w.safeLane, s.laneSpacing);
                float travel = Mathf.Abs(targetX - boatX);
                float timeNeeded = travel / s.steerSpeed + ReactionTimeSeconds;
                float timeAvailable = w.arrivalTime - (i == 0 ? 0f : waves[i - 1].arrivalTime);
                if (timeAvailable <= 0f) return "wave " + i + ": non-positive wave gap";
                if (timeNeeded > timeAvailable)
                {
                    return "wave " + i + ": needs " + timeNeeded.ToString("F2")
                        + "s but gap is " + timeAvailable.ToString("F2") + "s";
                }
                // Whirlpool spawn must not overlap the starting boat spot and
                // must be visible at least 1.5s before arrival.
                for (int b = 0; b < w.bannedLanes.Length; b++)
                {
                    float laneX = LaneX(w.bannedLanes[b], s.laneSpacing);
                    float wx = w.whirlXs[b];
                    if (Mathf.Abs(wx - boatX) < s.whirlHalfWidth + s.boatHalfWidth + 0.2f &&
                        w.arrivalTime - s.spawnLeadSeconds <= 0.01f)
                    {
                        return "wave " + i + ": whirlpool spawns overlapping the start position";
                    }
                    if (Mathf.Abs(wx - laneX) > s.whirlJitterX + 0.001f)
                    {
                        return "wave " + i + ": whirlpool jitter exceeds config";
                    }
                }

                // Visibility geometry: a whirlpool enters the visible band at
                // boatY + cameraYOffset + half camera height, so the time
                // from "first visible" to "reaches the boat line" is
                // (cameraYOffset + halfHeight) / forwardSpeed. That is the
                // player's real reaction window and must be >= 1.5s. Also the
                // spawn position (boatY + fwd * lead) must start OUTSIDE the
                // visible band, i.e. lead >= visible window.
                float visibleWindow = VisibleWindow(s);
                if (visibleWindow < 1.5f)
                {
                    return "wave " + i + ": visibility window " + visibleWindow.ToString("F2")
                        + "s < 1.5s (raise cameraYOffset)";
                }
                if (s.spawnLeadSeconds < visibleWindow)
                {
                    return "wave " + i + ": spawn lead " + s.spawnLeadSeconds.ToString("F2")
                        + "s < visible window " + visibleWindow.ToString("F2")
                        + "s (whirlpool would spawn on screen)";
                }

                prevSafe = w.safeLane;
                boatX = targetX;
            }

            // The sail duration must outlast the last arrival.
            float lastArrival = waves[waves.Count - 1].arrivalTime;
            if (s.sailDuration < lastArrival + 0.5f)
            {
                return "sail duration shorter than last wave arrival";
            }
            return null;
        }

        /// Runs the same reachability math without any Unity objects; used by
        /// the editor seed validator. Returns null when a seed is playable.
        public static string SimulateSeed(int seed, RiverSettings s)
        {
            List<RiverWave> waves = PlanWaves(seed, s);
            return ValidatePlan(waves, s);
        }
    }
}
