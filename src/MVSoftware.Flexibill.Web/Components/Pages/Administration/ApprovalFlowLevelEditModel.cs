namespace MVSoftware.Flexibill.Web.Components.Pages.Administration;

/// <summary>Lokaal bewerkmodel voor één niveau in het scherm "Fiateringsflow instellen" (nog geen sequence toegekend).</summary>
public sealed class ApprovalFlowLevelEditModel
{
    public Guid? ApproverId { get; set; }
    public decimal? MinimumAmount { get; set; }
}
