namespace LibraryGame.Domain;

public class GameItem
{
    public GameKind Kind { get; }
    public string Name { get; }
    public decimal Price { get; }
    public bool Available { get; }
    public string Platform { get; }
    public int Size { get; }

    public GameItem(
        GameKind kind,
        string name,
        decimal price,
        bool available,
        string platform = "",
        int size = 0)
    {
        Kind = kind;
        Name = name;
        Price = price;
        Available = available;
        Platform = platform;
        Size = size;
    }
}