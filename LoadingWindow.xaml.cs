using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;

namespace Z8Routegraph2n
{
    /// <summary>
    /// Interaktionslogik für LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
			m_defaultTitel = this.Title;
		}

		private string m_defaultTitel;
		private void SetzeTitel(IEnumerable<string> dateinamen)
		{
			if (dateinamen.Count() != 1)
			{
				this.Title = m_defaultTitel;
				return;
			}
			string single = dateinamen.Single();
			string dateiname = System.IO.Path.GetFileName(single);
			if (string.IsNullOrEmpty(dateiname))
				this.Title = m_defaultTitel;
			else
				this.Title = m_defaultTitel + " [" + dateiname + "]";
		}


		private bool m_canceling = false;
		private DateTime m_startedAt;
		private bool m_sucheStrDone = false;
		private int m_sucheStrNum = 0;
		private string m_sucheStrAktuell = "";
		private DateTime m_sucheStrAt;
		private bool m_ladeStrDone = false;
		private int m_ladeStrNum = 0;
		private int m_ladeFpnNum = 0;
		private DateTime m_ladeStrAt;
		private int m_linkStrNum = 0;
		private bool m_linkStrDone = false;
		private DateTime m_linkStrAt;
		private DateTime m_linkStrAfterDialogAt;

		private System.Windows.Threading.DispatcherTimer m_refreshingTimer = new();


		/** Liefert eine Liste von ST3-Dateien in diesem Verzeichnis zurueck. */
		private IEnumerable<string>? FindeSt3Rekursiv(IEnumerable<string> zusiDataDirs, IEnumerable<string>? ordnernamen, string filter)
		{
			if (ordnernamen == null)
				return null;
			var value = new List<string>();
			foreach (var dateiname in ordnernamen)
			{
				var dat = new Datei();
				dat.Dateiname = dateiname;
				foreach (var pfad1 in zusiDataDirs)
				{
					if (m_canceling)
						return null;
					if (!dat.Dateiname.ToLower().StartsWith(pfad1.ToLower()))
						continue;
					dat.Dateiname = dateiname.Substring(pfad1.Length);
					if (dat.Dateiname.StartsWith("\\"))
						dat.Dateiname = dat.Dateiname.Substring(1);

					foreach (var pfad2 in zusiDataDirs)
					{
						if (m_canceling)
							return null;
						var pfad3 = System.IO.Path.Combine(pfad2, dat.Dateiname);
						if (!System.IO.Directory.Exists(pfad3))
							continue;
						FindeSt3RekursivIntern(pfad3, filter, value);
					}
					goto exitForDateiname;
				}
				{
					if (m_canceling)
						return null;
					FindeSt3RekursivIntern(dateiname, filter, value);
				}
				exitForDateiname:
				;
			}
			return value;
		}
		private void FindeSt3RekursivIntern(string dir, string filter, List<string> value)
		{
			if (m_canceling)
				return;
			m_sucheStrAktuell = dir;
			var subfiles = System.IO.Directory.GetFiles(dir, filter, System.IO.SearchOption.TopDirectoryOnly);
			m_sucheStrNum += subfiles.Length;
			value.AddRange(subfiles);
			var subdirs = System.IO.Directory.GetDirectories(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
			foreach (var subdir in subdirs)
			{
				FindeSt3RekursivIntern(subdir, filter, value);
			}
		}


		/** Oeffnet Streckendateien und fuegt sie zur Liste der offenen Strecken hinzu. */
		private void OeffneStrecken(IEnumerable<string> zusiDataDirs, IEnumerable<string> dateinamen, Streckennetz streckennetz, bool anfuegen, System.Threading.SynchronizationContext synchronizationContext)
		{
			m_sucheStrNum = dateinamen.Count();
			m_sucheStrAt = DateTime.Now;
			m_sucheStrDone = true;
			var messages = new List<string>();
			bool hasErrors = false;

			var datList = new List<Datei>();
			var bufA = new Dictionary<string, Task<Zusi>>(System.StringComparer.InvariantCultureIgnoreCase);
			if (anfuegen)
				foreach (var kv in streckennetz)
					bufA.Add(kv.Key, Task<Zusi>.FromResult(kv.Value.ParentBuffer));
			foreach (var dateiname in dateinamen)
			{
				if (m_canceling)
					return;
				var dat = new Datei();
				dat.Dateiname = dateiname;
				foreach (var pfad1 in zusiDataDirs)
				{
					if (!dat.Dateiname.ToLower().StartsWith(pfad1.ToLower()))
						continue;
					dat.Dateiname = dateiname.Substring(pfad1.Length);
					if (dat.Dateiname.StartsWith("\\"))
						dat.Dateiname = dat.Dateiname.Substring(1);
					if (dateiname.ToLower() != Datei.TryFindFirstExistingFile(zusiDataDirs, dat.Dateiname).ToLower())
						messages.Add("Datei " + dat.Dateiname + " wird aus den Eigenen Daten geladen.");
					if (bufA.ContainsKey(dat.Dateiname))
						messages.Add("Datei " + dat.Dateiname + " bereits geladen.");
					break;
				}
				datList.Add(dat);
			}
			var datListNoAddList = new List<Datei>();
			while ((datListNoAddList.Count + datList.Count) > 0)
			{
				if (m_canceling)
					return;
				while (datList.Count > 0)
				{
					if (m_canceling)
						return;
					Datei dat = datList[0];
					datList.RemoveAt(0);
					dat.LoadAsync(zusiDataDirs, bufA);
					datListNoAddList.Add(dat);
				}
				if (datListNoAddList.Count > 0)
				{
					Datei dat = datListNoAddList[0];
					datListNoAddList.RemoveAt(0);

					try
					{
						if (dat.Content.Fahrplaene.Length > 0)
							m_ladeFpnNum++;
						else
							m_ladeStrNum++;
						foreach (var f1 in dat.Content.Fahrplaene) //Der Aufruf von dat.Content sorgt dafür, dass das Loading finalisiert wird.
						{
							foreach (var fs1 in f1.Streckendateien)
							{
								++m_sucheStrNum;
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
			}
			m_ladeStrAt = DateTime.Now;
			m_ladeStrDone = true;
			var buf = new Dictionary<string, Zusi>(System.StringComparer.InvariantCultureIgnoreCase);
			foreach (var kv in bufA)
			{
				if (!kv.Value.IsFaulted)
					buf.Add(kv.Key, kv.Value.Result);
			}
			var clearBuf = new List<string>();
			foreach (var b in buf)
			{
				if (m_canceling)
					return;
				try
				{
					var sub = b.Value.CollectDateiSubmemberExceptHauptlandschaft();
					foreach (var dat1 in sub)
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
				++m_linkStrNum;
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
				++m_linkStrNum;
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
				++m_linkStrNum;
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


			m_linkStrAt = DateTime.Now;
			m_linkStrAfterDialogAt = m_linkStrAt;
			m_linkStrDone = true;
			var timeDiff = m_linkStrAt.Subtract(m_sucheStrAt);
			System.Diagnostics.Debug.WriteLine("Lesen + Linken der Strecke in {0}", timeDiff);

			if (messages.Count > 0)
				//WPF bekommt es offenbar nicht hin, die MessageBox mit Visuellen Stilen zu zeichnen...
				//System.Windows.Application.Current.Dispatcher.Invoke(delegate ()
				synchronizationContext.Send(delegate(object? o)
				{
					System.Windows.Forms.MessageBox.Show((hasErrors ? "Fehler beim Laden:" : "Hinweis:") + "\r\n" + string.Join("\r\n", messages.ToArray()), hasErrors ? "Fehler beim Laden:" : "Hinweis:");
				}, null);
			m_linkStrAfterDialogAt = DateTime.Now;
			if (!anfuegen)
				streckennetz.Clear();
			streckennetz.AddByBuffer((streckenMitUtmPunkt.Count > 0) ? streckenMitUtmPunkt : buf);

			//ActionModulAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
			//ActionOrdnerAnfuegen.IsEnabled = !m_streckennetz.IsEmpty;
		}


		private bool OeffneDateienIntern(IEnumerable<string> zusiDataDirs, IEnumerable<string> ausgewaehlteDateien, Streckennetz streckennetz, bool anfuegen, bool findeSt3Rekursiv, System.Threading.SynchronizationContext synchronizationContext)
		{
			//IEnumerable<string> ordnernamen = ZeigeOrdnerOeffnenDialog();
			//IEnumerable<string>? dateinamen = FindeSt3RekursivAssyncGui(ordnernamen, "*.st3");
			IEnumerable<string>? dateinamen;
			if (findeSt3Rekursiv)
				dateinamen = FindeSt3Rekursiv(zusiDataDirs, ausgewaehlteDateien, "*.st3");
			else
				dateinamen = ausgewaehlteDateien;

			if (dateinamen != null && dateinamen.GetEnumerator().MoveNext())
			{
				OeffneStrecken(zusiDataDirs, dateinamen, streckennetz, anfuegen, synchronizationContext);
				return true;
			}
			else
				return false;

		}

		public static bool OeffneDateien(IEnumerable<string> zusiDataDirs, IEnumerable<string> ausgewaehlteDateien, Streckennetz streckennetz, bool anfuegen, bool findeSt3Rekursiv, Action<LoadingWindow?> beforeCloseDialog)
		{
			if (ausgewaehlteDateien.Count() == 0)
				return false;

			var value = new LoadingWindow();
			var reference = new LoadingWindow();
			bool success = false;
			value.SetzeTitel(ausgewaehlteDateien);
			value.m_startedAt = DateTime.Now;

			value.m_refreshingTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
			value.m_refreshingTimer.Tick += new EventHandler(delegate (object? sender, EventArgs e)
			{
				value.AktualisiereGui(reference);
			});

			var syncCtx = System.Threading.SynchronizationContext.Current;
			var loadingTask = Task.Run(delegate ()
			{
				success = value.OeffneDateienIntern(zusiDataDirs, ausgewaehlteDateien, streckennetz, anfuegen, findeSt3Rekursiv, syncCtx);
			});

			if (loadingTask.Wait(500))
			{
				if (success)
					beforeCloseDialog(null);
				return success;
			}

			value.AktualisiereGui(reference);
			if (syncCtx == null)
				throw new InvalidOperationException();
			loadingTask.ContinueWith(delegate(Task t)
			{
				//System.Windows.Application.Current.Dispatcher.Invoke(delegate ()
				syncCtx.Post(delegate(object? o)
				{
					value.AktualisiereGui(reference);
					value.UpdateLayout();
					var refreshingTimer = new System.Windows.Threading.DispatcherTimer();
					refreshingTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
					refreshingTimer.Tick += new EventHandler(delegate (object? sender, EventArgs e)
					{
						refreshingTimer.Stop();
						if (success)
							beforeCloseDialog(value);
						value.m_refreshingTimer.Stop();
						value.Close();
					});
					refreshingTimer.Start();
				}, null);
			});

			value.m_refreshingTimer.Start();
			value.ShowDialog();
			return success;
		}



		private bool AktualisiereGui(LoadingWindow reference)
		{
			var now = DateTime.Now;
			if (!m_sucheStrDone)
			{
				lblSuchen.Content = $"{reference.lblSuchen.Content} ({m_sucheStrNum} gefunden, {now.Subtract(m_startedAt).ToString("%m\\:ss")}, {m_sucheStrAktuell})";
			}
			else if (!reference.m_sucheStrDone)
			{
				lblSuchen.Content = $"{reference.lblSuchen.Content} Fertig! ({m_sucheStrNum} in {m_sucheStrAt.Subtract(m_startedAt).ToString("%m\\:ss\\.fff")})";
				reference.m_sucheStrDone = true;
			}

			if (!m_sucheStrDone)
			{ }
			else if (!m_ladeStrDone)
			{
				lblLaden.Content = $"{reference.lblLaden.Content} ({m_ladeFpnNum} Fpl, {m_ladeStrNum} Str von {m_sucheStrNum}, {now.Subtract(m_sucheStrAt).ToString("%m\\:ss")})";
			}
			else if (!reference.m_ladeStrDone)
			{
				lblLaden.Content = $"{reference.lblLaden.Content} Fertig! ({m_ladeFpnNum} Fpl, {m_ladeStrNum} Str, in {m_ladeStrAt.Subtract(m_sucheStrAt).ToString("%m\\:ss\\.fff")})";
				reference.m_ladeStrDone = true;
			}

			if (!m_ladeStrDone)
			{ }
			else if (!m_linkStrDone)
			{
				lblLink.Content = $"{reference.lblLink.Content} ({((double)m_linkStrNum / m_sucheStrNum / 3.0 * 100.0).ToString("0")} %, {now.Subtract(m_ladeStrAt).ToString("%m\\:ss")})";
			}
			else if (!reference.m_linkStrDone)
			{
				lblLink.Content = $"{reference.lblLink.Content} Fertig! (in {m_linkStrAt.Subtract(m_ladeStrAt).ToString("%m\\:ss\\.fff")})";
				reference.m_linkStrDone = true;
				btnCancel.IsEnabled = false;
				return true;
			}

			if (!m_linkStrDone)
			{ }
			else
			{
				lblView.Content = $"{reference.lblView.Content} ({now.Subtract(m_linkStrAfterDialogAt).ToString("%m\\:ss")})";
			}

			return false;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			m_canceling = true;
			btnCancel.IsEnabled = false;
		}
	}
}
