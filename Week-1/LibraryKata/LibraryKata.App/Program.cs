// If I have code from another namespace I want to use here - I use a using statement
using System.Security.Cryptography.X509Certificates;
using LibraryKata.Domain;
//using LibraryKata.Domain.Exceptions;
using LibraryKata.Domain.Repo;
using Serilog;

namespace LibraryKata.App; // A namespace is like a bucket or logical container for different
// related code files.
public class Program
{
    
    // Now we are moving away from the Python file style Top-Level statements
    // So we need a class to hold our Main() method. The previous style with no class
    // or main - implicity had a Main() under the hood. 

    // public - accessible across the program
    // static - Main can be called upon without a Program object. It is a Static/class method. 
    // void - it doesn't return anything
    public static async Task Main()
    {   

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // When I call dotnet run, it finds Main() and begins code execution at the first line of the 
        // main method. I wrote my code, inside DataTypesAndOperators() - a separate method. So if I want 
        // that code to run, I need to call it inside Main()
        Program.DataTypesAndOperators();
        // Wired in: these were defined but never called from Main, so the control-flow,
        // loops, and arrays demos never actually ran. Call them in source order.
        ControlFlow();
        Loops();
        ArraysWork();
        ClassesExample();
        OopDemo();
        CollectionsDemo();
        ExceptionDemo();
        AdvancedClassesDemo();
        await AsyncHttpDemo();

        Log.CloseAndFlush(); 
    }

    // private - accessible only within this class
    // static - it belongs to the class, not objects of the class
    // void - returns nothing
    private static void DataTypesAndOperators() // If I had arguments, or inputs for this method,
    { // they would go inside the parenthesis after the method name 
        Console.WriteLine("=== Data types and operators ==");

        // C# is a Strongly typed language
        // We cannot just create variables and shove whatever we want into them like JS or Python
        int copies = 3; // whole numbers
        double lateFee = 1; // floating point numbers (decimals)
        bool isMember = true; // true/false values
        char shelf = 'A'; // single character
        string title = "Clean Code"; // text, strings are reference types

        // Operators 
        string user = "Jon"; // Single = is the assignment operator. 
        int total = copies * 2; // example of an arithmetic operator, like + - * / 
        bool isEnough = total > 4; // comparison - This line compares the value in total to 4, if it is greater
        // than 4, isEnough will get 'true', otherwise it will get 'false'
        // >, <, >=, <= - comparison operators
        bool exactlySix = total == 6; // equality. Single equals is assignment, double equals is equality.
        // unlike JS there is NO === all equality in C# is Strict equality
        bool lendable = isMember && isEnough; //logical operators
        // && - and, || - or, ! - reverses the condition that follows, ^ logical XOR - returns true if ONLY one condition is true

        // This is the basic way to construct strings from other strings
        // String concat - it works! But it can be messy
        Console.WriteLine(title + " has been checked out by " + user);

        // We can create much cleaner formatted strings
        // using String Interpolation - a string with a $ before the opening quote
        Console.WriteLine($"{title} on shelf {shelf}: {copies} copies, fee {lateFee}"); 

        // C# has ALOT of shorthands and little shortcuts that you can find and use 
        // to make your code easier to write. For example, lets say I want to add 1 to the value of total
        // I could do something like
        // total = total + 1; - ORRR
        total += 1; // arithmetic shorthand for the same thing, also works for *= /= -=

    }

    private static void ControlFlow()
    {
        Console.WriteLine("\n== Control Flow ==");


        // If - else if - else
        int copiesAvailable = 0; 
        bool isMember = true;

        if(copiesAvailable > 1 ) 
            Console.WriteLine("Many available for checkout!");
        else if (copiesAvailable == 1) {
            Console.WriteLine("Last copy!");
        }
        else {
            Console.WriteLine("Out of stock!");
            Console.WriteLine("Check again later!");
        }

        // Switch
        string genre = "Mystery";


        // Classic switch - notice C# cares about intent alot! No fall through like in other langauges
        switch (genre)
        {
            case "Mystery":
                Console.WriteLine("Check section A!");
                break;
            case "Science-Fiction":
                Console.WriteLine("Check Section F!");
                break;
            default:  // While optional, a default case to catch any edge cases is best practices
                Console.WriteLine("Uh oh");
                break;
        }

        // New in .NET 8, Switch Expressions! You don't have to use these - they prooobably wont come up in QC
        // but they're used out in real world code, so here is an example. In a switch expression, we want 
        // a return value from the switch - we can then use that value to print out a result
        string section = genre switch
        {
          // This is my expression body
          "Mystery" => "Section A",
          "Science-Fiction" => "Section F",
          _ => "Uh oh" //default  
        };
        Console.WriteLine(section);

    }

