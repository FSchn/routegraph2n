using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;

namespace ZusiCLIProject.Routegraph2
{
    public class StreckeView : ScrollViewer
    {
        public StreckeView()
        {
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            this.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            this.CanContentScroll = true;
            this.Cursor = System.Windows.Input.Cursors.SizeAll;
			//this->setTransformationAnchor(QGraphicsView::AnchorUnderMouse);
			//this->setRenderHint(QPainter::Antialiasing);
			this.PreviewMouseWheel += StreckeView_MouseWheel;
			this.PreviewMouseDoubleClick += StreckeView_MouseDoubleClick;
			this.PreviewMouseMove += StreckeView_MouseMove;
			this.PreviewMouseUp += StreckeView_MouseUp;
			this.PreviewMouseDown += StreckeView_MouseDown;
            this.MouseEnter += delegate (object sender, System.Windows.Input.MouseEventArgs e) { m_currentMouseLoc = e.GetPosition(this); };
			this.MouseLeave += delegate (object sender, System.Windows.Input.MouseEventArgs e) { m_currentMouseLoc = null; };
			this.Background = new SolidColorBrush(Colors.White);
			m_reverseTransform.Matrix = Matrix.Identity;

		}

		public void Skalieren(double faktor)
        {
            if (m_scaler.LayoutTransform is MatrixTransform)
            {
                if (m_scaler.LayoutTransform == MatrixTransform.Identity) m_scaler.LayoutTransform = new MatrixTransform();
                var mTransf = (MatrixTransform)m_scaler.LayoutTransform;
                var mat = mTransf.Matrix;
				var matDownwards = m_currentMouseLoc.HasValue ? this.TransformToDescendant((System.Windows.Media.Visual)m_scaler.Content) : Transform.Identity;
				var scenePoint = m_currentMouseLoc.HasValue ? matDownwards.Transform(m_currentMouseLoc.Value) : new System.Windows.Point();
				//var scenePoint = m_currentMouseLoc.HasValue ? mTransf.Inverse.Transform(
                //    m_currentMouseLoc.Value + new System.Windows.Vector(this.ContentHorizontalOffset, this.ContentVerticalOffset)) : new System.Windows.Point();
				mat.Scale(faktor, faktor);
				mTransf.Matrix = mat;
                var mat2 = mat;
                mat2.Invert();
                m_reverseTransform.Matrix = mat2;

				if (m_currentMouseLoc.HasValue)
				{
                    UpdateLayout();
					var matUpwards = ((System.Windows.Media.Visual)m_scaler.Content).TransformToAncestor(this);
					var newScenePoint = matUpwards.Transform(scenePoint);
					//var newScenePoint = mTransf.Transform(scenePoint);
					this.ScrollToHorizontalOffset(this.HorizontalOffset + newScenePoint.X - m_currentMouseLoc.Value.X/* - this.ContentHorizontalOffset*/);
					this.ScrollToVerticalOffset(this.VerticalOffset + newScenePoint.Y - m_currentMouseLoc.Value.Y/* - this.ContentVerticalOffset*/);
				}
			}
            else
				m_scaler.LayoutTransform = new ScaleTransform(faktor, faktor); //2x Center bei Bedarf.
		}
		public void Rotieren(double angle/*, System.Windows.Point center*/)
		{
			if (m_scaler.LayoutTransform is MatrixTransform)
			{
				if (m_scaler.LayoutTransform == MatrixTransform.Identity) m_scaler.LayoutTransform = new MatrixTransform();
				var mTransf = (MatrixTransform)m_scaler.LayoutTransform;
				var mat = mTransf.Matrix;
                var matDownwards = m_currentMouseLoc.HasValue ? this.TransformToDescendant((System.Windows.Media.Visual)m_scaler.Content) : Transform.Identity;
				var scenePoint = m_currentMouseLoc.HasValue ? matDownwards.Transform(m_currentMouseLoc.Value) : new System.Windows.Point();
				//var scenePoint = m_currentMouseLoc.HasValue ? mTransf.Inverse.Transform(
				//	m_currentMouseLoc.Value + new System.Windows.Vector(this.ContentHorizontalOffset, this.ContentVerticalOffset)) : new System.Windows.Point();
				mat.Rotate(angle);
				mTransf.Matrix = mat;
				var mat2 = mat;
				mat2.Invert();
				m_reverseTransform.Matrix = mat2;

				if (m_currentMouseLoc.HasValue)
				{
					UpdateLayout();
					var matUpwards = ((System.Windows.Media.Visual)m_scaler.Content).TransformToAncestor(this);
					var newScenePoint = matUpwards.Transform(scenePoint);
					//var newScenePoint = mTransf.Transform(scenePoint);
                    //var oldScrPos = System.Windows.Forms.Cursor.Position;
                    //var screenLoc = new System.Drawing.Point((int) (oldScrPos.X - m_currentMouseLoc.Value.X + newScenePoint.X),
					//	(int) (oldScrPos.Y - m_currentMouseLoc.Value.Y + newScenePoint.Y));
					this.ScrollToHorizontalOffset(this.HorizontalOffset + newScenePoint.X - m_currentMouseLoc.Value.X/* - this.ContentHorizontalOffset*/);
					this.ScrollToVerticalOffset(this.VerticalOffset + newScenePoint.Y - m_currentMouseLoc.Value.Y/* - this.ContentVerticalOffset*/);
				}
			}
			else
				m_scaler.LayoutTransform = new RotateTransform(angle, 0, 0 /*center.X, center.Y*/);
		}

