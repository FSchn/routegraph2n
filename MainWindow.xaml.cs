using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;

namespace ZusiCLIProject.Routegraph2
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			ActionModulAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
			ActionOrdnerAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;

			isInCtor = true;
			GleisfunktionMenuItem.IsChecked = true;
			isInCtor = false;

			var grpTransf = new TransformGroup();
			grpTransf.Children.Add(new ScaleTransform(1, -1));
			grpTransf.Children.Add(new TranslateTransform(0, m_legendeView.Height));
			m_legendeView.RenderTransform = grpTransf;
		}
		bool isInCtor = false;

		Streckennetz m_streckennetz = new();
		StreckeScene? m_streckeScene = null;
		//QGraphicsScene m_legendeScene;

		private void SetzeAnsichtZurueck() 
		{
			m_streckeView.SkaliereAufAnsicht();
		}

		/** Oeffnet Streckendateien und fuegt sie zur Liste der offenen Strecken hinzu. */
		private void OeffneStrecken(IEnumerable<string> dateinamen) 
		{ 
			var timer = DateTime.Now;

			var messages = new List<string>();
			bool hasErrors = false;

			var zusiDataDirs = Datei.GetZusiDataDirs();
			var datList = new List<Datei>();
			var buf = new Dictionary<string, Zusi>(System.StringComparer.InvariantCultureIgnoreCase);
			foreach (var kv in m_streckennetz)
				buf.Add(kv.Key, kv.Value.ParentBuffer);
			foreach (var dateiname in dateinamen)
			{
				var dat = new Datei();
				dat.Dateiname = dateiname;
				foreach(var pfad1 in zusiDataDirs)
				{
					if (!dat.Dateiname.ToLower().StartsWith(pfad1.ToLower()))
						continue;
					dat.Dateiname = dateiname.Substring(pfad1.Length);
					if (dat.Dateiname.StartsWith("\\"))
						dat.Dateiname = dat.Dateiname.Substring(1);
					if (dateiname.ToLower() != Datei.TryFindFirstExistingFile(zusiDataDirs, dat.Dateiname).ToLower())
						messages.Add("Datei " + dat.Dateiname + " wird aus den Eigenen Daten geladen.");
					if (buf.ContainsKey(dat.Dateiname))
						messages.Add("Datei " + dat.Dateiname + " bereits geladen.");
					break;
				}
				datList.Add(dat);
			}
			while(datList.Count > 0)
			{
				Datei dat = datList[0];
				datList.RemoveAt(0);

				try
				{
					dat.Load(zusiDataDirs, buf);

					foreach (var f1 in dat.Content.Fahrplaene)
					{
						foreach (var fs1 in f1.Streckendateien)
						{
							datList.Add(fs1.Datei);
						}
					}
				}
				catch (Exception ex)
				{
					messages.Add("Fehler beim Laden von " + dat.Dateiname + " (" + ex.Message + ")");
					hasErrors = true;
				}
			}
			var clearBuf = new List<string>();
			foreach(var b in buf)
			{
				try
				{
					var sub = b.Value.CollectDateiSubmemberExceptHauptlandschaft();
					foreach(var dat1 in sub)
					{
						dat1.Peek(buf);
					}
					b.Value.FillParentBuffer();
				}
				catch (Exception ex)
				{
					messages.Add("Fehler beim Vorbereiten von " + b.Key + " (" + ex.Message + ")");
					hasErrors = true;
					clearBuf.Add(b.Key);
				}
			}
			foreach (string b in clearBuf)
				buf.Remove(b);
			clearBuf.Clear();
			foreach (var b in buf)
			{
				try
				{
					foreach (Strecke s in b.Value.Strecken)
					{
						s.UpdateBufferIfInvalidated();
						s.LoadReferenzenDestinationBuffer();
						foreach (Strecke.Referenz.Eintrag e in b.Value.CollectReferenzSubmember())
						{
							e.TryLoad(s);
						}
					}
				}
				catch (Exception ex)
				{
					messages.Add("Fehler beim Linken von " + b.Key + " (" + ex.Message + ")");
					hasErrors = true;
					clearBuf.Add(b.Key);
				}
			}
			foreach (var b in buf)
			{
				try
				{
					foreach (Strecke s in b.Value.Strecken)
					{
						s.RefreshStreckenelementNachfolgerBuffer(delegate (int src, Datei d1, int target) { });
					}
				}
				catch (Exception ex)
				{
					messages.Add("Fehler beim Linken von " + b.Key + " (" + ex.Message + ")");
					hasErrors = true;
					clearBuf.Add(b.Key);
				}
			}
			foreach (string b in clearBuf)
				buf.Remove(b);
			clearBuf.Clear();

			var timeDiff = DateTime.Now.Subtract(timer);
			System.Console.WriteLine("Lesen der Strecke in {0}", timeDiff);

			if (messages.Count > 0) 
				System.Windows.MessageBox.Show((hasErrors ? "Fehler beim Laden:" : "Hinweis:") + "\r\n" + string.Join("\r\n", messages.ToArray()), hasErrors ? "Fehler beim Laden:" : "Hinweis:");
			m_streckennetz.AddByBuffer(buf);

			ActionModulAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
			ActionOrdnerAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
		}

		/** Zeigt die aktuell geladenen Strecken im Strecken-Widget an. */
		private void AktualisiereDarstellung() 
		{
			Visualisierung visualisierung;
			if (GleisfunktionMenuItem.IsChecked)
				visualisierung = new GleisfunktionVisualisierung();
			else if (KruemmungMenuItem.IsChecked)
				visualisierung = new KruemmungVisualisierung();
			else if(UeberhoehungMenuItem.IsChecked)
				visualisierung = new UeberhoehungVisualisierung();
			else if(NeigungMenuItem.IsChecked)
				visualisierung = new NeigungVisualisierung();
			else if(GeschwindigkeitMenuItem.IsChecked)
				visualisierung = new GeschwindigkeitVisualisierung();
			else if(OberbauMenuItem.IsChecked)
				visualisierung = new OberbauVisualisierung();
			else if(FahrleitungMenuItem.IsChecked)
				visualisierung = new FahrleitungVisualisierung();
			else
				visualisierung = new GleisfunktionVisualisierung();

			var timer = DateTime.Now;
			m_streckeScene = new StreckeScene(m_streckennetz, visualisierung);
			var timeDiff = DateTime.Now.Subtract(timer);
			System.Console.WriteLine("Erstellen der Segmente in {0}", timeDiff);
			this.m_streckeView.ResetScene(this.m_streckeScene);

			m_legendeView.Children.Clear();
			var legende = visualisierung.Legende;
			if (legende != null)
			{
				m_legendeView.Children.Add(legende);
				if (visualisierung.LegendeWidth != null)
					legende.RenderTransform = new TranslateTransform(-visualisierung.LegendeWidth.Value / 2.0f, 0);
			}
		}

		/** Zeigt einen Dialog zum Oeffnen einer Strecke mit passendem Startverzeichnis und liefert den Dateinamen zurueck. */
		private IEnumerable<string> ZeigeStreckeOeffnenDialog() 
		{ 
			var dialog = new ZusiCLIProject.FileLibrary.CommonGUI.OpenFileDialogDualFolder();
			dialog.Filter = "Strecken oder Fahrpläne|*.fpn;*.st3|Strecken|*.st3|Fahrpläne|*.fpn";
			dialog.Multiselect = true;
			dialog.Title = "Strecke oder Fahrplan öffnen...";
			dialog.BasepathsForAutomaticLinkCreation = Datei.GetZusiDataDirs();
			if (Datei.GetZusiDataDirs().Length > 0)
				dialog.InitialFolder = Datei.GetZusiDataDirs().First();
			dialog.ShowDialog();
			return dialog.FileNames;
		}

		/** Zeigt einen Dialog zum Oeffnen eines Streckenordners und liefert eine Liste von ST3-Dateien in diesem Verzeichnis zurueck. */
		private IEnumerable<string> ZeigeOrdnerOeffnenDialog()
		{
			var dialog = new ZusiCLIProject.FileLibrary.CommonGUI.OpenDirectoryDialogDualFolder();
			dialog.Multiselect = true;
			dialog.Title = "Streckenordner öffnen...";
			dialog.BasepathsForAutomaticLinkCreation = Datei.GetZusiDataDirs();
			if (Datei.GetZusiDataDirs().Length > 0)
				dialog.InitialFolder = Datei.GetZusiDataDirs().First();
			dialog.ShowDialog();
			return dialog.FileNames;
		}
		private IEnumerable<string>? GetStreckeFromOrdnerOeffnenDialog(IEnumerable<string>? ordnernamen, string searchPattern)
		{
			if (ordnernamen == null)
				return null;
			var value = new List<string>();
			var zusiDataDirs = Datei.GetZusiDataDirs();
			foreach (var dateiname in ordnernamen)
			{
				var dat = new Datei();
				dat.Dateiname = dateiname;
				foreach (var pfad1 in zusiDataDirs)
				{
					if (!dat.Dateiname.ToLower().StartsWith(pfad1.ToLower()))
						continue;
					dat.Dateiname = dateiname.Substring(pfad1.Length);
					if (dat.Dateiname.StartsWith("\\"))
						dat.Dateiname = dat.Dateiname.Substring(1);

					foreach (var pfad2 in zusiDataDirs)
					{
						var pfad3 = System.IO.Path.Combine(pfad2, dat.Dateiname);
						var pfad4 = System.IO.Directory.GetFiles(pfad3, searchPattern, System.IO.SearchOption.AllDirectories);
						value.AddRange(pfad4);
					}
					goto exitForDateiname;
				}
				{
					var pfad4 = System.IO.Directory.GetFiles(dateiname, searchPattern, System.IO.SearchOption.AllDirectories);
					value.AddRange(pfad4);
				}
				exitForDateiname:
				;
			}
			return value;
		}

		public void ModulOeffnen(IEnumerable<string> dateinamen)
		{
			m_streckennetz.Clear();
			OeffneStrecken(dateinamen);
			AktualisiereDarstellung();
			SetzeAnsichtZurueck();
		}

		private void ModulOeffnen_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<string> dateinamen = ZeigeStreckeOeffnenDialog();

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				m_streckennetz.Clear();
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
				SetzeAnsichtZurueck();
			}
		}
		private void ModulAnfuegen_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<string> dateinamen = ZeigeStreckeOeffnenDialog();

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
			}
		}
		private void OrdnerOeffnen_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<string> ordnernamen = ZeigeOrdnerOeffnenDialog();
			IEnumerable<string>? dateinamen = GetStreckeFromOrdnerOeffnenDialog(ordnernamen, "*.st3");

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				m_streckennetz.Clear();
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
				SetzeAnsichtZurueck();
			}
		}
		private void OrdnerAnfuegen_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<string> ordnernamen = ZeigeOrdnerOeffnenDialog();
			IEnumerable<string>? dateinamen = GetStreckeFromOrdnerOeffnenDialog(ordnernamen, "*.st3");

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
			}
		}


		private void VisualisierungTriggered(object sender, RoutedEventArgs e)
		{
			if (isInCtor)
				return;
			GleisfunktionMenuItem.IsChecked = GleisfunktionMenuItem == sender;
			KruemmungMenuItem.IsChecked = KruemmungMenuItem == sender;
			UeberhoehungMenuItem.IsChecked = UeberhoehungMenuItem == sender;
			NeigungMenuItem.IsChecked = NeigungMenuItem == sender;
			GeschwindigkeitMenuItem.IsChecked = GeschwindigkeitMenuItem == sender;
			OberbauMenuItem.IsChecked = OberbauMenuItem == sender;
			FahrleitungMenuItem.IsChecked = FahrleitungMenuItem == sender;

			// Transformation und Scroll-Position speichern und wiederherstellen
			var tranfsorm = m_streckeView.RenderTransform;
			var centerPointH = m_streckeView.HorizontalOffset;
			var centerPointV = m_streckeView.VerticalOffset;
			AktualisiereDarstellung();
			m_streckeView.RenderTransform = tranfsorm;
			m_streckeView.ScrollToHorizontalOffset(centerPointH);
			m_streckeView.ScrollToVerticalOffset(centerPointV);
		}

		private void VergroessernMenuItem_Click(object sender, RoutedEventArgs e)
		{
			m_streckeView.Vergroessern();
		}

		private void VerkleinernMenuItem_Click_1(object sender, RoutedEventArgs e)
		{
			m_streckeView.Verkleinern();
		}
	}
}
