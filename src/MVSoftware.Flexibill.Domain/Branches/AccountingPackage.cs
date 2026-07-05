namespace MVSoftware.Flexibill.Domain.Branches;

/// <summary>
/// De zeven losse boekhoudkoppelingen uit het Functioneel Ontwerp, hoofdstuk 6.6 -
/// e-Boekhouden, Snelstart en Yuki zijn ieder een eigen pakket/connector (Technisch
/// Ontwerp, hoofdstuk 13.1), ook al stonden ze in het FO als één regel.
/// </summary>
public enum AccountingPackage
{
    ExactOnline,
    Afas,
    VismaNet,
    EBoekhouden,
    Snelstart,
    Yuki,
    EAccounting
}