    private static void  Loops()
    {
        // C# provides for loops as well, same as Java and any other language
        // For, while, do-while, etc
        for (int day = 1; day <= 3; day++)
        {
            Console.WriteLine($"Reminder day {day}: fee so far {CalculateLateFee(day)}");
        }
        
        // FIX: this while loop had no braces, so ONLY the WriteLine was the loop body and the
        // onShelf-- decrement sat OUTSIDE the loop - onShelf never changed, an infinite loop.
        // Brace the body so the counter decrements each pass and the loop terminates.
        int onShelf = 3;
        while (onShelf > 0)
        {
            Console.WriteLine($"{onShelf} copies on the shelf!");
            onShelf--; // quick decrement shorthand
        }

        Console.WriteLine("No copies on shelf!");

        string myString = "dog";

        myString = "cat";
    }

    // I can use this shorthand for one line methods
    private static decimal CalculateLateFee(int daysLate) => daysLate * 2;


    private static void ArraysWork()
    {
        // C# provides for Arrays as well as lists and other collections - we'll get to those later. 
        string[] books = { "Dune", "Harry Potter", "Percy Jackson", "Lord of the Rings" };
        
        Console.WriteLine(books[2]); // I can access indiviudal elements - keeping in mind we index at 0
        
        // C# allows for for-each loops
        foreach (string book in books)
        {
            Console.WriteLine(book);
        }

    }

    private static void ClassesExample()
    {
        Console.WriteLine("Using our domain Book class");

        // Instantiating my first book, calling the constructor via "new" keyword
        Book dune = new Book("Dune", "Frank Herbert", 3);
        Book littlePrince = new Book("The Little Prince", "Antoine de Saint-Exupéry", 0);

        // If I want to print book info, I can just pass the book variable
        // It calls the toString() for me. The next two lines do the same thing
        Console.WriteLine(dune);
        Console.WriteLine(littlePrince.ToString());

        Console.WriteLine($"Checking out Dune: {dune.Checkout()}"); // true
        Console.WriteLine($"Checking out The Little Prince: {littlePrince.Checkout()}"); //false

    }

    public static void OopDemo()
    {
     
        Console.WriteLine("\n\n == OOP Demo stuff == ");

        // Leveraging polymorphism - Books, ReferenceBooks, Magazines - all are LibraryItems.
        LibraryItem[] catalog =
        {
            new Book("Dune", "Frank Herbert", 2),
            new ReferenceBook("C# Language Standards", "Microsoft", "Technology"),
            new Magazine("Sports Illustrated", "Francisco", 5, "Conde Naste")    
        };

        foreach(LibraryItem item in catalog)
        {
            Console.WriteLine(item.Describe());
        }

        // We can even use interfaces as reference types
        foreach(LibraryItem item in catalog)
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

        // override vs new behavior
        Magazine wired = new Magazine("Wired", "Luis", 3, "Conde Nast");
        LibraryItem baseMag = wired;

        Console.WriteLine("== Override vs new on the same object, different ref type");
        Console.WriteLine($"Magazine reference -> {wired.ShelfLabel()}");
        Console.WriteLine($"LibraryItem reference -> {baseMag.ShelfLabel()}");

    }

    // Collections demo stuff
    private static void CollectionsDemo()
    {
        Console.WriteLine("==== COLLECTIONS DEMO STUFF =====");

        //Creating a catalog object
        // Because this is backed by a list, it grows and shrinks for us
        Catalog catalog = new();

        // I could create my objects
        Book dune = new Book("Dune", "Frank Herbert", 3);

        // Then add them - we now go through Catalog.Add(), which wraps the private list.
        // We never touch catalog._items directly anymore: the list is the Catalog's business.
        catalog.Add(dune);

        // I can also just call a constructor inside the Add() method call
        // Methods having their arguments satisfied by the return of other methods is a common pattern
        // and sometimes you'll get like 4-5 callbacks deep in tools like ASP.NET
        catalog.Add(new ReferenceBook("C# Language Specs", "Microsoft", "Technology"));
        catalog.Add(new Magazine("Nat Geo", "Charlie", 4, "Conde Naste"));

        // Count is a wrapper property; catalog[0] uses the indexer - reads like an array,
        // but it's read-only, so no one can do catalog[0] = somethingElse.
        Console.WriteLine($"Catalog holds {catalog.Count}; first is {catalog[0].Title}");

        // The other containers, each reached through intent-named methods instead of raw fields:
        // STACK (LIFO) - return cart: the last book dropped is the first re-shelved.
        catalog.DropInReturnCart(catalog[0]);
        catalog.DropInReturnCart(catalog[2]);
        Console.WriteLine($"Return cart has {catalog.CartCount}; reshelving \"{catalog.Reshelve().Title}\" first");

        // QUEUE (FIFO) - holds line: the first member to ask is the first served.
        catalog.PlaceHold("Ada");
        catalog.PlaceHold("Grace");
        Console.WriteLine($"{catalog.HoldsWaiting} holds waiting; serving {catalog.ServeNextHold()} first");

        // LINKEDLIST - a reading list we reorder; AddNextUp jumps to the front.
        catalog.AddToReadingList(catalog[0]);
        catalog.AddNextUp(catalog[1]);
        Console.WriteLine("Reading list order:");
        foreach (LibraryItem item in catalog.ReadingList)
        {
            Console.WriteLine($"  - {item.Title}");
        }

        // Enum + Struct use
        ItemKind kind = ItemKind.Magazine; // example of selecting an enum value
        
        ShelfLocation location = new ShelfLocation(3,12); // struct - looks alot like a class, but it is a VALUE type

        Console.WriteLine($"{kind} sits at {location}"); 

        Book duneCopy = dune; // copies the reference
        // lets say I modify duneCopy, what happens to the data in dune?
        // all we copied was the pointer - these two things are not independent

        ShelfLocation location2 = location; // copies the data/fields 
        // these are not linked in the same way, I can edit the data in one without touching the other

        // Generics: our own Shelf<T> that can hold anything - though technically all the collections
        // we used thusfar have been generic classes themselves
        Shelf<LibraryItem> shelf = new Shelf<LibraryItem>(2);
        Shelf<int> intShelf = new Shelf<int>(200);
        
        shelf.TryAdd(catalog[0]);
        shelf.TryAdd(catalog[1]);

        Console.WriteLine($"Trying to add a third thing in our catalog: {shelf.TryAdd(catalog[2])}");

        
    }

