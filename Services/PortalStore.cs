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
                    var changed = EnsureDemoBusiness()
                                  | EnsureVenueImages()
                                  | EnsureSignaturePromos()
                                  | EnrichZeroStatPromotions();
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
        EnsureDemoBusiness();
        EnsureVenueImages();
        EnsureSignaturePromos();
        EnrichZeroStatPromotions();
        _loaded = true;
        await PersistAsync();
    }

    private bool EnsureDemoBusiness()
    {
        var changed = false;
        if (_data.Business.ContactEmail != "demo@crewhop.com")
        {
            _data.Business.ContactEmail = "demo@crewhop.com";
            changed = true;
        }

        if (_data.Business.OwnerName != "Demo User")
        {
            _data.Business.OwnerName = "Demo User";
            changed = true;
        }

        return changed;
    }

    private static readonly Dictionary<string, string> DefaultVenueImages = new()
    {
        ["venue-oak-room"] = "images/venues/cafe.svg",
        ["venue-blue-harbor"] = "images/venues/barbershop.svg",
        ["venue-nightingale"] = "images/venues/bar.svg",
        ["venue-riverside"] = "images/venues/terrace.svg"
    };

    private bool EnsureVenueImages()
    {
        var changed = false;
        var blue = _data.Venues.FirstOrDefault(v => v.Id == "venue-blue-harbor");
        if (blue is not null
            && (blue.Name.Contains("Café", StringComparison.OrdinalIgnoreCase)
                || blue.Category.Equals("Café", StringComparison.OrdinalIgnoreCase)))
        {
            blue.Name = "Blue Harbor Barber";
            blue.Category = "Barbershop";
            if (!blue.ImageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                blue.ImageUrl = "images/venues/barbershop.svg";
            }

            changed = true;
        }

        foreach (var venue in _data.Venues)
        {
            if (!string.IsNullOrWhiteSpace(venue.ImageUrl))
            {
                continue;
            }

            venue.ImageUrl = DefaultVenueImages.TryGetValue(venue.Id, out var path)
                ? path
                : "images/venues/cafe.svg";
            changed = true;
        }

        return changed;
    }

    private bool EnsureSignaturePromos()
    {
        var changed = false;
        var potato = _data.Promotions.FirstOrDefault(p =>
            p.Title.Equals("Potato", StringComparison.OrdinalIgnoreCase)
            || p.Id == "promo-potato");

        if (potato is not null)
        {
            potato.Id = "promo-student-night";
            potato.VenueId = "venue-nightingale";
            potato.Title = "Student Night";
            potato.Description = "Show a student ID after 8pm for a free soft drink with any main.";
            potato.DiscountLabel = "Free drink";
            potato.Status = "Running";
            if (potato.Views <= 0)
            {
                potato.Views = 2840;
                potato.Redemptions = 317;
                potato.Spend = 88;
            }

            changed = true;
        }
        else if (!_data.Promotions.Any(p => p.Id == "promo-student-night"
                                           || p.Title.Equals("Student Night", StringComparison.OrdinalIgnoreCase)))
        {
            _data.Promotions.Insert(0, new Promotion
            {
                Id = "promo-student-night",
                VenueId = "venue-nightingale",
                Title = "Student Night",
                Description = "Show a student ID after 8pm for a free soft drink with any main.",
                Status = "Running",
                StartDate = new DateOnly(2026, 11, 21),
                EndDate = new DateOnly(2026, 12, 31),
                DiscountLabel = "Free drink",
                Views = 2840,
                Redemptions = 317,
                Spend = 88
            });
            changed = true;
        }

        return changed;
    }

    public async Task UpdateVenueImageAsync(string id, string imageUrl)
    {
        var venue = _data.Venues.FirstOrDefault(v => v.Id == id);
        if (venue is null || string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        venue.ImageUrl = imageUrl;
        await PersistAsync();
        Changed?.Invoke();
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
