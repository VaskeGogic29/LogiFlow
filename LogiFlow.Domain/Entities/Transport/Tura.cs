using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Invoicing;
using LogiFlow.Domain.Entities.Packing;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Transport
{
    public class Tura : BaseEntity
    {
        private readonly List<Isporuka> _isporuke = [];

        public string Oznaka { get; private set; } = null!; //npr. T-20261120-Z03

        public int VozacId { get; private set; }
        public Vozac Vozac { get; private set; } = null!;

        public int ZonaId { get; private set; }              // snimak zone, ne menja se sa vozacem
        public Zona Zona { get; private set; } = null!;

        public int? VoziloId { get; private set; }           // upisuje se pri polasku 
        public Vozilo? Vozilo { get; private set; }

        public DateOnly Datum { get; private set; }
        public DateTime PlaniraniPocetak { get; private set; }
        public DateTime? StvarniPocetak { get; private set; }
        public DateTime? StvarniZavrsetak { get; private set; }

        public StatusTure Status { get; private set; }
        public string? Razlog { get; private set; }          // za prekid ili otkazivanje

        public byte[] RowVersion { get; private set; } = [];

        public IReadOnlyCollection<Isporuka> Isporuke => _isporuke;

        public int UkupnoPaketa =>
            _isporuke.Where(i => i.Status != StatusIsporuke.Otkazana).Sum(i => i.BrojPaketa);

        public decimal? UkupnaTezinaKg
        {
            get
            {
                var aktivne = _isporuke.Where(i => i.Status != StatusIsporuke.Otkazana).ToList();
                return aktivne.Any(i => i.UkupnaTezinaKg is null)
                    ? null
                    : aktivne.Sum(i => i.UkupnaTezinaKg!.Value);
            }
        }

        private Tura() { } // za EF Core

        public Tura(string oznaka, Vozac vozac, DateOnly datum, DateTime planiraniPocetak)
        {
            if (string.IsNullOrWhiteSpace(oznaka))
                throw new DomainException("Oznaka ture je obavezna.");

            Oznaka = oznaka.Trim().ToUpperInvariant();
            Vozac = vozac;
            VozacId = vozac.Id;
            ZonaId = vozac.ZonaId;
            Datum = datum;
            PlaniraniPocetak = planiraniPocetak;
            Status = StatusTure.Planirana;
        }

        // ---------- Planiranje ----------

        public Isporuka DodajIsporuku(Faktura faktura, IReadOnlyCollection<Paket> paketiFakture, int brojPokusaja)
        {
            ProveriStatus("dodavanje isporuke", StatusTure.Planirana);
            if (faktura.Status != StatusFakture.Izdata)
                throw new DomainException("Isporuka se može planirati samo za izdatu fakturu.");

            var adresa = faktura.Narudzbina.SnimakAdrese;
            if (adresa.ZonaId != ZonaId)
                throw new DomainException("Adresa isporuke ne pripada zoni ove ture.");
            if (_isporuke.Any(i => i.Faktura == faktura || (faktura.Id != 0 && i.FakturaId == faktura.Id)))
                throw new DomainException("Faktura je već u ovoj turi.");

            var paketi = paketiFakture.Where(p => p.Status != StatusPaketa.Otkazan).ToList();

            if (paketi.Count == 0)
                throw new DomainException("Faktura nema pakete za isporuku.");
            if (paketi.Any(p => p.FakturaId != faktura.Id))
                throw new DomainException("Svi paketi moraju pripadati ovoj fakturi.");
            if (paketi.Any(p => p.UkupnoPaketa != paketi.Count))
                throw new DomainException("Moraju se predati svi paketi fakture.");
            if (paketi.Any(p => p.Status is not (StatusPaketa.SpremanZaTransport or StatusPaketa.VracenUMagacin)))
                throw new DomainException("Svi paketi moraju biti spremni za transport.");

            var isporuka = new Isporuka(this, faktura, adresa, paketi, _isporuke.Count + 1, brojPokusaja);
            _isporuke.Add(isporuka);
            return isporuka;
        }

        public void UkloniIsporuku(int isporukaId)
        {
            ProveriStatus("uklanjanje isporuke", StatusTure.Planirana);
            //_isporuke.Remove(NadjiIsporuku(isporukaId));
            //prosireno resenje koje oslobadja pakete
            var isporuka = NadjiIsporuku(isporukaId);
            isporuka.OslobodiPakete();
            _isporuke.Remove(isporuka);

            var redni = 1;
            foreach (var i in _isporuke.OrderBy(x => x.RedniBrojUTuri))
                i.PostaviRedniBroj(redni++);
        }

        public void Zakljucaj()
        {
            if (_isporuke.Count == 0)
                throw new DomainException("Tura bez isporuka ne može da se zaključa.");
            PromeniStatus(StatusTure.Zakljucana, StatusTure.Planirana);
        }

        // vozac menja redosled do kraja ture, posle polaska samo neisporucene, posebna logika (isporuceni paketi yadryavaju svoj prvobitni broj)
        public void PromeniRedosled(IReadOnlyList<int> noviRedosledIds)
        {
            ProveriStatus("promena redosleda", StatusTure.Planirana, StatusTure.Zakljucana, StatusTure.UToku);

            var pomerljive = _isporuke.Where(i => i.MozeDaSePomera).ToList();
            if (noviRedosledIds.Count != pomerljive.Count
                || noviRedosledIds.Distinct().Count() != pomerljive.Count
                || !noviRedosledIds.All(id => pomerljive.Any(i => i.Id == id)))
                throw new DomainException("Novi redosled mora da sadrži tačno sve neisporučene isporuke ture.");

            var slobodnaMesta = pomerljive.Select(i => i.RedniBrojUTuri).Order().ToList();
            for (var k = 0; k < noviRedosledIds.Count; k++)
                pomerljive.First(i => i.Id == noviRedosledIds[k]).PostaviRedniBroj(slobodnaMesta[k]);
        }

        // sef prebacuje turu na kolegu pre polaska
        public void PromeniVozaca(Vozac noviVozac)
        {
            ProveriStatus("promena vozača", StatusTure.Planirana, StatusTure.Zakljucana);
            if (noviVozac == Vozac || (noviVozac.Id != 0 && noviVozac.Id == VozacId))
                throw new DomainException("Tura je već dodeljena ovom vozaču.");
            if (!noviVozac.IsDostupan)
                throw new DomainException("Izabrani vozač nije dostupan.");

            Vozac = noviVozac;
            VozacId = noviVozac.Id;
        }

        // ---------- Izvrsenje ----------

        public void ZapocniTuru(Vozilo vozilo, DateTime sada)
        {
            if (_isporuke.Count == 0)
                throw new DomainException("Tura bez isporuka ne može da počne.");
            if (!vozilo.IsActive)
                throw new DomainException("Vozilo nije aktivno.");

            PromeniStatus(StatusTure.UToku, StatusTure.Planirana, StatusTure.Zakljucana);
            Vozilo = vozilo;
            VoziloId = vozilo.Id;
            StvarniPocetak = sada;
            foreach (var i in _isporuke)
                i.OznaciNaPutu();
        }

        // Idempotentno: ponovljen zahtev za vec isporucenu stanicu vraca false i ne menja nista
        public bool PotvrdiIsporuku(int isporukaId, string imePrimaoca, string? napomena, DateTime sada)
        {
            var isporuka = NadjiIsporuku(isporukaId);
            if (isporuka.Status == StatusIsporuke.Isporucena)
                return false;

            ProveriStatus("potvrda isporuke", StatusTure.UToku);
            isporuka.Potvrdi(imePrimaoca, napomena, sada);
            return true;
        }

        //isto idempotentno
        public bool OznaciNeuspesnuIsporuku(int isporukaId, RazlogNeuspehaIsporuke razlog, string? napomena, DateTime sada)
        {
            var isporuka = NadjiIsporuku(isporukaId);
            if (isporuka.Status == StatusIsporuke.Neuspesna)
                return false;

            ProveriStatus("evidentiranje neuspešne isporuke", StatusTure.UToku);
            isporuka.OznaciNeuspesnu(razlog, napomena, sada);
            return true;
        }

        public void ZavrsiTuru(DateTime sada)
        {
            if (_isporuke.Any(i => i.Status == StatusIsporuke.NaPutu))
                throw new DomainException("Tura ne može da se završi dok postoje nerešene isporuke.");
            PromeniStatus(StatusTure.Zavrsena, StatusTure.UToku);
            StvarniZavrsetak = sada;
        }

        public void Prekini(string razlog, DateTime sada)
        {
            PostaviRazlog(razlog);
            PromeniStatus(StatusTure.Prekinuta, StatusTure.UToku);
            StvarniZavrsetak = sada;
            foreach (var i in _isporuke.Where(x => x.Status == StatusIsporuke.NaPutu))
                i.Otkazi();
        }

        public void Otkazi(string razlog)
        {
            PostaviRazlog(razlog);
            PromeniStatus(StatusTure.Otkazana, StatusTure.Planirana, StatusTure.Zakljucana);
            foreach (var i in _isporuke)
                i.Otkazi();
        }

        // ---------- Pomocne ----------

        private Isporuka NadjiIsporuku(int isporukaId) =>
            _isporuke.FirstOrDefault(i => i.Id == isporukaId)
            ?? throw new DomainException("Isporuka ne postoji u ovoj turi.");

        private void PromeniStatus(StatusTure novi, params StatusTure[] dozvoljeniIz)
        {
            if (!dozvoljeniIz.Contains(Status))
                throw new InvalidStatusTransitionException(nameof(Tura), Status, novi);
            Status = novi;
        }

        private void ProveriStatus(string operacija, params StatusTure[] dozvoljeni)
        {
            if (!dozvoljeni.Contains(Status))
                throw new DomainException($"Tura: {operacija} nije dozvoljeno u statusu '{Status}'.");
        }

        private void PostaviRazlog(string razlog)
        {
            if (string.IsNullOrWhiteSpace(razlog))
                throw new DomainException("Razlog je obavezan.");
            Razlog = razlog.Trim();
        }
    }
}
