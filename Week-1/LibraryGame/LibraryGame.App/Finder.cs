namespace LibraryGame.App;

using LibraryGame.Domain;

public static class Finder
{
    public static T? FindById<T>(List<T> items, int id) where T : Game
    {
        foreach (T item in items)
        {
            if (item.Id == id)
                return item;
        }

        return null;
    }
}
