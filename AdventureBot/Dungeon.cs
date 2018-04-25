using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;

namespace AdventureBot
{
    public class Dungeon
    {
        private Room[,,] _rooms;
        private Language _language;
        private Player _player;
        private Random _rnd;
        private LiteDatabase _db;
        public int XSize { get; set; }
        public int YSize { get; set; }
        public int ZSize { get; set; }

        public Dungeon(LiteDatabase db)
        {
            _rooms = new Room[Program.XSize, Program.YSize, Program.ZSize];
            _language = new Language();
            _player = new Player();
            _rnd = new Random();
            _db = db;
        }

        // Process this player's input, note upsert is done in main program after all processing
        public string Process(Player p, long statusId, string content)
        {
            string toReturn = "";

            // Update with last status update id to prevent re-doing same command
            if (!Program.Debug)
            {
                p.LastStatusId = statusId;  // Only update this if we're not debugging
            }

            // parse string
            content = Program.TagRegex.Replace(content.Replace("<br />", "\n"), "").Trim();
            //var words = content.Trim().Split(' ');
            var contentScan = "|"+content.Replace(" ", "| |")+"|";
            contentScan = contentScan.Replace("?", "");
            contentScan = contentScan.Replace("!", "").ToLower();
            var punctuation = contentScan.Where(Char.IsPunctuation).Distinct().ToArray();
            var words = contentScan.Split().Select(x => x.Trim(punctuation));

            // Directions
            if (words.Any("|north|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitNorth == (int) ExitStates.Open)
                {
                    p.Y--;
                    toReturn += "You exit north. \r\n" +
                                "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                GetRoomExits(p.X, p.Y, p.Z);
                }
                toReturn += "You can't go that way.";
            }
            if (words.Any("|east|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitEast == (int)ExitStates.Open)
                {
                    p.X++;
                    toReturn += "You exit east. \r\n" +
                                "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                GetRoomExits(p.X, p.Y, p.Z);
                }
                toReturn += "You can't go that way.";
            }
            if (words.Any("|south|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitSouth == (int)ExitStates.Open)
                {
                    p.Y++;
                    toReturn += "You exit south. \r\n" +
                                "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                GetRoomExits(p.X, p.Y, p.Z);
                }
                toReturn += "You can't go that way.";
            }
            if (words.Any("|west|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitWest == (int)ExitStates.Open)
                {
                    p.X--;
                    toReturn += "You exit west. \r\n" +
                                "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                GetRoomExits(p.X, p.Y, p.Z);
                }
                toReturn += "You can't go that way.";
            }
            if (words.Any("|up|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitUp == (int)ExitStates.Open)
                {
                    p.Z--;
                    toReturn += "You ascend to safer places in the dungeon - you go up. \r\n" +
                                "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                GetRoomExits(p.X, p.Y, p.Z);
                }
                toReturn += "You can't go that way.";
            }
            if (words.Any("|down|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitDown == (int)ExitStates.Open)
                {
                    p.Z++;
                    toReturn += "You descend deeper into the dungeon - you go down. \r\n" +
                                "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                GetRoomExits(p.X, p.Y, p.Z);
                }
                toReturn += "You can't go that way.";
            }

            // Look
            if (words.Any("|look|".Contains))
            {
                toReturn += "You are in " + GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                            GetRoomExits(p.X, p.Y, p.Z);
            }

            // Help
            if (words.Any("|score|".Contains) || words.Any("|status|".Contains))
            {
                toReturn += "Status:\r\n";
                toReturn += "Name: "+p.Username+"\r\n";
                toReturn += "Location co-ords: " + p.X + p.Y + p.Z + "\r\n";
                toReturn += "Level: " + p.Level + "\r\n";
                toReturn += "Health: " + p.Health + "\r\n";
                toReturn += "Strengh: " + p.Strength + "\r\n";
                toReturn += "Magic: " + p.Magic + "\r\n";
                toReturn += "Luck: " + p.Luck + "\r\n";
                toReturn += "Current weapon: " + p.Weapon + "\r\n";
                return toReturn;
            }

            // Help
            if (words.Any("|help|".Contains))
            {
                toReturn += "Commands so far are north, east, south, west, up, down, look, status and help. ";
                return toReturn;
            }

            if (toReturn == "")
            {
                // Don't understand
                toReturn =
                    "I'm sorry, I didn't understand that. Try using simple words, type 'help' for a full list of commands I understand.";
            }
            else
            {
                // Get players in room with you
                var playersInRoom = GetPlayersInRoom(p.X, p.Y, p.Z);
                if (playersInRoom != null)
                {
                    string playersAdd="";
                    foreach (var player in playersInRoom)
                    {
                        if (player.Username != p.Username)
                        {
                            playersAdd += "@"+ player.Username + ", ";
                        }
                    }
                    if (playersAdd != "")
                    {
                        toReturn += "\r\nAlso here is "+ playersAdd;
                    }
                    toReturn = toReturn.TrimEnd(' '); toReturn = toReturn.TrimEnd(',');
                }
            }

            return toReturn;
        }


        // Load dungeon into memory
        public void LoadDungeon()
        {
            // Get collection
            var dbRooms = _db.GetCollection<Room>("rooms");

            Console.WriteLine("Loading rooms from db...");
            for (var z = 0; z < Program.ZSize; z++)
            {
                for (var x = 0; x < Program.XSize; x++)
                {
                    for (var y = 0; y < Program.YSize; y++)
                    {
                            _rooms[x,y,z] = dbRooms.FindOne(r => r.X == x && r.Y==y && r.Z==z);
                    }
                }
            }
            Console.WriteLine("Complete...");

            //var rm = dbRooms.FindOne(x => x.X == 0 );
            //Console.WriteLine("Room: "+rm.Title+" ["+rm.Id+"]");
        }

        // List all players
        public void LoadPlayers()
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);

            Console.WriteLine("Loading players from db...");
            var allPlayers = dbPlayers.Find(r => r.X == 0);
            foreach (var p in allPlayers)
            {
                Console.WriteLine("Player "+p.Id+"."+p.Username+" in rm "+p.X+"," + p.Y + "," + p.Z);
            }
            Console.WriteLine("Complete...");

            //var rm = dbRooms.FindOne(x => x.X == 0 );
            //Console.WriteLine("Room: "+rm.Title+" ["+rm.Id+"]");
        }

        // Load dungeon into memory
        public Player LoadPlayer(string username)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);

            Player player = dbPlayers.FindOne(r => r.Username == username);
            return player;
        }

        // Add player
        public Player AddPlayer(string username)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            var p = new Player
            {
                Username = username,
                X = 0, Y=0, Z=0,
                IsActive = true,
                Health = 100,
                IsMonster = false,
                IsNPC = false,
                Luck = 5,
                Magic = 5,
                Strength = 5,
                LastStatusId = 0,
                Level = 1
            };
            MovePlayerRnd(p, 0);
            dbPlayers.Upsert(p);
            return p;
        }

        public void UpsertPlayer(Player p)
        {
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            dbPlayers.Upsert(p);
        }

        // Move payer to random room
        public void MovePlayerRnd(Player p, int level)
        {
            p.X = _rnd.Next(Program.XSize);
            p.Y = _rnd.Next(Program.YSize);
            p.Z = level;
        }

        // Add player
        public void DeletePlayer(string username)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            dbPlayers.Delete(x => x.Username == username);
        }

