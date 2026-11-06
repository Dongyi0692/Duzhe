using System;

namespace Duzhe.Core
{
    /// Immutable gameplay event (TechnicalAndInteractionSpec section 4).
    /// Every event carries chapterId/stageId/sourceId/runId; stale or duplicate
    /// events must be dropped by consumers, never re-processed.
    public struct GameEvent
    {
        public GameEventType type;
        public string chapterId;
        public string stageId;
        public string sourceId;
        public string runId;

        public GameEvent(GameEventType type, string chapterId, string stageId, string sourceId, string runId)
        {
            this.type = type;
            this.chapterId = chapterId ?? string.Empty;
            this.stageId = stageId ?? string.Empty;
            this.sourceId = sourceId ?? string.Empty;
            this.runId = runId ?? string.Empty;
        }

        public override string ToString()
        {
            return string.Format("[GameEvent {0} ch={1} stage={2} src={3} run={4}]",
                type, chapterId, stageId, sourceId, runId);
        }
    }
}
