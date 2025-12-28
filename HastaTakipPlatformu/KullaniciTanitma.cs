using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HastaTakipPlatformu
{
    public partial class KullaniciTanitma : Form
    {
        SqlConnection baglanti = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=SOHATS;Integrated Security=True");

        public KullaniciTanitma()
        {
            InitializeComponent();

            // Wire up buttons
            this.button1.Click += Button1_Click; // Kaydet
            this.button2.Click += Button2_Click; // Çıkış

            // Wire up kodu textbox to load user on Enter or leave
            this.textBox6.KeyDown += TextBox6_KeyDown;
            this.textBox6.Leave += (s, e) => { LoadUserByKodu(textBox6.Text.Trim()); };
        }

        private void TextBox6_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadUserByKodu(textBox6.Text.Trim());
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private bool UserExists(string kodu)
        {
            if (string.IsNullOrWhiteSpace(kodu)) return false;
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM kullanicilar WHERE kodu=@kodu", baglanti))
                {
                    cmd.Parameters.AddWithValue("@kodu", kodu);
                    object o = cmd.ExecuteScalar();
                    int count = 0;
                    if (o != null && int.TryParse(o.ToString(), out count))
                        return count > 0;
                }
            }
            catch { }
            finally { try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { } }
            return false;
        }

        private void LoadUserByKodu(string kodu)
        {
            if (string.IsNullOrWhiteSpace(kodu)) return;

            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 * FROM kullanicilar WHERE kodu=@kodu", baglanti))
                {
                    cmd.Parameters.AddWithValue("@kodu", kodu);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // populate controls, checking DBNull
                            textBox3.Text = dr["ad"] != DBNull.Value ? dr["ad"].ToString() : "";
                            textBox2.Text = dr["soyad"] != DBNull.Value ? dr["soyad"].ToString() : "";
                            textBox1.Text = dr["sifre"] != DBNull.Value ? dr["sifre"].ToString() : "";
                            textBox5.Text = dr["unvan"] != DBNull.Value ? dr["unvan"].ToString() : "";
                            // use ColumnExists helper instead of dr.Table.Columns
                            comboBox1.Text = ColumnExists(dr, "cinsiyet") && dr["cinsiyet"] != DBNull.Value ? dr["cinsiyet"].ToString() : "";
                            textBox4.Text = ColumnExists(dr, "tel") && dr["tel"] != DBNull.Value ? dr["tel"].ToString() : "";

                            if (ColumnExists(dr, "dogumTarihi") && dr["dogumTarihi"] != DBNull.Value)
                            {
                                DateTime d;
                                if (DateTime.TryParse(dr["dogumTarihi"].ToString(), out d)) dateTimePicker1.Value = d;
                            }
                            if (ColumnExists(dr, "iseBaslama") && dr["iseBaslama"] != DBNull.Value)
                            {
                                DateTime d2;
                                if (DateTime.TryParse(dr["iseBaslama"].ToString(), out d2)) dateTimePicker2.Value = d2;
                            }
                            else if (ColumnExists(dr, "iseBaslamaTarihi") && dr["iseBaslamaTarihi"] != DBNull.Value)
                            {
                                DateTime d2;
                                if (DateTime.TryParse(dr["iseBaslamaTarihi"].ToString(), out d2)) dateTimePicker2.Value = d2;
                            }

                            textBox7.Text = ColumnExists(dr, "adres") && dr["adres"] != DBNull.Value ? dr["adres"].ToString() : "";
                        }
                        else
                        {
                            // clear fields except kodu
                            textBox3.Text = textBox2.Text = textBox1.Text = textBox5.Text = textBox4.Text = textBox7.Text = "";
                            comboBox1.Text = "";
                            dateTimePicker1.Value = DateTime.Now;
                            dateTimePicker2.Value = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcı yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }
        }

        private bool ColumnExists(SqlDataReader reader, string columnName)
        {
            try
            {
                var schema = reader.GetSchemaTable();
                if (schema == null) return false;
                foreach (DataRow row in schema.Rows)
                {
                    if (row["ColumnName"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private bool IsColumnIdentity(string tableName, string columnName)
        {
            try
            {
                // Use a local connection so we don't close the shared 'baglanti' unexpectedly
                using (var conn = new SqlConnection(baglanti.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT COLUMNPROPERTY(OBJECT_ID(@table), @column, 'IsIdentity')";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@table", tableName);
                        cmd.Parameters.AddWithValue("@column", columnName);
                        object o = cmd.ExecuteScalar();
                        int val = 0;
                        if (o != null && int.TryParse(o.ToString(), out val))
                            return val == 1;
                    }
                }
            }
            catch { }
            return false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            // basic validation
            if (string.IsNullOrWhiteSpace(textBox3.Text) || string.IsNullOrWhiteSpace(textBox2.Text) || string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Lütfen ad, soyad ve şifre alanlarını doldurunuz.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string kodu = textBox6.Text.Trim();
            string ad = textBox3.Text.Trim();
            string soyad = textBox2.Text.Trim();
            string sifre = textBox1.Text.Trim();
            string unvan = textBox5.Text.Trim();
            bool yetki = true; // default yetki
            string cinsiyet = comboBox1.SelectedItem?.ToString() ?? comboBox1.Text ?? "";
            string tel = textBox4.Text.Trim();
            DateTime dogum = dateTimePicker1.Value.Date;
            DateTime iseBaslama = dateTimePicker2.Value.Date;
            string adres = textBox7.Text.Trim();

            bool exists = false;
            if (!string.IsNullOrWhiteSpace(kodu)) exists = UserExists(kodu);

            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                if (exists)
                {
                    // Try update full schema first
                    string updFull = "UPDATE kullanicilar SET ad=@ad, soyad=@soyad, sifre=@sifre, yetki=@yetki, unvan=@unvan, cinsiyet=@cinsiyet, tel=@tel, dogumTarihi=@dogum, iseBaslama=@iseBaslama, adres=@adres WHERE kodu=@kodu";
                    using (SqlCommand cmd = new SqlCommand(updFull, baglanti))
                    {
                        cmd.Parameters.AddWithValue("@ad", ad);
                        cmd.Parameters.AddWithValue("@soyad", soyad);
                        cmd.Parameters.AddWithValue("@sifre", sifre);
                        cmd.Parameters.AddWithValue("@yetki", yetki);
                        cmd.Parameters.AddWithValue("@unvan", unvan);
                        cmd.Parameters.AddWithValue("@cinsiyet", cinsiyet);
                        cmd.Parameters.AddWithValue("@tel", tel);
                        cmd.Parameters.AddWithValue("@dogum", dogum);
                        cmd.Parameters.AddWithValue("@iseBaslama", iseBaslama);
                        cmd.Parameters.AddWithValue("@adres", adres);
                        cmd.Parameters.AddWithValue("@kodu", kodu);
                        try
                        {
                            int aff = cmd.ExecuteNonQuery();
                            if (aff > 0) MessageBox.Show("Kullanıcı bilgileri güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            else MessageBox.Show("Güncelleme yapılmadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        catch (SqlException)
                        {
                            // try minimal update
                        }
                    }

                    string updMin = "UPDATE kullanicilar SET ad=@ad, soyad=@soyad, sifre=@sifre, yetki=@yetki, unvan=@unvan WHERE kodu=@kodu";
                    using (SqlCommand cmd2 = new SqlCommand(updMin, baglanti))
                    {
                        cmd2.Parameters.AddWithValue("@ad", ad);
                        cmd2.Parameters.AddWithValue("@soyad", soyad);
                        cmd2.Parameters.AddWithValue("@sifre", sifre);
                        cmd2.Parameters.AddWithValue("@yetki", yetki);
                        cmd2.Parameters.AddWithValue("@unvan", unvan);
                        cmd2.Parameters.AddWithValue("@kodu", kodu);

                        int aff2 = cmd2.ExecuteNonQuery();
                        if (aff2 > 0) MessageBox.Show("Kullanıcı bilgileri güncellendi (minimal).", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else MessageBox.Show("Güncelleme yapılamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // Insert new user (try full then minimal)
                    bool koduIsIdentity = IsColumnIdentity("kullanicilar", "kodu");

                    if (!koduIsIdentity)
                    {
                        string sqlFull = "INSERT INTO kullanicilar (kodu, ad, soyad, sifre, yetki, unvan, cinsiyet, tel, dogumTarihi, iseBaslama, adres) " +
                                         "VALUES (@kodu, @ad, @soyad, @sifre, @yetki, @unvan, @cinsiyet, @tel, @dogum, @iseBaslama, @adres)";
                        using (SqlCommand cmd = new SqlCommand(sqlFull, baglanti))
                        {
                            cmd.Parameters.AddWithValue("@kodu", string.IsNullOrWhiteSpace(kodu) ? (object)DBNull.Value : kodu);
                            cmd.Parameters.AddWithValue("@ad", ad);
                            cmd.Parameters.AddWithValue("@soyad", soyad);
                            cmd.Parameters.AddWithValue("@sifre", sifre);
                            cmd.Parameters.AddWithValue("@yetki", yetki);
                            cmd.Parameters.AddWithValue("@unvan", unvan);
                            cmd.Parameters.AddWithValue("@cinsiyet", cinsiyet);
                            cmd.Parameters.AddWithValue("@tel", tel);
                            cmd.Parameters.AddWithValue("@dogum", dogum);
                            cmd.Parameters.AddWithValue("@iseBaslama", iseBaslama);
                            cmd.Parameters.AddWithValue("@adres", adres);

                            try
                            {
                                int affected = cmd.ExecuteNonQuery();
                                if (affected > 0) MessageBox.Show("Kullanıcı başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                else MessageBox.Show("Kullanıcı kaydedilemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            catch (SqlException)
                            {
                                // fallback
                            }
                        }
                    }
                    else
                    {
                        // kodu is identity -> omit kodu and return generated id
                        string sqlFullNoKodu = "INSERT INTO kullanicilar (ad, soyad, sifre, yetki, unvan, cinsiyet, tel, dogumTarihi, iseBaslama, adres) " +
                                               "VALUES (@ad, @soyad, @sifre, @yetki, @unvan, @cinsiyet, @tel, @dogum, @iseBaslama, @adres); SELECT SCOPE_IDENTITY();";
                        using (SqlCommand cmd = new SqlCommand(sqlFullNoKodu, baglanti))
                        {
                            cmd.Parameters.AddWithValue("@ad", ad);
                            cmd.Parameters.AddWithValue("@soyad", soyad);
                            cmd.Parameters.AddWithValue("@sifre", sifre);
                            cmd.Parameters.AddWithValue("@yetki", yetki);
                            cmd.Parameters.AddWithValue("@unvan", unvan);
                            cmd.Parameters.AddWithValue("@cinsiyet", cinsiyet);
                            cmd.Parameters.AddWithValue("@tel", tel);
                            cmd.Parameters.AddWithValue("@dogum", dogum);
                            cmd.Parameters.AddWithValue("@iseBaslama", iseBaslama);
                            cmd.Parameters.AddWithValue("@adres", adres);

                            try
                            {
                                object res = cmd.ExecuteScalar();
                                if (res != null)
                                {
                                    MessageBox.Show("Kullanıcı başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    // set generated id back to textbox6
                                    textBox6.Text = res.ToString();
                                }
                                else
                                {
                                    MessageBox.Show("Kullanıcı kaydedilemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                return;
                            }
                            catch (SqlException)
                            {
                                // fallback
                            }
                        }
                    }

                    // if full failed or was fallback needed, try minimal
                    bool triedMin = false;
                    if (!koduIsIdentity)
                    {
                        string sqlMin = "INSERT INTO kullanicilar (kodu, ad, soyad, sifre, yetki, unvan) VALUES (@kodu, @ad, @soyad, @sifre, @yetki, @unvan)";
                        using (SqlCommand cmd2 = new SqlCommand(sqlMin, baglanti))
                        {
                            cmd2.Parameters.AddWithValue("@kodu", string.IsNullOrWhiteSpace(kodu) ? (object)DBNull.Value : kodu);
                            cmd2.Parameters.AddWithValue("@ad", ad);
                            cmd2.Parameters.AddWithValue("@soyad", soyad);
                            cmd2.Parameters.AddWithValue("@sifre", sifre);
                            cmd2.Parameters.AddWithValue("@yetki", yetki);
                            cmd2.Parameters.AddWithValue("@unvan", unvan);

                            int affected2 = cmd2.ExecuteNonQuery();
                            triedMin = true;
                            if (affected2 > 0)
                            {
                                MessageBox.Show("Kullanıcı başarıyla kaydedildi (minimal şema).", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Kullanıcı kaydedilemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        // kodu is identity, minimal without kodu
                        string sqlMinNoKodu = "INSERT INTO kullanicilar (ad, soyad, sifre, yetki, unvan) VALUES (@ad, @soyad, @sifre, @yetki, @unvan); SELECT SCOPE_IDENTITY();";
                        using (SqlCommand cmd2 = new SqlCommand(sqlMinNoKodu, baglanti))
                        {
                            cmd2.Parameters.AddWithValue("@ad", ad);
                            cmd2.Parameters.AddWithValue("@soyad", soyad);
                            cmd2.Parameters.AddWithValue("@sifre", sifre);
                            cmd2.Parameters.AddWithValue("@yetki", yetki);
                            cmd2.Parameters.AddWithValue("@unvan", unvan);

                            object res2 = cmd2.ExecuteScalar();
                            if (res2 != null)
                            {
                                MessageBox.Show("Kullanıcı başarıyla kaydedildi (minimal şema).", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                textBox6.Text = res2.ToString();
                            }
                            else
                            {
                                MessageBox.Show("Kullanıcı kaydedilemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }
        }
    }
}
