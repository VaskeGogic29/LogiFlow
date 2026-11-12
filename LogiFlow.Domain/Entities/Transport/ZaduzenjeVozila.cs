using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Transport
{
    public class ZaduzenjeVozila : BaseEntity
    {
        public int VozacId { get; private set; }
        public Vozac Vozac { get; private set; } = null!;

        public int VoziloId { get; private set; }
        public Vozilo Vozilo { get; private set; } = null!;

        public DateTime ZaduzenOd { get; private set; }
        public DateTime? RazduzenU { get; private set; }      // null dok zaduženje traje
        public int ZaduzioKorisnikId { get; private set; }    // Sef
        public string? Napomena { get; private set; }

        public bool JeAktivno => RazduzenU is null;

        private ZaduzenjeVozila() { } // za EF Core

        internal ZaduzenjeVozila(Vozac vozac, Vozilo vozilo, int zaduzioKorisnikId, DateTime sada, string? napomena)
        {
            Vozac = vozac;
            VozacId = vozac.Id;
            Vozilo = vozilo;
            VoziloId = vozilo.Id;
            ZaduzioKorisnikId = zaduzioKorisnikId;
            ZaduzenOd = sada;
            Napomena = napomena?.Trim();
        }

        internal void Razduzi(DateTime sada)
        {
            if (!JeAktivno)
                throw new DomainException("Zaduženje je već završeno.");
            if (sada < ZaduzenOd)
                throw new DomainException("Razduženje ne može biti pre zaduženja.");
            RazduzenU = sada;
        }
    }
}
