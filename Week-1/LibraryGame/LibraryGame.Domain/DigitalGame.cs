namespace LibraryGame.Domain;

public class DigitalGame : Game
{
    public int Size {get; set;}
    public DigitalGame(string name, decimal price, bool available,int size) : base(name, price, available)
    {
        Size = size;
    }

    // Polymorphism: this override replaces the base description for physical games.
    public override void Describe()
    {
        string availabilityText = GetAvailabilityText();
        Console.WriteLine($"Digital game {Id}: {Name} of size {Size} GB costs {Price} dollars and is {availabilityText}.");
    }

}