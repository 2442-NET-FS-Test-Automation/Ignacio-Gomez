namespace Library.Domain;

public class Book : LibraryIteam, ILendable
{
    // What is unique to a book
    public int CopiesAvailable {get; private set;}

    // We take in all our arguments for the parents + child
    public Book(string title, string author, int copiesAvailable) : base(title, author)
    {
        CopiesAvailable = copiesAvailable;
    }
    public override string Describe()
    {
        return $"{Id}: {Title} by {Author} has {CopiesAvailable} copies available for checkoout";

    }

    public bool Checkout()
    {
        if (CopiesAvailable == 0)
            return false;

        //Otherfise, we pass over the above code block
        CopiesAvailable--;
        return true;
    }

    public void Return() => CopiesAvailable++;
}