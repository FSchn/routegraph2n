using ZusiCLIProject.FileLibrary.Zusi3;
using Color = System.Windows.Media.Color;
using ZusiCLIProject.Routegraph2;
using System;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public class NeigungSegmentierer : Segmentierer
    {
        protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            bool gleisfunktionGleich = vorgaenger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion) ==
                 nachfolger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion);
            bool neigungGleich = (Math.Abs(Math.Truncate(vorgaenger.ParentBuffer.Neigung / NeigungVisualisierung.NeigungAbstufung)) ==
                Math.Abs(Math.Truncate(nachfolger.ParentBuffer.Neigung / NeigungVisualisierung.NeigungAbstufung)));
            return !gleisfunktionGleich || !neigungGleich;
        }
    }
    public class NeigungVisualisierung : Visualisierung
    {
        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            int neigungIndex = Math.Max(0, Math.Min(AnzahlNeigungsStufen - 1, (int)(Math.Abs(Math.Truncate(item.Start.ParentBuffer.Neigung / NeigungAbstufung)))));

            if (item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.KeineGleisfunktion))
                item.Stroke = new SolidColorBrush(Colors.LightGray);
            else
                item.Stroke = new SolidColorBrush(colormap_[neigungIndex]);
        }

        internal const float NeigungAbstufung = 0.005f;
        private const int AnzahlNeigungsStufen = 8;
        private static readonly Color[] colormap_;
        static NeigungVisualisierung()
        {
            colormap_ = new Color[AnzahlNeigungsStufen] {
                Color.FromRgb(  0,   0, 0 ),
                Color.FromRgb(255, 128, 255 ),
                Color.FromRgb(  0, 128, 255 ),
                Color.FromRgb(  0, 255, 255 ),
                Color.FromRgb(  0, 255, 0 ),
                Color.FromRgb(255, 230, 0 ),
                Color.FromRgb(255, 128, 0 ),
                Color.FromRgb(255,   0, 0 ),
            };
        }
        public override Segmentierer Segmentierer { get { return new NeigungSegmentierer(); } }
        public override System.Windows.Controls.Canvas Legende
        {
            get
            {
				System.Windows.Controls.Canvas result = new System.Windows.Controls.Canvas();
                var segmentierer = Segmentierer;
                for (int i = 0; i < AnzahlNeigungsStufen; ++i) {
                    Strecke.ElementInfo pseudoelement = new(); // nicht wiederverwenden wegen Neigungs-Cache
                    pseudoelement.ParentBuffer = new Strecke.Element();
                    pseudoelement.ParentBuffer.GreenDirectionInfoIfSetOnZusi = pseudoelement;
                    pseudoelement.ParentBuffer.BlueLocation = new Location();
                    pseudoelement.ParentBuffer.BlueLocation.Z = /* Rundungsfehler-Ausgleich */ 1.01f * i * NeigungAbstufung * LegendeElementLaenge;
                    NeuesLegendeElement(result, segmentierer, pseudoelement,
                        i == AnzahlNeigungsStufen - 1 ?
                            ("⩾ " + ((i * NeigungAbstufung * 1000.0)).ToString("0")) + "‰" :
                            ("< " + (((i + 1) * NeigungAbstufung * 1000.0)).ToString("0")) + "‰");
                }
                return result;
            }
        }
    }
}


