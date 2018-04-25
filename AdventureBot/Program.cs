using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using TootNet;

namespace AdventureBot
{
    class Program
    {
        internal static string Username = "YOUR_EMAIL";
        internal static string Password = "YOUR_PASSWORD";
        internal static string InstanceName = "botsin.space";
        internal static int XSize = 5;
        internal static int YSize = 5;
        internal static int ZSize = 5;

        public static readonly Regex TagRegex =
            new Regex(@"<(""[^""]*""|'[^']*'|[^'"">])*>", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            try
            {
                Task t = MainAsync(args);
                t.Wait();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error. Process will die.");
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        static async Task MainAsync(string[] args)
        {
            using (var db =
                new LiteDatabase(
                    @"C:\AdventureBot\MyData.db"))
            {

                var language = new Language();
                var dungeon = new Dungeon(db);
                dungeon.LoadDungeon();

                var authorize = new Authorize();
                await authorize.CreateApp(Program.InstanceName, "Tootnet", Scope.Read | Scope.Write | Scope.Follow);

                Console.WriteLine("Logging in " + Program.Username);
                var tokens = await authorize.AuthorizeWithEmail(Program.Username, Program.Password);

                Console.WriteLine("toot [message] - toot message as AdventureBot");
                Console.WriteLine("notification - list notifications");
                Console.WriteLine("add - add/upset players from notif list into db... ALSO reomoves unfollow");
                Console.WriteLine("process - process mentions as commands");
                Console.WriteLine("loop - run add and process with pause on endless loop");
                Console.WriteLine("players - list current players");
                Console.WriteLine("createnew - !!!! wipe and create new dungeon");
                Console.WriteLine("quit");

                var command = args;
                var commandWord = (args == null) ? "" : args.FirstOrDefault()?.ToLower();
                Console.WriteLine("...");
                while (commandWord != "exit")
                {
                    switch (commandWord)
                    {
                        case "quit":
                        case "exit":
                            return;

                        case "toot":
                            if (command.Length <= 1)
                                break;
                            var text = command[1].Trim();
                            var post = await tokens.Statuses.PostAsync(status => text);
                            Console.WriteLine(post.Account.DisplayName + "<br>" + post.Account.Acct);
                            Console.WriteLine(TagRegex.Replace(post.Content.Replace("<br />", "\n"), "").Trim());
                            Console.WriteLine(post.CreatedAt);
                            break;

                        case "notification":
                            var notifications = await tokens.Notifications.GetAsync();
                            foreach (var notification in notifications)
                            {
                                Console.WriteLine("-----[start]-------");
                                Console.WriteLine(notification.Account.DisplayName + "<br>" + notification.Account.Acct);
                                Console.WriteLine(notification.Type);
                                if (notification.Type == "mention" || notification.Type == "reblog" ||
                                    notification.Type == "favourite")
                                {
                                    Console.WriteLine(notification.Status.Id+": "+
                                        notification.Status.Account.DisplayName + "<br>" +
                                        notification.Status.Account.Acct);
                                    Console.WriteLine(TagRegex
                                        .Replace(notification.Status.Content.Replace("<br />", "\n"), "").Trim());
                                }
                                Console.WriteLine(notification.CreatedAt);
                                Console.WriteLine("------[end]------");
                            }
                            break;

                        // Add new players
                        case "add":
                            await DoAdds(tokens, db, dungeon);
                            break;

                        // Each player that has mentioned us, process their input
                        case "process":
                            await DoProcess(tokens, db, dungeon);
                            break;

                        // Add and process in andless loop - for task running from scheduler run with dotnet adventurebot.dll loop
                        case "loop":
                            while (true)
                            {
                                await DoAdds(tokens, db, dungeon);
                                await DoProcess(tokens, db, dungeon);
                                Thread.Sleep(900000);  // 15 mins
                            };
                            //await Task.Delay(20);
                            return;

                        case "createnew":
                            dungeon.CreateDungeon();
                            break;

                        case "load":
                            dungeon.LoadDungeon();
                            break;

                        case "players":
                            dungeon.LoadPlayers();
                            break;
                    }
                    Console.Write("command: ");
                    command = Console.ReadLine().Trim().Split(' ', 2);
                    commandWord = command.First().ToLower();
                }
            } // end of db using
        }

        // Add new users to game
        public static async Task DoAdds(Tokens tokens, LiteDatabase db, Dungeon dungeon)
        {
            Console.WriteLine("Checking for followers...");
            var addNotif = await tokens.Notifications.GetAsync();
            foreach (var notification in addNotif)
            {
                if (notification.Type == "follow")
                {
                    Player p = dungeon.LoadPlayer(notification.Account.Acct);
                    if (p != null)
                    {
                        Console.WriteLine("Player " + p.Username + " already exists in db...");
                    }
                    else
                    {
                        Console.WriteLine("Adding new player " + notification.Account.Acct + "...");
                        dungeon.AddPlayer(notification.Account.Acct);
                        var addMessage = "Welcome @" + notification.Account.Acct +
                                         ", you have joined the game. Type 'help' for commands. " +
                                         "You are in " + dungeon.GetRoomName(p.X, p.Y, p.Z) + "<br>" +
                                         dungeon.GetRoomExits(p.X, p.Y, p.Z);
                        await tokens.Statuses.PostAsync(status => addMessage, in_reply_to_account_id => notification.Account.Id, visibility => "private");
                    }
                }
                if (notification.Type == "unfollow")
                {
                    dungeon.DeletePlayer(notification.Account.Acct);
                    var delMessage = "Farewell @" + notification.Account.Acct +
                                     ", you have left the game. Follow to to play again. ";
                    await tokens.Statuses.PostAsync(status => delMessage, in_reply_to_account_id => notification.Account.Id, visibility => "private");
                }

            }
        }

        // Process each mention as command
        public static async Task DoProcess(Tokens tokens, LiteDatabase db, Dungeon dungeon)
        {            
            Console.WriteLine("Processing players...");
            var pn = await tokens.Notifications.GetAsync();
            foreach (var notification in pn)
            {
                if (notification.Type == "mention")
                {
                    var mentionToReturn = TagRegex.Replace(notification.Status.Content.Replace("<br />", "\n"), "").Trim();
                    mentionToReturn = (mentionToReturn.Length > 30) ? mentionToReturn.Substring(30) : mentionToReturn;
                    Console.WriteLine("M#" + notification.Status.Id + " " + notification.Account.Acct + ": "+ mentionToReturn);
                    var currentStatusId = notification.Status.Id;
                    Player pl = dungeon.LoadPlayer(notification.Account.Acct);
                    //if (pl != null && pl.LastStatusId<currentStatusId)  // Only last command!
                    if (pl != null)
                    {
                        var response = dungeon.Process(pl, notification.Status.Id, notification.Status.Content);
                        dungeon.UpsertPlayer(pl);
                        if (response != null)
                        {
                            //await tokens.Statuses.PostAsync(status => "@" + notification.Account.Acct + " " + response, in_reply_to_account_id => notification.Account.Id, visibility => "private");
                            Console.Write(">> toot to " + notification.Account.Acct);
                            Console.WriteLine(" : " + response);
                        }
                    }
                    Console.WriteLine("---");
                }
            }
        }



    }
}