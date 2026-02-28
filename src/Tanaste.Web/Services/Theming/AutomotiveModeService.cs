namespace Tanaste.Web.Services.Theming;

/// <summary>
/// Per-circuit (scoped) service that tracks whether Automotive Mode is active
/// for this browser tab.
///
/// <para>
/// Automotive Mode is a high-contrast, large-touch-target display mode intended for
/// use at a distance — on a media-room TV, a wall-mounted tablet, or in a vehicle.
/// When active, the library grid filters to Audio-only content and all UI elements
/// are enlarged.
/// </para>
///
/// <para>
/// Scoped (one per Blazor Server circuit) so that a tablet in Automotive Mode does
/// not affect any other open browser session.
/// </para>
///
/// <para>
/// Components subscribe to <see cref="OnChanged"/> and call <c>StateHasChanged()</c>
/// in the handler.  They must unsubscribe in <c>Dispose()</c> to prevent memory leaks.
/// </para>
/// </summary>
public sealed class AutomotiveModeService
{
    /// <summary>Whether Automotive Mode is currently active for this circuit.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Fires whenever <see cref="IsActive"/> changes.
    /// Subscribe in <c>OnInitialized</c> and unsubscribe in <c>Dispose()</c>.
    /// </summary>
    public event Action? OnChanged;

    /// <summary>Toggles Automotive Mode on or off and notifies all subscribers.</summary>
    public void Toggle()
    {
        IsActive = !IsActive;
        OnChanged?.Invoke();
    }
}
