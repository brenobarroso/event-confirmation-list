namespace event_confirmation_list.Models;

public class Guest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
}
