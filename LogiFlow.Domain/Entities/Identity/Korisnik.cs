using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Identity
{
    public class Korisnik : BaseEntity, ISoftDelete
    {
        private readonly List<KorisnikUloga> _uloge = [];
        private readonly List<KorisnikOvlascenje> _dodatnaOvlascenja = [];

        public string Email { get; private set; } = null!;
        public string? KorisnickoIme { get; private set; }
        public string PasswordHash { get; private set; } = null!;
        public string Ime { get; private set; } = null!;
        public string Prezime { get; private set; } = null!;
        public string? Telefon { get; private set; }
        public bool IsActive { get; private set; }

        public DateTime? DatumPoslednjePrijave { get; private set; }
        public int BrojNeuspesnihPrijava { get; private set; }
        public DateTime? ZakljucanDo { get; private set; }
        public string SecurityStamp { get; private set; } = null!;

        public IReadOnlyCollection<KorisnikUloga> Uloge => _uloge;
        public IReadOnlyCollection<KorisnikOvlascenje> DodatnaOvlascenja => _dodatnaOvlascenja;

        private Korisnik() { } // za EF Core

        public Korisnik(string email, string passwordHash, string ime, string prezime,
                        string? telefon = null, string? korisnickoIme = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Email je obavezan.");
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("Lozinka je obavezna.");

            Email = email.Trim().ToLowerInvariant();
            PasswordHash = passwordHash;
            KorisnickoIme = korisnickoIme?.Trim();
            IsActive = true;
            AzurirajProfil(ime, prezime, telefon);
            NoviSecurityStamp();
        }

        public void AzurirajProfil(string ime, string prezime, string? telefon)
        {
            if (string.IsNullOrWhiteSpace(ime) || string.IsNullOrWhiteSpace(prezime))
                throw new DomainException("Ime i prezime su obavezni.");

            Ime = ime.Trim();
            Prezime = prezime.Trim();
            Telefon = telefon?.Trim();
        }

        public void PromeniLozinku(string noviPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(noviPasswordHash))
                throw new DomainException("Lozinka je obavezna.");

            PasswordHash = noviPasswordHash;
            NoviSecurityStamp(); // stari JWT/refresh tokeni postaju nevažeći
        }

        public void Deaktiviraj()
        {
            if (!IsActive) return;
            IsActive = false;
            NoviSecurityStamp();
        }

        public void Aktiviraj()
        {
            if (IsActive) return;
            IsActive = true;
            BrojNeuspesnihPrijava = 0;
            ZakljucanDo = null;
        }

        public bool JeZakljucan(DateTime sada) => ZakljucanDo.HasValue && ZakljucanDo.Value > sada;

        public void EvidentirajUspesnuPrijavu(DateTime sada)
        {
            DatumPoslednjePrijave = sada;
            BrojNeuspesnihPrijava = 0;
            ZakljucanDo = null;
        }

        public void EvidentirajNeuspesnuPrijavu(DateTime sada, int maksimalnoPokusaja, TimeSpan trajanjeZakljucavanja)
        {
            BrojNeuspesnihPrijava++;
            if (BrojNeuspesnihPrijava >= maksimalnoPokusaja)
            {
                ZakljucanDo = sada.Add(trajanjeZakljucavanja);
                BrojNeuspesnihPrijava = 0;
            }
        }

        public void DodeliUlogu(Uloga uloga)
        {
            if (ImaUlogu(uloga))
                throw new DomainException($"Korisnik već ima ulogu '{uloga.Naziv}'.");

            _uloge.Add(new KorisnikUloga(this, uloga));
            NoviSecurityStamp(); // promena uloga mora poništiti stare tokene
        }

        public void UkloniUlogu(Uloga uloga)
        {
            var veza = _uloge.FirstOrDefault(ku => JeIstaUloga(ku, uloga))
                ?? throw new DomainException($"Korisnik nema ulogu '{uloga.Naziv}'.");

            _uloge.Remove(veza);
            NoviSecurityStamp();
        }

        public void DodeliOvlascenje(Ovlascenje ovlascenje, DateTime? vaziDo, DateTime sada)
        {
            if (_dodatnaOvlascenja.Any(ko => JeIstoOvlascenje(ko, ovlascenje) && ko.JeVazece(sada)))
                throw new DomainException($"Korisnik već ima važeće ovlašćenje '{ovlascenje.Kod}'.");

            _dodatnaOvlascenja.Add(new KorisnikOvlascenje(this, ovlascenje, vaziDo, sada));
            NoviSecurityStamp(); // novo pravo ulazi u token tek posle ponovnog izdavanja
        }

        public void OpozoviOvlascenje(Ovlascenje ovlascenje, DateTime sada)
        {
            var vazece = _dodatnaOvlascenja.FirstOrDefault(ko => JeIstoOvlascenje(ko, ovlascenje) && ko.JeVazece(sada))
                ?? throw new DomainException($"Korisnik nema važeće ovlašćenje '{ovlascenje.Kod}'.");

            vazece.Opozovi(sada);
            NoviSecurityStamp(); // stari JWT sa ovim pravom odmah prestaje da važi
        }

        // Radi samo ako su uloge učitane iz baze (Include).
        public bool ImaUlogu(string nazivUloge) => _uloge.Any(ku => ku.Uloga.Naziv == nazivUloge);

        private bool ImaUlogu(Uloga uloga) => _uloge.Any(ku => JeIstaUloga(ku, uloga));

        private static bool JeIstaUloga(KorisnikUloga ku, Uloga uloga) =>
            ku.Uloga == uloga || (uloga.Id != 0 && ku.UlogaId == uloga.Id);

        private static bool JeIstoOvlascenje(KorisnikOvlascenje ko, Ovlascenje ovlascenje) =>
            ko.Ovlascenje == ovlascenje || (ovlascenje.Id != 0 && ko.OvlascenjeId == ovlascenje.Id);


        private void NoviSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");
    }
}
