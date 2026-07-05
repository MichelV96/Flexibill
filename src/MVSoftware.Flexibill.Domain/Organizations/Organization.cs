using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Organizations;

/// <summary>
/// Aggregate root voor een administratie (Functioneel Ontwerp, hoofdstuk 3.1). Bewaakt
/// welke modules actief zijn (hoofdstuk 11) en de abonnementsvorm (hoofdstuk 12).
/// CRM Leveranciers en Factuurverwerking horen bij de basis en zitten hier expres niet
/// in <see cref="ActiveModules"/> - die zijn nooit uit te zetten.
/// </summary>
public sealed class Organization : Entity, IAuditable
{
    public string Name { get; private set; } = string.Empty;
    public ChamberOfCommerceNumber? ChamberOfCommerceNumber { get; private set; }
    public string? LogoUrl { get; private set; }
    public SubscriptionPlan SubscriptionPlan { get; private set; } = SubscriptionPlan.UsageBased;
    public FlexibillModule ActiveModules { get; private set; } = FlexibillModule.None;

    private Organization() { }

    public static Organization Create(string name, ChamberOfCommerceNumber? chamberOfCommerceNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An organization requires a name.", nameof(name));
        }

        return new Organization
        {
            Name = name,
            ChamberOfCommerceNumber = chamberOfCommerceNumber
        };
    }

    public bool IsModuleActive(FlexibillModule module) => ActiveModules.HasFlag(module);

    /// <summary>UC-B6: Beheerder schakelt een module aan.</summary>
    public void ActivateModule(FlexibillModule module) => ActiveModules |= module;

    /// <summary>UC-B6: Beheerder schakelt een module uit.</summary>
    public void DeactivateModule(FlexibillModule module) => ActiveModules &= ~module;

    public void ChangeSubscriptionPlan(SubscriptionPlan plan) => SubscriptionPlan = plan;

    public void UpdateBranding(string? logoUrl) => LogoUrl = logoUrl;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An organization requires a name.", nameof(name));
        }

        Name = name;
    }
}
