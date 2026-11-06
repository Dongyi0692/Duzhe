using UnityEngine;

namespace Duzhe.Scenes
{
    /// Classroom scene behaviour for CH04. The teacher, the student and the
    /// blackboard hotspot are assembled at FIXED R04 positions, so the
    /// auto-started reunion/lesson dialogue can never race a walk-in.
    /// Only the farewell needs the references (fade the teacher, keep the
    /// student and the room).
    public class ClassroomCast : MonoBehaviour
    {
        [Header("R04 Classroom layout (world) - C12 framing")]
        public Vector2 studentSpot = new Vector2(-1f, -1.75f);   // CL_Student (608,416)
        public Vector2 teacherSpot = new Vector2(3f, 2.6f);      // moved down into the arrival frame
        public Vector2 blackboardSpot = new Vector2(4f, 4f);     // reachable in front of CL_Blackboard

        [Header("Wired by ProjectSetupUtility")]
        public GameObject student;
        public GameObject teacher;
        public GameObject blackboardProp;
    }
}
