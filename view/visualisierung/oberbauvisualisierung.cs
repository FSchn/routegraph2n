using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;
using Color = System.Windows.Media.Color;
using System;
using System.Windows.Media;
using System.Collections.Generic;

namespace ZusiCLIProject.Routegraph2
{
    public class OberbauSegmentierer : Segmentierer
    {
        protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            return vorgaenger.ParentBuffer.OberbauTyp != nachfolger.ParentBuffer.OberbauTyp;
        }
    }
    public class OberbauVisualisierung : Visualisierung
    {
        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            var oberbauName = item.Start.ParentBuffer.OberbauTyp + "";
            if (colors_.TryGetValue(oberbauName, out Color value))
                item.Stroke = new SolidColorBrush(value);
            else
            {
                int kNumColors = 14;
                int[] colormap = { 0, 40, 53, 70, 130, 160, 180, 197, 214, 247, 258, 270, 286, 300 };
                Color color = ColorFromHSV(
                        colormap[colors_.Count % kNumColors],
                        255,
                        Math.Max(0, (int)(255 - (colors_.Count / kNumColors) * 64)));
                colors_.Add(oberbauName, color);
                item.Stroke = new SolidColorBrush(color);
            }
        }
        public override Segmentierer Segmentierer { get { return new OberbauSegmentierer(); } }
        public override System.Windows.Controls.Canvas Legende
        {
            get
            {
				System.Windows.Controls.Canvas result = new System.Windows.Controls.Canvas();
                var segmentierer = Segmentierer;
                Strecke.ElementInfo pseudoelement = new();
                pseudoelement.ParentBuffer = new Strecke.Element();
                pseudoelement.ParentBuffer.GreenDirectionInfoIfSetOnZusi = pseudoelement;
                foreach (var it in colors_) {
                    pseudoelement.ParentBuffer.OberbauTyp = it.Key;
                    NeuesLegendeElement(result, segmentierer, pseudoelement, it.Key == "" ? "(k.A.)" : it.Key);
                }
                return result;
            }
        }
        private static readonly Dictionary<string, Color> colors_ = new ();
    }
}
