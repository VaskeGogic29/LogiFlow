using LogiFlow.Domain.Entities.Common;

namespace LogiFlow.Domain.Entities.Identity
{
    public class KorisnikUloga : BaseEntity
    {
        public int KorisnikId { get; private set; }
        public Korisnik Korisnik { get; private set; } = null!;

        public int UlogaId { get; private set; }
        public Uloga Uloga { get; private set; } = null!;

        private KorisnikUloga() { } // za EFCore read

        internal KorisnikUloga(Korisnik korisnik, Uloga uloga)
        {
            Korisnik = korisnik;
            KorisnikId = korisnik.Id;
            Uloga = uloga;
            UlogaId = uloga.Id;
        }
    }
}
