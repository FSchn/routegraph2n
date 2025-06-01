using ZusiCLIProject.Routegraph2;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public class GleisfunktionVisualisierung : Visualisierung
    {
        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            item.Stroke = new SolidColorBrush(item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.KeineGleisfunktion)
                ? Colors.LightGray : Colors.Black);

		}
        public override Segmentierer Segmentierer { get { return new GleisfunktionSegmentierer(); } }
        public override System.Windows.Controls.Canvas? Legende { get { return null; } }
    }
}
