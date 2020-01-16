using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Security.Cryptography;

namespace WindowsFormsApplication1
{
    public partial class frmMain : Form
    {
        #region Properties
        public string ValidationKey { get; set; }
        public DateTime ValidationDate { get; set; }
        DateTime FakeDate = DateTime.Parse("2100-01-01T00:00:00");
        public string UserEmail { get; set; }

        private XElement Key;
        private XElement Email;
        private XElement Date;
        private XElement Config;
        #endregion

        #region Events
        public frmMain()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            SaveToSettings();
        }

        private void txtEmail_TextChanged(object sender, EventArgs e)
        {
            UserEmail = txtEmail.Text;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            LoadSettings();
            txtEmail.Text = UserEmail;
            txtValidation.Text = ValidationKey;
        }

        private void txtValidation_TextChanged(object sender, EventArgs e)
        {
            ValidationKey = txtValidation.Text;
        }
        #endregion

        private void LoadSettings()
        {
            InitSettings();
            DateTime dt;
            UserEmail = Email.Value;
            ValidationKey = Key.Value;
            DateTime.TryParse(Date.Value, out dt);
            ValidationDate = dt;
        }

        private void InitSettings()
        {
            if (File.Exists(SettingPath()))
            {
                Config = XElement.Load(SettingPath());
            }
            else
            {
                Config = new XElement("Settings",
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XElement("UserEmail", ""),
                new XElement("VK", ""),
                new XElement("ValidationDate", ""));
            }

            Email = Config.Element("UserEmail") ?? new XElement("UserEmail", "");
            Key = Config.Element("VK") ?? new XElement("VK", "");
            Date = Config.Element("ValidationDate") ?? new XElement("ValidationDate", "");

            if (string.IsNullOrEmpty(Email.Value))
                Config.Add(Email);

            if (string.IsNullOrEmpty(Date.Value))
                Config.Add(Date);

            if (string.IsNullOrEmpty(Key.Value))
                Config.Add(Key);

        }

        private void GenerateKey()
        {
            if (!string.IsNullOrEmpty(txtEmail.Text))
            {
                ValidationDate = FakeDate;
                ValidationKey = CreateHash(Environment.MachineName + this.txtEmail.Text.Trim() + ValidationDate);
                txtValidation.Text = ValidationKey;
            }
        }

        private bool VerifyKey()
        {
            if (string.IsNullOrEmpty(UserEmail) || !DateTime.Equals(ValidationDate, FakeDate))
            {
                return false;
            }

            return Verify(Environment.MachineName + UserEmail + ValidationDate, ValidationKey);
        }

        private void SaveToSettings()
        {
            if (VerifyKey())
            {
                try
                {
                    if (Config == null || string.IsNullOrEmpty(Email.Value) || string.IsNullOrEmpty(Key.Value)
                        || string.IsNullOrEmpty(Date.Value))
                    {
                        InitSettings();
                    }

                    Email.Value = UserEmail;
                    Key.Value = ValidationKey;
                    Date.Value = ValidationDate.ToString("s");

                    XmlWriterSettings xms = new XmlWriterSettings
                    {
                        Indent = true,
                        OmitXmlDeclaration = false
                    };

                    using (XmlWriter xml = XmlWriter.Create(SettingPath(true), xms))
                    {
                        Config.WriteTo(xml);
                        xml.Flush();
                    }

                    MessageBox.Show("Key Saved");
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                GenerateKey();
                SaveToSettings();
            }
        }

        private static string SettingPath(bool Save = false)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            folder = Path.Combine(folder, "cYo");
            folder = Path.Combine(folder, "ComicRack");

            if (Save && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            folder = Path.Combine(folder, "Config.xml");

            return folder;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        #region Password
        private static readonly HashAlgorithm algorithm = new SHA1Managed();

        public static byte[] CreateByteHash(string text)
        {
            return CreateByteHash(Encoding.UTF8.GetBytes(text));
        }

        public static byte[] CreateByteHash(byte[] text)
        {
            if (text.Length == 0)
            {
                return new byte[0];
            }
            return algorithm.ComputeHash(text);
        }

        public static string CreateHash(string text)
        {
            return Convert.ToBase64String(CreateByteHash(text));
        }

        public static bool Verify(string text, string hashValue)
        {
            return (!string.IsNullOrEmpty(hashValue) && hashValue.Equals(CreateHash(text)));
        }
        #endregion

    }
}
