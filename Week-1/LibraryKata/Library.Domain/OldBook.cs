//Lets actually start modeling stuff
namespace Library.Domain;

public class Book
{
    // Things about a book we can model - what is the shape
    // Because I want to use no-arg Constructos, its best practice to make
    // my properties nullable
    public string? Title {get; private set; } //auto property syntax - no writing getters and setters
    
    public string? Author {get; private set; }

    public int? CopiesAvailable {get; private set; }

    //The same way we can have static methods (belongs to the class)
    // we ca have static properties/members

    private static int _nextId = 1; //By convention, static properties have an underscore
    public int Id {get;}

    // Every class has a very specific method within it
    // The constructor - you can have as many as you need

    public Book(string title, string author, int copiesAvailable)
    {
        Id = _nextId++;
        Title = title;
        Author = author;
        CopiesAvailable = copiesAvailable;
    }

    public Book() {}

    //Our first instance method -  no 'static' keyword
    // an accees modifier
    public bool Checkout()
    {
        if (CopiesAvailable == 0)
            return false;

        //Otherfise, we pass over the above code block
        CopiesAvailable--;
        return true;
    }

    public void Return() => CopiesAvailable++;

    //Overriding a toString
    public override string ToString()
    {
        // Commented out below is a call to base.ToString()
        // We can use the base keyword to the parent
        // Book parents
        //return base.ToString();
        return $"{Title} by {Author}: {CopiesAvailable} available for checkout";

    }

}