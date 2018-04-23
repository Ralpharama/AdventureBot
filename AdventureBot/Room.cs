namespace AdventureBot
{
    enum ExitStates
    {
        Open, Unlocked, Locked, NoExit, Blocked
    }

    public class Room
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string Title { get; set; }

        // Exits
        public int ExitNorth { get; set; }

        public int ExitEast { get; set; }
        public int ExitSouth { get; set; }
        public int ExitWest { get; set; }
        public int ExitUp { get; set; }
        public int ExitDown { get; set; }

        public Room(int x, int y, int z, string title)
        {
            X = x;
            Y = y;
            Z = z;
            Title = title;
            ExitNorth = (int)ExitStates.NoExit;
            ExitEast = (int)ExitStates.NoExit;
            ExitSouth = (int)ExitStates.NoExit;
            ExitWest = (int)ExitStates.NoExit;
            ExitUp = (int)ExitStates.NoExit;
            ExitDown = (int)ExitStates.NoExit;
        }
    }
}