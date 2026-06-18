namespace LibraryGame.Domain.Repo;

public class InMemoryGameRepository : IGameRepository
{
    private readonly List<Game> _games = new();

    public IReadOnlyList<Game> GetAll()
    {
        return _games.AsReadOnly();
    }

    public Game? FindById(int id)
    {
        foreach (Game game in _games)
        {
            if (game.Id == id)
            {
                return game;
            }
        }

        return null;
    }

    public Game GetById(int id)
    {
        return FindById(id) ?? throw new InvalidOperationException($"Game with id {id} was not found.");
    }

    public void Add(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        _games.Add(game);
    }

    public bool Remove(int id)
    {
        Game? game = FindById(id);

        if (game is null)
            return false;

        return _games.Remove(game);
    }
}
