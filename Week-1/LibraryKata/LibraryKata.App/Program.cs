using Library.Domain;

namespace LibraryKata.App;

public class Program
{
    public static void Main()
    {
        DataTypesAndOperators();
        ClassesExample();
        OopDemo();
    }

    private static void DataTypesAndOperators()
    {
        Console.WriteLine("=== Data types and operators ==");

        int copies = 3;
        double lateFee = 1;
        bool isMember = true;
        char shelf = 'A';
        string title = "Clean Code";
        string user = "Jon";
        int total = copies * 2;
        bool isEnough = total > 4;
        bool exactlySix = total == 6;
        bool lendable = isMember && isEnough;

        Console.WriteLine(title + " has been checked out by " + user);
        Console.WriteLine($"{title} on shelf {shelf}: {copies} copies, fee {lateFee}");

        total += 1;
    }

    private static void ControlFlow()
    {
        Console.WriteLine("\n== Control Flow ==");

        int copiesAvailable = 0;
        bool isMember = true;

        Console.WriteLine($"Member status: {isMember}");

        if (copiesAvailable > 1)
            Console.WriteLine("Many available for checkout!");
        else if (copiesAvailable == 1)
            Console.WriteLine("Last copy!");
        else
        {
            Console.WriteLine("Out of stock!");
            Console.WriteLine("Check again later!");
        }

        string genre = "Mystery";

        switch (genre)
        {
            case "Mystery":
                Console.WriteLine("Check section A!");
                break;
            case "Science-Fiction":
                Console.WriteLine("Check Section F!");
                break;
            default:
                Console.WriteLine("Uh oh");
                break;
        }

        string section = genre switch
        {
            "Mystery" => "Section A",
            "Science-Fiction" => "Section F",
            _ => "Uh oh"
        };
        Console.WriteLine(section);
    }

    private static void Loops()
    {
        for (int day = 1; day <= 3; day++)
        {
            Console.WriteLine($"Reminder day {day}: fee so far {CalculateLateFee(day)}");
        }

        int onShelf = 3;
        while (onShelf > 0)
        {
            Console.WriteLine($"{onShelf} copies on the shelf!");
            onShelf--;
        }

        Console.WriteLine("No copies on shelf!");
    }

    private static decimal CalculateLateFee(int daysLate) => daysLate * 2;

    private static void ArraysWork()
    {
        string[] books = { "Dune", "Harry Potter", "Percy Jackson", "Lord of the Rings" };

        Console.WriteLine(books[2]);

        foreach (string book in books)
        {
            Console.WriteLine(book);
        }
    }

    private static void ClassesExample()
    {
        Console.WriteLine("Using our domain Book class");

        Book dune = new Book("Dune", "Frank Herbert", 3);
        Book littlePrince = new Book("The Little Prince", "Antoine de Saint-Exupery", 0);

        Console.WriteLine(dune);
        Console.WriteLine(littlePrince.ToString());

        Console.WriteLine($"Checking out Dune: {dune.Checkout()}");
        Console.WriteLine($"Checking out The Little Prince: {littlePrince.Checkout()}");
    }

    public static void OopDemo()
    {
        Console.WriteLine("\n\n == OOP Demo stuff == ");

        LibraryItem[] catalog =
        {
            new Book("Dune", "Frank Herbert", 2),
            new ReferenceBook("C# Language Standards", "Microsoft", "Technology"),
            new Magazine("Sports Illustrated", "Francisco", 5, "Conde Naste")
        };

        foreach (LibraryItem item in catalog)
        {
            Console.WriteLine(item.Describe());
        }

        foreach (LibraryItem item in catalog)
        {
            if (item is ILendable lendable)
            {
                Console.WriteLine($"{item.Title}: checkout -> {lendable.Checkout()}");
            }
            else
            {
                Console.WriteLine($"{item.Title} is Reference only.");
            }
        }

        Magazine wired = new Magazine("Wired", "Luis", 3, "Conde Nast");
        LibraryItem baseMag = wired;

        Console.WriteLine("== Override vs new on the same object, different ref type");
        Console.WriteLine($"Magazine reference -> {wired.ShelfLabel()}");
        Console.WriteLine($"LibraryItem reference -> {baseMag.ShelfLabel()}");
    }
}
