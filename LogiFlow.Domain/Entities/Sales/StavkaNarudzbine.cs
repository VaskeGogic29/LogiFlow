using LogiFlow.Domain.Entities.Catalog;
using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Sales
{
    public class StavkaNarudzbine : BaseEntity
    {
        public int NarudzbinaId { get; private set; }
        public Narudzbina Narudzbina { get; private set; } = null!;

        public int ProizvodId { get; private set; }
        public Proizvod Proizvod { get; private set; } = null!;

        public int Kolicina { get; private set; }
        public decimal JedinicnaCena { get; private set; }   // bez PDV, snimak
        public decimal PdvStopa { get; private set; }        // snimak stope
        public decimal IznosBezPdv { get; private set; }
        public decimal IznosPdv { get; private set; }

        private StavkaNarudzbine() { } // za EF Core

        internal StavkaNarudzbine(Narudzbina narudzbina, Proizvod proizvod, int kolicina)
        {
            Narudzbina = narudzbina;
            NarudzbinaId = narudzbina.Id;
            Proizvod = proizvod;
            ProizvodId = proizvod.Id;
            JedinicnaCena = proizvod.TrenutnaCena;
            PdvStopa = proizvod.PdvStopa;
            PromeniKolicinu(kolicina);
        }

        internal void PromeniKolicinu(int kolicina)
        {
            if (kolicina <= 0)
                throw new DomainException("Količina mora biti veća od nule.");

            Kolicina = kolicina;
            IznosBezPdv = Kolicina * JedinicnaCena;
            IznosPdv = Math.Round(IznosBezPdv * PdvStopa / 100m, 2, MidpointRounding.AwayFromZero);
        }
    }
}
