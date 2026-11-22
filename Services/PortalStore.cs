using System.Net.Http.Json;
using System.Text.Json;
using CrewHop.Portal.Models;
using Microsoft.JSInterop;

namespace CrewHop.Portal.Services;

public sealed class PortalStore
{
    private const string StorageKey = "crewhop.portal.data";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private PortalData _data = new();
    private bool _loaded;

    public PortalStore(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public event Action? Changed;

    public BusinessProfile Business => _data.Business;
    public IReadOnlyList<Venue> Venues => _data.Venues;
    public IReadOnlyList<Promotion> Promotions => _data.Promotions;
    public IReadOnlyList<Campaign> Campaigns => _data.Campaigns;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        try
        {
            var raw = await _js.InvokeAsync<string?>("crewhopStorage.get", StorageKey);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var parsed = JsonSerializer.Deserialize<PortalData>(raw, JsonOptions);
                if (parsed is not null)
                {
                    _data = parsed;
                    var changed = EnsureSignaturePromos() | EnrichZeroStatPromotions();
                    if (changed)
                    {
                        await PersistAsync();
                    }

                    _loaded = true;
                    return;
                }
            }
        }
        catch
        {
            // Fall through to seed.
        }

        var seed = await _http.GetFromJsonAsync<PortalData>("data/seed.json", JsonOptions)
                   ?? throw new InvalidOperationException("Could not load portal data.");
        _data = seed;
        EnsureSignaturePromos();
        EnrichZeroStatPromotions();
        _loaded = true;
        await PersistAsync();
    }

    private bool EnsureSignaturePromos()
    {
        var potato = _data.Promotions.FirstOrDefault(p =>
            p.Title.Equals("Potato", StringComparison.OrdinalIgnoreCase));

        if (potato is null)
        {
            _data.Promotions.Insert(0, new Promotion
            {
                Id = "promo-potato",
                VenueId = "venue-oak-room",
                Title = "Potato",
                Description = "Pay twice, get nothing",
                Status = "Running",
                StartDate = new DateOnly(2026, 11, 21),
                EndDate = new DateOnly(2026, 12, 31),
                DiscountLabel = "-200% off",
                Views = 2840,
                Redemptions = 317,
                Spend = 88
            });
            return true;
        }

        if (potato.Views <= 0 && potato.Redemptions <= 0)
        {
            potato.Views = 2840;
            potato.Redemptions = 317;
            potato.Spend = 88;
            if (string.IsNullOrWhiteSpace(potato.Description))
            {
                potato.Description = "Pay twice, get nothing";
            }

            if (string.IsNullOrWhiteSpace(potato.DiscountLabel))
            {
                potato.DiscountLabel = "-200% off";
            }

            return true;
        }

        return false;
    }

    private bool EnrichZeroStatPromotions()
    {
        var changed = false;
        foreach (var promo in _data.Promotions.Where(p => p.Views <= 0 && p.Redemptions <= 0))
        {
            ApplyLaunchStats(promo);
            changed = true;
        }

        return changed;
    }

    private static void ApplyLaunchStats(Promotion promo)
    {
        var seed = Math.Abs(HashCode.Combine(promo.Id, promo.Title));
        promo.Views = 800 + seed % 4200;
        promo.Redemptions = Math.Max(12, promo.Views / (8 + seed % 7));
        promo.Spend = 35 + seed % 160;
    }

    public Venue? GetVenue(string id) => _data.Venues.FirstOrDefault(v => v.Id == id);

    public IEnumerable<Promotion> PromotionsForVenue(string venueId) =>
        _data.Promotions.Where(p => p.VenueId == venueId);

    public IEnumerable<Campaign> CampaignsForVenue(string venueId) =>
        _data.Campaigns.Where(c => c.VenueId == venueId);

    public async Task AddPromotionAsync(CreatePromotionRequest request)
    {
        var promo = new Promotion
        {
            Id = $"promo-{Guid.NewGuid():N}"[..13],
            VenueId = request.VenueId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DiscountLabel = request.DiscountLabel.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "Running",
            Views = 0,
            Redemptions = 0,
            Spend = 0
        };
        ApplyLaunchStats(promo);

        _data.Promotions.Insert(0, promo);
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task AddCampaignAsync(CreateCampaignRequest request)
    {
        var campaign = new Campaign
        {
            Id = $"camp-{Guid.NewGuid():N}"[..12],
            VenueId = request.VenueId,
            Name = request.Name.Trim(),
            Objective = request.Objective.Trim(),
            Audience = request.Audience.Trim(),
            Budget = request.Budget,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "Scheduled",
            Impressions = 0,
            Clicks = 0,
            Bookings = 0
        };

        _data.Campaigns.Insert(0, campaign);
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task SetPromotionStatusAsync(string id, string status)
    {
        var promo = _data.Promotions.FirstOrDefault(p => p.Id == id);
        if (promo is null)
        {
            return;
        }

        promo.Status = status;
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task RemovePromotionAsync(string id)
    {
        _data.Promotions.RemoveAll(p => p.Id == id);
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task SetCampaignStatusAsync(string id, string status)
    {
        var campaign = _data.Campaigns.FirstOrDefault(c => c.Id == id);
        if (campaign is null)
        {
            return;
        }

        campaign.Status = status;
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task RemoveCampaignAsync(string id)
    {
        _data.Campaigns.RemoveAll(c => c.Id == id);
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task SetVenueStatusAsync(string id, string status)
    {
        var venue = _data.Venues.FirstOrDefault(v => v.Id == id);
        if (venue is null)
        {
            return;
        }

        venue.Status = status;
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task RemoveVenueAsync(string id)
    {
        _data.Venues.RemoveAll(v => v.Id == id);
        _data.Promotions.RemoveAll(p => p.VenueId == id);
        _data.Campaigns.RemoveAll(c => c.VenueId == id);
        await PersistAsync();
        Changed?.Invoke();
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_data, JsonOptions);
        await _js.InvokeVoidAsync("crewhopStorage.set", StorageKey, json);
    }
}
