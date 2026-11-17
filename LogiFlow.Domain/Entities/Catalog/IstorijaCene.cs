using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Catalog
{
    public class IstorijaCene : BaseEntity
    {
        public int ProizvodId { get; private set; }
        public Proizvod Proizvod { get; private set; } = null!;

        public decimal Cena { get; private set; }
        public DateTime VaziOd { get; private set; }
        public DateTime? VaziDo { get; private set; }          // null = trenutno vazeca cena
        public int? PromenioKorisnikId { get; private set; }

        private IstorijaCene() { } // za EF Core

        internal IstorijaCene(Proizvod proizvod, decimal cena, DateTime vaziOd, int? promenioKorisnikId)
        {
            Proizvod = proizvod;
            ProizvodId = proizvod.Id;
            Cena = cena;
            VaziOd = vaziOd;
            PromenioKorisnikId = promenioKorisnikId;
        }

        internal void Zatvori(DateTime sada)
        {
            if (VaziDo is not null)
                throw new DomainException("Cena je već zatvorena.");
            if (sada < VaziOd)
                throw new DomainException("Kraj važenja ne može biti pre početka.");
            VaziDo = sada;
        }
    }
}
