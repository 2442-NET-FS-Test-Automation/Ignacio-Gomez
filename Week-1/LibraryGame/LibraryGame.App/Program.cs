using LibraryGame.Domain;

namespace LibraryGame.App;

public class Program
{
    public static void Main()
    {
        List<Game> games = SeedGames();
        bool running = true;

        while (running)
        {
            PrintMenu();
            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1":
                    ListGames(games);
                    break;
                case "2":
                    AddGame(games);
                    break;
                case "3":
                    ChangeGameAvailability(games);
                    break;
                case "0":
                    running = false;
                    Console.WriteLine("Goodbye!");
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static List<Game> SeedGames()
    {
        return new List<Game>
        {
            GameFactory.Create(new GameItem(GameKind.StandardGame, "Zelda", 199, true)),
            GameFactory.Create(new GameItem(GameKind.StandardGame, "Metroid", 299, true)),
            GameFactory.Create(new GameItem(GameKind.PhysicalGame, "Mario Kart", 249, true, platform: "Nintendo Switch")),
            GameFactory.Create(new GameItem(GameKind.DigitalGame, "Cyberpunk", 200, true, size: 64))
        };
    }

    private static void PrintMenu()
    {
        Console.WriteLine("== Library Game Manager ==");
        Console.WriteLine("1. List games");
        Console.WriteLine("2. Add game");
        Console.WriteLine("3. Change availability");
        Console.WriteLine("0. Exit");
        Console.Write("Choose an option: ");
    }

    private static void ListGames(List<Game> games)
    {
        Console.WriteLine();
        Console.WriteLine("== Game Catalog ==");

        // Polymorphism happens here: each Game reference calls the real object's Describe().
        foreach (Game game in games)
        {
            game.Describe();
        }
    }

    private static void AddGame(List<Game> games)
    {
        Console.Write("Game name: ");
        string name = Console.ReadLine() ?? "";

        Console.Write("Game price: ");
        decimal price = decimal.Parse(Console.ReadLine() ?? "0");

        Console.Write("Available? (1 yes / 0 no): ");
        int status = int.Parse(Console.ReadLine() ?? "0");

        Console.Write("Game type (1 standard / 2 physical / 3 digital): ");
        string typeChoice = Console.ReadLine() ?? "";

        bool available = status == 1;
        GameKind kind = typeChoice switch
        {
            "1" => GameKind.StandardGame,
            "2" => GameKind.PhysicalGame,
            "3" => GameKind.DigitalGame,
            _ => GameKind.StandardGame
        };

        string platform = "";
        int size = 0;

        if (kind == GameKind.PhysicalGame)
        {
            Console.Write("Platform: ");
            platform = Console.ReadLine() ?? "";
        }
        else if (kind == GameKind.DigitalGame)
        {
            Console.Write("Game size in GB: ");
            size = int.Parse(Console.ReadLine() ?? "0");
        }

        GameItem item = new GameItem(kind, name, price, available, platform, size);
        Game newGame = GameFactory.Create(item);
        games.Add(newGame);

        Console.WriteLine($"Game {newGame.Name} was added.");
    }

    private static void ChangeGameAvailability(List<Game> games)
    {
        Console.Write("Enter game id: ");
        int gameId = int.Parse(Console.ReadLine() ?? "0");

        Console.Write("New status (1 available / 0 not available): ");
        int status = int.Parse(Console.ReadLine() ?? "0");

        Game? selectedGame = FindGameById(games, gameId);

        if (selectedGame is null)
        {
            Console.WriteLine("Game not found.");
            return;
        }

        bool changed = selectedGame.ChangeStatus(status);

        if (changed)
        {
            Console.WriteLine("Game status updated.");
        }
        else
        {
            Console.WriteLine("Game status was not updated.");
        }
    }

    private static Game? FindGameById(List<Game> games, int gameId)
    {
        foreach (Game game in games)
        {
            if (game.Id == gameId)
            {
                return game;
            }
        }

        return null;
    }
}
