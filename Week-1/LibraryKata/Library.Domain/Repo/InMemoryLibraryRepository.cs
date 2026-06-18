//using LibraryKata.Domain.Exceptions;
using Serilog;

namespace LibraryKata.Domain.Repo;

public class InMemoryLibraryRepository : ILibraryRepository
{
    private readonly Dictionary<int, LibraryItem> _items = new();

    public IReadOnlyList<LibraryItem> GetAll()
    {
        return _items.Values.ToList();
    }

    public LibraryItem? FindById(int id)
{
    foreach (LibraryItem item in _items.Values)
    {
        if (item.Id == id)
            return item;
    }

    return null;
}

    public LibraryItem GetById(int id)
    {
        // LibraryItem? item = FindById(id);

        // if (item is not null)
        //     return item;

        // Log.Warning("Lookup failed for item with id {Id}", id);
        // throw new ItemNotFoundException(id);

        if (_items.TryGetValue(id, out LibraryItem? item))
        {
            return item;
        }
        Log.Warning($"Lookup failed for id {id}", id);
        throw new ItemNotFoundException(id);
    }

    public void Add(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (FindById(item.Id) is not null)
        {
            Log.Warning("Attempted to add duplicate item with id {Id}", item.Id);
            throw new LibraryException($"Library item with id {item.Id} already exists.");
        }

        //_items.Add(item);
        _items.Add(item.Id, item);
        Log.Information("Added {Title} with id {Id}", item.Title, item.Id);
    }

    public bool Remove(int id)
    {
        // LibraryItem? item = FindById(id);

        // if (item is null)
        // {
        //     Log.Information("Remove failed for item with id {Id}", id);
        //     return false;
        // }

        // bool removed = _items.Remove(item);

        if (_items.Remove(id))
        {
            Log.Information("Removed item with id {id}", id);
            return true;
        }

        Log.Information("Removal failed for item with id {id}", id);
        return false;
        
    }
}
