namespace LibraryKata.Domain;

// Structs are for small bundles of data with no identity. 
// They look kind of like classes but they are VALUE types
// Meaning - two structs of the same type with the same data are identical
// If I compare those two structs with .equals() I get true 
public readonly struct ShelfLocation
{
    public int Aisle { get; }
    public int Shelf { get; }

    public ShelfLocation(int aisle, int shelf)
    {
        Aisle = aisle;
        Shelf = shelf;
    }

    public override string ToString()
    {
        return $"Aisle {Aisle}, Shelf {Shelf}";
    }
}