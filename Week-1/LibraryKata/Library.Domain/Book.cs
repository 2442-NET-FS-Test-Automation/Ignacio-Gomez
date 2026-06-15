namespace Library.Domain;

public class Book : LibraryItem, ILendable
{
    public int CopiesAvailable { get; private set; }

    public Book(string title, string author, int copiesAvailable) : base(title, author)
    {
        CopiesAvailable = copiesAvailable;
    }

    public override string Describe()
    {
        return $"{Id}: {Title} by {Author} has {CopiesAvailable} copies available for checkout";
    }

    public bool Checkout()
    {
        if (CopiesAvailable == 0)
            return false;

        CopiesAvailable--;
        return true;
    }

    public void Return() => CopiesAvailable++;
}
