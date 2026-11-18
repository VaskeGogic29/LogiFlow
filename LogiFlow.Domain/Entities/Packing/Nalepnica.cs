using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Invoicing;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;
using LogiFlow.Domain.ValueObjects;

namespace LogiFlow.Domain.Entities.Packing
{
    public class Nalepnica : BaseEntity
    {
        public int PaketId { get; private set; }                    // jedna nalepnica po paketu
        public Paket Paket { get; private set; } = null!;

        public string PoslednjeTriCifreFakture { get; private set; } = null!;
        public string ImeKlijenta { get; private set; } = null!;
        public AdresaSnimak AdresaIsporuke { get; private set; } = null!;
        public string OznakaKutije { get; private set; } = null!;  // npr. "2/3"

        public int Verzija { get; private set; } //vazno zbog QR koda
        public DateTime GenerisanaU { get; private set; }
        public int BrojStampanja { get; private set; }
        public DateTime? PoslednjaStampaU { get; private set; }

        private Nalepnica() { } // za EF Core

        public Nalepnica(Paket paket, Faktura faktura, DateTime sada)
        {
            //neka ostane da se razmotri, da li je pametno da se nalepnica pravi tek kad se spakuje PAKET
            if (paket.Status != StatusPaketa.SpremanZaTransport || paket.UkupnoPaketa is null)
                throw new DomainException("Nalepnica se pravi tek kad je pakovanje fakture završeno.");
            if (paket.FakturaId != faktura.Id)
                throw new DomainException("Paket ne pripada ovoj fakturi.");

            Paket = paket;
            PaketId = paket.Id;
            PoslednjeTriCifreFakture = faktura.PoslednjeTriCifre;
            ImeKlijenta = faktura.Kupac.Naziv;
            AdresaIsporuke = faktura.Narudzbina.SnimakAdrese;
            OznakaKutije = $"{paket.RedniBroj}/{paket.UkupnoPaketa}";
            Verzija = 1;
            GenerisanaU = sada;
        }

        public void EvidentirajStampu(DateTime sada)
        {
            BrojStampanja++;
            PoslednjaStampaU = sada;
        }

        // Posle izdavanja novog QR koda
        public void Regenerisi(DateTime sada)
        {
            Verzija++;
            GenerisanaU = sada;
            BrojStampanja = 0;
            PoslednjaStampaU = null;
        }
    }
}
