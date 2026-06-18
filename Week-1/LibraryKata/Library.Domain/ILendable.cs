namespace LibraryKata.Domain;

// Interfaces in C# - they are a contract for behaviors - they do not define the implementation of the methods within
// 
public interface ILendable
{
    // Only method signatures, not bodies, not even access modifiers 
    bool Checkout();
    void Return();
}