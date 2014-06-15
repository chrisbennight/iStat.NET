using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace istatServer
{
    public partial class Settings : Form
    {
        private readonly string _pinFileFullPath;
        private readonly string _clientFileFullPath;


        public class AuthCodeChangedArgs : EventArgs
        {
            public string AuthCode { get; set; }
        }
        public event EventHandler<AuthCodeChangedArgs> AuthCodeChanged;


        public event EventHandler AuthReset;

        public void OnAuthReset()
        {
            if (AuthReset != null)
                AuthReset(this, null);
        }

        public void OnAuthCodeChanged(string authCode)
        {
            if (AuthCodeChanged != null) 
                AuthCodeChanged(this, new AuthCodeChangedArgs{AuthCode = authCode});
        }

        public Settings(string pinFileFullPath, string clientFileFullPath)
        {
            InitializeComponent();
            _pinFileFullPath = pinFileFullPath;
            _clientFileFullPath = clientFileFullPath;


            pinText.KeyPress += PinTextKeyPress;
            if (File.Exists(pinFileFullPath))
                pinText.Text = File.ReadAllText(pinFileFullPath);
        }

        private void PinTextKeyPress(object sender, KeyPressEventArgs e)
        {
            //only allow numeric value
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        
        private void PinTextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox) sender;
            int val;
            //enable button if 5 numeric values are in the textbox
            if (tb.Text != null && tb.Text.Length == 5 && int.TryParse(tb.Text, out val))
            {
                saveButton.Enabled = true;
            }
            else
            {
                saveButton.Enabled = false;
            }
        }

        private void SaveClick(object sender, EventArgs e)
        {
            if (File.Exists(_pinFileFullPath))
                File.Delete(_pinFileFullPath);
            File.WriteAllText(_pinFileFullPath, pinText.Text, Encoding.UTF8);
            OnAuthCodeChanged(pinText.Text);
            this.Hide();
        }

        private void CancelClick(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void ResetAuthButtonClick(object sender, EventArgs e)
        {
            if (File.Exists(_clientFileFullPath))
                File.Delete(_clientFileFullPath);
        }
    }
}
