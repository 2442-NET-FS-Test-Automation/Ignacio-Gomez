namespace LibraryGame.Domain;

public class StandardGame : Game
{
    public StandardGame(string name, decimal price, bool available)
        : base(name, price, available)
    {
    }
}