        public IEnumerable<Player> GetPlayersInRoom(int x, int y, int z)
        {
            var players = _db.GetCollection<Player>("players");
            IEnumerable<Player> toReturn =  players.Find(r => r.X == x && r.Y == y && r.Z == z);
            return toReturn;
        }

        // Wipe and overwrite db with new dungeon (careful!)
        public void CreateDungeon()
        {
            // Get collection
            var dbRooms = _db.GetCollection<Room>("rooms");

            // Create, if not exists, new indexes
            dbRooms.EnsureIndex(x => x.X);
            dbRooms.EnsureIndex(x => x.Y);
            dbRooms.EnsureIndex(x => x.Z);

            for (int z = 0; z < Program.ZSize; z++)
            {
                for (int x = 0; x < Program.XSize; x++)
                {
                    for (int y = 0; y < Program.YSize; y++)
                    {
                        // Make room
                        Room currentRoom = dbRooms.FindOne(r => r.X == x && r.Y == y && r.Z == z);
                        if (currentRoom != null)
                        {
                            _rooms[x, y, z] = currentRoom;
                        }
                        else
                        {
                            _rooms[x, y, z] = new Room();
                        }
                        ClearRoom(x, y, z, _language.GetARandomLocationName());

                        // Exits
                        if (y != 0) // Can't go north from top edge
                        {
                            if (_rnd.Next(10) < 6 && y > 0)
                            {
                                _rooms[x, y, z].ExitNorth = (int)ExitStates.Open;
                                _rooms[x, y-1, z].ExitSouth = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y - 1, z]);
                            }
                        }
                        if (x != Program.XSize) // Can't go east from right edge
                        {
                            if (_rnd.Next(10) < 6 && x < XSize)
                            {
                                _rooms[x, y, z].ExitEast = (int) ExitStates.Open;
                                _rooms[x+1, y, z].ExitWest = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x + 1, y, z]);
                            }
                        }
                        if (y != Program.YSize) // Can't go south from bottom edge
                        {
                            if (_rnd.Next(10) < 6 && y < YSize)
                            {
                                _rooms[x, y, z].ExitSouth = (int) ExitStates.Open;
                                _rooms[x, y+1, z].ExitNorth = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y + 1, z]);
                            }
                        }
                        if (x != 0) // Can't go west from left edge
                        {
                            if (_rnd.Next(10) < 6 && x > 0)
                            {
                                _rooms[x, y, z].ExitWest = (int) ExitStates.Open;
                                _rooms[x-1, y, z].ExitEast = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x - 1, y, z]);
                            }
                        }
                        if (z != 0) // Can't go up from top level
                        {
                            if (_rnd.Next(10) < 6 && z > 0)
                            {
                                _rooms[x, y, z].ExitUp = (int)ExitStates.Open;
                                _rooms[x, y, z-1].ExitDown = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y, z - 1]);
                            }
                        }
                        if (z != Program.ZSize) // Can't go down from bottom level
                        {
                            if (_rnd.Next(10) < 6 && z < ZSize)
                            {
                                _rooms[x, y, z].ExitDown = (int) ExitStates.Open;
                                _rooms[x, y, z+1].ExitUp = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y, z + 1]);
                            }
                        }

                        dbRooms.Upsert(_rooms[x,y,z]);
                        Console.WriteLine(GetRoomName(x,y,z));
                        Console.WriteLine(GetRoomExits(x,y,z));
                    }

                }
            }

        }

        public string GetRoomName(int x, int y, int z)
        {
            return _rooms[x, y, z].Title;
            //+ " {" + x + "," + y + "," + z + "}";
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
            toReturn = toReturn.TrimEnd(' ');toReturn = toReturn.TrimEnd(',');
            return toReturn;
        }

        public void ClearRoom(int x, int y, int z, string title)
        {
            _rooms[x, y, z].X = x;
            _rooms[x, y, z].Y = y;
            _rooms[x, y, z].Z = z;
            _rooms[x, y, z].Title = title;
            _rooms[x, y, z].ExitNorth = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitEast = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitSouth = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitWest = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitUp = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitDown = (int)ExitStates.NoExit;
        }


    }
}