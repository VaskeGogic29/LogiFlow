using LogiFlow.Domain.Entities.Catalog;
using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;
using LogiFlow.Domain.ValueObjects;

namespace LogiFlow.Domain.Entities.Sales
{
    public class Narudzbina : BaseEntity
    {
        private readonly List<StavkaNarudzbine> _stavke = [];

        public string BrojNarudzbine { get; private set; } = null!;

        public int KlijentId { get; private set; }
        public Klijent Klijent { get; private set; } = null!;

        public int AdresaIsporukeId { get; private set; }
        public Adresa AdresaIsporuke { get; private set; } = null!;
        public AdresaSnimak SnimakAdrese { get; private set; } = null!;

        public DateTime DatumKreiranja { get; private set; }
        public DateOnly? ZeljeniDatumIsporuke { get; private set; }
        public string? NapomenaKlijenta { get; private set; }

        public StatusNarudzbine Status { get; private set; }
        public string? RazlogOtkazivanja { get; private set; }

        public decimal UkupnoBezPdv { get; private set; }
        public decimal UkupanPdv { get; private set; }
        public decimal UkupnoSaPdv { get; private set; }

        public byte[] RowVersion { get; private set; } = [];

        public IReadOnlyCollection<StavkaNarudzbine> Stavke => _stavke;

        private Narudzbina() { } // za EF Core

        public Narudzbina(string brojNarudzbine, Klijent klijent, int adresaIsporukeId,
                          IEnumerable<(Proizvod Proizvod, int Kolicina)> stavke, DateTime sada,
                          string? napomenaKlijenta = null, DateOnly? zeljeniDatumIsporuke = null)
        {
            if (string.IsNullOrWhiteSpace(brojNarudzbine))
                throw new DomainException("Broj narudžbine je obavezan.");
            if (zeljeniDatumIsporuke.HasValue && zeljeniDatumIsporuke.Value < DateOnly.FromDateTime(sada))
                throw new DomainException("Željeni datum isporuke ne može biti u prošlosti.");

            var adresa = klijent.NadjiAktivnuAdresu(adresaIsporukeId);   // IDOR zaštita

            BrojNarudzbine = brojNarudzbine.Trim();
            Klijent = klijent;
            KlijentId = klijent.Id;
            AdresaIsporuke = adresa;
            AdresaIsporukeId = adresa.Id;
            SnimakAdrese = adresa.NapraviSnimak();
            DatumKreiranja = sada;
            NapomenaKlijenta = napomenaKlijenta?.Trim();
            ZeljeniDatumIsporuke = zeljeniDatumIsporuke;
            Status = StatusNarudzbine.Kreirana;

            foreach (var (proizvod, kolicina) in stavke)
                DodajStavku(proizvod, kolicina);

            if (_stavke.Count == 0)
                throw new DomainException("Narudžbina mora imati bar jednu stavku.");
        }

        // ---------- Izmene dok je Kreirana ----------

        public void DodajStavku(Proizvod proizvod, int kolicina)
        {
            ProveriDaSeMozeMenjati();
            if (!proizvod.IsActive)
                throw new DomainException($"Proizvod '{proizvod.Naziv}' nije dostupan.");

            var postojeca = NadjiStavku(proizvod.Id, proizvod);
            if (postojeca is not null)
                postojeca.PromeniKolicinu(postojeca.Kolicina + kolicina);
            else
                _stavke.Add(new StavkaNarudzbine(this, proizvod, kolicina));

            PreracunajUkupno();
        }

        public void PromeniKolicinu(int proizvodId, int novaKolicina)
        {
            ProveriDaSeMozeMenjati();
            var stavka = NadjiStavku(proizvodId)
                ?? throw new DomainException("Stavka ne postoji u narudžbini.");

            stavka.PromeniKolicinu(novaKolicina);
            PreracunajUkupno();
        }

        public void UkloniStavku(int proizvodId)
        {
            ProveriDaSeMozeMenjati();
            var stavka = NadjiStavku(proizvodId)
                ?? throw new DomainException("Stavka ne postoji u narudžbini.");
            if (_stavke.Count == 1)
                throw new DomainException("Narudžbina mora imati bar jednu stavku. Otkažite je umesto toga.");

            _stavke.Remove(stavka);
            PreracunajUkupno();
        }

        public void IzmeniNapomenu(string? napomena)
        {
            ProveriDaSeMozeMenjati();
            NapomenaKlijenta = napomena?.Trim();
        }

        // ---------- Prelazi statusa ----------

        public void Potvrdi()
        {
            if (!AdresaIsporuke.ImaZonu)
                throw new DomainException("Adresa isporuke nema dodeljenu zonu. Šef prvo dodeljuje zonu.");

            PromeniStatus(StatusNarudzbine.Potvrdjena, StatusNarudzbine.Kreirana);
            SnimakAdrese = SnimakAdrese with { ZonaId = AdresaIsporuke.ZonaId };
        }

        public void Otkazi(string razlog)
        {
            PostaviRazlog(razlog);
            PromeniStatus(StatusNarudzbine.Otkazana, StatusNarudzbine.Kreirana);
        }

        public void OtkaziPotvrdjenu(string razlog)
        {
            PostaviRazlog(razlog);
            PromeniStatus(StatusNarudzbine.Otkazana, StatusNarudzbine.Potvrdjena);
        }

        public void ZapocniPakovanje() =>
            PromeniStatus(StatusNarudzbine.UPakovanju, StatusNarudzbine.Potvrdjena);

        public void OznaciKaoSpakovanu() =>
            PromeniStatus(StatusNarudzbine.Spakovana, StatusNarudzbine.UPakovanju);

        public void OznaciUTransportu() =>
            PromeniStatus(StatusNarudzbine.UTransportu,
                          StatusNarudzbine.Spakovana, StatusNarudzbine.NeuspesnaIsporuka);

        public void OznaciKaoIsporucenu() =>
            PromeniStatus(StatusNarudzbine.Isporucena, StatusNarudzbine.UTransportu);

        public void OznaciNeuspesnuIsporuku() =>
            PromeniStatus(StatusNarudzbine.NeuspesnaIsporuka, StatusNarudzbine.UTransportu);

        // ---------- Pomoćne ----------

        private void PromeniStatus(StatusNarudzbine novi, params StatusNarudzbine[] dozvoljeniIz)
        {
            if (!dozvoljeniIz.Contains(Status))
                throw new InvalidStatusTransitionException(nameof(Narudzbina), Status, novi);
            Status = novi;
        }

        private void ProveriDaSeMozeMenjati()
        {
            if (Status != StatusNarudzbine.Kreirana)
                throw new DomainException("Narudžbina se može menjati samo dok čeka potvrdu.");
        }

        private void PostaviRazlog(string razlog)
        {
            if (string.IsNullOrWhiteSpace(razlog))
                throw new DomainException("Razlog otkazivanja je obavezan.");
            RazlogOtkazivanja = razlog.Trim();
        }

        private StavkaNarudzbine? NadjiStavku(int proizvodId, Proizvod? proizvod = null) =>
            _stavke.FirstOrDefault(s =>
                (proizvod is not null && s.Proizvod == proizvod) || (proizvodId != 0 && s.ProizvodId == proizvodId));

        private void PreracunajUkupno()
        {
            UkupnoBezPdv = _stavke.Sum(s => s.IznosBezPdv);
            UkupanPdv = _stavke.Sum(s => s.IznosPdv);
            UkupnoSaPdv = UkupnoBezPdv + UkupanPdv;
        }
    }
}
