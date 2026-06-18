namespace LibraryKata.Domain;

public class ReferenceBook : LibraryItem
{
    // Our reference book will have a section where it lives
    public string Section { get;}

    public ReferenceBook(string title, string author, string section) 
        : base (title, author)
    {
        Section = section;
    }

    public override string Describe()
    {
        return $"{Id}: {Title} by {Author} -- reference only, {Section} section.";
    }

    // Overriding ShelfLabel() - this is a "true" override
    public override string ShelfLabel()
    {
        return $"REF-{Id} {Title} {Section}";
    }

}