using System.Collections.Generic;
using Duzhe.Chapters;
using UnityEngine;

namespace Duzhe.River
{
    /// Crossing-local state only. A crash resets EXACTLY these fields -
    /// puzzle/handover/ritual/played-dialogue flags live in
    /// ChapterRuntimeState and are never touched here (spec section 6).
    public class RiverSession
    {
        public enum Phase { Idle, Briefing, Sailing, Crashed, Succeeded }

        public Phase phase = Phase.Idle;
        /// Briefing only shows once per visit; a retry skips it and the
        /// briefing time never counts into the 10 seconds.
        public bool briefingShown;
        /// Effective sailing time accumulated (game time, pause-freeze via
        /// deltaTime).
        public float elapsed;
        /// Boat x position this attempt.
        public float boatX;
        /// Which waves already spawned their whirlpools.
        public readonly HashSet<int> spawnedWaves = new HashSet<int>();
        /// Attempt counter for diagnostics.
        public int attemptCount;

        public void ResetAttempt()
        {
            phase = Phase.Briefing; // retries skip the briefing panel
            elapsed = 0f;
            boatX = 0f;
            spawnedWaves.Clear();
            attemptCount++;
        }
    }
}
