namespace LibraryKata.App;
public class Program
{
    // public - accesible across the program
    // static - Main can be called upon without a Program object. 
    // void it doesnt return anything
    public static void Main()
    {
        DataTypesAndOperators();
    }
    //private - accessible only within this class
    // static - it belongs to the class, not objects
    // void - return nothing
    private static void DataTypesAndOperators()
    {
        Console.WriteLine("==Data types and operators==");
        
        int copies = 3;
        double lateFee = 1;
        bool isMember = true;
        char shelf = 'A';
        string title = "Clean code";


    }
}
