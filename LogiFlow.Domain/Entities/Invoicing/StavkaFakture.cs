using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Sales;
using LogiFlow.Domain.Enums;

namespace LogiFlow.Domain.Entities.Invoicing
{
    public class StavkaFakture : BaseEntity
    {
        public int FakturaId { get; private set; }
        public Faktura Faktura { get; private set; } = null!;

        public int StavkaNarudzbineId { get; private set; }          // trag do narudzbine
        public StavkaNarudzbine StavkaNarudzbine { get; private set; } = null!;

        public int ProizvodId { get; private set; }                  // za izvestaje
        public string NazivProizvoda { get; private set; } = null!;  // snimak
        public string SifraProizvoda { get; private set; } = null!;  // snimak
        public JedinicaMere JedinicaMere { get; private set; }       // snimak

        public int Kolicina { get; private set; }
        public decimal JedinicnaCena { get; private set; }
        public decimal PdvStopa { get; private set; }
        public decimal IznosBezPdv { get; private set; }
        public decimal IznosPdv { get; private set; }
        public decimal Ukupno { get; private set; }

        private StavkaFakture() { } // za EF Core

        internal StavkaFakture(Faktura faktura, StavkaNarudzbine stavka)
        {
            Faktura = faktura;
            FakturaId = faktura.Id;
            StavkaNarudzbine = stavka;
            StavkaNarudzbineId = stavka.Id;

            ProizvodId = stavka.ProizvodId;
            NazivProizvoda = stavka.Proizvod.Naziv;
            SifraProizvoda = stavka.Proizvod.Sifra;
            JedinicaMere = stavka.Proizvod.JedinicaMere;

            Kolicina = stavka.Kolicina;
            JedinicnaCena = stavka.JedinicnaCena;     // cena iz narudzbine, ne trenutna
            PdvStopa = stavka.PdvStopa;
            IznosBezPdv = stavka.IznosBezPdv;
            IznosPdv = stavka.IznosPdv;
            Ukupno = IznosBezPdv + IznosPdv;
        }
    }
}
