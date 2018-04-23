using System;

namespace AdventureBot
{
    public class Dungeon
    {
        private Room[,,] _rooms;
        private Language _language;
        private Random _rnd;
        public int XSize { get; set; }
        public int YSize { get; set; }
        public int ZSize { get; set; }

        public Dungeon()
        {
            _rooms = new Room[Program.XSize, Program.YSize, Program.ZSize];
            _language = new Language();
            _rnd = new Random();
        }

        public void CreateDungeon()
        {
            for (int x = 0; x < Program.XSize; x++)
            {
                for (int y = 0; y < Program.YSize; y++)
                {
                    for (int z = 0; z < Program.ZSize; z++)
                    {
                        // Make room
                        _rooms[x, y, z] = new Room(x, y, z, _language.GetARandomLocationName());

                        // Exits
                        if (y != 0) // Can't go north from top edge
                        {
                            if (_rnd.Next(10) < 5 && y > 0)
                            {
                                _rooms[x, y, z].ExitNorth = (int)ExitStates.Open;
                                _rooms[x, y-1, z].ExitSouth = (int)ExitStates.Open;
                            }
                        }
                        if (x != Program.XSize) // Can't go east from right edge
                        {
                            if (_rnd.Next(10) < 5 && x < XSize)
                            {
                                _rooms[x, y, z].ExitEast = (int) ExitStates.Open;
                                _rooms[x+1, y, z].ExitWest = (int)ExitStates.Open;
                            }
                        }
                        if (y != Program.YSize) // Can't go south from bottom edge
                        {
                            if (_rnd.Next(10) < 5 && y < YSize)
                            {
                                _rooms[x, y, z].ExitSouth = (int) ExitStates.Open;
                                _rooms[x, y+1, z].ExitNorth = (int)ExitStates.Open;
                            }
                        }
                        if (x != 0) // Can't go west from left edge
                        {
                            if (_rnd.Next(10) < 5 && x > 0)
                            {
                                _rooms[x, y, z].ExitWest = (int) ExitStates.Open;
                                _rooms[x-1, y, z].ExitEast = (int)ExitStates.Open;
                            }
                        }
                        if (z != 0) // Can't go up from top level
                        {
                            if (_rnd.Next(10) < 5 && z > 0)
                            {
                                _rooms[x, y, z].ExitUp = (int)ExitStates.Open;
                                _rooms[x, y, z-1].ExitDown = (int)ExitStates.Open;
                            }
                        }
                        if (z != Program.ZSize) // Can't go down from bottom level
                        {
                            if (_rnd.Next(10) < 5 && z < ZSize)
                            {
                                _rooms[x, y, z].ExitDown = (int) ExitStates.Open;
                                _rooms[x, y, z+1].ExitUp = (int)ExitStates.Open;
                            }
                        }

                        Console.WriteLine(GetRoomName(x,y,z));
                        Console.WriteLine(GetRoomExits(x,y,z));
                    }

                }
            }

        }

        public string GetRoomName(int x, int y, int z)
        {
            return _rooms[x, y, z].Title + " {" + x + "," + y + "," + z + "}";
        }

        public string GetRoomExits(int x, int y, int z)
        {
            var toReturn = "";
            if (_rooms[x, y, z].ExitNorth < 2)
            {
                toReturn += "north, ";
            }
            if (_rooms[x, y, z].ExitEast < 2)
            {
                toReturn += "east, ";
            }
            if (_rooms[x, y, z].ExitSouth < 2)
            {
                toReturn += "south, ";
            }
            if (_rooms[x, y, z].ExitWest < 2)
            {
                toReturn += "west, ";
            }
            if (_rooms[x, y, z].ExitUp < 2)
            {
                toReturn += "up, ";
            }
            if (_rooms[x, y, z].ExitDown < 2)
            {
                toReturn += "down, ";
            }
            if (toReturn != "")
            {
                toReturn = "There are exits to the " + toReturn;
            }
            return toReturn;
        }

    }
}