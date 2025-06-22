using ZusiCLIProject.Routegraph2;
using Color = System.Windows.Media.Color;
using System.Windows.Shapes;
using System.Windows.Media;
using System;

namespace ZusiCLIProject.Routegraph2
{
    public class DreieckItem : Shape
    {
        protected const double Sqrt_3 = 1.73205080756887729352;
        protected const double Seitenlaenge = 6.0;
        protected const double Hoehe = Seitenlaenge * Sqrt_3 / 2.0;

        // Maße des Dreiecks (in Szeneneinheiten = Metern)

        public double Phi { get; private set; } // im Bogenmass
        public string Text { get; private set; }
        public Color Farbe { get; private set; }
        public System.Windows.Media.PathGeometry Path { get; private set; }
        public System.Windows.Media.PointCollection Points { get; private set; }


		public DreieckItem(double phi, string text, Color farbe)
        {
            Phi = phi;
            Text = text;
            Farbe = farbe;
			// Y-Koordinate invertieren, da sie bei Qts Koordinatensystem nach unten statt nach oben zeigt
			Points = new System.Windows.Media.PointCollection(
                new System.Windows.Point[] {
                    new System.Windows.Point(Hoehe * Math.Cos(phi), -(Hoehe * Math.Sin(phi))),
                    new System.Windows.Point(Seitenlaenge / 2 * Math.Cos(phi - Math.PI / 2), -(Seitenlaenge / 2 * Math.Sin(phi - Math.PI / 2))),
                    new System.Windows.Point(Seitenlaenge / 2 * Math.Cos(phi + Math.PI / 2), -(Seitenlaenge / 2 * Math.Sin(phi + Math.PI / 2)))
                }
            );
            Path = new System.Windows.Media.PathGeometry(new[] {
                new System.Windows.Media.PathFigure(Points[0], new []
                {
                    new System.Windows.Media.LineSegment(Points[1], true),
                    new System.Windows.Media.LineSegment(Points[2], true)
                }, true)
            }, System.Windows.Media.FillRule.EvenOdd, RenderTransform);

            Fill = new SolidColorBrush(Farbe);
            //this->setToolTip(this->m_text);
            //this->setZValue(ZWERT_MARKIERUNG);
        }
        protected override System.Windows.Media.Geometry DefiningGeometry => Path;

        /*public QRectF BoundingRect()
        {
            return QRectF(-Seitenlaenge, -Seitenlaenge, 2 * Seitenlaenge, 2 * Seitenlaenge);
        }

        public void Paint(QPainter *painter, const QStyleOptionGraphicsItem *option, QWidget *widget)
        {
            Q_UNUSED(widget);

            qreal lod = option->levelOfDetailFromTransform(painter->worldTransform());
            if (lod > 0.5) {
                this.GetLabel().setVisible(true);
            } else if (this->m_label) {
                this.m_label->setVisible(false);
            }
            if (lod <= 0.1) return;

            painter->setPen(Qt::NoPen);
            painter->setBrush(this->m_farbe);
            painter->drawConvexPolygon(this->m_points, 3);
        }*/

        private Label? m_label = null;

        public Label? Label
        {
            get
            {
                if (m_label == null && Text != null)
                {
                    m_label = new(Text, VisualTreeHelper.GetDpi(this));
                    m_label.Farbe = Farbe;
                    //m_label->setFlag(QGraphicsItem::ItemIgnoresTransformations);

                    m_label.Pos = Points[0]; // Spitze

                    // TODO: optimiert auf Zusi-3-Strecken
                    if (Phi <= (-Math.PI/2) || Phi >= (Math.PI/2))
                    {
                        m_label.TextAlignment = System.Windows.TextAlignment.Right;
                    }
                    if (Phi < 0)
                    {
                        m_label.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    }
                    m_label.MinLod = 0.5f;
				}
                return m_label;
            }
        }


		public void MoveBy(double x, double y)
		{
			var tgr = new TransformGroup();
			tgr.Children.Add(Path.Transform);
			tgr.Children.Add(new TranslateTransform(x, y));
			Path.Transform = tgr;
            Label?.AssignLocalTransform(tgr);
		}
	}
}
