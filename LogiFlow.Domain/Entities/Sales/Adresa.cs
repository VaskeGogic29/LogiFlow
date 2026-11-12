using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Transport;
using LogiFlow.Domain.Exceptions;
using LogiFlow.Domain.ValueObjects;

namespace LogiFlow.Domain.Entities.Sales
{
    public class Adresa : BaseEntity, ISoftDelete
    {
        public int KlijentId { get; private set; }
        public Klijent Klijent { get; private set; } = null!;

        public string? Naziv { get; private set; }            // opcioni naziv npr. Magacin Zarkovo
        public string Ulica { get; private set; } = null!;
        public string Broj { get; private set; } = null!;
        public string Grad { get; private set; } = null!;
        public string PostanskiBroj { get; private set; } = null!;
        public string? Napomena { get; private set; }          // sprat, interfon, telefon itd

        public int? ZonaId { get; private set; }               // null dok Sef ne dodeli
        public Zona? Zona { get; private set; }

        public bool IsPodrazumevana { get; private set; }
        public bool IsActive { get; private set; }

        public bool ImaZonu => ZonaId.HasValue;

        private Adresa() { } // za EF Core

        internal Adresa(Klijent klijent, string? naziv, string ulica, string broj,
                        string grad, string postanskiBroj, string? napomena)
        {
            Klijent = klijent;
            KlijentId = klijent.Id;
            IsActive = true;
            Izmeni(naziv, ulica, broj, grad, postanskiBroj, napomena);
        }

        internal void Izmeni(string? naziv, string ulica, string broj, string grad,
                             string postanskiBroj, string? napomena)
        {
            if (string.IsNullOrWhiteSpace(ulica) || string.IsNullOrWhiteSpace(broj) ||
                string.IsNullOrWhiteSpace(grad) || string.IsNullOrWhiteSpace(postanskiBroj))
                throw new DomainException("Ulica, broj, grad i poštanski broj su obavezni.");

            var promenjenaLokacija =
                Ulica != ulica.Trim() || Broj != broj.Trim() ||
                Grad != grad.Trim() || PostanskiBroj != postanskiBroj.Trim();

            Naziv = naziv?.Trim();
            Ulica = ulica.Trim();
            Broj = broj.Trim();
            Grad = grad.Trim();
            PostanskiBroj = postanskiBroj.Trim();
            Napomena = napomena?.Trim();

            if (promenjenaLokacija)
            {
                ZonaId = null;   // nova lokacija: Šef mora ponovo da dodeli zonu (P-9)
                Zona = null;
            }
        }

        public void DodeliZonu(Zona zona)
        {
            if (!IsActive)
                throw new DomainException("Zona se ne može dodeliti neaktivnoj adresi.");
            if (!zona.IsActive)
                throw new DomainException($"Zona '{zona.Naziv}' nije aktivna.");

            Zona = zona;
            ZonaId = zona.Id;
        }

        public AdresaSnimak NapraviSnimak() =>
            new(Naziv, Ulica, Broj, Grad, PostanskiBroj, Napomena, ZonaId);

        internal void PostaviPodrazumevanu(bool podrazumevana) => IsPodrazumevana = podrazumevana;

        internal void Deaktiviraj()
        {
            IsActive = false;
            IsPodrazumevana = false;
        }
    }
}
