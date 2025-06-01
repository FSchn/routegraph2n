using ZusiCLIProject.Routegraph2;
using ZusiCLIProject.FileLibrary.Zusi3;
using Color = System.Windows.Media.Color;
using System;
using System.Windows.Media;
using System.Collections.Generic;

namespace ZusiCLIProject.Routegraph2
{
    public class FahrleitungSegmentierer : Segmentierer
    {
        protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            return vorgaenger.ParentBuffer.Stromsystem != nachfolger.ParentBuffer.Stromsystem || ((vorgaenger.ParentBuffer.Drahthoehe == 0) != (nachfolger.ParentBuffer.Drahthoehe == 0));
        }
    }
    public class FahrleitungVisualisierung : Visualisierung
    {

        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            var fahrleitungTyp = item.Start.ParentBuffer.Stromsystem;
            var drahthoehe = item.Start.ParentBuffer.Drahthoehe;

            if (farben_.TryGetValue(fahrleitungTyp, out var value))
                item.Stroke = new SolidColorBrush(value.Item2);
            else
                item.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            if (fahrleitungTyp != Strecke.Element.Stromsysteme.Ohne && drahthoehe == 0)
                item.StrokeDashArray = new DoubleCollection(new double[] { 1, 2 }); //new DoubleCollection(new double[] { 5, 5 });
			else
                item.StrokeDashArray = null;
        }

        public override Segmentierer Segmentierer { get { return new FahrleitungSegmentierer(); } }
        public override System.Windows.Controls.Canvas Legende
        {
            get
            {
				System.Windows.Controls.Canvas result = new System.Windows.Controls.Canvas();
                var segmentierer = Segmentierer;
                Strecke.ElementInfo pseudoelement = new();
                pseudoelement.ParentBuffer = new Strecke.Element();
                pseudoelement.ParentBuffer.GreenDirectionInfoIfSetOnZusi = pseudoelement;
                pseudoelement.ParentBuffer.Drahthoehe = 1;
                foreach (var it in farben_)
                {
                    pseudoelement.ParentBuffer.Stromsystem = it.Key;
                    NeuesLegendeElement(result, segmentierer, pseudoelement, it.Value.Item1);
                }
                pseudoelement.ParentBuffer.Stromsystem = Strecke.Element.Stromsysteme.Unbestimmt;
                pseudoelement.ParentBuffer.Drahthoehe = 0;
                NeuesLegendeElement(result, segmentierer, pseudoelement, "gestrichelt = Fahrdrahthöhe 0");
                return result;
            }
        }

        private static readonly Dictionary<Strecke.Element.Stromsysteme, Tuple<string, Color>> farben_;
        static FahrleitungVisualisierung()
        {
            farben_ = new Dictionary<Strecke.Element.Stromsysteme, Tuple<string, Color>>
            {
                { Strecke.Element.Stromsysteme.Ohne, new Tuple<string, Color>("Ohne", 
                Color.FromRgb(40, 214, 40)) },
                { Strecke.Element.Stromsysteme.Unbestimmt, new Tuple<string, Color>("Unbestimmt", 
                Color.FromRgb(128, 128, 128)) },
                { Strecke.Element.Stromsysteme.AC_15000V_16_7_Hz, new Tuple<string, Color>("15 kV, 16,7 Hz", 
                Color.FromRgb(255, 0, 0)) },
                { Strecke.Element.Stromsysteme.AC_25000V_50_Hz, new Tuple<string, Color>("25 kV, 50 Hz", 
                Color.FromRgb(0, 121, 255)) },
                { Strecke.Element.Stromsysteme.DC_1500V, new Tuple<string, Color>("1500 V, Gleichstrom", 
                Color.FromRgb(206, 133, 89)) },
                { Strecke.Element.Stromsysteme.DC_1200V_Stromschiene, new Tuple<string, Color>("1200 V, Gleichstrom (Stromschiene)", 
                Color.FromRgb(255, 149, 202)) },
                { Strecke.Element.Stromsysteme.DC_3000V, new Tuple<string, Color>("3 kV, Gleichstrom", 
                Color.FromRgb(0, 202, 202)) }
            };
        }

    }
}
