namespace LibraryKata.Domain.Repo;

public interface ILibraryRepository
{
    IReadOnlyList<LibraryItem> GetAll();
    LibraryItem? FindById(int id);
    LibraryItem GetById(int id);
    void Add(LibraryItem item);
    bool Remove(int id);
}
