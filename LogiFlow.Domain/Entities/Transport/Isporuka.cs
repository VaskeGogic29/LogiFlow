using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Invoicing;
using LogiFlow.Domain.Entities.Packing;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;
using LogiFlow.Domain.ValueObjects;

namespace LogiFlow.Domain.Entities.Transport
{
    public class Isporuka : BaseEntity
    {
        private readonly List<Paket> _paketi = [];
        public int TuraId { get; private set; }
        public Tura Tura { get; private set; } = null!;

        public int FakturaId { get; private set; }
        public Faktura Faktura { get; private set; } = null!;

        public AdresaSnimak AdresaIsporuke { get; private set; } = null!;
        public int RedniBrojUTuri { get; private set; }
        public int BrojPokusaja { get; private set; }

        public int BrojPaketa { get; private set; }             // snimak pri planiranju
        public decimal? UkupnaTezinaKg { get; private set; }    // null ako neka kutija nije merena

        public StatusIsporuke Status { get; private set; }
        public DateTime? VremeIshoda { get; private set; } //reseno uspesno ili neuspesno

        public string? ImePrimaoca { get; private set; }
        public RazlogNeuspehaIsporuke? RazlogNeuspeha { get; private set; }
        public string? Napomena { get; private set; }

        public byte[] RowVersion { get; private set; } = [];

        public IReadOnlyCollection<Paket> Paketi => _paketi;

        public bool MozeDaSePomera =>
            Status is StatusIsporuke.Planirana or StatusIsporuke.NaPutu;

        private Isporuka() { } // za EF Core

        internal Isporuka(Tura tura, Faktura faktura, AdresaSnimak adresa, IReadOnlyCollection<Paket> paketi, int redniBroj, int brojPokusaja)
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

            BrojPaketa = paketi.Count;
            UkupnaTezinaKg = paketi.Any(p => p.TezinaKg is null)
                ? null
                : paketi.Sum(p => p.TezinaKg!.Value);

            foreach (var paket in paketi)
            {
                paket.DodeliIsporuci(this);
                _paketi.Add(paket);
            }
        }

        internal void PostaviRedniBroj(int redniBroj)
        {
            if (redniBroj < 1)
                throw new DomainException("Redni broj u turi mora biti pozitivan.");
            RedniBrojUTuri = redniBroj;
        }

        internal void OznaciNaPutu()
        {
            PromeniStatus(StatusIsporuke.NaPutu, StatusIsporuke.Planirana);
            foreach (var paket in _paketi)
                paket.UtovariUVozilo();
        }

        internal void Potvrdi(string imePrimaoca, string? napomena, DateTime sada)
        {
            if (string.IsNullOrWhiteSpace(imePrimaoca))
                throw new DomainException("Ime primaoca je obavezno.");

            PromeniStatus(StatusIsporuke.Isporucena, StatusIsporuke.NaPutu);
            ImePrimaoca = imePrimaoca.Trim();
            Napomena = napomena?.Trim();
            VremeIshoda = sada;
            foreach (var paket in _paketi)
                paket.OznaciIsporucen();
        }

        internal void OznaciNeuspesnu(RazlogNeuspehaIsporuke razlog, string? napomena, DateTime sada)
        {
            if (!Enum.IsDefined(razlog))
                throw new DomainException("Nepoznat razlog neuspeha.");
            if (razlog == RazlogNeuspehaIsporuke.Ostalo && string.IsNullOrWhiteSpace(napomena))
                throw new DomainException("Za razlog 'Ostalo' napomena je obavezna.");

            PromeniStatus(StatusIsporuke.Neuspesna, StatusIsporuke.NaPutu);
            RazlogNeuspeha = razlog;
            Napomena = napomena?.Trim();
            VremeIshoda = sada;
            foreach (var paket in _paketi)
                paket.OznaciNijeIsporucen();
        }

        internal void Otkazi()
        {
            PromeniStatus(StatusIsporuke.Otkazana, StatusIsporuke.Planirana, StatusIsporuke.NaPutu);
            foreach (var paket in _paketi.ToList())
            {
                if (paket.Status == StatusPaketa.DodeljenTuri)        // jos u magacinu
                {
                    paket.OslobodiIzIsporuke();
                    _paketi.Remove(paket);
                }
                else if (paket.Status == StatusPaketa.UVozilu)        // prekid ture u voznji
                {
                    paket.OznaciNijeIsporucen();
                }
            }
        }

        internal void OslobodiPakete()     // pre uklanjanja stanice iz planirane ture
        {
            foreach (var paket in _paketi)
                paket.OslobodiIzIsporuke();
            _paketi.Clear();
        }


        private void PromeniStatus(StatusIsporuke novi, params StatusIsporuke[] dozvoljeniIz)
        {
            if (!dozvoljeniIz.Contains(Status))
                throw new InvalidStatusTransitionException(nameof(Isporuka), Status, novi);
            Status = novi;
        }
    }
}
