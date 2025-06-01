
using System.Linq;
using ZusiCLIProject.FileLibrary.Zusi3;
using ZusiCLIProject.Routegraph2;

namespace ZusiCLIProject.Routegraph2
{
    public class Segmentierer
    {
        protected virtual bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            return false;
        }
        public virtual bool IstSegmentStart(Strecke.ElementInfo elementUndRichtung)
        {
            if (elementUndRichtung.GegenrichtungBuffer.NachfolgerBuffer.Length != 1 ||
				elementUndRichtung.GegenrichtungBuffer.NachfolgerBuffer[0] == null ||
				elementUndRichtung.GegenrichtungBuffer.NachfolgerIsFremdmodulBuffer.All(b => b))
            {
                return true;
            }

			//if (elementUndRichtung.GegenrichtungBuffer.NachfolgerBuffer.Length == 0) -> Bereits oben erledigt
			var vorgaenger = elementUndRichtung.GegenrichtungBuffer.NachfolgerBuffer[0].GegenrichtungBuffer;
            if (vorgaenger.NachfolgerBuffer.Length != 1 || vorgaenger.NachfolgerBuffer[0] != elementUndRichtung)
                return true;
			//var vorgaenger = elementUndRichtung.vorgaenger();
            return /*!vorgaenger.hatNachfolger(0) || vorgaenger.nachfolgerElementeSindInAnderemModul() ||
                    (vorgaenger.nachfolger(0) != elementUndRichtung) ||*/
                    IstSegmentGrenze(vorgaenger, elementUndRichtung);
        }
        public virtual bool IstSegmentEnde(Strecke.ElementInfo elementUndRichtung) 
        {
            return IstSegmentStart(elementUndRichtung.GegenrichtungBuffer);
        }
        public virtual bool BeideRichtungen 
        {
            get
            {
                return false;
            }
        }    
    }
    public abstract class RichtungsInfoSegmentierer : Segmentierer
    {
        /*protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger)
        {
            var vorgaengerRichtungsInfo = vorgaenger.richtungsInfo();
            var nachfolgerRichtungsInfo = nachfolger.richtungsInfo();
            if (vorgaengerRichtungsInfo.has_value() != nachfolgerRichtungsInfo.has_value()) { //<- Hat hier immer eine Value
                return true;
            } else {
                return !vorgaengerRichtungsInfo.has_value() || IstSegmentGrenze(*vorgaengerRichtungsInfo, *nachfolgerRichtungsInfo);
            }
        }*/
        public override bool IstSegmentEnde(Strecke.ElementInfo elementUndRichtung)
        {
            if (elementUndRichtung.NachfolgerBuffer.Length != 1 || elementUndRichtung.NachfolgerBuffer[0] == null)
            {
                return true;
			}

			var nachfolger = elementUndRichtung.NachfolgerBuffer[0];

			if (nachfolger.GegenrichtungBuffer.NachfolgerBuffer.Length != 1 || nachfolger.GegenrichtungBuffer.NachfolgerBuffer[0] == null 
                || nachfolger.GegenrichtungBuffer.NachfolgerBuffer[0].GegenrichtungBuffer != elementUndRichtung)
				return true;

			return /*!nachfolger.hatVorgaenger() || nachfolger.vorgaengerElementeSindInAnderemModul() ||
                    (nachfolger.vorgaenger(0) != elementUndRichtung) ||*/
                    IstSegmentGrenze(elementUndRichtung, nachfolger);
        }
        public override bool BeideRichtungen
		{
			get
            {
                return true;
            }
        }
   
        protected abstract bool IstSegmentGrenze2(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger);
    }
    public class GleisfunktionSegmentierer : Segmentierer
    {
        protected override bool IstSegmentGrenze(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger) 
        {
            return vorgaenger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion) !=
                 nachfolger.ParentBuffer.HasFunktion(Strecke.Element.Elementfunktion.KeineGleisfunktion);
        }
    }
    public class GeschwindigkeitSegmentierer : RichtungsInfoSegmentierer
    {
        protected override bool IstSegmentGrenze2(Strecke.ElementInfo vorgaenger, Strecke.ElementInfo nachfolger) 
        {
            return vorgaenger.vMax != nachfolger.vMax;
        }

    }
}
