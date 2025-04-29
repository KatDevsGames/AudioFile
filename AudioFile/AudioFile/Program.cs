namespace AudioFile;

static class Program
{
    static int Main()
    {
        using (Game game = new()) game.Run();
        return 0;
    }
}