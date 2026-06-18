namespace LibraryKata.Domain;

public class LibraryException : Exception
{
    // The base class just contains a message
    public LibraryException(string message) : base(message)
    {
        
    }
}