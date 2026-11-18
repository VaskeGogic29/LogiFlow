using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Invoicing
{
    public class PristupniTokenFakture : BaseEntity
    {
        public int FakturaId { get; private set; }
        public Faktura Faktura { get; private set; } = null!;

        public string TokenHash { get; private set; } = null!;   // u bazi samo hash
        public NamenaTokena Namena { get; private set; }
        public DateTime IsticeU { get; private set; }
        public DateTime? OpozvanU { get; private set; }
        public int BrojKoriscenja { get; private set; }
        public DateTime? PoslednjeKoriscenjeU { get; private set; }

        private PristupniTokenFakture() { } // za EF Core

        internal PristupniTokenFakture(Faktura faktura, string tokenHash, NamenaTokena namena,
                                       DateTime isticeU, DateTime sada)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new DomainException("Hash tokena je obavezan.");
            if (isticeU <= sada)
                throw new DomainException("Rok tokena mora biti u budućnosti.");

            Faktura = faktura;
            FakturaId = faktura.Id;
            TokenHash = tokenHash;
            Namena = namena;
            IsticeU = isticeU;
        }

        public bool JeVazeci(DateTime sada) => OpozvanU is null && IsticeU > sada;

        // Poziva QR endpoint posle provere prijave i aktivnog naloga
        public void EvidentirajKoriscenje(DateTime sada)
        {
            if (!JeVazeci(sada))
                throw new DomainException("Token nije važeći.");
            BrojKoriscenja++;
            PoslednjeKoriscenjeU = sada;
        }

        internal void Opozovi(DateTime sada)
        {
            if (OpozvanU is null) OpozvanU = sada;
        }
    }
}
