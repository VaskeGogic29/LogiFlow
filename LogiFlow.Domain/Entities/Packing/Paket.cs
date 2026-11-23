using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Invoicing;
using LogiFlow.Domain.Entities.Transport;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;


namespace LogiFlow.Domain.Entities.Packing
{
    public class Paket : BaseEntity
    {
        private readonly List<StavkaPaketa> _stavke = [];

        public int FakturaId { get; private set; }
        public Faktura Faktura { get; private set; } = null!;

        public int RedniBroj { get; private set; }           // x u "x/N", po zavrsetku
        public int? UkupnoPaketa { get; private set; }       // N, null dok pakovanje traje
        public StatusPaketa Status { get; private set; }

        public decimal? TezinaKg { get; private set; }
        public int SpakovaoKorisnikId { get; private set; }
        public DateTime? DatumPotvrdeSadrzaja { get; private set; }
        public string? KodPaketa { get; private set; }       // npr. P-2026-000456-02, po zavrsetku

        public int? IsporukaId { get; private set; }         // trenutna (poslednja) isporuka
        public Isporuka? Isporuka { get; private set; }

        public byte[] RowVersion { get; private set; } = [];

        public IReadOnlyCollection<StavkaPaketa> Stavke => _stavke;

        private Paket() { } // za EF Core

        public Paket(Faktura faktura, int redniBroj, int spakovaoKorisnikId)
        {
            if (faktura.Status != StatusFakture.Izdata)
                throw new DomainException("Pakuje se samo izdata (nestornirana) faktura.");
            if (redniBroj <= 0)
                throw new DomainException("Redni broj paketa mora biti pozitivan.");

            Faktura = faktura;
            FakturaId = faktura.Id;
            RedniBroj = redniBroj;
            SpakovaoKorisnikId = spakovaoKorisnikId;
            Status = StatusPaketa.UPripremi;
        }

        // ---------- Punjenje kutije ----------

        // spakovanoUDrugimPaketima: koliko je od ove stavke vec u OSTALIM kutijama iste fakture (racuna servis).
        public void DodajStavku(StavkaFakture stavka, int kolicina, int spakovanoUDrugimPaketima)
        {
            ProveriStatus(StatusPaketa.UPripremi, "Stavke se dodaju samo u kutiju koja je u pripremi.");
            if (stavka.Faktura != Faktura && stavka.FakturaId != FakturaId)
                throw new DomainException("Stavka ne pripada fakturi ovog paketa.");
            if (kolicina <= 0)
                throw new DomainException("Količina mora biti veća od nule.");

            var postojeca = NadjiStavku(stavka);
            var uOvomPaketu = postojeca?.Kolicina ?? 0;

            if (spakovanoUDrugimPaketima + uOvomPaketu + kolicina > stavka.Kolicina)
                throw new DomainException(
                    $"'{stavka.NazivProizvoda}': preostalo za pakovanje je " +
                    $"{stavka.Kolicina - spakovanoUDrugimPaketima - uOvomPaketu}.");

            if (postojeca is not null)
                postojeca.PovecajKolicinu(kolicina);
            else
                _stavke.Add(new StavkaPaketa(this, stavka, kolicina));
        }

        public void UkloniStavku(StavkaFakture stavka)
        {
            ProveriStatus(StatusPaketa.UPripremi, "Stavke se uklanjaju samo iz kutije koja je u pripremi.");
            var postojeca = NadjiStavku(stavka)
                ?? throw new DomainException("Ta stavka nije u ovoj kutiji.");
            _stavke.Remove(postojeca);
        }

        public void PotvrdiSadrzaj(DateTime sada, decimal? tezinaKg = null)
        {
            ProveriStatus(StatusPaketa.UPripremi, "Sadržaj se potvrđuje samo za kutiju u pripremi.");
            if (_stavke.Count == 0)
                throw new DomainException("Ne može se potvrditi prazan paket.");
            if (tezinaKg is <= 0)
                throw new DomainException("Težina mora biti veća od nule.");

            TezinaKg = tezinaKg;
            DatumPotvrdeSadrzaja = sada;
            Status = StatusPaketa.SadrzajPotvrdjen;
        }

        public void Otkljucaj()   // ispravka pre zavrsetka pakovanja; servis upisuje audit
        {
            ProveriStatus(StatusPaketa.SadrzajPotvrdjen, "Otključava se samo kutija sa potvrđenim sadržajem.");
            DatumPotvrdeSadrzaja = null;
            Status = StatusPaketa.UPripremi;
        }

