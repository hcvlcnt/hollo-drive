namespace backend.Services;

public class HolloServerOptions
{
    public const string SectionName = "HolloServer";

    public string Id { get; set; } = "hollo-local";
    public string Name { get; set; } = "Hollo Casa";
    public string PublicUrl { get; set; } = "http://localhost:8080/api/";
}
