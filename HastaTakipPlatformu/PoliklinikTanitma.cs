using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace HastaTakipPlatformu
{
    public partial class PoliklinikTanitma : Form
    {
        // reuse same connection string as other forms
        private SqlConnection baglanti = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=SOHATS;Integrated Security=True");

        public PoliklinikTanitma()
        {
            InitializeComponent();
            this.Load += PoliklinikTanitma_Load;
            this.comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            this.button1.Click += Button1_Click;
        }

        private void PoliklinikTanitma_Load(object sender, EventArgs e)
        {
            LoadPoliklinikler();
        }

        private void LoadPoliklinikler()
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                // Select all and filter in C# to avoid SQL conversion errors
                string sql = "SELECT poliklinikAdi, aciklama, durum FROM poliklinik ORDER BY poliklinikAdi";
                using (SqlDataAdapter da = new SqlDataAdapter(sql, baglanti))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // create filtered table with same schema
                    DataTable filtered = dt.Clone();

                    foreach (DataRow row in dt.Rows)
                    {
                        object val = row["durum"];
                        string sval = val == null || val == DBNull.Value ? string.Empty : val.ToString().Trim().ToLower();

                        bool aktif = false;
                        if (sval == "1" || sval == "true" || sval == "yes" || sval == "t" || sval == "y")
                            aktif = true;
                        else
                        {
                            // try parse numeric
                            int ival;
                            if (int.TryParse(sval, out ival) && ival == 1) aktif = true;
                        }

                        if (aktif)
                            filtered.ImportRow(row);
                    }

                    comboBox1.DataSource = filtered;
                    comboBox1.DisplayMember = "poliklinikAdi";
                    comboBox1.ValueMember = "poliklinikAdi"; // no id column available, use name as value

                    if (filtered.Rows.Count > 0)
                    {
                        comboBox1.SelectedIndex = 0;
                        var drv = comboBox1.SelectedItem as DataRowView;
                        if (drv != null) textBox1.Text = drv["aciklama"].ToString();
                    }
                    else
                    {
                        textBox1.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Poliklinikler yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var drv = comboBox1.SelectedItem as DataRowView;
                if (drv != null)
                {
                    textBox1.Text = drv["aciklama"].ToString();
                    // update checkbox according to durum column if available
                    try
                    {
                        checkBox1.Checked = Convert.ToBoolean(drv["durum"]);
                    }
                    catch
                    {
                        bool durumVal;
                        if (bool.TryParse(Convert.ToString(drv["durum"]), out durumVal))
                            checkBox1.Checked = durumVal;
                    }
                }
                else
                {
                    textBox1.Text = string.Empty;
                }
            }
            catch { }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string poliklinikAdi = comboBox1.Text.Trim();
            string aciklama = textBox1.Text.Trim();
            bool durum = checkBox1.Checked;

            if (string.IsNullOrWhiteSpace(poliklinikAdi))
            {
                MessageBox.Show("Poliklinik adı boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                // check if exists (case-insensitive)
                using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(1) FROM poliklinik WHERE LOWER(poliklinikAdi)=@adi", baglanti))
                {
                    cmdCheck.Parameters.AddWithValue("@adi", poliklinikAdi.ToLower());
                    int exists = Convert.ToInt32(cmdCheck.ExecuteScalar());
                    if (exists > 0)
                    {
                        MessageBox.Show("Bu isimde bir poliklinik zaten var.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                // insert new poliklinik
                using (SqlCommand cmdIns = new SqlCommand("INSERT INTO poliklinik (poliklinikAdi, aciklama, durum) VALUES (@adi, @aciklama, @durum)", baglanti))
                {
                    cmdIns.Parameters.AddWithValue("@adi", poliklinikAdi);
                    cmdIns.Parameters.AddWithValue("@aciklama", aciklama);
                    // pass proper bit value
                    cmdIns.Parameters.AddWithValue("@durum", durum ? (object)1 : (object)0);

                    int affected = cmdIns.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        MessageBox.Show("Poliklinik başarıyla eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // reload list and select the newly added item
                        LoadPoliklinikler();
                        // try to select by text
                        for (int i = 0; i < comboBox1.Items.Count; i++)
                        {
                            var drv = comboBox1.Items[i] as DataRowView;
                            if (drv != null && string.Equals(Convert.ToString(drv["poliklinikAdi"]), poliklinikAdi, StringComparison.CurrentCultureIgnoreCase))
                            {
                                comboBox1.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Poliklinik eklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Poliklinik eklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }
        }
    }
}
