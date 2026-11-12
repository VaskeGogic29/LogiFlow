using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Identity
{
    public class RefreshToken : BaseEntity
    {
        public int KorisnikId { get; private set; }
        public Korisnik Korisnik { get; private set; } = null!;

        public string TokenHash { get; private set; } = null!;
        public DateTime IsticeU { get; private set; }
        public DateTime? OpozvanU { get; private set; }
        public string? ZamenjenTokenHash { get; private set; }   // ovde upisujemo novi token kojim je aktuelni zamenjen

        private RefreshToken() { } // za EF Core

        public RefreshToken(int korisnikId, string tokenHash, DateTime isticeU, DateTime sada)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new DomainException("Hash tokena je obavezan.");
            if (isticeU <= sada)
                throw new DomainException("Rok tokena mora biti u budućnosti.");

            KorisnikId = korisnikId;
            TokenHash = tokenHash;
            IsticeU = isticeU;
        }

        public bool JeAktivan(DateTime sada) => OpozvanU is null && IsticeU > sada;

        public void Zameni(string noviTokenHash, DateTime sada)
        {
            if (!JeAktivan(sada))
                throw new DomainException("Token nije aktivan i ne može se zameniti.");

            OpozvanU = sada;
            ZamenjenTokenHash = noviTokenHash;
        }

        public void Opozovi(DateTime sada)
        {
            if (OpozvanU is null) OpozvanU = sada;
        }
    }
}
