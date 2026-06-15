// Lets actually start modeling stuff
namespace Library.Domain;

public class OldBook
{
    public string? Title { get; private set; }
    public string? Author { get; private set; }
    public int? CopiesAvailable { get; private set; }

    private static int _nextId = 1;

    public int Id { get; }

    public OldBook(string title, string author, int copiesAvailable)
    {
        Id = _nextId++;
        Title = title;
        Author = author;
        CopiesAvailable = copiesAvailable;
    }

    public OldBook()
    {
    }

    public bool Checkout()
    {
        if (CopiesAvailable == 0)
            return false;

        CopiesAvailable--;
        return true;
    }

    public void Return() => CopiesAvailable++;

    public override string ToString()
    {
        return $"{Title} by {Author}: {CopiesAvailable} available for checkout";
    }
}
