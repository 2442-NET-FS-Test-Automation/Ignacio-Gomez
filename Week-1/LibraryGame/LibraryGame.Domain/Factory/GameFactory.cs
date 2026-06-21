namespace LibraryGame.Domain;

public enum GameKind
{
    StandardGame,
    PhysicalGame,
    DigitalGame
}

public static class GameFactory
{
    public static Game Create(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        switch (item.Kind)
        {
            case GameKind.StandardGame:
                return new StandardGame(item.Name, item.Price, item.Available);

            case GameKind.PhysicalGame:
                return new PhysicalGame(item.Name, item.Price, item.Available, item.Platform);

            case GameKind.DigitalGame:
                return new DigitalGame(item.Name, item.Price, item.Available, item.Size);

            default:
                throw new ArgumentOutOfRangeException(nameof(item.Kind), item.Kind, "Unknown game kind.");
        }
    }
}