		public void Vergroessern()
        {
            this.Skalieren(1.1);
        }

		public void Verkleinern()
        {
            this.Skalieren(1.0 / 1.1);
        }
        public void SkaliereAufAnsicht(bool drehungZuruckesetzen)
		{
            if (drehungZuruckesetzen)
            {
				m_scaler.LayoutTransform = DefaultTransform.Clone();
			}
			System.Diagnostics.Debug.WriteLine("UpdateLayout...");
			var timer = DateTime.Now;
            m_scaler.UpdateLayout();
			var timeDiff = DateTime.Now.Subtract(timer);
			System.Diagnostics.Debug.WriteLine("UpdateLayout in {0}", timeDiff);
			timer = DateTime.Now;
			this.InvalidateMeasure();
			timeDiff = DateTime.Now.Subtract(timer);
			System.Diagnostics.Debug.WriteLine("InvalidateMeasure in {0}", timeDiff);
			timer = DateTime.Now;
			this.UpdateLayout();
			timeDiff = DateTime.Now.Subtract(timer);
			System.Diagnostics.Debug.WriteLine("UpdateLayout in {0}", timeDiff);
			var sceneSize = m_scaler.DesiredSize;
            bool hasRun = false;
            SizeChangedEventHandler sizeChange = delegate (object sender, SizeChangedEventArgs e)
            {
                if (hasRun)
                    return;
                hasRun = true;
				this.UpdateLayout();
				var mySize = new System.Windows.Size(this.ViewportWidth, this.ViewportHeight);
				if (mySize.Width == 0 || mySize.Height == 0 || sceneSize.Width == 0 || sceneSize.Height == 0)
                    return;
				var scale = new System.Windows.Size(mySize.Width / sceneSize.Width, mySize.Height / sceneSize.Height);
                Skalieren(System.Math.Min(scale.Width, scale.Height));
			};

            if (this.ViewportWidth == 0)
            {
                this.SizeChanged += sizeChange;
			}
            else
                sizeChange(this, null);

		}

		public MatrixTransform DefaultTransform { set; get; } = new MatrixTransform(Matrix.Identity);

