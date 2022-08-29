using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using DGVPrinterHelper;


namespace Transparent_Form
{
    public partial class PrintScoreForm : Form
    {
        ScoreClass score = new ScoreClass();
        DGVPrinter printer = new DGVPrinter();
        public PrintScoreForm()
        {
            InitializeComponent();
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            DataGridView_score.DataSource = score.getList(new MySqlCommand("SELECT score.StudentId, student.StdFirstName, student.StdLastName, score.CourseName, score.Score, score.Description FROM student INNER JOIN score ON score.StudentId = student.StdId WHERE CONCAT(student.StdFirstName, student.StdLastName, score.CourseName)LIKE '%" + textBox_search.Text + "%'"));
        }

        private void button_print_Click(object sender, EventArgs e)
        {
            //We need DGVprinter helper for print pdf file
            printer.Title = "Mdemy Student score list";
            printer.SubTitle = string.Format("Date: {0}", DateTime.Now.Date);
            printer.SubTitleFormatFlags = StringFormatFlags.LineLimit | StringFormatFlags.NoClip;
            printer.PageNumbers = true;
            printer.PageNumberInHeader = false;
            printer.PorportionalColumns = true;
            printer.HeaderCellAlignment = StringAlignment.Near;
            printer.Footer = "Mdemy";
            printer.FooterSpacing = 15;
            printer.printDocument.DefaultPageSettings.Landscape = true;
            printer.PrintDataGridView(DataGridView_score);
        }

        private void PrintScoreForm_Load(object sender, EventArgs e)
        {
            showScore();
        }
        //to show score list
       
        public void showScore()
        {
            DataGridView_score.DataSource = score.getList(new MySqlCommand("SELECT score.StudentId,student.StdFirstName,student.StdLastName,score.CourseName,score.Score,score.Description FROM student INNER JOIN score ON score.StudentId = student.StdId"));
            
            
            //SqlConnection con = new SqlConnection(conString);
            //con.Open();
            //if (con.State == System.Data.ConnectionState.Open)
            //{
            //    SqlDataAdapter sda = new SqlDataAdapter("SELECT eb_number, Sign_In, Sign_out, Date FROM sign_In_Out_Table WHERE eb_number='" + Username_Alerts_lbl.Text + "'", con);
            //    sda.SelectCommand.Parameters.AddWithValue("eb_number", Username_Alerts_lbl.Text);
            //    DataTable dt = new DataTable();
            //    sda.Fill(dt);
            //    dataGridView1.DataSource = dt;
            //    {

            //    }

            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoginForm login = new LoginForm();
            this.Hide();
            login.Show();
        }

        private void textBox_search_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox_stdId_TextChanged(object sender, EventArgs e)
        {
                
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox_stdId_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button_Update_Click(object sender, EventArgs e)
        {
            if(textBox_stdId.Text=="")
            {
                MessageBox.Show("Please input id do you want to update");
                textBox_stdId.Focus();
            }
            else
            {
                UpdatePassword updatePassword = new UpdatePassword();
                this.Hide();
                updatePassword.Show();

            }



        }

        private void DataGridView_score_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        

      

        

        private void button_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PrintScoreForm printScore = new PrintScoreForm();
            printScore.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("file:///C:/Users/aslan/Desktop/Final%20Main/Calculyator/index.html");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://localhost:44363/");
        }
    }
}
