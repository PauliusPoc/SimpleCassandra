using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cassandra;

namespace CassandraForms
{
    public partial class Form2 : Form
    {
        Form1 frm = new Form1();
        static ISession session = Form1.sendSession;
        User user = UserSession.GetUser();

        public Form2()
        {
            InitializeComponent();
            setUserInfo();
        }

        //Add new card
        private void button1_Click(object sender, EventArgs e)
        {
            string[] arr = new string[1];
            arr[0] = textBox1.Text;

            var map = new Dictionary<string, int>();
            map.Add("0", 0);
            string statement = "INSERT INTO cards(card_id, card_tickets, active_ticket, money) VALUES (?,?,?, 500) IF NOT EXISTS;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(arr[0], map, "");
            var rowSet = session.Execute(boundStatement);
            foreach (var row in rowSet)
            {
                if (row[0].ToString() == "False") { label11.Text = "Card is already added"; return; }
            }

            statement = "UPDATE usr SET usr_cards = usr_cards + ? WHERE usr_email = ? IF EXISTS;";
            preparedStatement = session.Prepare(statement);

            boundStatement = preparedStatement.Bind(arr, user.email);
            session.Execute(boundStatement);
            setUserInfo();
            label11.Text = "Card is added";
        }

        //Buy new ticket
        private void button2_Click(object sender, EventArgs e)
        {
            string type = comboBox3.SelectedItem.ToString();

            getBoughtTickets();
            int ammount = int.Parse(textBox2.Text);
            float price = getFullPrice();
            string card_id = comboBox1.SelectedItem.ToString();
            

            string statement = "SELECT money FROM cards WHERE card_id = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement= preparedStatement.Bind(card_id);
            var rowSet = session.Execute(boundStatement);
            foreach (var row in rowSet)
            {
                float money = row.GetValue<float>("money");
                if (money >= price)
                {
                    if (user.tickDict != null)
                    {
                        if (user.tickDict.ContainsKey(type)) //if exists add to current ammount
                            user.tickDict[type] = user.tickDict[type] + int.Parse(textBox2.Text);
                        else user.tickDict.Add(type, ammount); // ticket_type, ammount

                        statement = "INSERT INTO cards(card_id, card_tickets, money) VALUES (?,?,?);";
                        preparedStatement = session.Prepare(statement);
                        boundStatement = preparedStatement.Bind(card_id, user.tickDict, money - price ); // combobox = cardnumbnumber
                        session.Execute(boundStatement);
                        label11.Text = "Tickets bought";
                    }
                    else
                    {
                        user.tickDict.Add(type, int.Parse(textBox2.Text));

                        statement = "INSERT INTO cards(card_id, card_tickets, money) VALUES (?,?,?);";
                        preparedStatement = session.Prepare(statement);
                        boundStatement = preparedStatement.Bind(user.tickDict, comboBox1.SelectedItem.ToString(), money - price); // combobox = cardnumbnumber
                        session.Execute(boundStatement);
                        label11.Text = "Tickets bought";
                    }

                    setUserInfo();
                }
                else label11.Text = "Not enough money";
            }
            
        }

        //Activate ticket
        private void button3_Click(object sender, EventArgs e)
        {
            string type = comboBox4.SelectedItem.ToString();
            string card_id = comboBox2.SelectedItem.ToString();

            getBoughtTickets();

            isActive(card_id);

            string statement = "SELECT active_ticket FROM cards WHERE card_id = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(card_id);

            var rowSet = session.Execute(boundStatement);
            foreach (var row in rowSet)
            {
                if (row[0].ToString() == "")
                {
                    statement = "SELECT time FROM tickets WHERE ticket_type = ? ;";
                    preparedStatement = session.Prepare(statement);
                    boundStatement = preparedStatement.Bind(type);

                    var times = session.Execute(boundStatement);
                    string active_time = "";
                    foreach (var time in times) active_time = time.GetValue<string>("time");


                    //Prepare the statements involved in a profile update once
                    var insertAticket = session.Prepare("INSERT INTO cards(card_id, active_ticket) VALUES(?,?)");
                    DateTime due = DateTime.Now;
                    due = due.AddSeconds(int.Parse(active_time));
                    var createAticket = session.Prepare("INSERT INTO active_ticket(aticket_id, aticket_type, owner, datetime) VALUES (now(),?,?,?) USING TTL " + active_time + "");
                    user.tickDict[type] = user.tickDict[type] - 1;
                    var decreaseTickets = session.Prepare("INSERT INTO cards(card_id, card_tickets) VALUES (?,?)");
                    

                    var batch = new BatchStatement()
                        .Add(insertAticket.Bind(card_id, type))
                        .Add(createAticket.Bind(type, card_id, due))
                        .Add(decreaseTickets.Bind(card_id, user.tickDict));
                    
                    session.Execute(batch);


                    label11.Text = "Ticket was activated succesfully ";
                }
                else label11.Text = "Ticket is already active";
            }
            
        }

