namespace LibraryGame.Domain;

public interface ILendable
{
    void Describe();
    bool ChangeStatus(int status);
}
