namespace PlayerDB.DataModel;

public class Player
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ClanName { get; set; }
    public string? Toon { get; set; }
    public int? MostRecentMmrT { get; set; }
    public int? MostRecentMmrP { get; set; }
    public int? MostRecentMmrZ { get; set; }
    public DateTime? MmrLastUpdatedUtcT {get; set;}
    public DateTime? MmrLastUpdatedUtcP { get; set; }
    public DateTime? MmrLastUpdatedUtcZ { get; set; }
    public List<BuildOrder>? BuildOrders { get; set; }
}