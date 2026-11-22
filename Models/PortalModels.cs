namespace CrewHop.Portal.Models;

public sealed class SeedData
{
    public List<CrewMember> Crew { get; set; } = new();
    public List<Shift> Shifts { get; set; } = new();
    public PotatoPromo PotatoPromo { get; set; } = new();
    public DashboardStats Dashboard { get; set; } = new();
}

public sealed class CrewMember
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Location { get; set; } = "";
    public string Status { get; set; } = "Active";
}

public sealed class Shift
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public string StartsAt { get; set; } = "";
    public string EndsAt { get; set; } = "";
    public int OpenSlots { get; set; }
    public int FilledSlots { get; set; }
}

public sealed class PotatoPromo
{
    public string Title { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Body { get; set; } = "";
    public string Cta { get; set; } = "";
    public PotatoStats Stats { get; set; } = new();
}

public sealed class PotatoStats
{
    public int VenuesLive { get; set; }
    public int ShiftsFilled { get; set; }
    public int AvgFillMinutes { get; set; }
    public double RetentionPercent { get; set; }
}

public sealed class DashboardStats
{
    public int ActiveCrew { get; set; }
    public int OpenShifts { get; set; }
    public int FilledToday { get; set; }
    public double FillRate { get; set; }
}
