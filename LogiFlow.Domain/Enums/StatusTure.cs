namespace LogiFlow.Domain.Enums
{
    public enum StatusTure
    {
        Planirana = 1,   // dodaju se isporuke
        Zakljucana = 2,  // proslo vreme polaska
        UToku = 3,       // vozac krenuo
        Zavrsena = 4,
        Prekinuta = 5,   // krenula, pa prekinuta 
        Otkazana = 6     // otkazana pre polaska
    }
}
