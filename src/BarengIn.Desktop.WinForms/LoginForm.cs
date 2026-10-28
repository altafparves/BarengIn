using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarengIn
{
    public class LoginForm : Form
    {
        private Label lblTitle = new Label();
        private Label lblSubtitle = new Label();
        private Label lblRole = new Label();
        private Label lblUsername = new Label();
        private Label lblPassword = new Label();
        private ComboBox cbRole = new ComboBox();
        private TextBox tbUsername = new TextBox();
        private TextBox tbPassword = new TextBox();
        private Button btnLogin = new Button();
        private Button btnClose = new Button();

        public LoginForm()
        {
            Text = "BarengIn - Login";
            ClientSize = new Size(360, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);

            lblTitle.Text = "BarengIn";
            lblTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(34, 139, 94);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(20, 25);

            lblSubtitle.Text = "Carpool to campus, matched by your class schedule.";
            lblSubtitle.ForeColor = Color.Gray;
            lblSubtitle.Size = new Size(320, 45);
            lblSubtitle.Location = new Point(22, 78);

            lblRole.Text = "I am a :";
            lblRole.AutoSize = true;
            lblRole.Location = new Point(20, 130);

            cbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRole.Items.Add("Passenger");
            cbRole.Items.Add("Driver");
            cbRole.SelectedIndex = 0;
            cbRole.Location = new Point(20, 154);
            cbRole.Width = 320;

            lblUsername.Text = "Student ID (NIM) :";
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(20, 198);

            tbUsername.Location = new Point(20, 222);
            tbUsername.Width = 320;

            lblPassword.Text = "Password :";
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(20, 266);

            tbPassword.Location = new Point(20, 290);
            tbPassword.Width = 320;
            tbPassword.UseSystemPasswordChar = true;

            btnLogin.Text = "Login";
            btnLogin.Location = new Point(20, 345);
            btnLogin.Size = new Size(150, 40);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.BackColor = Color.FromArgb(34, 139, 94);
            btnLogin.ForeColor = Color.White;
            btnLogin.Click += btnLogin_Click;

            btnClose.Text = "Close";
            btnClose.Location = new Point(190, 345);
            btnClose.Size = new Size(150, 40);
            btnClose.Click += btnClose_Click;

            Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle, lblRole, cbRole,
                lblUsername, tbUsername, lblPassword, tbPassword,
                btnLogin, btnClose
            });
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (tbUsername.Text.Trim() == "" || tbPassword.Text == "")
            {
                MessageBox.Show("Please enter your NIM and password.");
                return;
            }

            HomeForm home = new HomeForm(tbUsername.Text, cbRole.Text);
            home.FormClosed += (s, args) => this.Show();
            this.Hide();
            home.Show();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}