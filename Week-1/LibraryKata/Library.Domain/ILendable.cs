namespace Library.Domain;

//Interface in c# they are a contract for behaviors - they do not define the implementation of the methods
public interface ILendable
{
    //Only method signatures, not bodies, not even access modifiers
    bool Checkout();
    void Return();
}