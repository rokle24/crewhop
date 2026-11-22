namespace CrewHop.Portal.Models;

public sealed class PortalData
{
    public BusinessProfile Business { get; set; } = new();
    public List<Venue> Venues { get; set; } = [];
    public List<Promotion> Promotions { get; set; } = [];
    public List<Campaign> Campaigns { get; set; } = [];
}

public sealed class BusinessProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string OwnerName { get; set; } = "";
}

public sealed class Venue
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Status { get; set; } = "Active";
    public int WeeklyVisitors { get; set; }
    public double Rating { get; set; }
    public string ImageTone { get; set; } = "forest";
}

public sealed class Promotion
{
    public string Id { get; set; } = "";
    public string VenueId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string DiscountLabel { get; set; } = "";
    public int Views { get; set; }
    public int Redemptions { get; set; }
    public double Spend { get; set; }
}

public sealed class Campaign
{
    public string Id { get; set; } = "";
    public string VenueId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Objective { get; set; } = "";
    public string Audience { get; set; } = "";
    public decimal Budget { get; set; }
    public string Status { get; set; } = "Draft";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Bookings { get; set; }
}

public sealed class CreatePromotionRequest
{
    public string VenueId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DiscountLabel { get; set; } = "";
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
}

public sealed class CreateCampaignRequest
{
    public string VenueId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Objective { get; set; } = "Increase foot traffic";
    public string Audience { get; set; } = "Locals nearby";
    public decimal Budget { get; set; } = 250;
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
}
