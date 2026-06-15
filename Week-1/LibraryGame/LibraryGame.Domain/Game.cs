namespace LibraryGame.Domain;

public class Game : ILendable
{
    public static int Counter {get; private set;} = 1;
    public int Id {get; private set;}
    public string Name {get; set;}
    public decimal Price{get; set;}

    public bool Available{get; private set;}


    public Game(string name, decimal price, bool available)
    {
        Id = Counter++;
        Name = name;
        Price = price;
        Available = available;
    }

    // Protected: Game and its child classes can reuse this helper, but Program cannot call it.
    protected string GetAvailabilityText()
    {
        return Available ? "available" : "not available";
    }

    // Base version for polymorphism: child classes can override this behavior.
    public virtual void Describe()
    {
        string availabilityText = GetAvailabilityText();
        Console.WriteLine($"Game {Id}: {Name} costs {Price} dollars and is {availabilityText}.");
    }

    public bool ChangeStatus(int status)
    {
        if (status == 1)
        {
            Available = true;
            return true;
        }
        else if (status == 0)
        {
            Available = false;
            return true;
        }
        else
        {
            Console.WriteLine("Option invalid, no changes in Record");
            return false;
        }
    }

    // Overloading: same method name, different parameter list.
    public bool ChangeStatus()
    {
        Available = !Available;
        return true;
    }
}
