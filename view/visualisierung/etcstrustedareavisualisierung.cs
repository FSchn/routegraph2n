using ZusiCLIProject.Routegraph2;
using ZusiCLIProject.FileLibrary.Zusi3;
using Color = System.Windows.Media.Color;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Drawing;

namespace ZusiCLIProject.Routegraph2
{
	public class EtcsTrustedAreaSegmentierer : Segmentierer
	{
		protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
		{
			bool gleisfunktionGleich = vorgaenger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion) ==
				 nachfolger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion);
			bool trustedAreaGleich = vorgaenger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.EtcsTrustedArea) ==
				 nachfolger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.EtcsTrustedArea);
            return !gleisfunktionGleich || !trustedAreaGleich;
		}
	}
	public class EtcsTrustedAreaVisualisierung : Visualisierung
	{

		public override void SetzeDarstellung(StreckensegmentItem item)
		{
			item.Stroke = new SolidColorBrush(item.Start.ParentBuffer.HasFunktion(FileLibrary.Zusi3.Strecke.Element.Elementfunktion.EtcsTrustedArea)
				? Color.FromRgb(0, 200, 0) : Colors.Black);
		}

		public override Segmentierer Segmentierer { get { return new EtcsTrustedAreaSegmentierer(); } }
		public override System.Windows.Controls.Canvas Legende
		{
			get
			{
				System.Windows.Controls.Canvas result = new System.Windows.Controls.Canvas();
				var segmentierer = Segmentierer;
				Strecke.ElementInfo pseudoelement = new();
				pseudoelement.ParentBuffer = new Strecke.Element();
				pseudoelement.ParentBuffer.GreenDirectionInfoIfSetOnZusi = pseudoelement;
				for (int i = 0; i <= 1; ++i)
				{
					pseudoelement.ParentBuffer.Funktionen = (i == 0 ? 0 : (int)Strecke.Element.Elementfunktion.EtcsTrustedArea);
					NeuesLegendeElement(result, segmentierer, pseudoelement, i == 0 ? "Keine Trusted Area" : "Trusted Area");
				}
				return result;
			}
		}

	}
}
