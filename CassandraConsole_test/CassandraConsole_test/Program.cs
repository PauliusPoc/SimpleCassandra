using Cassandra;
using Cassandra.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CassandraConsole_test
{
    class Program
    {
        static Cluster cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
        static ISession session = cluster.Connect("vkort");

        static void Main()
        {
            /*Cluster cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
            ISession session = cluster.Connect("vkort");
            var jonasdx = session.Prepare("INSERT INTO usr(" +
                "usr_ID, usr_Name, usr_Surname) values (?, ?, ?);");
            var statement = jonasdx.Bind(1, "jonas", "dx");
            var statement2 = jonasdx.Bind(1, "jonas", "dx");
            session.Execute(statement);*/
            //NewUser();
            string response;
            while (!UserSession.CheckState())
            {
                Console.Clear();
                Console.WriteLine("1) Register\n2) Login");
                response = Console.ReadLine();
                if (response == "1") NewUser();
                else if (response == "2") while(Login() == 0);
            }

            Console.WriteLine("1) Add new card\n2) Buy tickets\n3) Activate ticket");
            response = Console.ReadLine();
            if (response == "1") AddCard();
            if (response == "2") BuyTicket();
            if (response == "3") ActivateTicket();









        }

        public static void CreateKeyspace()
        {
            Cluster cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
            ISession session = cluster.Connect();
            //session.Execute("CREATE KEYSPACE vkort WITH REPLICATION = { 'class' : 'SimpleStrategy', 'replication_factor' : 1 };");
            session = cluster.Connect("vkort");
            session.Execute("CREATE TABLE " +
                "usr (" +
                "usr_email text PRIMARY KEY," +
                "usr_name text," +
                "usr_surname text," +
                "usr_cards set<text>);");

            session.Execute("CREATE TABLE " +
                "cards (" +
                "card_ID text PRIMARY KEY," +
                "card_name text," +
                "card_owner text," +
                "card_tickets map<text,int>);");

            session.Execute("CREATE TABLE " +
                "tickets (" +
                "ticket_ID int PRIMARY KEY," +
                "ticket_type text);");
        }

        public static void NewUser()
        {
            string usr_email;
            string usr_name;
            string usr_surname;

            #region Enter Info
            Console.WriteLine("Enter your email");
            usr_email = Console.ReadLine();
            Console.WriteLine("Enter your name");
            usr_name = Console.ReadLine();
            Console.WriteLine("Enter your surname");
            usr_surname = Console.ReadLine();
            #endregion

            string statement = "INSERT INTO usr(usr_email, usr_name, usr_surname, usr_cards) VALUES (?,?,?,?) IF NOT EXISTS;";
            //TODO IF EXISTS
            PreparedStatement preparedStatement = session.Prepare(statement);
            string[] arr = new string[1];
            arr[0] = "";

            BoundStatement boundStatement = preparedStatement.Bind(usr_email, usr_name, usr_surname, arr);
            session.Execute(boundStatement);

            User user = new User
            {
                email = usr_email
            };
            UserSession.Login(user);
        }

        public static int Login()
        {
            string usr_email;

            Console.WriteLine("\nEnter your email to login");
            Console.WriteLine("To exit login page, insert '1'");
            usr_email = Console.ReadLine();
            if (usr_email == "1") return 1;
            string statement = "SELECT usr_email FROM usr WHERE usr_email = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(usr_email);
            var email = session.Execute(boundStatement);
            int count = 0;
            foreach (var row in email)
            {
                usr_email= row.GetValue<string>("usr_email");
                count++;
            }
            if (count == 0)
            {
                Console.WriteLine("Enter valid email address");
                return 0;
            }
            

            UserSession.Logout();
            User user = new User
            {
                email = usr_email
            };
            UserSession.Login(user);
            return 2;
        }

        public static void AddCard()
        {
            string[] arr = new string[1];
            Console.WriteLine("Enter your card number");
            arr[0] = Console.ReadLine();

            var map = new Dictionary<string, int>();
            map.Add("0", 0);
            string statement = "INSERT INTO cards(card_id, card_tickets) VALUES (?,?) IF NOT EXISTS;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(arr[0], map);
            session.Execute(boundStatement);

            statement = "UPDATE usr SET usr_cards = usr_cards + ? WHERE usr_email = ? IF EXISTS;";
            preparedStatement = session.Prepare(statement);

            boundStatement = preparedStatement.Bind(arr, UserSession.GetUser().email);
            session.Execute(boundStatement);
            Console.ReadLine();
        }

        public static void BuyTicket()
        {
            string response;
            string statement = "SELECT usr_cards FROM usr WHERE usr_email = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(UserSession.GetUser().email);
            var cards = session.Execute(boundStatement);
            List<string> arr = new List<string>();
            foreach (var row in cards) arr = row.GetValue<List<string>>("usr_cards");

            int count = 0;
            Console.WriteLine("Select wanted cards number\n");
            foreach (var elem in arr)
            {
                if (count != 0) Console.WriteLine(count.ToString() + ") " + "{0}", elem);
                count++;
            }
            count = 0;
            int card_id = int.Parse(Console.ReadLine());

            cards = session.Execute("SELECT ticket_type FROM tickets");
            List<string> tarr = new List<string>();
            foreach (var row in cards) tarr.Add(row.GetValue<string>("ticket_type"));

            Console.WriteLine("Select wanted ticket's number\n");
            foreach (var elem in tarr)
            {
                if (count != 0) Console.WriteLine(count.ToString() + ") " + "{0}", elem);
                count++;
            }
            response = Console.ReadLine(); 

            NewTicket(arr[card_id], response);
        }

        public static void NewTicket(string card_id, string t_id)
        {
            Console.WriteLine("Ammount:");
            int qnty = int.Parse(Console.ReadLine());
            var map = new Dictionary<string, int>();

            string statement = "SELECT card_tickets FROM cards WHERE card_id = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(card_id);
            var tickets = session.Execute(boundStatement);
            var tick = new SortedDictionary<string, int>();
            foreach (var row in tickets)
            {
                tick = row.GetValue<SortedDictionary<string,int>>("card_tickets");
            }
            //if(tick[t_id] == null)
            map.Add(t_id, qnty);
            //map.Add(t_id, tick[t_id] + qnty);
            statement = "INSERT INTO cards(card_id, card_tickets) VALUES (?,?);";
            preparedStatement = session.Prepare(statement);
            boundStatement = preparedStatement.Bind(card_id, map);
            session.Execute(boundStatement);

            Main();
        }

        public static void ActivateTicket()
        {
            string statement = "SELECT usr_cards FROM usr WHERE usr_email = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(UserSession.GetUser().email);
            var cards = session.Execute(boundStatement);
            List<string> arr = new List<string>();
            foreach (var row in cards) arr = row.GetValue<List<string>>("usr_cards");

            Console.WriteLine("Select wanted cards number:\n");
            int count = 0;
            foreach (var elem in arr)
            {
                if(count != 0) Console.WriteLine(count.ToString() + ") " + "{0}", elem);
                count++;
            }
            count = 0;
            int card_id = int.Parse(Console.ReadLine());
            string card_id_real = arr[card_id];

            var ticket_map = new Dictionary<string, string>(); // <ID,TYPE>
            cards = session.Execute("SELECT * FROM tickets");
            List<string> tarr = new List<string>();
            foreach (var row in cards) ticket_map.Add(row.GetValue<string>("ticket_id"), row.GetValue<string>("ticket_type"));

            //FIND ALL HAVING TICKET IDs
            statement = "SELECT card_tickets FROM cards WHERE card_id = ? ;";
            preparedStatement = session.Prepare(statement);
            boundStatement = preparedStatement.Bind(card_id_real);
            var tickets = session.Execute(boundStatement);
            var tick = new SortedDictionary<string, int>();
            foreach (var row in tickets) tick = row.GetValue<SortedDictionary<string, int>>("card_tickets");
            //tick card tickets
            //ticket_map < ID,TYPE >
            //tarr ticket types
            //FIND ALL TICKET TYPES


             Console.WriteLine("Select wanted ticket's number\n");
            List<string> t_existing = new List<string>();
            foreach (var item in tick)
            {
                Console.WriteLine(count.ToString() + ") " + "{0}", ticket_map[item.Key]);
                t_existing.Add(item.Key);
                count++;
            }

            
            foreach ( var item in ticket_map)
            {
                
            }


            string response = Console.ReadLine();
            string t_id = t_existing[int.Parse(response)];

            

            NewATicket(arr[card_id], t_id);
        }

        public static void NewATicket(string card_id, string t_id)
        {

            string statement = "SELECT card_tickets FROM cards WHERE card_id = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(card_id);
            var tickets = session.Execute(boundStatement);
            var tick = new SortedDictionary<string, int>();
            foreach (var row in tickets)
            {
                tick = row.GetValue<SortedDictionary<string, int>>("card_tickets");
            }

            ;
            var map = new Dictionary<string, int>();
            map.Add(t_id, tick[t_id] - 1);
            
            


            statement = "SELECT ticket_type, time FROM tickets WHERE ticket_id = ? ;";
            preparedStatement = session.Prepare(statement);
            boundStatement = preparedStatement.Bind(t_id);
            var types = session.Execute(boundStatement);
            string active_type = "";
            string active_time = "";
            foreach (var row in types)
            {
                active_type = row.GetValue<string>("ticket_type");
                active_time = row.GetValue<string>("time");
            }


            statement = "INSERT INTO cards(card_id, active_ticket) VALUES (?,?);";
            preparedStatement = session.Prepare(statement);
            boundStatement = preparedStatement.Bind(card_id, active_type);
            session.Execute(boundStatement);

            statement = "INSERT INTO active_ticket(aticket_id, aticket_type) VALUES (now(),?) USING TTL " + active_time + " ;";
            preparedStatement = session.Prepare(statement);
            boundStatement = preparedStatement.Bind(active_type);
            session.Execute(boundStatement);
        }
    }

    

    
}
/*
 * 
 * Turim Ticket <ID, qty>           Ticket (ID, TYPE)
 * islistinam turimus ticketus (rodo tik su >0 is turimu ticketu istrinam jeigu qty tampa 0)
 * uzcashinam turimu ticketu ID masyve
 * pasirenkam kuri aktyvuoti is israsyto listo
 * turimiticketai[ listonr -1 ] pasirenki ticketa
 * Aktyvuojam:
 * Pagal ID sukuriam aktyvuota bilieta ir pagal tai nusprendžiam koks jo ttl
 * Ticket <ID, qty - 1>
 *
 */