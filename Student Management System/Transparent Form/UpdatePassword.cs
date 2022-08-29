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

namespace Transparent_Form
{
    public partial class UpdatePassword : Form
    {
        
        string connectiondb = @"datasource=localhost;database=studentdb;port=3306;username=root;password=";
        MySqlConnection con = null;
        public UpdatePassword()
        {
            InitializeComponent();
            customizeDesign();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        private void customizeDesign()
        {
            panel_DashboardSubmenu.Visible = false;

        }
        private void hideSubmenu()
        {
            if (panel_DashboardSubmenu.Visible == true)
                panel_DashboardSubmenu.Visible = false;
            
        }
          
        private void showSubmenu(Panel submenu)
        {
            if (submenu.Visible == false)
            {
                hideSubmenu();
                submenu.Visible = true;
            }
            else
            {
                submenu.Visible = false;
            }

        }
        private void button_std_Click(object sender,EventArgs e)
        {
           
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (txtOldPwd.Text == txtOldPwd.Text)
            {
                label5.ForeColor = Color.Black;
                label5.Text = "Please Enter Current Password!";
            }
            else
            {
                label4.ForeColor = Color.Black;
                label4.Text = "Please Enter Current Password!";
            }
        }

        private void label2_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void UpdatePassword_Load(object sender, EventArgs e)
        {
             
            try
            {
                con = new MySqlConnection(connectiondb);
                con.Open();
            }
            catch (MySqlException)
            {
                MessageBox.Show("Connection Failed");
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            

                if (txtid.Text == "")
                {
                    MessageBox.Show("Please input id do you want to update");
                    txtid.Focus();
                }
                else
                {

                    string sql = "Update student set PASSWORD='" + txtnewpwd.Text + "'where StdId = '" + txtid.Text + "'and PASSWORD ='" + txtOldPwd.Text + "'";
                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    try
                    {

                        if (cmd.ExecuteNonQuery() == 1)
                        {
                            label1.Text = "Update Paswword Successfully";
                            txtid.Focus();
                        }
                        else
                        {
                            label1.Text = "Password Dont Update";
                        }
                    }
                    catch (MySqlException)
                    {
                        MessageBox.Show("Connection Failed");
                    }

                }
            
         

            
            
           
        }

        private void button_Home_Click(object sender, EventArgs e)
        {
            hideSubmenu();

            PrintScoreForm printScore = new PrintScoreForm();
            this.Hide();
            printScore.Show();
            //UpdatePassword updatePassword = new UpdatePassword();
            //this.Hide();
            //updatePassword.Show();

        }

        private void button_login_Page_Click(object sender, EventArgs e)
        {
            hideSubmenu();

            LoginForm login = new LoginForm();
            this.Hide();
            login.Show();
        }

        private void button_refreshpage_Click(object sender, EventArgs e)
        {
            hideSubmenu();
            UpdatePassword updatePassword = new UpdatePassword();
            this.Hide();
            updatePassword.Show();
        }

        private void button_exit_Click(object sender, EventArgs e)
        {
            hideSubmenu();
            Application.Exit();
        }

        private void button_Dashboard_Click(object sender, EventArgs e)
        {
            showSubmenu(panel_DashboardSubmenu);
        }

        private void txtid1_Click(object sender, EventArgs e)
        {

        }

        private void txtid_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtnewpwd_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void txtConPwd_TextChanged(object sender, EventArgs e)
        {
            if(txtConPwd.Text == txtnewpwd.Text)
            {
                label4.ForeColor = Color.Green;
                label4.Text = "PassWord same!";
            }
            else
            {
                label4.ForeColor = Color.Red;
                label4.Text = "Worng don't same!";
            }
        }

        private void confirmpwd_Click(object sender, EventArgs e)
        {

        }

        private void button_Calc_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("file:///C:/Users/aslan/Desktop/Final%20Main/Calculyator/index.html");
        }

        private void button_Lib_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://localhost:44363/");
        }
    }
}
