using Microsoft.JSInterop;

namespace CrewHop.Portal.Services;

/// <summary>
/// UI preference for the operator name shown in the shell.
/// Not an access-control mechanism — Cursor private/team hosting gates the app.
/// </summary>
public sealed class OperatorProfile
{
    private const string StorageKey = "crewhop.operator.profile";
    private readonly IJSRuntime _js;
    private bool _hydrated;

    public OperatorProfile(IJSRuntime js) => _js = js;

    public string Email { get; private set; } = "";
    public string DisplayName { get; private set; } = "";

    public bool HasProfile => !string.IsNullOrWhiteSpace(DisplayName);

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
