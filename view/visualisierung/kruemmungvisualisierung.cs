using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;
using System;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public class KruemmungSegmentierer : Segmentierer
    {
        protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            bool gleisfunktionGleich = vorgaenger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion) ==
                 nachfolger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion);
            float vorgaengerKruemmung = Math.Abs(vorgaenger.ParentBuffer.Kruemmung);
            float nachfolgerKruemmung = Math.Abs(nachfolger.ParentBuffer.Kruemmung);
            bool kruemmungGleich =
                    (vorgaengerKruemmung < KruemmungVisualisierung.MaxRadiusKruemmung && nachfolgerKruemmung < KruemmungVisualisierung.MaxRadiusKruemmung) ||
                    (vorgaengerKruemmung > KruemmungVisualisierung.MinRadiusKruemmung && nachfolgerKruemmung > KruemmungVisualisierung.MinRadiusKruemmung) ||
                    (vorgaengerKruemmung == nachfolgerKruemmung);  // absichtlicher bitweiser Float-Vergleich! Sonst fehlerhafte Segmentierung bei mehreren aufeinanderfolgenden Segmenten
            return !gleisfunktionGleich || !kruemmungGleich;
        }
    }
    public class KruemmungVisualisierung : Visualisierung
    {
        private const float MaxRadius = 10000;  // alles ueber diesem Radius ist schwarz
        private const float MidRadius = 1000;  // bis zu diesem Radius geht die Farbe von schwarz nach gruen ueber
        private const float MinRadius = 100;  // bis zu diesem Radius geht die Farbe von gruen (h=83) ueber gelb nach rot (h=0) ueber

        internal const float MaxRadiusKruemmung = 1.0f / MaxRadius;
        private const float MidRadiusKruemmung = 1.0f / MidRadius;
        internal const float MinRadiusKruemmung = 1.0f / MinRadius;

        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            float kruemmung = Math.Abs(item.Start.ParentBuffer.Kruemmung);
            float farbton = 83;
            float saettigung = 255;
            float wert = 0;
            if (kruemmung >= MidRadiusKruemmung) {
                // Kleine Radien (Weichen): Lineare Interpolation ueber den Radius
                wert = 255;
                farbton = Math.Min(83.0f, Math.Max(0.0f, 83.0f - (1 / kruemmung - MidRadius) / (MinRadius - MidRadius) * 83));
            } else if (kruemmung >= MaxRadiusKruemmung) {
                // Grosse Radien (Gleisboegen): Lineare Interpolation ueber die Kruemmung (bei einer Klothoide nimmt die Kruemmung linear zu)
                wert = Math.Min(255.0f, Math.Max(0.0f, 255.0f - (kruemmung - MidRadiusKruemmung) / (MaxRadiusKruemmung - MidRadiusKruemmung) * 255));
            }

            if (item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.KeineGleisfunktion))
            {
                saettigung /= 2;
                wert = 192 + saettigung / 2;
            }


			item.Stroke = new SolidColorBrush(ColorFromHSV((int)farbton, (int)saettigung, (int)wert));
        }
        public override Segmentierer Segmentierer { get { return new KruemmungSegmentierer(); } }

        public override System.Windows.Controls.Canvas Legende
        {
            get
            {
				System.Windows.Controls.Canvas result = new System.Windows.Controls.Canvas();
                var segmentierer = Segmentierer;
                Strecke.ElementInfo pseudoelement = new();
                pseudoelement.ParentBuffer = new Strecke.Element();
                pseudoelement.ParentBuffer.GreenDirectionInfoIfSetOnZusi = pseudoelement;
                foreach (float r  in new float[] { MinRadius, (MinRadius + MidRadius) / 2, MidRadius, 1 / ((MidRadiusKruemmung + MaxRadiusKruemmung) / 2), MaxRadius }) {
                    pseudoelement.ParentBuffer.Kruemmung = 1 / r;
                    NeuesLegendeElement(result, segmentierer, pseudoelement,
                        (r == MaxRadius ? "r ⩾ " :
                            (r == MinRadius ? "r ⩽ " : ("r = ")))
                        + r.ToString());
                }
                return result;
            }
        }
    }
}
