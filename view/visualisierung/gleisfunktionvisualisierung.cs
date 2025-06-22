using ZusiCLIProject.Routegraph2;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public class GleisfunktionVisualisierung : Visualisierung
    {
        public GleisfunktionVisualisierung(Visualisierung? gleisdarstellung = null)
        {
            Gleisdarstellung = gleisdarstellung;
        }
        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            if (item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.KeineGleisfunktion))
                item.Stroke = new SolidColorBrush(Colors.LightGray);
            else if (Gleisdarstellung == null)
                item.Stroke = new SolidColorBrush(Colors.Black);
            else
                Gleisdarstellung.SetzeDarstellung(item);
		}
		public Visualisierung? Gleisdarstellung { get; private set; }
		public override Segmentierer Segmentierer { get { return new GleisfunktionSegmentierer((Gleisdarstellung == null) ? null : Gleisdarstellung.Segmentierer); } }
        public override System.Windows.Controls.Canvas? Legende { get { return (Gleisdarstellung == null) ? null : Gleisdarstellung.Legende; } }
		public override float? LegendeWidth { get { return (Gleisdarstellung == null) ? null : Gleisdarstellung.LegendeWidth; } }
	}
}
