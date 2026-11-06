using System.Collections;
using Duzhe.Presentation;
namespace Duzhe.Scenes {
 public class ChapterMemoryCast : PresentationCast {
  private string ImageId(int index) {
   if(presentationId=="PR_CH01_Memory") return "CG"+(9+index).ToString("00")+"_FamilyMemory0"+(index+1);
   if(presentationId=="PR_CH03_Memory") return "CG"+(12+index).ToString("00")+"_BandMemory0"+(index+1);
   return "CG"+(15+index).ToString("00")+"_TeacherMemory0"+(index+1);
  }
  public override IEnumerator OnPresentationBegin(string id) { PresentationCG.Show(ImageId(0)); yield break; }
  public override IEnumerator OnPresentationSegmentFinished(string id,string segment) { if(segment.EndsWith("Memory01")) PresentationCG.Show(ImageId(1)); if(segment.EndsWith("Memory02")) PresentationCG.Show(ImageId(2)); yield break; }
  public override IEnumerator OnPresentationVisuals(string id) { PresentationCG.Hide(); yield break; }
 }
}