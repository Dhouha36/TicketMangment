public class TicketFilterRequest
{
    public int? UserId      { get; set; }
    public int? OwnerId { get; set; }
    public int? PersonnelId { get; set; }
    public int? ProjetId { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End    { get; set; }
    public string  Granularity { get; set; } = "monthly";
}
