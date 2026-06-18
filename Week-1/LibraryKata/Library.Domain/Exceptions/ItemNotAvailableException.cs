namespace LibraryKata.Domain;

public class ItemNotAvailableException : LibraryException
{
    public ItemNotAvailableException(int itemId)
        : base($"Library item with id {itemId} has no copies available to borrow.")
    {
    }
}