        public void setUserInfo()
        {
            //Get all user cards
            string statement = "SELECT usr_cards FROM usr WHERE usr_email = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(user.email);
            var cards = session.Execute(boundStatement);
            foreach (var row in cards) user.cardArr = row.GetValue<List<string>>("usr_cards");

            comboBox1.DataSource = user.cardArr;
            comboBox2.DataSource = user.cardArr;
            comboBox5.DataSource = user.cardArr;
            comboBox6.DataSource = user.cardArr;

            //Get ticket types
            comboBox3.Items.Clear();
            cards = session.Execute("SELECT ticket_type FROM tickets");
            List<string> tarr = new List<string>();
            foreach (var row in cards)
                comboBox3.Items.Add(row.GetValue<string>("ticket_type"));
        }

        public void isActive(string card_id)
        {
            //check if still active, set new value if not active
            string statement = "SELECT count(*) FROM active_ticket WHERE owner = ?;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(card_id);
            var active = session.Execute(boundStatement);
            int count = 0;
            foreach (var elem in active)
            {
                string lol = elem[0].ToString();
                if (elem[0].ToString() == "0")
                {
                    statement = "INSERT INTO cards (card_id, active_ticket) VALUES (?,?) ;";
                    preparedStatement = session.Prepare(statement);
                    boundStatement = preparedStatement.Bind(card_id, "");
                    session.Execute(boundStatement);
                    count++;
                }
            }

            if (count == 1) { label10.Text = "Inactive"; label15.Text = "Inactive"; label17.Text = comboBox5.SelectedItem.ToString(); }
            else
            {
                statement = "SELECT aticket_type, datetime FROM active_ticket WHERE owner = ?;";
                preparedStatement = session.Prepare(statement);
                boundStatement = preparedStatement.Bind(card_id);
                var rowSet = session.Execute(boundStatement);
                foreach (var row in rowSet)
                {
                    label17.Text = comboBox5.SelectedItem.ToString();
                    label10.Text = row.GetValue<string>("aticket_type");
                    label15.Text = row.GetValue<DateTime>("datetime").ToString();
                }
            }

            //get money adn set to Balance label
            label2.Text = getMoney(card_id).ToString();
        }

        //Add money
        private void button6_Click(object sender, EventArgs e)
        {
            string card_id = comboBox6.SelectedItem.ToString();
            float curmoney = getMoney(card_id);
            float addmoney = float.Parse(textBox3.Text);

            string statement = "INSERT INTO cards(card_id, money) VALUES (?,?);";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(card_id, curmoney + addmoney);
            session.Execute(boundStatement);
            label11.Text = "Money added";

            isActive(card_id);
            setUserInfo();

        }

        public float getMoney(string card_id)
        {
            string statement = "SELECT money FROM cards WHERE card_id = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(card_id);
            var moneySet = session.Execute(boundStatement);
            float value = 0;
            foreach (var el in moneySet)
                value = el.GetValue<float>("money");
            return value;
        }

        public void getBoughtTickets()
        {
            comboBox4.Items.Clear();
            //FIND ALL HAVING TICKET IDs
            string statement = "SELECT card_tickets FROM cards WHERE card_id = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(comboBox2.Text);
            var rowSet = session.Execute(boundStatement);
            var tickets = new SortedDictionary<string, int>(); // <ticket_type_ID, ammount> TODO change ID to TYPE
            foreach (var row in rowSet)
                user.tickDict = row.GetValue<SortedDictionary<string, int>>("card_tickets");
            if (user.tickDict != null)
            {
                foreach (var entry in user.tickDict)
                    comboBox4.Items.Add(entry.Key);
            }
            
        }

        public void getTicketsAmmount()
        {
            label12.Text = user.tickDict[comboBox4.SelectedItem.ToString()].ToString();
        }

        public float getFullPrice()
        {
            float price = 0;
            string type = comboBox3.SelectedItem.ToString();
            int ammount = int.Parse(textBox2.Text);

            string statement = "SELECT ticket_price FROM tickets WHERE ticket_type = ? ;";
            PreparedStatement preparedStatement = session.Prepare(statement);
            BoundStatement boundStatement = preparedStatement.Bind(type);
            var priceRow = session.Execute(boundStatement);
            foreach (var elem in priceRow) price = elem.GetValue<float>("ticket_price");



            return price * ammount;
        }
        
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox2.SelectedItem.ToString() != "") getBoughtTickets();
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            getTicketsAmmount();
        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            isActive(comboBox5.SelectedItem.ToString());
            setUserInfo();
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            
        }

        

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            label19.Text = getFullPrice().ToString();
        }

        
    }
}
