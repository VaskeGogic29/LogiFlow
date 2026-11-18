using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Invoicing;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Packing
{
    public class StavkaPaketa : BaseEntity
    {
        public int PaketId { get; private set; }
        public Paket Paket { get; private set; } = null!;

        public int StavkaFaktureId { get; private set; }
        public StavkaFakture StavkaFakture { get; private set; } = null!;

        public int Kolicina { get; private set; }

        private StavkaPaketa() { } // za EF Core

        internal StavkaPaketa(Paket paket, StavkaFakture stavka, int kolicina)
        {
            if (kolicina <= 0)
                throw new DomainException("Količina mora biti veća od nule.");

            Paket = paket;
            PaketId = paket.Id;
            StavkaFakture = stavka;
            StavkaFaktureId = stavka.Id;
            Kolicina = kolicina;
        }

        internal void PovecajKolicinu(int dodatno) => Kolicina += dodatno;
    }
}
