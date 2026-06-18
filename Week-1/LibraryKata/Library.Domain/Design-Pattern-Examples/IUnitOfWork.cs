using LibraryKata.Domain.Repo;

namespace LibraryKata.Domain;

public interface IUnitOfWork
{
    ILibraryRepository Items {get;}
    void Stage(string change);
    int Commit();

}