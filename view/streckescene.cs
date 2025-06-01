
using System;
using System.Windows;
using System.Collections.Generic;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;
using Colors = System.Windows.Media.Colors;
using System.Windows.Shapes;
using System.Windows.Media;

namespace ZusiCLIProject.Routegraph2
{
    public class StreckeScene : System.Windows.Controls.Canvas
	{
		private enum SignalTyp
		{
			Unbestimmt = 0,
            Tafel = 1,
            Weiche = 2,
            Gleissperre = 3,
            Bahnuebergang = 4,
            Rangiersignal = 5,
            Vorsignal = 6,
            Einfahrsignal = 7,
            Zwischensignal = 8,
            Ausfahrsignal = 9,
            Blocksignal = 10,
            Deckungssignal = 11,
            LZB_Block = 12,
            Hilfshauptsignal = 13,
            Sonstiges = 14,
        };
		private enum ReferenzpunktTyp
		{
			Aufgleispunkt = 0,
            Modulgrenze = 1,
            Register = 2,
            Weiche = 3,
            Signal = 4,
            Aufloesepunkt = 5,
            Signalhaltfall = 6
        };
        public StreckeScene(Streckennetz streckennetz, Visualisierung visualisierung)
        {
            int anzahlSegmente = 0;
            int anzahlStreckenelemente = 0;

            // Berechne Bounding-Rect der Szene aus den Koordinaten der Streckenelemente (plus etwas konstantem Puffer)
            // Das genuegt als Annaeherung und spart das aufwaendige, detaillierte Berechnen des Bounding-Rects durch Qt.
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            // Berechne UTM-Referenzpunkt als Mittelwert der Strecken-Referenzpunkte
            double utmRefWe = 0.0;
            double utmRefNs = 0.0;

            var anzahlStrecken = streckennetz.Count;  // TODO .size()
            foreach (var it in streckennetz)
            {
                Strecke strecke = it.Value;
                //if (strecke.UTMPoint != null) {
                utmRefWe += strecke.UTMPoint.WE / (double)anzahlStrecken;
                utmRefNs += strecke.UTMPoint.NS / (double)anzahlStrecken;
                //}
            }
            m_utmRefPunkt.WE = (int)utmRefWe;
            m_utmRefPunkt.NS = (int)utmRefNs;

            Segmentierer segmentierer = visualisierung.Segmentierer;
			//var richtungen_zusi2 = { StreckenelementRichtung::Norm };
			//var richtungen_zusi3 = { StreckenelementRichtung::Norm, StreckenelementRichtung::Gegen };


			foreach (var it in streckennetz)
            {
                Strecke strecke = it.Value;
                Strecke.UTM strecke_utm = strecke.UTMPoint;
                var utm_dx = 1000 * (strecke_utm.WE - m_utmRefPunkt.WE);
                var utm_dy = 1000 * (strecke_utm.NS - m_utmRefPunkt.NS);

                // Die Betriebsstellen werden pro Streckenmodul beschriftet, da manche Betriebsstellennamen
                // (z.B. Sbk-Bezeichnungen) in mehreren Modulen vorkommen und dann falsch platziert wuerden.
                Dictionary<string, System.Windows.Rect> betriebsstellenKoordinaten = new();

                bool istZusi2 = false;
                //var richtungen = istZusi2 ? richtungen_zusi2 : richtungen_zusi3;
                float offset = (segmentierer.BeideRichtungen || istZusi2) ? 0.49f : 0.0f;

                foreach (var streckenelement in strecke.Streckenelemente)
                {
                    minX = Math.Min(minX, streckenelement.GreenLocation.X + utm_dx);
                    minX = Math.Min(minX, streckenelement.BlueLocation.X + utm_dx);
                    maxX = Math.Max(maxX, streckenelement.GreenLocation.X + utm_dx);
                    maxX = Math.Max(maxX, streckenelement.BlueLocation.X + utm_dx);
                    minY = Math.Min(minY, streckenelement.GreenLocation.Y + utm_dy);
                    minY = Math.Min(minY, streckenelement.BlueLocation.Y + utm_dy);
                    maxY = Math.Max(maxY, streckenelement.GreenLocation.Y + utm_dy);
                    maxY = Math.Max(maxY, streckenelement.BlueLocation.Y + utm_dy);

                    anzahlStreckenelemente++;
                    foreach (var elementRichtung in new Strecke.ElementInfo[] { streckenelement.GreenDirectionInfo, streckenelement.BlueDirectionInfo })
                    {
                        // Streckenelement-Segmente
                        if (segmentierer.IstSegmentStart(elementRichtung))
                        {
                            var item = new StreckensegmentItem(elementRichtung, segmentierer, offset);
                            var startNr = streckenelement.Nummer;
                            var endeNr = item.Ende.ParentBuffer.Nummer;

                            // Fuer Zusi-3-Strecken wird jedes Segment doppelt gefunden (einmal von jedem Ende).
                            // Manche Visualisierungen sind nicht richtungsspezifisch und brauchen daher nur eines davon.
                            // Behalte nur die Segmente, deren Endelement eine groessere Nummer hat als das Startelement.
                            // (Fuer 1-Element-Segmente behalte dasjenige, das in Normrichtung beginnt).
                            if (istZusi2 || segmentierer.BeideRichtungen || endeNr > startNr ||
                                    (endeNr == startNr && elementRichtung == elementRichtung.ParentBuffer.GreenDirectionInfo))
                            {
                                // Zusi 3: x = Ost, y = Nord
                                visualisierung.SetzeDarstellung(item);
                                item.MoveBy(utm_dx, utm_dy);
                                AddChild(item);
								anzahlSegmente++;
                            }
                        }

                        // Signale
                        foreach (var signal in elementRichtung.Signale)
                        {
                            if (!string.IsNullOrEmpty(signal.Signalname) &&
                                    (istZusi2 || (true
                                    && (SignalTyp)(signal.SignalTyp) != SignalTyp.Weiche
                                    && (SignalTyp)(signal.SignalTyp) != SignalTyp.Unbestimmt
                                    && (SignalTyp)(signal.SignalTyp) != SignalTyp.Sonstiges
                                    && (SignalTyp)(signal.SignalTyp) != SignalTyp.Bahnuebergang))) {
                                var vecBlue = elementRichtung.ParentBuffer.BlueLocation.Subtract(elementRichtung.ParentBuffer.GreenLocation);
                                float vecNeedReversed = (elementRichtung.ParentBuffer.BlueDirectionInfo == elementRichtung ? 1 : -1);
                                System.Windows.Point vec = new(vecNeedReversed * vecBlue.X, vecNeedReversed * vecBlue.Y);
                                double phi = Math.Atan2(-vec.Y, vec.X);
                                System.Windows.Media.Color farbe = Colors.Red;
                                switch ((SignalTyp)(signal.SignalTyp)) {
                                    case SignalTyp.Vorsignal:
                                        farbe = Colors.DarkGreen;
                                        break;
                                    case SignalTyp.Gleissperre:
                                    case SignalTyp.Rangiersignal:
                                        farbe = Colors.Blue;
                                        break;
                                    default:
                                        break;
                                }

                                var si = new DreieckItem(phi, signal.Signalname, farbe);
                                string tooltip = signal.NameBetriebsstelle + " " + signal.Signalname;
                                if (!string.IsNullOrEmpty(signal.Stellwerk)) {
                                    tooltip += "\n[" + signal.Stellwerk + "]";
                                }
                                si.ToolTip = tooltip;
                                Location posL = elementRichtung.ParentBuffer.BlueDirectionInfo == elementRichtung ? elementRichtung.ParentBuffer.BlueLocation : elementRichtung.ParentBuffer.GreenLocation;
                                System.Windows.Point pos = new(posL.X, posL.Y);
								si.MoveBy(pos.X, pos.Y); //si->setPos(pos);
								si.MoveBy(utm_dx, utm_dy);
								AddChild(si);
								AddChild(si.Label);

								if ((SignalTyp)(signal.SignalTyp) != SignalTyp.Vorsignal && !string.IsNullOrEmpty(signal.NameBetriebsstelle))
                                {
                                    if (!betriebsstellenKoordinaten.TryGetValue(signal.NameBetriebsstelle, out var r))
                                    {
                                        betriebsstellenKoordinaten.Add(signal.NameBetriebsstelle, new System.Windows.Rect(pos, pos));
                                    }
                                    else
                                    {
                                        r.Union(pos);
                                        betriebsstellenKoordinaten[signal.NameBetriebsstelle] = r;
                                    }
                                }
                            }
						} //signal
					} //elementRichtung
				} //streckenelement

				foreach (var refpunkt in strecke.Referenzen)
                {
                    if (refpunkt.Destination == null) continue;

                    if ((ReferenzpunktTyp)(refpunkt.Typ) == ReferenzpunktTyp.Aufgleispunkt)
                    {
                        var elementRichtung = refpunkt.Destination;
                        var vecBlue = elementRichtung.ParentBuffer.BlueLocation.Subtract(elementRichtung.ParentBuffer.GreenLocation);
                        float vecNeedReversed = (elementRichtung.ParentBuffer.BlueDirectionInfo == elementRichtung ? 1 : -1);
                        System.Windows.Point vec = new(vecNeedReversed * vecBlue.X, vecNeedReversed * vecBlue.Y);
                        double phi = Math.Atan2(-vec.Y, vec.X);

						Location posL = elementRichtung.ParentBuffer.BlueDirectionInfo == elementRichtung ? elementRichtung.ParentBuffer.BlueLocation : elementRichtung.ParentBuffer.GreenLocation;
						System.Windows.Point pos = new(posL.X, posL.Y);

						var si = new DreieckItem(phi, refpunkt.Info, Colors.Magenta);
                        si.MoveBy(pos.X, pos.Y); //si.setPos(pos);
                        si.MoveBy(utm_dx, utm_dy);
						AddChild(si);
						AddChild(si.Label);
					}
				} //refpunkt

				foreach (var p in betriebsstellenKoordinaten) {
                    string betriebsstelle = p.Key;
                    Rect r = p.Value;
					System.Windows.Point c = r.Location + (((Vector)r.Size) / 2.0);

#if false
                    auto ri = this->addRect(p.second);
                    ri->moveBy(1000 * (strecke->utmPunkt.UTM_WE - this->m_utmRefPunkt.UTM_WE), 1000 * (strecke->utmPunkt.UTM_NS - this->m_utmRefPunkt.UTM_NS));
#endif

                    var ti = new Label(betriebsstelle);
                    ti.TextAlignment = System.Windows.TextAlignment.Center;
                    ti.VerticalAlignment = VerticalAlignment.Center;
                    ti.Pos = c;
                    ti.MoveBy(utm_dx, utm_dy);
					ti.Farbe = Colors.Black;
					AddChild(ti);
				} //betriebsstellenKoordinaten
			}
            // TODO: Kreise ohne jegliche Weichen werden nicht als Segmente erkannt.

            System.Console.WriteLine("{0} Segmente für {1} Streckenelemente", anzahlSegmente, anzahlStreckenelemente);

            if (minX == float.MaxValue)
                minX = 0;
            if (minY == float.MaxValue)
                minY = 0;
			if (maxX == float.MinValue)
				maxX = 0;
            if (maxY == float.MinValue)
                maxY = 0;

            var grpTransf = new TransformGroup();
			grpTransf.Children.Add(new ScaleTransform(1, -1));
			grpTransf.Children.Add(new TranslateTransform(-(Math.Min(minX, maxX) - 10), Math.Max(minY, maxY) + 10));
            this.RenderTransform = grpTransf;
			this.Width = -Math.Min(minX, maxX) + Math.Max(minX, maxX) + 20;
            this.Height = -Math.Min(minY, maxY) + Math.Max(minY, maxY) + 20;
            DisplaArea = new Rect(new System.Windows.Point(Math.Min(minX, maxX) - 10, Math.Min(minY, maxY) - 10), 
                                       new System.Windows.Point(Math.Max(minX, maxX) + 10, Math.Max(minY, maxY) + 10));
		}

		public Rect DisplaArea { get; private set; }

        public List<IIgnoreTransformation> IgnoreTransformations { get; private set; } = new();
        public void AddChild(UIElement element)
        {
            this.Children.Add(element);
            if (element is IIgnoreTransformation)
                IgnoreTransformations.Add((IIgnoreTransformation)element);
        }
		private Strecke.UTM m_utmRefPunkt = new();
	}
    public interface IIgnoreTransformation
    {
        public void AssignInverseTransform(System.Windows.Media.Transform transform);
        public void SetLod(double value);
    }

}
