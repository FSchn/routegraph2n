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
				this.StrokeThickness = ((m_minBreite * m_lod) > 1 ) ? m_breite : (m_minBreite / m_lod);

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

			}
		}

		private double m_breite = 1;
        private double m_lod = 1;
        private double m_minBreite;


        public void AssignInverseTransform(System.Windows.Media.Transform transform) { }
		public void SetLod(double value)
        {
			m_lod = value;
			this.StrokeThickness = ((m_minBreite * m_lod) > 1) ? m_breite : (m_minBreite / m_lod);
		}
	}
}
