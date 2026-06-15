namespace LibraryGame.Domain;

public class DigitalGame : Game
{
    public int Size {get; set;}
    public DigitalGame(string name, decimal price, bool available,int size) : base(name, price, available)
    {
        Size = size;
    }

    // Polymorphism: digital games answer Describe() with their own version.
    public override void Describe()
    {
        string availabilityText = GetAvailabilityText();
        Console.WriteLine($"Digital game {Id}: {Name} of size {Size} costs {Price} dollars and is {availabilityText}.");
    }

}
