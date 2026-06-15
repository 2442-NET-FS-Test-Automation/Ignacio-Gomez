namespace Library.Domain;

public class ReferenceBook : LibraryItem
{
    public string Section { get; }

    public ReferenceBook(string title, string author, string section) : base(title, author)
    {
        Section = section;
    }

    public override string Describe()
    {
        return $"{Id}: {Title} by {Author} -- reference only, {Section} section.";
    }

    public override string ShelfLabel()
    {
        return $"REF-{Id} {Title} {Section}";
    }
}
