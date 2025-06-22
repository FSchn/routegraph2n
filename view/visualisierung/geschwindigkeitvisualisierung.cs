using ZusiCLIProject.Routegraph2;
using Color = System.Windows.Media.Color;
using System.Windows.Media;
using ZusiCLIProject.FileLibrary.Zusi3;
using System;

namespace ZusiCLIProject.Routegraph2
{
    public class GeschwindigkeitVisualisierung : Visualisierung
    {
        private static Color FarbeByV(int geschwindigkeit)
        {
            if (geschwindigkeit <= 0) {
                return Color.FromRgb(128, 128, 128);
			} else {
                int[] colormap = new int[17] { 300, 286, 270, 258, 247, 214, 197, 180, 160, 130, 100, 70, 58, 50, 40, 30, 0 };
                int hue = colormap[Math.Min(16, (geschwindigkeit - 1) / 10)];  /* <= 10 km/h, <= 20 km/h, ..., >= 170 km/h */
                return ColorFromHSV(hue, 255, hue == 130 ? 230 : 255);
            }
        }

        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            var geschwindigkeit = item.Start.vMax;
            if (item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.KeineGleisfunktion))
                item.Stroke = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            else
                item.Stroke = new SolidColorBrush(FarbeByV((int)Math.Round(geschwindigkeit * 3.6)));
        }

        public override Segmentierer Segmentierer { get { return new GeschwindigkeitSegmentierer(); } }
        public override System.Windows.Controls.Canvas Legende
        {
            get
            {
				System.Windows.Controls.Canvas result = new System.Windows.Controls.Canvas();
                var segmentierer = Segmentierer;
                Strecke.ElementInfo pseudoelement = new();
                pseudoelement.ParentBuffer = new Strecke.Element();
                pseudoelement.ParentBuffer.GreenDirectionInfoIfSetOnZusi = pseudoelement;
                for (int v = 0; v <= 170; v += 10) {
                    pseudoelement.vMax = v / 3.6f;
                    NeuesLegendeElement(result, segmentierer, pseudoelement, v == 0 ? "undefiniert" :
                    (v == 170 ? "> 160" : ("⩽ " + v.ToString())));
            }
            return result;
            }
        }
    }
}
