namespace LogiFlow.Domain.ValueObjects
{
    // Snimak kupca ili prodavca u trenutku izdavanja fakture.
    public sealed record SubjektSnimak(
        string Naziv,
        string? Pib,
        string? MaticniBroj,
        string Ulica,
        string Broj,
        string Grad,
        string PostanskiBroj);
}
