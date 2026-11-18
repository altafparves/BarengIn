using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarengIn
{
    public class HomeForm : Form
    {
        private Label lblWelcome = new Label();
        private Label lblOrigin = new Label();
        private Label lblFaculty = new Label();
        private Label lblTime = new Label();
        private Label lblCo2 = new Label();
        private TextBox tbOrigin = new TextBox();
        private ComboBox cbFaculty = new ComboBox();
        private DateTimePicker dtpTime = new DateTimePicker();
        private ListView lvRides = new ListView();
        private Button btnFind = new Button();
        private Button btnRequest = new Button();
        private Button btnLogout = new Button();
        private double co2Saved = 0;

        public HomeForm(string nim, string role)
        {
            Text = "BarengIn - Find a Ride";
            ClientSize = new Size(560, 450);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);

            lblWelcome.Text = "Hi, " + nim + " (" + role + ")";
            lblWelcome.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblWelcome.ForeColor = Color.FromArgb(34, 139, 94);
            lblWelcome.AutoSize = true;
            lblWelcome.Location = new Point(20, 15);

            lblOrigin.Text = "Pickup area :";
            lblOrigin.AutoSize = true;
            lblOrigin.Location = new Point(20, 63);
            tbOrigin.Location = new Point(140, 60);
            tbOrigin.Width = 200;

            lblFaculty.Text = "Destination :";
            lblFaculty.AutoSize = true;
            lblFaculty.Location = new Point(20, 98);
            cbFaculty.DropDownStyle = ComboBoxStyle.DropDownList;
            cbFaculty.Items.Add("Engineering (FT)");
            cbFaculty.Items.Add("MIPA");
            cbFaculty.Items.Add("Economics and Business (FEB)");
            cbFaculty.Items.Add("Medicine");
            cbFaculty.Items.Add("Social and Political Sciences");
            cbFaculty.SelectedIndex = 0;
            cbFaculty.Location = new Point(140, 95);
            cbFaculty.Width = 200;

            lblTime.Text = "Class starts :";
            lblTime.AutoSize = true;
            lblTime.Location = new Point(20, 133);
            dtpTime.Format = DateTimePickerFormat.Time;
            dtpTime.ShowUpDown = true;
            dtpTime.Location = new Point(140, 130);
            dtpTime.Width = 110;

            btnFind.Text = "Find Rides";
            btnFind.Location = new Point(370, 85);
            btnFind.Size = new Size(170, 45);
            btnFind.FlatStyle = FlatStyle.Flat;
            btnFind.BackColor = Color.FromArgb(34, 139, 94);
            btnFind.ForeColor = Color.White;
            btnFind.Click += btnFind_Click;

            lvRides.View = View.Details;
            lvRides.FullRowSelect = true;
            lvRides.GridLines = true;
            lvRides.MultiSelect = false;
            lvRides.Columns.Add("Driver", 120);
            lvRides.Columns.Add("Faculty", 150);
            lvRides.Columns.Add("Departs", 80);
            lvRides.Columns.Add("Seats", 60);
            lvRides.Columns.Add("Cost", 90);
            lvRides.Location = new Point(20, 180);
            lvRides.Size = new Size(520, 195);

            btnRequest.Text = "Request Ride";
            btnRequest.Location = new Point(20, 392);
            btnRequest.Size = new Size(150, 40);
            btnRequest.FlatStyle = FlatStyle.Flat;
            btnRequest.BackColor = Color.FromArgb(34, 139, 94);
            btnRequest.ForeColor = Color.White;
            btnRequest.Click += btnRequest_Click;

            lblCo2.Text = "Estimated CO2 saved: 0.0 kg";
            lblCo2.AutoSize = true;
            lblCo2.Location = new Point(190, 402);

            btnLogout.Text = "Logout";
            btnLogout.Location = new Point(440, 392);
            btnLogout.Size = new Size(100, 40);
            btnLogout.Click += btnLogout_Click;

            Controls.AddRange(new Control[] {
                lblWelcome, lblOrigin, tbOrigin, lblFaculty, cbFaculty,
                lblTime, dtpTime, btnFind, lvRides,
                btnRequest, lblCo2, btnLogout
            });
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            if (tbOrigin.Text.Trim() == "")
            {
                MessageBox.Show("Please enter your pickup area.");
                return;
            }

            lvRides.Items.Clear();
            string faculty = cbFaculty.Text;
            DateTime t = dtpTime.Value;

            string[,] rides = {
                { "Altaf", "30 min", "3", "Rp 5.000" },
                { "Dimas", "45 min", "2", "Rp 7.000" },
                { "Diffie", "60 min", "1", "Rp 6.000" },
                
            };

            for (int i = 0; i < rides.GetLength(0); i++)
            {
                int minutesBefore = int.Parse(rides[i, 1].Replace(" min", ""));
                string departs = t.AddMinutes(-minutesBefore).ToString("HH:mm");

                ListViewItem item = new ListViewItem(rides[i, 0]);
                item.SubItems.Add(faculty);
                item.SubItems.Add(departs);
                item.SubItems.Add(rides[i, 2]);
                item.SubItems.Add(rides[i, 3]);
                lvRides.Items.Add(item);
            }
        }

        private void btnRequest_Click(object sender, EventArgs e)
        {
            if (lvRides.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a ride first.");
                return;
            }

            string driver = lvRides.SelectedItems[0].Text;
            MessageBox.Show("Request sent to " + driver + ". Waiting for driver approval.");

            co2Saved += 1.2;
            lblCo2.Text = "Estimated CO2 saved: " + co2Saved.ToString("0.0") + " kg";
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}