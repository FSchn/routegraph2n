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
        public Label(string text, DpiScale predetectDpi)
        {
            Text = text;
            m_textBlock.Text = text;
			System.Windows.Media.FormattedText formattedText  = new(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
				new System.Windows.Media.Typeface(m_textBlock.FontFamily, m_textBlock.FontStyle, m_textBlock.FontWeight, m_textBlock.FontStretch),
				m_textBlock.FontSize, m_textBlock.Foreground, predetectDpi.PixelsPerDip);
			m_rectangle.Fill = new SolidColorBrush(Colors.White);
            m_rectangle.Opacity = 0.75;
            m_rectangle.Height = formattedText.Height;
            m_rectangle.Width = formattedText.Width;
            WidthCalculated = formattedText.Width;
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
        bool m_opaque = true;
		public void SetLod(double value)
        {
            bool opaque = (value > MinLod);
            if (opaque == m_opaque)
                return;
            m_opaque = opaque;
			this.Visibility =  m_opaque ? Visibility.Visible : Visibility.Collapsed;
        }
	}
}
