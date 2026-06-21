namespace LibraryKata.Domain;

public static class LibraryItemFactory
{
    // we will use that enum
    public static LibraryItem Create(
        ItemKind kind,
        string title,
        string author,
        int copies = 1,
        string section = "General",
        string publisher = "N/A")
    {
        //This method is going to use a switch to call the correct constructor
        switch (kind)
        {
            case ItemKind.Book:
                return new Book(title, author, copies);
            
            case ItemKind.ReferenceBook:
                return new ReferenceBook(title, author, section);

            case ItemKind.Magazine:
                return new Magazine(title, author, copies, publisher);

            default:
                throw new LibraryException($"Unknown item kind: {kind}");
        } 
    }
}
