namespace PlayerDB.DataModel;

public class Player
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ClanName { get; set; }
    public string? Toon { get; set; }
    public List<BuildOrder>? BuildOrders { get; set; }
}