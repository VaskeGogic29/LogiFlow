using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Invoicing;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;
using LogiFlow.Domain.ValueObjects;

namespace LogiFlow.Domain.Entities.Transport
{
    public class Isporuka : BaseEntity
    {
        public int TuraId { get; private set; }
        public Tura Tura { get; private set; } = null!;

        public int FakturaId { get; private set; }
        public Faktura Faktura { get; private set; } = null!;

        public AdresaSnimak AdresaIsporuke { get; private set; } = null!;
        public int RedniBrojUTuri { get; private set; }
        public int BrojPokusaja { get; private set; }

        public StatusIsporuke Status { get; private set; }
        public DateTime? VremeIsporuke { get; private set; }
        public string? ImePrimaoca { get; private set; }
        public string? Napomena { get; private set; }

        public byte[] RowVersion { get; private set; } = [];

        public bool MozeDaSePomera =>
            Status is StatusIsporuke.Planirana or StatusIsporuke.NaPutu;

        private Isporuka() { } // za EF Core

        internal Isporuka(Tura tura, Faktura faktura, AdresaSnimak adresa, int redniBroj, int brojPokusaja)
        {
            if (brojPokusaja < 1)
                throw new DomainException("Broj pokušaja mora biti najmanje 1.");

            Tura = tura;
            TuraId = tura.Id;
            Faktura = faktura;
            FakturaId = faktura.Id;
            AdresaIsporuke = adresa;
            PostaviRedniBroj(redniBroj);
            BrojPokusaja = brojPokusaja;
            Status = StatusIsporuke.Planirana;
        }

        internal void PostaviRedniBroj(int redniBroj)
        {
            if (redniBroj < 1)
                throw new DomainException("Redni broj u turi mora biti pozitivan.");
            RedniBrojUTuri = redniBroj;
        }

        internal void OznaciNaPutu() =>
            PromeniStatus(StatusIsporuke.NaPutu, StatusIsporuke.Planirana);

        internal void Potvrdi(string imePrimaoca, string? napomena, DateTime sada)
        {
            if (string.IsNullOrWhiteSpace(imePrimaoca))
                throw new DomainException("Ime primaoca je obavezno.");

            PromeniStatus(StatusIsporuke.Isporucena, StatusIsporuke.NaPutu);
            ImePrimaoca = imePrimaoca.Trim();
            Napomena = napomena?.Trim();
            VremeIsporuke = sada;
        }

        internal void Otkazi() =>
            PromeniStatus(StatusIsporuke.Otkazana, StatusIsporuke.Planirana, StatusIsporuke.NaPutu);

        private void PromeniStatus(StatusIsporuke novi, params StatusIsporuke[] dozvoljeniIz)
        {
            if (!dozvoljeniIz.Contains(Status))
                throw new InvalidStatusTransitionException(nameof(Isporuka), Status, novi);
            Status = novi;
        }
    }
}
