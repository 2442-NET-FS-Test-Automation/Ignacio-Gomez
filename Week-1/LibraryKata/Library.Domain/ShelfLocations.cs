namespace LibraryKata.Domain;

// Struct are for small bundles of data with no identity
// They look like classes but they are values
// Meaning two structs of the same type the same data are identical
// If I compare those structs with equal I get true

public readonly struct ShelfLocation
{
    public int Aisle{get;}
    public int Shelf{get;}

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