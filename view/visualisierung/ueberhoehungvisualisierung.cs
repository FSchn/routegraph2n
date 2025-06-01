using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;
using Color = System.Windows.Media.Color;
using System;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public class UeberhoehungSegmentierer : Segmentierer
    {
        protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            bool gleisfunktionGleich = vorgaenger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion) ==
                 nachfolger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion);
            float vorgaengerUeberhoehung = Math.Abs(vorgaenger.ParentBuffer.Ueberhoehung);
            float nachfolgerUeberhoehung = Math.Abs(nachfolger.ParentBuffer.Ueberhoehung);
            bool ueberhoehungGleich = vorgaengerUeberhoehung == nachfolgerUeberhoehung;
            return !gleisfunktionGleich || !ueberhoehungGleich;
        }
    }
    public class UeberhoehungVisualisierung : Visualisierung
    {
        private const double MaxUeberhoehung = 0.124;  // im Bogenmass, entspricht 7.1 Grad (max. Ueberhoehung Regelspur gem. EBO)
        public override void SetzeDarstellung(StreckensegmentItem item)
        {
            float ueberhoehung = Math.Abs(item.Start.ParentBuffer.Ueberhoehung);
            if (item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.KeineGleisfunktion))
                item.Stroke = new SolidColorBrush(Color.FromRgb(192, (byte)(192 + Math.Min(1.0f, ueberhoehung / MaxUeberhoehung) * (255-192)), 192));
            else
                item.Stroke = new SolidColorBrush(Color.FromRgb(0, (byte)(Math.Min(1.0f, ueberhoehung / MaxUeberhoehung) * 255), 0));
        }
        public override Segmentierer Segmentierer { get { return new GleisfunktionSegmentierer(); } }
        public override System.Windows.Controls.Canvas? Legende { get { return null; } }
    }
}



