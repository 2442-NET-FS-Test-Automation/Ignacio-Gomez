namespace LibraryGame.Domain.Repo;

public interface IGameRepository
{
    IReadOnlyList<Game> GetAll();
    Game? FindById(int id);
    Game GetById(int id);
    void Add(Game game);
    bool Remove(int id);
}
