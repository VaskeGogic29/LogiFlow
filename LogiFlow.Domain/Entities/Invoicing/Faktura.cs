using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Sales;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;
using LogiFlow.Domain.ValueObjects;

namespace LogiFlow.Domain.Entities.Invoicing
{
    public class Faktura : BaseEntity
    {
        public const string PodrazumevanaValuta = "RSD";

        private readonly List<StavkaFakture> _stavke = [];

        public string BrojFakture { get; private set; } = null!;     // npr. IF26-000456

        public int NarudzbinaId { get; private set; }
        public Narudzbina Narudzbina { get; private set; } = null!;

        public int KlijentId { get; private set; }
        public Klijent Klijent { get; private set; } = null!;

        public SubjektSnimak Kupac { get; private set; } = null!;
        public SubjektSnimak Prodavac { get; private set; } = null!;

        public DateTime DatumIzdavanja { get; private set; }
        public DateOnly DatumPrometa { get; private set; }
        public DateOnly RokPlacanja { get; private set; }             // samo podatak na računu

        public StatusFakture Status { get; private set; }
        public DateTime? DatumStorniranja { get; private set; }
        public string? RazlogStorniranja { get; private set; }

        public decimal UkupnoBezPdv { get; private set; }
        public decimal UkupanPdv { get; private set; }
        public decimal UkupnoSaPdv { get; private set; }
        public string Valuta { get; private set; } = PodrazumevanaValuta;

        //PDF FAKTURA - JOS PODLOZNO PROMENI

        public string? PdfPutanja { get; private set; }               // interna putanja, nikad javni URL
        public string? PdfHash { get; private set; }                  // SHA-256, dokaz integriteta

        public IReadOnlyCollection<StavkaFakture> Stavke => _stavke;

        // Ne cuva se u bazi
        public string PoslednjeTriCifre => BrojFakture[^3..];

        private Faktura() { } // za EF Core

        public static Faktura Izdaj(string brojFakture, Narudzbina narudzbina, SubjektSnimak prodavac,
                                    DateTime sada, int rokPlacanjaDana)
        {
            if (string.IsNullOrWhiteSpace(brojFakture) || brojFakture.Trim().Length < 3)
                throw new DomainException("Broj fakture je obavezan.");
            if (narudzbina.Status != StatusNarudzbine.Potvrdjena)
                throw new DomainException("Faktura se izdaje samo za potvrđenu narudžbinu.");
            if (rokPlacanjaDana < 0)
                throw new DomainException("Rok plaćanja ne može biti negativan.");

            var klijent = narudzbina.Klijent;
            var adresa = narudzbina.SnimakAdrese;
            var danas = DateOnly.FromDateTime(sada);

            var faktura = new Faktura
            {
                BrojFakture = brojFakture.Trim(),
                Narudzbina = narudzbina,
                NarudzbinaId = narudzbina.Id,
                Klijent = klijent,
                KlijentId = klijent.Id,
                Kupac = new SubjektSnimak(klijent.Naziv, klijent.Pib, klijent.MaticniBroj,
                                          adresa.Ulica, adresa.Broj, adresa.Grad, adresa.PostanskiBroj),
                Prodavac = prodavac,
                DatumIzdavanja = sada,
                DatumPrometa = danas,
                RokPlacanja = danas.AddDays(rokPlacanjaDana),
                Status = StatusFakture.Izdata
            };

            foreach (var stavka in narudzbina.Stavke)
                faktura._stavke.Add(new StavkaFakture(faktura, stavka));

            faktura.UkupnoBezPdv = faktura._stavke.Sum(s => s.IznosBezPdv);
            faktura.UkupanPdv = faktura._stavke.Sum(s => s.IznosPdv);
            faktura.UkupnoSaPdv = faktura.UkupnoBezPdv + faktura.UkupanPdv;

            return faktura;
        }


        //VIDECEMO JOS
        public void PostaviPdf(string putanja, string hash)
        {
            if (string.IsNullOrWhiteSpace(putanja) || string.IsNullOrWhiteSpace(hash))
                throw new DomainException("Putanja i hash PDF-a su obavezni.");
            if (PdfHash is not null && PdfHash != hash)
                throw new DomainException("PDF izdate fakture se ne sme promeniti (hash se razlikuje).");

            PdfPutanja = putanja;
            PdfHash = hash;
        }

        public void Storniraj(string razlog, DateTime sada)
        {
            if (Status != StatusFakture.Izdata)
                throw new InvalidStatusTransitionException(nameof(Faktura), Status, StatusFakture.Stornirana);
            if (string.IsNullOrWhiteSpace(razlog))
                throw new DomainException("Razlog storniranja je obavezan.");

            Status = StatusFakture.Stornirana;
            RazlogStorniranja = razlog.Trim();
            DatumStorniranja = sada;
        }

    }
}
