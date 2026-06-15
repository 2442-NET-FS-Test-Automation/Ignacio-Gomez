namespace LibraryGame.Domain;

public class PhysicalGame : Game
{
    public string Platform { get; set; }

    public PhysicalGame(string name, decimal price, bool available, string platform)
        : base(name, price, available)
    {
        Platform = platform;
    }

    // Polymorphism: this override replaces the base description for physical games.
    public override void Describe()
    {
        string availabilityText = GetAvailabilityText();
        Console.WriteLine($"Physical game {Id}: {Name} for {Platform} costs {Price} dollars and is {availabilityText}.");
    }

    public void ShowPlatform()
    {
        Console.WriteLine($"Physical game {Name} is for {Platform}.");
    }
}