		private void StreckeView_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.None)
            {
                var t = DateTime.Now;
                this.Skalieren(Math.Pow(4.0 / 3.0, (e.Delta / 240.0)));
                System.Diagnostics.Debug.WriteLine("StreckeView_MouseWheel " + DateTime.Now.Subtract(t).ToString());
                e.Handled = true;
			}
		}

		private bool m_rechteMaustasteGedrueckt = false;
		private bool m_linkeMaustasteGedrueckt = false;
        private System.Windows.Point m_dragStart;
        private bool m_StreckeView_MouseDown_IgnoredByScrollBar = false;
		private void StreckeView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            if (e.GetPosition(this).X > this.ViewportWidth || e.GetPosition(this).Y > this.ViewportHeight)
            {
                e.Handled = false;
                m_StreckeView_MouseDown_IgnoredByScrollBar = true;
				return;
            }
			//this->setCursor(Qt::ClosedHandCursor);
			if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
			{
				this.Cursor = System.Windows.Input.Cursors.ScrollNS;
				this.m_rechteMaustasteGedrueckt = true;
                this.m_linkeMaustasteGedrueckt = false;
                this.m_dragStart = e.GetPosition(this);
                e.Handled = true;
			}
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
			{
                this.m_rechteMaustasteGedrueckt = false;
                this.m_linkeMaustasteGedrueckt = true;
				this.m_dragStart = e.GetPosition(this);
                e.Handled = true;
			}
        }

        private System.Windows.Point? m_currentMouseLoc;
		private void StreckeView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
            if (m_currentMouseLoc != null)
				m_currentMouseLoc = e.GetPosition(this);
			if (m_StreckeView_MouseDown_IgnoredByScrollBar)
			{
				e.Handled = false;
				return;
			}
			if (this.m_rechteMaustasteGedrueckt)
            {
                var dxy = e.GetPosition(this) - this.m_dragStart;
				this.m_dragStart = e.GetPosition(this);
                this.Rotieren(dxy.Y);
                e.Handled = true;
            }

            if (this.m_linkeMaustasteGedrueckt)
            {
                // Unendliches Scrollen. Die Cursorposition wird an die gegenüberliegende Kante gesetzt,
                // wenn der Cursor den Rand des Bildschirms erreicht hat.
                // Adapted from Okular source code (ui/pageview.cpp).
                var mousePos = e.GetPosition(this);
                var delta = this.m_dragStart - mousePos;

                var mouseContainer = new System.Windows.Rect(this.RenderSize);
                // If the delta is huge it probably means we just wrapped in that direction
                var absDelta = new System.Windows.Point(Math.Abs(delta.X), Math.Abs(delta.Y));
                if (absDelta.Y > mouseContainer.Height / 2)
                {
                    delta.Y = (mouseContainer.Height - absDelta.Y);
                }
                if (absDelta.X > mouseContainer.Width / 2)
                {
                    delta.X = (mouseContainer.Width - absDelta.X);
                }

                // TODO: If we wrap both left/right and top/bottom, do not call QCursor::setPos() twice
                // wrap mouse from top to bottom
                if (mousePos.Y <= mouseContainer.Top + 4 &&
					 VerticalOffset < ScrollableHeight - 10)
                {
                    mousePos.Y = (mouseContainer.Bottom - 5);
					System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)PointToScreen(mousePos).X, (int)PointToScreen(mousePos).Y);
				}
                // wrap mouse from bottom to top
                else if (mousePos.Y >= mouseContainer.Bottom - 4 &&
						  VerticalOffset > 10)
                {
                    mousePos.Y = (mouseContainer.Top + 5);
					System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)PointToScreen(mousePos).X, (int)PointToScreen(mousePos).Y);
				}
                // wrap mouse from left to right
                if (mousePos.X <= mouseContainer.Left + 4 &&
                     HorizontalOffset < ScrollableWidth - 10)
                {
                    mousePos.X = (mouseContainer.Right - 5);
					System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)PointToScreen(mousePos).X, (int)PointToScreen(mousePos).Y);
				}
                // wrap mouse from right to left
                else if (mousePos.X >= mouseContainer.Right - 4 &&
						  HorizontalOffset > 10)
                {
                    mousePos.X = (mouseContainer.Left + 5);
					System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)PointToScreen(mousePos).X, (int)PointToScreen(mousePos).Y);
				}

                // remember last position
                this.m_dragStart = mousePos;

				// scroll page by position increment
				// TODO: Does this trigger two draw events?
				ScrollToHorizontalOffset(HorizontalOffset + delta.X);
				ScrollToVerticalOffset(VerticalOffset + delta.Y);
            }
        }

		private void StreckeView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            if (m_StreckeView_MouseDown_IgnoredByScrollBar)
			{
				e.Handled = false;
				m_StreckeView_MouseDown_IgnoredByScrollBar = false;
				return;
			}
            this.m_rechteMaustasteGedrueckt = false;
            this.m_linkeMaustasteGedrueckt = false;
			//this->setCursor(Qt::OpenHandCursor);
			this.Cursor = System.Windows.Input.Cursors.SizeAll;
        }

		private void StreckeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right) 
            {
                SkaliereAufAnsicht(true);
			}
		}

		public System.Windows.Controls.ContentControl m_scaler = new();
		public System.Windows.Media.MatrixTransform m_reverseTransform = new();
        public EventHandler m_reverseTransform_Changed = null;
		public void ResetScene(StreckeScene p)//(System.Windows.Media.Visual p)
        {
			m_scaler.Content = p;
			Content = m_scaler;
            Measure(new System.Windows.Size(Double.PositiveInfinity, Double.PositiveInfinity));
            m_reverseTransform.Changed -= m_reverseTransform_Changed;
			m_reverseTransform_Changed = delegate (object? sender, EventArgs e)
            {
                var p1 = m_reverseTransform.Transform(new System.Windows.Point(0, 0));
                var p2 = m_reverseTransform.Transform(new System.Windows.Point(1, 0));
                var lod = 1/((((System.Windows.Vector)p1) - ((System.Windows.Vector)p2)).Length);
                foreach (IIgnoreTransformation itf in p.IgnoreTransformations)
                {
                    itf.SetLod(lod);
				}
				UpdateBackgroundStrokeThickness(0.5 / lod);
			};
			m_reverseTransform.Changed += m_reverseTransform_Changed;

			foreach (IIgnoreTransformation itf in p.IgnoreTransformations)
            {
                itf.AssignInverseTransform(m_reverseTransform);
			}
            if (m_grid.Parent != null)
            {
				((StreckeScene)m_grid.Parent).Children.Remove(m_grid);
			}
			m_grid.Children.Clear();
            m_gridItems.Clear();
            m_gridItems = CreateBackground(p.DisplaArea);
			foreach(var i in m_gridItems) { m_grid.Children.Add(i); }
			{
				var p1 = m_reverseTransform.Transform(new System.Windows.Point(0, 0));
				var p2 = m_reverseTransform.Transform(new System.Windows.Point(1, 0));
				var lod = 1 / ((((System.Windows.Vector)p1) - ((System.Windows.Vector)p2)).Length);
				foreach (IIgnoreTransformation itf in p.IgnoreTransformations)
				{
					itf.SetLod(lod);
				}
                UpdateBackgroundStrokeThickness(0.5 / lod);
			};
            p.Children.Insert(0, m_grid);
		}

		public System.Windows.Controls.Canvas m_grid = new();
        public System.Collections.Generic.List<System.Windows.Shapes.Line> m_gridItems = new();
		public static System.Collections.Generic.List<System.Windows.Shapes.Line> CreateBackground(System.Windows.Rect rect)
        {
            var value = new System.Collections.Generic.List<System.Windows.Shapes.Line>();

			int gridSize = 1000;

            float left = (int)(rect.Left) - ((int)(rect.Left) % gridSize);
			float top = (int)(rect.Top) - ((int)(rect.Top) % gridSize);

            List<System.Windows.Point> points = new();

            for (int i = 0; left + i * gridSize < rect.Right; ++i)
            {
                float x = left + i * gridSize;
                points.Add(new System.Windows.Point(x, rect.Top));
                points.Add(new System.Windows.Point(x, rect.Bottom));
            }
            for (int i = 0; top + i * gridSize < rect.Bottom; ++i)
            {
				float y = top + i * gridSize;
                points.Add(new System.Windows.Point(rect.Left, y));
                points.Add(new System.Windows.Point(rect.Right, y));
            }

            for (int i = 0; i < points.Count; i += 2) 
            {
                var ln1 = new System.Windows.Shapes.Line();
                ln1.SetPts(points[i], points[i + 1]);
                ln1.Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220));
				value.Add(ln1);
            }
            return value;
        }
        public void UpdateBackgroundStrokeThickness(double strokeThickness)
		{
			foreach (var i in m_gridItems) 
            { 
                i.StrokeThickness = strokeThickness;
            }
		}
    }
    public static class LineExt
    {
        public static void SetPts(this System.Windows.Shapes.Line ln, System.Windows.Point p1, System.Windows.Point p2)
        {
            ln.X1 = p1.X;
            ln.X2 = p2.X;
            ln.Y1 = p1.Y; 
            ln.Y2 = p2.Y;
        }
    }
}
