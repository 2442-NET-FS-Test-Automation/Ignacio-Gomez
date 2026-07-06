namespace DsaThreading;

public class Bank
{
    public long Balance;
    private readonly object _gate = new();
    public void DepositUnsafe(long amount) => Balance += amount;
    public void DepositSafe(long amount)
    {
        lock (_gate)
        {
            Balance += amount;
        }
    }
}