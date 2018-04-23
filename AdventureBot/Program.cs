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
        internal static string Username = "YOUR_EMAIL_HERE";
        internal static string Password = "YOUR_PASSWORD_HERE";
        internal static string InstanceName = "botsin.space";
        internal static int XSize = 3;
        internal static int YSize = 3;
        internal static int ZSize = 3;

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
                var dungeon = new Dungeon();
       
                var authorize = new Authorize();
                await authorize.CreateApp(Program.InstanceName, "Tootnet", Scope.Read | Scope.Write | Scope.Follow);

                Console.WriteLine("Logging in " + Program.Username);
                var tokens = await authorize.AuthorizeWithEmail(Program.Username, Program.Password);

                //Console.WriteLine("toot [status]");
                Console.WriteLine("notification - list notifs");
                Console.WriteLine("follows -  send message to all followers");
                Console.WriteLine("reply - send message to all recent mentions");
                Console.WriteLine("createnew - wipe and create new dungeon");
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

                            Console.WriteLine(post.Account.DisplayName + "\t\t" + post.Account.Acct);
                            Console.WriteLine(TagRegex.Replace(post.Content.Replace("<br />", "\n"), "").Trim());
                            Console.WriteLine(post.CreatedAt);
                            break;

                        case "notification":
                            var notifications = await tokens.Notifications.GetAsync();
                            Console.WriteLine("--------------------");
                            foreach (var notification in notifications)
                            {
                                Console.WriteLine(notification.Account.DisplayName + "\t\t" + notification.Account.Acct);
                                Console.WriteLine(notification.Type);
                                if (notification.Type == "mention" || notification.Type == "reblog" ||
                                    notification.Type == "favourite")
                                {
                                    Console.WriteLine("==========");
                                    Console.WriteLine(notification.Status.Account.DisplayName + "\t\t" +
                                                      notification.Status.Account.Acct);
                                    Console.WriteLine(TagRegex
                                        .Replace(notification.Status.Content.Replace("<br />", "\n"), "").Trim());
                                    Console.WriteLine("==========");
                                }
                                Console.WriteLine(notification.CreatedAt);
                                Console.WriteLine("--------------------");
                            }
                            break;

                        case "follows":
                            Console.WriteLine("Checking for followers...");
                            var toReplyF = await tokens.Notifications.GetAsync();
                            string messageToSendF = "Hello, adventurer! \r" +
                                                    "You find yourself in '" +
                                                    dungeon.GetRoomName(0, 0, 0) +
                                                    "\r" + dungeon.GetRoomExits(0, 0, 0);
                            foreach (var notification in toReplyF)
                            {
                                if (notification.Type == "follow")
                                {
                                    //await tokens.Statuses.PostAsync(status => "@"+notification.Account.Acct+" "+messageToSendF);
                                    await tokens.Statuses.PostAsync(status => "@" + notification.Account.Acct + " " + messageToSendF, in_reply_to_account_id => notification.Account.Id, visibility => "private");
                                    Console.WriteLine("Sending toot to " + notification.Account.Acct);
                                }
                            }
                            //Thread.Sleep(900000);  // 15 mins
                            //await Task.Delay(20);
                            break;

                        case "reply":
                            Console.WriteLine("Checking for mentions...");
                            var toReply = await tokens.Notifications.GetAsync();
                            var dateAnHourAgo = DateTime.Now.AddHours(-2);
                            var dateOneAnd34HoursAgo = DateTime.Now.AddMinutes(-105);
                            string messageToSend = "Hello, adventurer! You find yourself in '" +
                                                   language.GetARandomLocationName() + "'. ";
                            foreach (var notification in toReply)
                            {
                                if (notification.Type == "mention")
                                {
                                    if (notification.CreatedAt > dateAnHourAgo && notification.CreatedAt < dateOneAnd34HoursAgo)
                                    {
                                        await tokens.Statuses.PostAsync(status => messageToSend);
                                        Console.WriteLine("Sending toot to " + notification.Status.Account.DisplayName);
                                    }
                                }
                            }
                            //Thread.Sleep(900000);  // 15 mins
                            //await Task.Delay(20);
                            break;

                        case "createnew":
                            dungeon.CreateDungeon();
                            break;
                    }
                    Console.Write("command: ");
                    command = Console.ReadLine().Trim().Split(' ', 2);
                    commandWord = command.First().ToLower();
                }
            } // end of db using
        }
    }
}