using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FirebirdSql.Data.FirebirdClient;


namespace GedNavigator
{
    public partial class frmMain : Form
    {
        FbConnection fb;
        public frmMain()
        {
            InitializeComponent();
        }

        private void btnSelectDB_Click(object sender, EventArgs e)
        {
            OpenFileDialog oflDatabase = new OpenFileDialog();
            oflDatabase.Filter = "Firebird Database Files|*.fdb"; ;
            if (oflDatabase.ShowDialog() == DialogResult.OK)
            {
                tbDatabase.Text = oflDatabase.FileName;
            }

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //формируем connection string для последующего соединения с нашей базой данных
            FbConnectionStringBuilder fb_con = new FbConnectionStringBuilder();
            fb_con.Charset = "WIN1251"; //используемая кодировка
            fb_con.UserID = "SYSDBA"; //логин
            fb_con.Password = "masterkey"; //пароль
            fb_con.Database = tbDatabase.Text;
            fb_con.Dialect = 3;
            //указываем тип сервера (0 - "полноценный Firebird" (classic или super server), 1 - встроенный (embedded))
            switch (cbServerType.SelectedIndex)
            {
                case 0:
                    //fb_con.ServerType = FbServerType.Default;
                    fb_con.Port = (int)numPort.Value;
                    fb_con.DataSource = tbServer.Text;
                    break;
                case 1:
                    fb_con.ServerType = FbServerType.Embedded;
                    break;

            }
            
            if (fb_con.Database == "" & (fb_con.DataSource == "" | cbServerType.SelectedIndex == 0)){
                MessageBox.Show(this, "Неверные данные.", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            fb = new FbConnection(fb_con.ToString()); //передаем нашу строку подключения объекту класса FbConnection
            try
            {
                fb.Open(); //открываем БД

                FbDatabaseInfo fb_inf = new FbDatabaseInfo(fb); //информация о БД

                slbInfo.Text = "Connected. Info: " + fb_inf.ServerClass + "; " + fb_inf.ServerVersion;
                btnRefresh.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            tbServer.Text = Properties.Settings.Default.Server;
            numPort.Value = Properties.Settings.Default.Port;
            if (numPort.Value == 0) {
                numPort.Value = 3050;
            }
            tbDatabase.Text = Properties.Settings.Default.Database;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Server = tbServer.Text;
            Properties.Settings.Default.Port = numPort.Value;
            Properties.Settings.Default.Database = tbDatabase.Text;
            Properties.Settings.Default.Save();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            string sqlQry = "SELECT RDB$RELATION_NAME NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG <> 1 ORDER BY 1";

            using (FbDataReader reader = new FbCommand(sqlQry, fb).ExecuteReader())
            {

                try
                {
                    while (reader.Read()) //пока не прочли все данные выполняем...
                    {
                        cbTables.Items.Add(reader.GetString(0).Trim(' '));
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    reader.Close();
                }
            }
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            if (tbSqlQry.Text == "") return;

            FbDataAdapter da = new FbDataAdapter(tbSqlQry.Text, fb);
            DataSet ds = new DataSet();
            try
            {
                da.Fill(ds);
                dgvTableView.DataSource = ds.Tables[0];
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (cbTables.Text == "") return;
            tbSqlQry.Text = "SELECT * FROM " + cbTables.Text;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (fb.State == ConnectionState.Open)
            {
                fb.Close();
                slbInfo.Text = "Connected closed";
                btnRefresh.Enabled = false;
            }
        }

        private void cbServerType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isDefaultSrv = false;

            ComboBox senderComboBox = (ComboBox)sender;
            switch(senderComboBox.SelectedIndex){
                case 0:
                    isDefaultSrv = true;
                    break;
                case 1:
                    isDefaultSrv = false;
                    break;
            }
            tbServer.Enabled = isDefaultSrv;
            numPort.Enabled = isDefaultSrv;
        }
    }
}
