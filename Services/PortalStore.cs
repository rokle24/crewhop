using System.Net.Http.Json;
using System.Text.Json;
using CrewHop.Portal.Models;
using Microsoft.JSInterop;

namespace CrewHop.Portal.Services;

public sealed class PortalStore
{
    private const string StorageKey = "crewhop.portal.seed";
    private const string SessionKey = "crewhop.portal.signedIn";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private SeedData? _data;
    private bool _signedIn;

    public PortalStore(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public bool IsSignedIn => _signedIn;
    public SeedData Data => _data ?? new SeedData();
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        try
        {
            var flag = await _js.InvokeAsync<string?>("crewhopStorage.get", SessionKey);
            _signedIn = string.Equals(flag, "1", StringComparison.Ordinal);
        }
        catch
        {
            _signedIn = false;
        }

        var cached = await TryReadCacheAsync();
        if (cached is not null)
        {
            _data = cached;
            Changed?.Invoke();
            return;
        }

        var seed = await _http.GetFromJsonAsync<SeedData>("data/seed.json", JsonOptions)
                   ?? new SeedData();
        _data = seed;
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task SignInAsync()
    {
        _signedIn = true;
        try
        {
            await _js.InvokeVoidAsync("crewhopStorage.set", SessionKey, "1");
        }
        catch
        {
            // Splash is UX-only; storage failures must not block navigation.
        }

        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        _signedIn = false;
        try
        {
            await _js.InvokeVoidAsync("crewhopStorage.remove", SessionKey);
        }
        catch
        {
        }

        Changed?.Invoke();
    }

    public async Task ResetDemoDataAsync()
    {
        var seed = await _http.GetFromJsonAsync<SeedData>("data/seed.json", JsonOptions)
                   ?? new SeedData();
        _data = seed;
        await PersistAsync();
        Changed?.Invoke();
    }

    private async Task PersistAsync()
    {
        if (_data is null)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            await _js.InvokeVoidAsync("crewhopStorage.set", StorageKey, json);
        }
        catch
        {
        }
    }

    private async Task<SeedData?> TryReadCacheAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("crewhopStorage.get", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<SeedData>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
