using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static ZusiCLIProject.FileLibrary.Zusi3.Strecke;

namespace ZusiCLIProject.Routegraph2
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			isInCtor = true;
			InitializeComponent();
			DefaultTitel = this.Title;

			ActionModulAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
			ActionOrdnerAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;

			GleisfunktionMenuItem.IsChecked = true;
			KeineMenuItem.IsChecked = true;
			isInCtor = false;

			var grpTransf = new TransformGroup();
			grpTransf.Children.Add(new ScaleTransform(1, -1));
			grpTransf.Children.Add(new TranslateTransform(0, m_legendeView.Height));
			m_legendeView.RenderTransform = grpTransf;

			for(int i = 0; true; ++i)
			{
				object? val = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Zusi3\\Fahrsim\\Fahrplan", i.ToString(), null);
				if (val == null)
					break;
				string pfad = (string)val;
				if (string.IsNullOrEmpty(pfad))
					break;
				if (i == 0)
				{
					var sep = new Separator();
					MenuDatei.Items.Insert(MenuDatei.Items.IndexOf(ActionOrdnerAnfuegen) + 1, sep);
				}
				var men1 = new MenuItem();
				men1.Header = (i + 1).ToString() + " " + System.IO.Path.GetFileName(pfad);
				if (System.IO.File.Exists(Datei.TryFindFirstExistingFile(Datei.GetZusiDataDirs(), pfad)))
				{
					men1.Click += delegate (object sender, RoutedEventArgs e)
					{
						IEnumerable<string> dateinamen = new[] { Datei.TryFindFirstExistingFile(Datei.GetZusiDataDirs(), pfad) };
						SetzeTitel(dateinamen);
						m_streckennetz.Clear();
						OeffneStrecken(dateinamen);
						AktualisiereDarstellung();
						SetzeAnsichtZurueck();
					};
				}
				else
					men1.IsEnabled = false;
				MenuDatei.Items.Insert(MenuDatei.Items.IndexOf(ActionOrdnerAnfuegen) + 2 + i, men1);
			}
		}
		private bool isInCtor = false;

		private Streckennetz m_streckennetz = new();
		private StreckeScene? m_streckeScene = null;
		//private QGraphicsScene m_legendeScene;
		private bool m_zeigeBetriebsstellen = false;

		private string DefaultTitel;
		private void SetzeTitel(IEnumerable<string> dateinamen)
		{
			if (dateinamen.Count() != 1)
			{
				this.Title = DefaultTitel;
				return;
			}
			string single = dateinamen.Single();
			string dateiname = System.IO.Path.GetFileName(single);
			if (string.IsNullOrEmpty(dateiname))
				this.Title = DefaultTitel;
			else
				this.Title = DefaultTitel + " [" + dateiname + "]";
		}

		private void SetzeAnsichtZurueck() 
		{
			m_streckeView.SkaliereAufAnsicht(true);
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
			var streckenMitUtmPunkt = new Dictionary<string, Zusi>(System.StringComparer.InvariantCultureIgnoreCase);
			foreach (var b in buf)
			{
				foreach (Strecke s in b.Value.Strecken)
				{
					if (s.UTMPoint.Zone != 0 && !streckenMitUtmPunkt.ContainsKey(b.Key))
						streckenMitUtmPunkt.Add(b.Key, b.Value);
				}
			}

			var timeDiff = DateTime.Now.Subtract(timer);
			System.Diagnostics.Debug.WriteLine("Lesen der Strecke in {0}", timeDiff);

			if (messages.Count > 0) 
				System.Windows.MessageBox.Show((hasErrors ? "Fehler beim Laden:" : "Hinweis:") + "\r\n" + string.Join("\r\n", messages.ToArray()), hasErrors ? "Fehler beim Laden:" : "Hinweis:");
			m_streckennetz.AddByBuffer((streckenMitUtmPunkt.Count > 0) ? streckenMitUtmPunkt : buf);

			ActionModulAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
			ActionOrdnerAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
		}

		/** Zeigt die aktuell geladenen Strecken im Strecken-Widget an. */
		private void AktualisiereDarstellung() 
		{
			Visualisierung? visualisierung;
			if (KeineMenuItem.IsChecked)
				visualisierung = null;
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
			else if(ETCSTrustedAreasMenuItem.IsChecked)
				visualisierung = new EtcsTrustedAreaVisualisierung();
			else
				visualisierung = null;

			if (GleisfunktionMenuItem.IsChecked)
				visualisierung = new GleisfunktionVisualisierung(visualisierung);

			var timer = DateTime.Now;
			m_streckeScene = new StreckeScene(m_streckennetz, visualisierung, m_zeigeBetriebsstellen, ETCSTrustedAreasMenuItem.IsChecked);
			var timeDiff = DateTime.Now.Subtract(timer);
			System.Diagnostics.Debug.WriteLine("Erstellen der Segmente in {0}", timeDiff);
			this.m_streckeView.ResetScene(this.m_streckeScene);

			m_legendeView.Children.Clear();
			
			var legende = (visualisierung == null) ? null : visualisierung.Legende;
			if (legende != null)
			{
				m_legendeView.Children.Add(legende);
				if (visualisierung?.LegendeWidth != null)
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
		private IEnumerable<string>? FindeSt3Rekursiv(IEnumerable<string>? ordnernamen, string filter)
		{
			bool wasCanceled = false;
			bool timeElapsed = false;
			return FindeSt3Rekursiv(ordnernamen, filter, ref wasCanceled, ref timeElapsed, null, null);
		}
		private IEnumerable<string>? FindeSt3Rekursiv(IEnumerable<string>? ordnernamen, string filter, ref bool wasCanceled, ref bool timeElapsed, TextBlock? refreshLabel, System.Threading.SynchronizationContext? synchronizationContext)
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
						if (!System.IO.Directory.Exists(pfad3))
							continue;
						FindeSt3RekursivIntern(pfad3, filter, value, ref wasCanceled, ref timeElapsed, refreshLabel, synchronizationContext);
					}
					goto exitForDateiname;
				}
				{
					FindeSt3RekursivIntern(dateiname, filter, value, ref wasCanceled, ref timeElapsed, refreshLabel, synchronizationContext);
				}
				exitForDateiname:
				;
			}
			return value;
		}
		private void FindeSt3RekursivIntern(string dir, string filter, List<string> value, ref bool wasCanceled, ref bool timeElapsed, TextBlock? refreshLabel, System.Threading.SynchronizationContext? synchronizationContext)
		{
			if (wasCanceled)
				return;
			if (timeElapsed && refreshLabel != null)
			{
				timeElapsed = false;
				if (synchronizationContext == null)
					refreshLabel.Text = dir;
				else
					synchronizationContext.Post(delegate (object? o)
					{
						refreshLabel.Text = dir;
					}, null);
			}
			var subfiles = System.IO.Directory.GetFiles(dir, filter, System.IO.SearchOption.TopDirectoryOnly);
			value.AddRange(subfiles);
			var subdirs = System.IO.Directory.GetDirectories(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
			foreach(var subdir in subdirs)
			{
				FindeSt3RekursivIntern(subdir, filter, value, ref wasCanceled, ref timeElapsed, refreshLabel, synchronizationContext);
			}
		}
		private delegate void OpenLoadingGuiBackgroundMethod(ref bool wasCanceled, ref bool timeElapsed, TextBlock caption, TextBlock file, System.Threading.SynchronizationContext? synchronizationContext);

		private IEnumerable<string>? FindeSt3RekursivAssyncGui(IEnumerable<string>? ordnernamen, string filter)
		{
			IEnumerable<string>? value = null;
			OpenLoadingGui(delegate (ref bool wasCanceled, ref bool timeElapsed, TextBlock caption, TextBlock file, System.Threading.SynchronizationContext? synchronizationContext)
			{
				if (synchronizationContext == null)
					caption.Text = "Suche Dateien...";
				else
					synchronizationContext.Post(delegate (object? o)
					{
						caption.Text = "Suche Dateien...";
					}, null);
				value = FindeSt3Rekursiv(ordnernamen, filter, ref wasCanceled, ref timeElapsed, file, synchronizationContext);
			});
			return value;
		}
		private void OpenLoadingGui(OpenLoadingGuiBackgroundMethod backgroundMethod)
		{
			var wnd = new System.Windows.Window();
			wnd.Width = 450;
			wnd.Height = 150;
			wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			wnd.ResizeMode = ResizeMode.NoResize;

			var grid = new Grid();
			grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
			grid.VerticalAlignment = VerticalAlignment.Top;
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			wnd.Content = grid;

			TextBlock caption = new TextBlock();
			caption.Text = "Suche Dateien...";
			grid.Children.Add(caption);
			Grid.SetRow(grid, 0);

			TextBlock file = new TextBlock();
			file.Text = "";
			grid.Children.Add(file);
			Grid.SetRow(file, 1);

			var prog = new System.Windows.Controls.ProgressBar();
			prog.IsIndeterminate = true;
			grid.Children.Add(prog);
			Grid.SetRow(prog, 2);

			var btn = new System.Windows.Controls.Button();
			btn.Content = "Abbrechen";
			grid.Children.Add(btn);
			Grid.SetRow(btn, 4);

			bool wasCanceled = false;
			bool timeElapsed = false;
			bool wndWasShown = false;

			btn.Click += delegate (object sender, RoutedEventArgs e) { wasCanceled = true; btn.IsEnabled = false; };

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(0.3);
			timer.Tick += (s, e) =>
			{
				timeElapsed = true;
			};
			timer.Start();

			BackgroundWorker worker = new BackgroundWorker();
			System.Threading.SynchronizationContext? synchronizationContext = System.Threading.SynchronizationContext.Current;
			worker.DoWork += (s, e) =>
			{
				backgroundMethod(ref wasCanceled, ref timeElapsed, caption, file, synchronizationContext);
			};
			worker.RunWorkerCompleted += (s, e) =>
			{
				if (wndWasShown)
					wnd.Close();
			};
			worker.RunWorkerAsync();

			for(int i = 0; i < 6; ++i)
			{
				if (!worker.IsBusy)
					return;
				System.Threading.Thread.Sleep(50);
				continue;
			}

			wndWasShown = true;
			wnd.ShowDialog();
		}

		public void ModulOeffnen(IEnumerable<string> dateinamen)
		{
			SetzeTitel(dateinamen);
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
				SetzeTitel(dateinamen);
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
				this.Title = DefaultTitel;
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
			}
		}
		private void OrdnerOeffnen_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<string> ordnernamen = ZeigeOrdnerOeffnenDialog();
			IEnumerable<string>? dateinamen = FindeSt3RekursivAssyncGui(ordnernamen, "*.st3");

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				SetzeTitel(ordnernamen);
				m_streckennetz.Clear();
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
				SetzeAnsichtZurueck();
			}
		}
		private void OrdnerAnfuegen_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<string> ordnernamen = ZeigeOrdnerOeffnenDialog();
			IEnumerable<string>? dateinamen = FindeSt3RekursivAssyncGui(ordnernamen, "*.st3");

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				this.Title = DefaultTitel;
				OeffneStrecken(dateinamen);
				AktualisiereDarstellung();
			}
		}


		private void GleisfunktionTriggered(object sender, RoutedEventArgs e)
		{
			// Transformation und Scroll-Position speichern und wiederherstellen
			var tranfsorm = m_streckeView.RenderTransform;
			var centerPointH = m_streckeView.HorizontalOffset;
			var centerPointV = m_streckeView.VerticalOffset;
			AktualisiereDarstellung();
			m_streckeView.RenderTransform = tranfsorm;
			m_streckeView.ScrollToHorizontalOffset(centerPointH);
			m_streckeView.ScrollToVerticalOffset(centerPointV);
		}
		private bool isInVisualisierung = false;
		private void VisualisierungTriggered(object sender, RoutedEventArgs e)
		{
			if (isInCtor)
				return;
			isInVisualisierung = true;
			KeineMenuItem.IsChecked = KeineMenuItem == sender;
			KruemmungMenuItem.IsChecked = KruemmungMenuItem == sender;
			UeberhoehungMenuItem.IsChecked = UeberhoehungMenuItem == sender;
			NeigungMenuItem.IsChecked = NeigungMenuItem == sender;
			GeschwindigkeitMenuItem.IsChecked = GeschwindigkeitMenuItem == sender;
			OberbauMenuItem.IsChecked = OberbauMenuItem == sender;
			FahrleitungMenuItem.IsChecked = FahrleitungMenuItem == sender;
			ETCSTrustedAreasMenuItem.IsChecked = ETCSTrustedAreasMenuItem == sender;

			// Transformation und Scroll-Position speichern und wiederherstellen
			var tranfsorm = m_streckeView.RenderTransform;
			var centerPointH = m_streckeView.HorizontalOffset;
			var centerPointV = m_streckeView.VerticalOffset;
			AktualisiereDarstellung();
			m_streckeView.RenderTransform = tranfsorm;
			m_streckeView.ScrollToHorizontalOffset(centerPointH);
			m_streckeView.ScrollToVerticalOffset(centerPointV);
			isInVisualisierung = false;
		}
		private void VisualisierungUnchecked(object sender, RoutedEventArgs e)
		{
			if (isInVisualisierung)
				return;
			((MenuItem)sender).IsChecked = true; //Unchecking nicht erlauben
		}

		private void VergroessernMenuItem_Click(object sender, RoutedEventArgs e)
		{
			m_streckeView.Vergroessern();
		}

		private void VerkleinernMenuItem_Click(object sender, RoutedEventArgs e)
		{
			m_streckeView.Verkleinern();
		}
		private void SetzeAnsichtZurueckMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SetzeAnsichtZurueck();
		}

		private void BetriebsstellennamenMenuItem_Checked(object sender, RoutedEventArgs e)
		{
			m_zeigeBetriebsstellen = BetriebsstellennamenMenuItem.IsChecked;

			if (isInCtor)
				return;

			// Transformation und Scroll-Position speichern und wiederherstellen
			// Transformation und Scroll-Position speichern und wiederherstellen
			var tranfsorm = m_streckeView.RenderTransform;
			var centerPointH = m_streckeView.HorizontalOffset;
			var centerPointV = m_streckeView.VerticalOffset;
			AktualisiereDarstellung();
			m_streckeView.RenderTransform = tranfsorm;
			m_streckeView.ScrollToHorizontalOffset(centerPointH);
			m_streckeView.ScrollToVerticalOffset(centerPointV);
		}
		private void Beenden_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//WPF bekommt es offenbar nicht hin, die MessageBox mit Visuellen Stilen zu zeichnen...
			System.Windows.Forms.MessageBox.Show("Routegraph2n " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() + "\r\n\r\n"+
				"Routegraph2n kann unter der GNU GENERAL PUBLIC LICENSE Version 3 verwendet werden." + "\r\n\r\n" + 
				"Folgende Zusi-Datenverzeichnisse wurden erkannt:\r\n" + string.Join("\r\n", Datei.GetZusiDataDirs()), "Routegraph2n");
		}
	}
}
