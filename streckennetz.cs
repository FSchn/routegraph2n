
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;

namespace ZusiCLIProject.Routegraph2
{
	public class Streckennetz : IEnumerable<KeyValuePair<string, Strecke>>
    { 
        public void AddByBuffer(Dictionary<string, Zusi> buffer)
        {
            //In dieser Implementierung wird das Linking der Strecken durch die Library erledigt.
            foreach(var item in buffer)
            {
                if (item.Value.Strecken.Length == 1)
                {
                    if (m_strecken.TryGetValue(item.Key, out var strecke))
                    {
                        if (strecke == item.Value.Strecken[0])
                            continue;
                    }
                    m_strecken.Add(item.Key, item.Value.Strecken[0]);
				}
            }

        }
        public void Clear()
        {
			m_strecken.Clear();
		}
        public Strecke? Get(string pfad)
        {
            if (m_strecken.TryGetValue(pfad, out Strecke? value))
                return value;
            else
                return null;
        }
        public string? GetPfad(Strecke strecke)
        {
            var itr = m_strecken.Where(delegate (KeyValuePair<string, Strecke> suche) { return suche.Value == strecke; });
            foreach(var item in itr) { return item.Key; }
            return null;
        }
        public int Count { get { return m_strecken.Count; } }
        public bool IsEmpty { get { return m_strecken.Count == 0; } }
        private readonly Dictionary<string, Strecke> m_strecken = new (System.StringComparer.InvariantCultureIgnoreCase);

		public IEnumerator<KeyValuePair<string, Strecke>> GetEnumerator()
		{
			return m_strecken.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_strecken.GetEnumerator();
		}
	}
}