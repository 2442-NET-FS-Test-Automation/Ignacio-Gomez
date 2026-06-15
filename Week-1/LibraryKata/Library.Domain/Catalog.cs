using Library.Domain;

namespace LibraryKata.Domain;

public class Catalog
{
    //List<T>: ordered, grow/shrink dynamically, accessible.
    // Your default collection
    public readonly List<LibraryItem> _items = new();
    public int Count => _items.Count();
    public void Add(LibraryItem item) => _items.Count(); 
    // STACK LIFO
    // Primary methods - Push(), Pop()
    public readonly Stack<LibraryItem> _returnCart = new();
    //Queue<T> FIFO
    // Primary methods Enqueue(): Join the back of the line, Dequeue() remover from the front of the line
    public readonly Queue<string> _holdQueue = new();
    //LinkedList:no index access
    public readonly LinkedList<LibraryItem> _readingList = new(); 



}