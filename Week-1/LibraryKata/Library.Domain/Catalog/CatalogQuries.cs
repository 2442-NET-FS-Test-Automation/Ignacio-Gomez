using System.Collections;

namespace LibraryKata.Domain;

public partial class Catalog : IEnumerable<LibraryItem>
{
    public IEnumerator<LibraryItem> GetEnumerator()
    {
        foreach (LibraryItem item in _items)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    //
    public IEnumerable<LibraryItem> Lendable()
    {
        foreach (LibraryItem item in _items)
        {
            if (_items is ILendable)
            {
                yield return item;
            }
        }
    }

    public List<LibraryItem> Find(Predicate<LibraryItem> match)
    {
        List<LibraryItem> foundItems = new();
        foreach(LibraryItem item in _items)
        {
            if (match(item))
            {
                foundItems.Add(item);
            }
        }
        return foundItems; 
    }
   
}
