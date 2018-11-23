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
    public partial class Form1 : Form
    {
        static Cluster cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
        static ISession session = cluster.Connect("vkort");

        public Form1()
        {
            InitializeComponent();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        //Registration
        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text) &&
                !string.IsNullOrWhiteSpace(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox3.Text))
            {
                string usr_email = textBox1.Text;
                string usr_name = textBox2.Text;
                string usr_surname = textBox2.Text;

                string statement = "INSERT INTO usr(usr_email, usr_name, usr_surname, usr_cards) VALUES (?,?,?,?) IF NOT EXISTS;";
                PreparedStatement preparedStatement = session.Prepare(statement);
                string[] arr = new string[1];
                arr[0] = "";


                BoundStatement boundStatement = preparedStatement.Bind(usr_email, usr_name, usr_surname, arr);
                var rowSet = session.Execute(boundStatement);
                foreach (var row in rowSet)
                {
                    if (row[0].ToString() == "False") { label7.Text = "Email is in use"; }
                    else
                    {
                        User user = new User
                        {
                            email = usr_email
                        };
                        UserSession.Login(user);

                        Form2 f = new Form2();
                        Hide();
                        f.ShowDialog();
                        Close();
                    }
                }
            }
            else
            {
                label7.Text = "Fill out all required fields";
                return;
            }
        }

        //Login
        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox4.Text))
            {
                string usr_email = textBox4.Text;

                string statement = "SELECT usr_email FROM usr WHERE usr_email = ? ;";
                PreparedStatement preparedStatement = session.Prepare(statement);
                BoundStatement boundStatement = preparedStatement.Bind(usr_email);
                var email = session.Execute(boundStatement);
                int count = 0;
                foreach (var row in email)
                {
                    usr_email = row.GetValue<string>("usr_email");
                    count++;
                }
                if (count == 0)
                {
                    label7.Text = "Enter valid email address";
                    return;
                }


                UserSession.Logout();
                User user = new User
                {
                    email = usr_email
                };
                UserSession.Login(user);

                Form2 f = new Form2();
                Hide();
                f.ShowDialog();
                Close();
            }
            else
            {
                label7.Text = "Fill out all required fields";
                return;
            }

        }

        public static ISession sendSession
        {
            get { return session; }
        }



    }
}
