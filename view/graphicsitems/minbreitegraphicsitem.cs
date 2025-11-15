using System.Windows.Media;
using System.Windows.Shapes;

namespace ZusiCLIProject.Routegraph2
{
    public abstract class MinBreiteGraphicsShape : Shape, IIgnoreTransformation
	{

        /** Ein GraphicsItem, das auf dem Bildschirm immer mindestens 0.5 Pixel breit ist.
         *  T muss von QAbstractGraphicsShapeItem ableiten. */
        public MinBreiteGraphicsShape(double breite = 1.0) { m_minBreite = breite; }

        //Skalierung muss in dieser Implementierung Zentral gemacht werden.
        public double Breite
        {
            get
            { return m_breite; }
            set
            {
                m_breite = value;
				//this.StrokeThickness = ((m_minBreite * m_lod) > 1 ) ? m_breite : (m_minBreite / m_lod);
				this.InvalidateVisual();
			}
        }

		public double MinBreite
		{
			get
			{ return m_minBreite; }
			set
			{
				m_minBreite = value;
				this.StrokeThickness = ((m_minBreite * m_lod) > 1) ? m_breite : (m_minBreite / m_lod);
				this.InvalidateVisual();
			}
		}

		private double m_breite = 1;
        private double m_lod = 1;
        private double m_minBreite;


        public void AssignInverseTransform(System.Windows.Media.Transform transform) { }
		public void SetLod(double value)
        {
			m_lod = value;
			//this.StrokeThickness = ((m_minBreite * m_lod) > 1) ? m_breite : (m_minBreite / m_lod); //ToDo: Performance-Hezard
			if (m_pen != null)
				m_pen.Thickness = ((m_minBreite * m_lod) > 1) ? m_breite : (m_minBreite / m_lod);
		}
		private Pen? m_pen = null;
		private static System.Collections.Generic.Dictionary<string, Pen> m_penBuffer = new();
		protected override void OnRender(DrawingContext drawingContext)
		{
			string penKey = (Stroke?.ToString() ?? "") + "//" + m_minBreite.ToString() + "//" + m_breite.ToString() + "//" + ((StrokeDashArray == null) ? "" : StrokeDashArray.ToString()) + "//" + StrokeDashOffset.ToString();
			if (!m_penBuffer.TryGetValue(penKey, out m_pen))
			{
				m_pen = new Pen(Stroke, ((m_minBreite * m_lod) > 1) ? m_breite : (m_minBreite / m_lod));
				if ((StrokeDashArray != null) && (StrokeDashArray.Count > 0))
					m_pen.DashStyle = new DashStyle(StrokeDashArray, StrokeDashOffset);
				m_penBuffer.Add(penKey, m_pen);
			}
			drawingContext.DrawGeometry(Fill, m_pen, DefiningGeometry);
		}
	}
}