    public static void ExceptionDemo()
    {
        Console.WriteLine("\n == Exceptions, patterns, logging ==");
        ILibraryRepository repo = new InMemoryLibraryRepository();

        IUnitOfWork libraryWork = new LibraryUnitOfWork(repo);

        LibraryItem dune = LibraryItemFactory.Create(ItemKind.Book, "Dune", "Frank", copies : 3);
        repo.Add(dune);
        repo.Add(LibraryItemFactory.Create(ItemKind.Magazine, "Wired", "Axel", copies:2));
        libraryWork.Stage("added 2 items");
        libraryWork.Commit();
        int missingId = 99;

        try
        {
            LibraryItem missing = repo.GetById(missingId);
            Console.WriteLine(missing.Describe());
        }
        catch (ItemNotFoundException ex)
        {
            Log.Error("Lookup failed for {id}: {Message}", missingId, ex.Message);
        }
        catch (LibraryException ex)
        {
            Log.Error("Library error: {Message}", ex.Message);
        }
        catch(Exception ex)
        {
            Log.Error("Non Library error: {Message}", ex.Message);
        }
        finally
        {
            Console.WriteLine("hit out finally back - lookup attemp done");
        }

        Book noCopies = new Book("Count of montecrito", "Alejandro Dumas", 0);

         try
        {
            Borrow(noCopies);
        }
        catch (ItemNotAvailableException ex)
        {
            Log.Warning("Borrow refused: {Message}", ex.Message);
        }
    }

    public static void Borrow(Book book)
    {
        // We can use the Checkout (boolean return) method from the book object
        // in an if or something
        if( !book.Checkout())
        {
            throw new ItemNotAvailableException(book.Id);
        }
    }

    public static void AdvancedClassesDemo()
    {
        Console.WriteLine("\n == Advanced classes Demo");
        Console.WriteLine(GC.GetTotalMemory(forceFullCollection: false) / 1024);

        ILibraryRepository repo = new InMemoryLibraryRepository();

        LibraryItem dune = LibraryItemFactory.Create(ItemKind.Book, "Dune", "Frank", copies : 3);
        repo.Add(dune);
        repo.Add(LibraryItemFactory.Create(ItemKind.Magazine, "Wired", "Axel", copies:2));
        repo.Add(LibraryItemFactory.Create(ItemKind.Book, "Divina Comedia", "Dante2", copies:2));

        Catalog catalog = new();

        foreach (LibraryItem item in repo.GetAll())
        {
            catalog.Add(item);
        }
        Console.WriteLine($"we have {catalog.Authors.Count} unique authors in our catalog");
        foreach (string author in catalog.Authors)
        {
            Console.WriteLine(author);
        }

        List<LibraryItem> byFrankHerbert = catalog.Find(item => item.Author == "Frankl Herbert");
        Console.WriteLine($"There are {byFrankHerbert.Count} books by Frank Herbert");

        Console.WriteLine("We have mix of lendable and non-lendable items");

        foreach(LibraryItem item in catalog.Lendable())
        {
            Console.WriteLine($"{item.Title}");
        }
    }
    
    public static async Task AsyncHttpDemo()
    {
        OpenLibraryClient client = new();

        string[] isbns = { "9780132350884", "9780201633610"};

        Task<LibraryItem?>[] fetchedBooks = new Task<LibraryItem?>[isbns.Length];

        for(int i = 0; i < isbns.Length; i++)
        {
            fetchedBooks[i] = client.FetchByIsbnAsync(isbns[i]);

        }
        LibraryItem?[] foundBooks = await Task.WhenAll(fetchedBooks);

        LibraryItem? firstBookFound = foundBooks.Length > 0 ? foundBooks[0] : null;

        Console.WriteLine($"Fetched: {firstBookFound?.Describe() ?? "nothing"}");

        
    }
}

