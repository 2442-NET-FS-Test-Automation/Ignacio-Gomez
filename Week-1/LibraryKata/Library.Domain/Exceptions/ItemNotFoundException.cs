using System.Data.Common;

namespace LibraryKata.Domain;

public class ItemNotFoundException: LibraryException
{
    //We can hold the offeding Id that triggered the exception
    public int Id {get; }
    public ItemNotFoundException(int id) 
        : base($"No library item with id {id}")
    {
        Id = id;
    } 
}