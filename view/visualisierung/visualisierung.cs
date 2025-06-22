using ZusiCLIProject.Routegraph2;
using ZusiCLIProject.FileLibrary.Zusi3;
using Color = System.Windows.Media.Color;
using System;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public abstract class Visualisierung
    {
        protected void NeuesLegendeElement(System.Windows.Controls.Canvas scene, Segmentierer segmentierer, Strecke.ElementInfo pseudoelement, string legende)
        {
            if (pseudoelement.ParentBuffer.GreenLocation == null)
                pseudoelement.ParentBuffer.GreenLocation = new Location();
            pseudoelement.ParentBuffer.GreenLocation.X = LegendenElementCurrentOffset; // 5.0f;//(float)(scene.Bounds.Right) + 5.0f;
            pseudoelement.ParentBuffer.GreenLocation.Y = -LegendeHight / 2;
			if (pseudoelement.ParentBuffer.BlueLocation == null)
                pseudoelement.ParentBuffer.BlueLocation = new Location();
            pseudoelement.ParentBuffer.BlueLocation.X = pseudoelement.ParentBuffer.GreenLocation.X + LegendeElementLaenge;
			pseudoelement.ParentBuffer.BlueLocation.Y = -LegendeHight / 2;

			if (pseudoelement.NachfolgerBuffer == null)
                pseudoelement.NachfolgerBuffer = new Strecke.ElementInfo[] { };
			if (pseudoelement.GegenrichtungBuffer.NachfolgerBuffer == null)
				pseudoelement.GegenrichtungBuffer.NachfolgerBuffer = new Strecke.ElementInfo[] { };

			StreckensegmentItem segmentItem = new (pseudoelement, segmentierer, 0);
            SetzeDarstellung(segmentItem);
			segmentItem.Breite = 5; //Geerbt von MinBreiteGraphicsItem
			segmentItem.MinBreite = 5; //Geerbt von MinBreiteGraphicsItem

			scene.Children.Add(segmentItem);

            Label label = new(legende, VisualTreeHelper.GetDpi(scene));
            //label.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            label.Pos = new System.Windows.Point(pseudoelement.ParentBuffer.BlueLocation.X + LegendeElementPadding / 2.0f, 0);
            label.Farbe = System.Windows.Media.Colors.Black;
			scene.Children.Add(label);

			LegendenElementCurrentOffset += System.Math.Max(LegendeElementLaenge, (float)label.WidthCalculated) + LegendeElementLaenge + LegendeElementPadding;
		}
		protected const float LegendeElementLaenge = 25.0f;
		protected const float LegendeElementPadding = 4.0f;
		protected const float LegendeHight = 18.0f;
		protected float LegendenElementCurrentOffset = 0.0f;

        public virtual void SetzeDarstellung(StreckensegmentItem element) { }
        public abstract Segmentierer Segmentierer { get; }
        public abstract System.Windows.Controls.Canvas? Legende { get; }
		public virtual float? LegendeWidth { get { return LegendenElementCurrentOffset; } }

		protected static Color ColorFromHSV(int hue, int saturation, int value)
        {
            double huef = (double)hue;
            double saturationf = ((double)saturation) / 255.0;
            int hi = Convert.ToInt32(Math.Floor(huef / 60)) % 6;
            double f = huef / 60 - Math.Floor(huef / 60);

            //value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturationf));
            byte q = Convert.ToByte(value * (1 - f * saturationf));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturationf));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

    }
}
