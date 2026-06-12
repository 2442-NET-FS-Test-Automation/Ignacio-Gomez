namespace Library.Domain;

public class Magazine : LibraryIteam, ILendable
{
    public int CirculationCopies {get; private set;}
    public string Publisher{get; private set;}
    public Magazine(string title, string author, int circulationCopies, string publisher) : base(title, author)
    {
        CirculationCopies = circulationCopies;
    }
    public override string Describe()
    {
        return $"{Title} magazine, published by  {Publisher}";
    }

    public new string ShelfLabel()
    {
        return $"MAG-{Id} {Title}";
    }

    public bool Checkout()
    {
        if (CopiesAvailable == 0)
            return false;

        //Otherfise, we pass over the above code block 
        CirculationCopies--;
        return true;
    }

    public void Return() => CirculationCopies++;
}