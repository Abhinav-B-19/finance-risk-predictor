public class User
{
    public int Id { get; set; }

    public string UserKey { get; set; } = Guid.NewGuid().ToString();

    public required string Name { get; set; }
    public required string Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Prediction> Predictions { get; set; } = new();
}