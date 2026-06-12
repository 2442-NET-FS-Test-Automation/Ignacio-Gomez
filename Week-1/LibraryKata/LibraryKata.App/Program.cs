using Library.Domain;

namespace LibraryKata.App;
public class Program
{
    // public - accesible across the program
    // static - Main can be called upon without a Program object. 
    // void it doesnt return anything
    public static void Main()
    {
        DataTypesAndOperators();
        ClassesExample();
    } 
    //private - accessible only within this class
    // static - it belongs to the class, not objects
    // void - return nothing
    private static void DataTypesAndOperators()
    {
        Console.WriteLine("==Data types and operators==");
        
        int copies = 3;
        double lateFee = 1;
        bool isMember = true;
        char shelf = 'A';
        string title = "Clean code";


    }

    private static void ControlFlow(){
        Console.WriteLine("\n== Control Flow==");
        int copiesAvailable = 1;
        bool isMember = true;

        if (copiesAvailable > 1)
            Console.WriteLine("Many available for checkout");
        else if (copiesAvailable == 1)
            Console.WriteLine("Last copy");
        else
        {
            Console.WriteLine("Out of stock");
        }

        // //Switch
        // string genre = "Mystery";

        // //Classic switch 
        // switch (genre)
        // {
        //     case "Mystery":
        //         Console.WriteLine("Check section A");
        //         break;
        //     case "Science-Fiction":
        //         Console.WriteLine("Check section B");
        //         break;
        //     default: // While optional a default case to catch any edge cases
        //         Console.WriteLine("Uh oh");
        //         break;
        // }

        // //New in .NET 8, Switch Expressions
        // // they are used out real world code
        // string section = genre switch
        // {
        //     //This is my expression body
        //     "Mystery" => "section A",
        //     "Science-Fiction" => "Section F",
        //     _ => "uH Oh"
        // };
        // Console.WriteLine(section);

    }
    private static void Loops()
    {
        for (int day = 1; day <= 3; day++)
        {
            Console.WriteLine($"Remainder day {day}: fee so far");
        }

        int onShelf = 3;
        while(onShelf > 0)
        {
            Console.WriteLine($"{onShelf} copies on the shelf");
            onShelf--;
        }
        Console.WriteLine("No copues on shelf");
    }

    private static decimal CalculateLateFee(int daysLate) => daysLate * 2;

    private static void ArrayWork()
    {
        string[] books = {"Dune", "Harry Potter", "Percy Jackson", "Lord of the rings"};
        Console.WriteLine(books[2]);
        foreach(string book in books)
        {
            Console.WriteLine(book);
        }
    }

    private static void ClassesExample()
    {
        Console.WriteLine("Using our domain Book class");

        // My first book, constructor
        Book dune = new Book("Dune", "Frank", 3);
        Book littlePrince = new Book("The Littel prince", "Antoine", 0);
        
        //Print book info
        Console.WriteLine(dune);
        Console.WriteLine(littlePrince.ToString());

        Console.WriteLine($"Check out Dune: {dune.Checkout()}");
        Console.WriteLine($"Checking out the little prince: {littlePrince.Checkout()}");
    }

}
