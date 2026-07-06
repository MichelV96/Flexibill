using MudBlazor;

namespace MVSoftware.Flexibill.Web.Theme;

/// <summary>
/// Het Flexibill-kleurenschema: teal als merkkleur (app-bar, primaire knoppen), houtskoolgrijs
/// als secundaire kleur voor gedempte tekst (`Color.Secondary` wordt door meerdere schermen
/// gebruikt voor toelichtende tekst, bijv. Login/Users/ApprovalFlow/Dashboard - de MudBlazor-
/// standaard Secondary-kleur is roze/rood en oogt daar als een foutmelding).
///
/// Typografie: Poppins voor koppen (H1-H6, sluit aan bij het geometrische "F"-wordmark in het
/// logo), Inter voor de rest (via `Default` - alle overige varianten zoals Body/Button/Caption
/// hebben zelf geen `FontFamily` gezet en vallen terug op `Default`). Beide fonts worden geladen
/// in `App.razor` naast het bestaande Roboto (dat als laatste fallback blijft staan).
/// </summary>
public static class FlexibillTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0E7C7B",
            PrimaryDarken = "#095554",
            PrimaryLighten = "#3A9998",
            Secondary = "#4B5563",
            SecondaryDarken = "#374151",
            SecondaryLighten = "#6B7280",
            AppbarBackground = "#0E7C7B",
            AppbarText = "#FFFFFF",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"] },
            H1 = new H1Typography { FontFamily = ["Poppins", "sans-serif"] },
            H2 = new H2Typography { FontFamily = ["Poppins", "sans-serif"] },
            H3 = new H3Typography { FontFamily = ["Poppins", "sans-serif"] },
            H4 = new H4Typography { FontFamily = ["Poppins", "sans-serif"] },
            H5 = new H5Typography { FontFamily = ["Poppins", "sans-serif"] },
            H6 = new H6Typography { FontFamily = ["Poppins", "sans-serif"] },
        },
    };
}
