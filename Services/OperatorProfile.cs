using Microsoft.JSInterop;

namespace CrewHop.Portal.Services;

/// <summary>
/// UI preference for the operator name shown in the shell.
/// Demo portal is limited to a single profile.
/// </summary>
public sealed class OperatorProfile
{
    public const string DemoEmail = "demo@crewhop.com";
    public const string DemoDisplayName = "Demo User";
    public const string DemoPassword = "demo";

    private const string StorageKey = "crewhop.operator.profile";
    private readonly IJSRuntime _js;
    private bool _hydrated;

    public OperatorProfile(IJSRuntime js) => _js = js;

    public string Email { get; private set; } = "";
    public string DisplayName { get; private set; } = "";

    public bool HasProfile =>
        Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase)
        && DisplayName.Equals(DemoDisplayName, StringComparison.Ordinal);

    public async Task EnsureHydratedAsync()
    {
        if (_hydrated)
        {
            return;
        }

        try
        {
            var raw = await _js.InvokeAsync<string?>("crewhopStorage.get", StorageKey);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var parts = raw.Split('|');
                if (parts.Length >= 2)
                {
                    Email = parts[0];
                    DisplayName = parts[1];
                }
            }
        }
        catch
        {
            // Ignore until JS is ready.
        }

        // Migrate older demo names to the single profile.
        if (!string.IsNullOrWhiteSpace(Email)
            && Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase)
            && !DisplayName.Equals(DemoDisplayName, StringComparison.Ordinal))
        {
            DisplayName = DemoDisplayName;
            await _js.InvokeVoidAsync("crewhopStorage.set", StorageKey, $"{Email}|{DisplayName}");
        }

        if (!string.IsNullOrWhiteSpace(Email)
            && !Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase))
        {
            Email = "";
            DisplayName = "";
            await _js.InvokeVoidAsync("crewhopStorage.remove", StorageKey);
        }

        _hydrated = true;
    }

    public async Task SetAsync(string email, string displayName)
    {
        Email = email.Trim();
        DisplayName = displayName.Trim();
        await _js.InvokeVoidAsync("crewhopStorage.set", StorageKey, $"{Email}|{DisplayName}");
    }

    public async Task ClearAsync()
    {
        Email = "";
        DisplayName = "";
        await _js.InvokeVoidAsync("crewhopStorage.remove", StorageKey);
        await _js.InvokeVoidAsync("crewhopStorage.remove", "crewhop.auth");
    }
}
