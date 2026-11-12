using LogiFlow.Domain.Entities.Common;

namespace LogiFlow.Domain.Entities.Identity
{
    public class UlogaOvlascenje : BaseEntity
    {
        public int UlogaId { get; private set; }
        public Uloga Uloga { get; private set; } = null!;

        public int OvlascenjeId { get; private set; }
        public Ovlascenje Ovlascenje { get; private set; } = null!;

        private UlogaOvlascenje() { } // za EF Core

        public UlogaOvlascenje(int ulogaId, int ovlascenjeId)
        {
            UlogaId = ulogaId;
            OvlascenjeId = ovlascenjeId;
        }
    }
}
