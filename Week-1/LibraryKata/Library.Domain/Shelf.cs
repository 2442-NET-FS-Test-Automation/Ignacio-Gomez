namespace LibraryKata.Domain;
// For demo sake, test write a generic 
// I want to create a shelf, and a shelf can hold anything
// T is the standard placeholder for ... "some type"
public class Shelf<T>
{
    private readonly T[] _slots;
    private int _used;
    public Shelf(int capacity)
    {
        _slots = new T[capacity];
    }
    
    public int Capacity => _slots.Length;
    public int Count => _used;

    //Method to add items to our shelf

    public bool TryAdd(T item)
    {
        if (_used == _slots.Length)
        {
            return false;
        }
        _slots[_used++] = item;
        return true;
    }

    //Method to allow index access
    public T Get(int index)
    {
        return _slots[index];
    }

}