using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ZusiCLIProject.Routegraph2;

namespace ZusiCLIProject.Routegraph2
{
    public class Label : Canvas, IIgnoreTransformation
    {
        public Label(string text)
        {
            Text = text;
            m_textBlock.Text = text;
            m_textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			m_rectangle.Fill = new SolidColorBrush(Colors.White);
            m_rectangle.Opacity = 0.75;
            m_rectangle.Height = m_textBlock.DesiredSize.Height;
            m_rectangle.Width = m_textBlock.DesiredSize.Width;
            WidthCalculated = m_textBlock.DesiredSize.Width;
			this.Children.Add(m_rectangle);
            this.Children.Add(m_textBlock);
			var tgr = new TransformGroup();
			tgr.Children.Add(new ScaleTransform(1, -1));
			tgr.Children.Add(new MatrixTransform(Matrix.Identity));
			tgr.Children.Add(new MatrixTransform(Matrix.Identity));
			tgr.Children.Add(new MatrixTransform(Matrix.Identity));
			this.RenderTransform = tgr;
			//SetLeft(m_rectangle, -m_rectangle.Width);
			//SetLeft(m_textBlock, -m_rectangle.Width);
			//SetTop(m_rectangle, -m_rectangle.Height);
			//SetTop(m_textBlock, -m_rectangle.Height);

			//ToDo: Align, falls nötig.
			Farbe = System.Windows.Media.Colors.Black;

		}
        public string Text { get; private set; }
        public double WidthCalculated { get; private set; }
        private TextBlock m_textBlock = new();
        private System.Windows.Shapes.Rectangle m_rectangle = new();

		public System.Windows.Media.Color Farbe
        {
            get { return m_farbe; }
            set
            {
                m_farbe = value;
                m_textBlock.Foreground = new System.Windows.Media.SolidColorBrush(value);
			}
        }
        private System.Windows.Media.Color m_farbe;

        private System.Windows.Point m_pos;
		public System.Windows.Point Pos
        {
            get { return m_pos; }
            set 
            {
				m_pos = value;
                ((TransformGroup)this.RenderTransform).Children[2] = new TranslateTransform(value.X, value.Y);
                this.RenderTransform = this.RenderTransform;
				//var grpTransf = new TransformGroup();
				//grpTransf.Children.Add(new ScaleTransform(1, -1));
				//grpTransf.Children.Add(new TranslateTransform(value.X, value.Y));
				//this.RenderTransform = grpTransf;
			}
        }
		public TextAlignment TextAlignment 
        { 
            get { return m_textBlock.TextAlignment; } 
            set 
            { 
                m_textBlock.TextAlignment = value;
				m_textBlock.RenderTransform = new TranslateTransform(0
                    - (TextAlignment == TextAlignment.Right ? WidthCalculated : 0)
                    - (TextAlignment == TextAlignment.Center ? WidthCalculated / 2 : 0)
                    , 0
					- (VerticalAlignment == VerticalAlignment.Top ? m_textBlock.DesiredSize.Height : 0)
					- (VerticalAlignment == VerticalAlignment.Center ? m_textBlock.DesiredSize.Height / 2 : 0));
                m_rectangle.RenderTransform = m_textBlock.RenderTransform;
				this.RenderTransform = this.RenderTransform;
			}
		}
		public new VerticalAlignment VerticalAlignment
		{
			get { return m_textBlock.VerticalAlignment; }
            set
			{
				m_textBlock.VerticalAlignment = value;
				m_textBlock.RenderTransform = new TranslateTransform(0
					- (TextAlignment == TextAlignment.Right ? WidthCalculated : 0)
					- (TextAlignment == TextAlignment.Center ? WidthCalculated / 2 : 0)
					, 0
					- (VerticalAlignment == VerticalAlignment.Top ? m_textBlock.DesiredSize.Height : 0)
					- (VerticalAlignment == VerticalAlignment.Center ? m_textBlock.DesiredSize.Height / 2 : 0));
				m_rectangle.RenderTransform = m_textBlock.RenderTransform;
				this.RenderTransform = this.RenderTransform;
			}
		}

		public void MoveBy(double x, double y)
		{
			Pos = new Point(Pos.X + x, Pos.Y + y);
		}
		public void AssignInverseTransform(System.Windows.Media.Transform transform)
        {
            //((TransformGroup)this.RenderTransform).Children[1] = transform;
            this.LayoutTransform = transform;
		}
		public void AssignLocalTransform(System.Windows.Media.Transform transform)
		{
			((TransformGroup)this.RenderTransform).Children[3] = transform;
		}
        public float MinLod { get; set; } = 0;
        bool m_opaque = false;
		public void SetLod(double value)
        {
            bool opaque = (value > MinLod);
            if (opaque == m_opaque)
                return;
            m_opaque = opaque;
			this.Opacity = m_opaque ? 1 : 0;
        }
	}
    /*public class Label : TextBlock
    {
        public Label(string text) : base(text)
        {
            this->setZValue(ZWERT_BESCHRIFTUNG);
        }

        public QRectF BoundingRect()
        {
            QRectF result = QGraphicsSimpleTextItem::boundingRect();

            if (m_alignment & Qt::AlignRight) {
                result.translate(-result.width(), 0);
            } else if (m_alignment & Qt::AlignHCenter) {
                result.translate(-result.width() / 2.0, 0);
            }

            if (m_alignment & Qt::AlignTop) {
                result.translate(0, -result.height());
            } else if (m_alignment & Qt::AlignVCenter) {
                result.translate(0, -result.height() / 2.0);
            }

            return result;
        }

        public void Paint(QPainter painter, QStyleOptionGraphicsItem option, QWidget widget)
        {
            painter->setPen(Qt::NoPen);
            painter->setBrush(QBrush(Qt::white));
            painter->setOpacity(0.75);
            painter->drawRect(this->boundingRect());

            painter->setPen(this->pen());
            painter->setBrush(this->brush());
            painter->setOpacity(1);
            painter->drawText(this->boundingRect(), this->text());
        }
    }*/
}
