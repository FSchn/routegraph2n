using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;
using static ZusiCLIProject.FileLibrary.Zusi3.Strecke;

namespace ZusiCLIProject.Routegraph2
{
    public class StreckensegmentItem : MinBreiteGraphicsShape //Shape//MinBreiteGraphicsItem<QGraphicsPathItem>
	{
        public StreckensegmentItem(Strecke.ElementInfo start,
                                         Segmentierer segmentierer, float offset/*,
                                         QGraphicsItem parent*/) //:
            //MinBreiteGraphicsItem<QGraphicsPathItem>(parent, 1.0f), start_(start)
        {
            Start = start;
			//this->setZValue(ZWERT_GLEIS);
            var path = new System.Windows.Media.PathFigure();
            var p1 = start.ParentBuffer.GreenDirectionInfo != start ? start.ParentBuffer.GreenLocation : start.ParentBuffer.BlueLocation; //Start
            var p2 = start.ParentBuffer.GreenDirectionInfo == start ? start.ParentBuffer.GreenLocation : start.ParentBuffer.BlueLocation; //Ende

            var vec = new Location();
            float veclen = 0;

            if (offset == 0.0f)
            {
                path.StartPoint = new Point(p1.X, p1.Y);
                path.Segments.Add(new LineSegment(new Point(p2.X, p2.Y), true));
            }
            else
            {
                vec = p2.Subtract(p1); 
                // Der transformierte Punkt ist (p1.X - sin(phi) * offset, p1.Y - cos(phi) * offset)
                // Dabei ist phi der Winkel, den das Streckenelement bildet, also atan2(-vec.Y, vec.X).
                // Es gilt: sin(atan2(y, x)) = x / sqrt(x^2+y^2), analog fuer cos.
                veclen = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
				path.StartPoint = new Point(p1.X + (vec.Y * offset / veclen), p1.Y - (vec.X * offset / veclen));
				path.Segments.Add(new LineSegment(new Point(p2.X + (vec.Y * offset / veclen), p2.Y - (vec.X * offset / veclen)), true));
            }

            var cur = start;
            while (!segmentierer.IstSegmentEnde(cur))
            {
                cur = cur.NachfolgerBuffer.Single();
                p1 = p2;
                p2 = cur.ParentBuffer.GreenDirectionInfo == cur ? cur.ParentBuffer.GreenLocation : cur.ParentBuffer.BlueLocation; //Ende

				if (offset == 0.0f)
                {
					path.Segments.Add(new LineSegment(new Point(p2.X, p2.Y), true));
				}
                else
				{
					vec = p2.Subtract(p1);
					veclen = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
					path.Segments.Add(new LineSegment(new Point(p2.X + (vec.Y * offset / veclen), p2.Y - (vec.X * offset / veclen)), true));
				}
            }

			Path = new PathGeometry(new PathFigure[]{ path });
            this.ToolTip = string.Format("Element {0}ff.", start.ParentBuffer.Nummer);
			Ende = cur;

			Stroke = new SolidColorBrush(Colors.Black);
		}
        public Strecke.ElementInfo Start { get; private set; }
        public Strecke.ElementInfo Ende { get; private set; }
		public System.Windows.Media.PathGeometry Path { get; private set; }
		protected override System.Windows.Media.Geometry DefiningGeometry => Path;

		public void MoveBy(double x, double y)
		{
			var tgr = new TransformGroup();
            tgr.Children.Add(Path.Transform);
            tgr.Children.Add(new TranslateTransform(x, y));
            Path.Transform = tgr;
		}
	}
}
