namespace LibraryKata.Domain;

public partial class Catalog
{
    // ENCAPSULATION change: these four collections used to be PUBLIC fields, so callers
    // reached straight in (catalog._items.Add(...)). That leaks the implementation - every
    // caller becomes coupled to "it's a List", and nothing stops them clearing or reordering
    // it behind the Catalog's back. We make the containers PRIVATE and expose intent-named
    // methods instead. The class still just wraps these containers, but now IT owns how they
    // are used, and we can swap a backing store or add validation later without touching a
    // single caller.

    // LIST<T>: ordered, grows/shrinks dynamically, accessible via index. Your default collection.
    private readonly List<LibraryItem> _items = new();

    // STACK<T>: Last-In-First-Out. The return cart - the most recently returned item is
    // re-shelved first. Push() puts an item on top, Pop() removes the top item.
    private readonly Stack<LibraryItem> _returnCart = new();

    // QUEUE<T>: First-In-First-Out. The holds line - customers placing holds on books.
    // Enqueue() joins the back of the line, Dequeue() is served from the front.
    private readonly Queue<string> _holdQueue = new();

    // LINKEDLIST<T>: cheap inserts/removals anywhere with no shifting, but NO index access.
    // A curated reading list we reorder often.
    private readonly LinkedList<LibraryItem> _readingList = new();

    private readonly HashSet<string> _authors = new();
    public IReadOnlyCollection<String> Authors => _authors;


    // --- List surface ---
    // Wrapping Add/Remove/index is the whole point of encapsulation: callers state intent,
    // the Catalog decides how. Count was already exposed this way; now the rest is too.
    public int Count => _items.Count;
    
    //public Library this 
    public LibraryItem this[int index] => _items[index]; // indexer: read catalog[0] like an array, but read-only
    public void Add(LibraryItem item)
    {
        _items.Add(item);
        _authors.Add(item.Author);
    }
    public bool IsEmpty => _items.Count == 0;
    //public void Add(LibraryItem item) => _items.Add(item);
    public bool Remove(LibraryItem item) => _items.Remove(item);

    // --- Stack surface (return cart) ---
    public void DropInReturnCart(LibraryItem item) => _returnCart.Push(item);
    public LibraryItem Reshelve() => _returnCart.Pop();   // most-recently-returned first (LIFO)
    public int CartCount => _returnCart.Count;

    // --- Queue surface (holds line) ---
    public void PlaceHold(string member) => _holdQueue.Enqueue(member);
    public string ServeNextHold() => _holdQueue.Dequeue(); // earliest request first (FIFO)
    public int HoldsWaiting => _holdQueue.Count;

    // --- LinkedList surface (reading list) ---
    public void AddToReadingList(LibraryItem item) => _readingList.AddLast(item);
    public void AddNextUp(LibraryItem item) => _readingList.AddFirst(item); // jump to the front of the list
    // Expose as IEnumerable so callers can foreach over it but cannot mutate the linked list directly.
    public IEnumerable<LibraryItem> ReadingList => _readingList;
}
