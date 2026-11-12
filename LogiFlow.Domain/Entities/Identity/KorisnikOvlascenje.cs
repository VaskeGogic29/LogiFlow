using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Identity
{
    public class KorisnikOvlascenje : BaseEntity
    {
        public int KorisnikId { get; private set; }
        public Korisnik Korisnik { get; private set; } = null!;

        public int OvlascenjeId { get; private set; }
        public Ovlascenje Ovlascenje { get; private set; } = null!;

        public DateTime? VaziDo { get; private set; }     // null = važi dok se ne opozove
        public DateTime? OpozvanoU { get; private set; }

        private KorisnikOvlascenje() { } // za EF Core

        internal KorisnikOvlascenje(Korisnik korisnik, Ovlascenje ovlascenje, DateTime? vaziDo, DateTime sada)
        {
            if (vaziDo.HasValue && vaziDo.Value <= sada)
                throw new DomainException("Rok važenja ovlašćenja mora biti u budućnosti.");

            Korisnik = korisnik;
            KorisnikId = korisnik.Id;
            Ovlascenje = ovlascenje;
            OvlascenjeId = ovlascenje.Id;
            VaziDo = vaziDo;
        }

        public bool JeVazece(DateTime sada) =>
            OpozvanoU is null && (VaziDo is null || VaziDo.Value > sada);

        internal void Opozovi(DateTime sada)
        {
            if (OpozvanoU is not null)
                throw new DomainException("Ovlašćenje je već opozvano.");
            OpozvanoU = sada;
        }
    }
}
