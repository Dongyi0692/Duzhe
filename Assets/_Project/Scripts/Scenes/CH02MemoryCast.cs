using System.Collections;
using Duzhe.Presentation;
namespace Duzhe.Scenes
{
    public class CH02MemoryCast : PresentationCast
    {
        public override IEnumerator OnPresentationBegin(string presId)
        {
            PresentationCG.Show("CG06_LoversMemory01");
            yield break;
        }
        public override IEnumerator OnPresentationSegmentFinished(string presId, string segmentId)
        {
            if (segmentId == "CH02_Memory01") PresentationCG.Show("CG07_LoversMemory02");
            if (segmentId == "CH02_Memory02") PresentationCG.Show("CG08_LoversMemory03");
            yield break;
        }
        public override IEnumerator OnPresentationVisuals(string presId)
        {
            PresentationCG.Hide();
            yield break;
        }
    }
}