        public void Otkazi()
        {
            ProveriStatus(StatusPaketa.UPripremi, "Otkazuje se samo kutija koja je u pripremi.");
            _stavke.Clear();
            Status = StatusPaketa.Otkazan;
        }

        // ---------- Zavrsetak pakovanja cele fakture ----------
        //kao mali domenski servis smesten unutar entiteta, bitno da je static jer zavisi od parametara, jedino ona moze da pozove zakljucaj
        public static void ZavrsiPakovanje(Faktura faktura, IReadOnlyCollection<Paket> paketiFakture)
        {
            var aktivni = paketiFakture
                .Where(p => p.Status != StatusPaketa.Otkazan)
                .OrderBy(p => p.RedniBroj)
                .ToList();

            if (aktivni.Count == 0)
                throw new DomainException("Faktura nema nijedan paket.");
            if (aktivni.Any(p => p.FakturaId != faktura.Id))
                throw new DomainException("Svi paketi moraju pripadati istoj fakturi.");
            if (aktivni.Any(p => p.Status != StatusPaketa.SadrzajPotvrdjen))
                throw new DomainException("Sve kutije moraju imati potvrđen sadržaj.");

            foreach (var stavka in faktura.Stavke)
            {
                var spakovano = aktivni
                    .SelectMany(p => p.Stavke)
                    .Where(s => s.StavkaFaktureId == stavka.Id)
                    .Sum(s => s.Kolicina);

                if (spakovano != stavka.Kolicina)
                    throw new DomainException(
                        $"'{stavka.NazivProizvoda}': spakovano {spakovano} od {stavka.Kolicina}.");
            }

            var ukupno = aktivni.Count;
            for (var i = 0; i < ukupno; i++)
                aktivni[i].Zakljucaj(i + 1, ukupno, faktura.BrojFakture);
        }


        // ---------- Transport (pozivaju Tura/Isporuka, zato internal) ----------

        internal void DodeliIsporuci(Isporuka isporuka)
        {
            PromeniStatus(StatusPaketa.DodeljenTuri, StatusPaketa.SpremanZaTransport, StatusPaketa.VracenUMagacin);
            Isporuka = isporuka;
            IsporukaId = isporuka.Id;
        }

        internal void OslobodiIzIsporuke()
        {
            PromeniStatus(StatusPaketa.SpremanZaTransport, StatusPaketa.DodeljenTuri);
            Isporuka = null;
            IsporukaId = null;
        }

        internal void UtovariUVozilo() =>
            PromeniStatus(StatusPaketa.UVozilu, StatusPaketa.DodeljenTuri);

        internal void OznaciIsporucen() =>
            PromeniStatus(StatusPaketa.Isporucen, StatusPaketa.UVozilu);

        internal void OznaciNijeIsporucen() =>
            PromeniStatus(StatusPaketa.NijeIsporucen, StatusPaketa.UVozilu);

        public void VratiUMagacin() =>      // operativac primi neisporucen paket nazad, namerno public
            PromeniStatus(StatusPaketa.VracenUMagacin, StatusPaketa.NijeIsporucen);

        // ---------- Pomocne ----------

        private void Zakljucaj(int redniBroj, int ukupnoPaketa, string brojFakture)
        {
            RedniBroj = redniBroj;               // 1..N bez rupa (otkazani preskoceni)
            UkupnoPaketa = ukupnoPaketa;
            KodPaketa = $"P-{brojFakture}-{redniBroj:00}";
            Status = StatusPaketa.SpremanZaTransport;
        }

        private void ProveriStatus(StatusPaketa ocekivani, string poruka)
        {
            if (Status != ocekivani)
                throw new DomainException(poruka);
        }

        private StavkaPaketa? NadjiStavku(StavkaFakture stavka) =>
            _stavke.FirstOrDefault(s => s.StavkaFakture == stavka || (stavka.Id != 0 && s.StavkaFaktureId == stavka.Id));

        private void PromeniStatus(StatusPaketa novi, params StatusPaketa[] dozvoljeniIz)
        {
            if (!dozvoljeniIz.Contains(Status))
                throw new InvalidStatusTransitionException(nameof(Paket), Status, novi);
            Status = novi;
        }
    }
}
