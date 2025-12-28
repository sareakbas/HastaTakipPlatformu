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
using System.Drawing.Printing;

namespace HastaTakipPlatformu
{
    public partial class Sorgulama : Form
    {
        private SqlConnection baglanti = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=SOHATS;Integrated Security=True");
        private PrintDocument printDoc;
        private int printRowIndex = 0;
        public Sorgulama()
        {
            InitializeComponent();
            this.button1.Click += Button1_Click; // Sorgula
            this.button3.Click += Button3_Click; // Temizle
            this.button2.Click += Button2_Click; // Yazdır (placeholder)
            this.Load += Sorgulama_Load;
        }

        private void Sorgulama_Load(object sender, EventArgs e)
        {
            // default: Hepsi
            radioButton3.Checked = true;
            // sensible default dates
            dateTimePicker1.Value = DateTime.Today.AddMonths(-1);
            dateTimePicker2.Value = DateTime.Today;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                string sql = @"SELECT s.*, d.ad AS hastaAd, d.soyad AS hastaSoyad FROM sevk s LEFT JOIN dosyalar d ON s.dosyaNo = d.dosyaNo 
                               WHERE CAST(s.sevkTarihi AS DATE) BETWEEN @start AND @end ORDER BY s.sevkTarihi";

                using (SqlCommand cmd = new SqlCommand(sql, baglanti))
                {
                    cmd.Parameters.AddWithValue("@start", dateTimePicker1.Value.Date);
                    cmd.Parameters.AddWithValue("@end", dateTimePicker2.Value.Date);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count == 0)
                        {
                            // diagnostic: check total rows in sevk table
                            int total = 0;
                            try
                            {
                                using (SqlCommand c2 = new SqlCommand("SELECT COUNT(1) FROM sevk", baglanti))
                                {
                                    total = Convert.ToInt32(c2.ExecuteScalar());
                                }
                            }
                            catch { }

                            string msg = "Sorgu sonucunda kayıt bulunamadı.";
                            msg += "\nTarih aralığını ve filtreleri kontrol edin.";
                            msg += "\nToplam sevk kaydı (tüm zaman): " + total;

                            // if there are records overall, show a small sample so user can verify schema/values
                            if (total > 0)
                            {
                                try
                                {
                                    using (SqlDataAdapter samp = new SqlDataAdapter("SELECT TOP 10 s.*, d.ad AS hastaAd, d.soyad AS hastaSoyad FROM sevk s LEFT JOIN dosyalar d ON s.dosyaNo=d.dosyaNo ORDER BY s.sevkTarihi", baglanti))
                                    {
                                        DataTable sample = new DataTable();
                                        samp.Fill(sample);
                                        dataGridView1.DataSource = sample;
                                        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                                        MessageBox.Show(msg + "\nÖrnek birkaç kayıt gösterildi (tarih filtresi dışında).", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        return;
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show(msg, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }

                            MessageBox.Show(msg, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dataGridView1.DataSource = null;
                            return;
                        }

                        // filter by taburcu option in C# to avoid SQL type conversion issues
                        DataTable filtered = dt.Clone();

                        foreach (DataRow row in dt.Rows)
                        {
                            string taburcuRaw = row.Table.Columns.Contains("taburcu") && row["taburcu"] != DBNull.Value ? row["taburcu"].ToString().Trim().ToLower() : string.Empty;

                            bool isTaburcu = false;
                            // consider common truthy values
                            if (taburcuRaw == "evet" || taburcuRaw == "true" || taburcuRaw == "1" || taburcuRaw == "yes" || taburcuRaw == "t")
                                isTaburcu = true;

                            if (radioButton1.Checked) // Taburcu olmuş
                            {
                                if (isTaburcu) filtered.ImportRow(row);
                            }
                            else if (radioButton4.Checked) // Taburcu olmamış
                            {
                                if (!isTaburcu) filtered.ImportRow(row);
                            }
                            else // Hepsi
                            {
                                filtered.ImportRow(row);
                            }
                        }

                        dataGridView1.DataSource = filtered;

                        // adjust column order/headers if present
                        if (dataGridView1.Columns.Contains("dosyaNo"))
                            dataGridView1.Columns["dosyaNo"].HeaderText = "DosyaNo";
                        if (dataGridView1.Columns.Contains("hastaAd"))
                            dataGridView1.Columns["hastaAd"].HeaderText = "Ad";
                        if (dataGridView1.Columns.Contains("hastaSoyad"))
                            dataGridView1.Columns["hastaSoyad"].HeaderText = "Soyad";
                        if (dataGridView1.Columns.Contains("sevkTarihi"))
                            dataGridView1.Columns["sevkTarihi"].HeaderText = "Sevk Tarihi";
                        if (dataGridView1.Columns.Contains("poliklinik"))
                            dataGridView1.Columns["poliklinik"].HeaderText = "Poliklinik";
                        if (dataGridView1.Columns.Contains("yapilanIslem"))
                            dataGridView1.Columns["yapilanIslem"].HeaderText = "Yapılan İşlem";
                        if (dataGridView1.Columns.Contains("doktor"))
                            dataGridView1.Columns["doktor"].HeaderText = "Doktor";
                        if (dataGridView1.Columns.Contains("taburcu"))
                            dataGridView1.Columns["taburcu"].HeaderText = "Taburcu";

                        // Make table less cramped
                        try
                        {
                            dataGridView1.EnableHeadersVisualStyles = false;
                            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10F);

                            // increase row height and padding for readability
                            dataGridView1.RowTemplate.Height = 36;
                            dataGridView1.DefaultCellStyle.Padding = new Padding(8);

                            // Use Fill mode with FillWeight to distribute space
                            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                            var fillWeights = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "islemId", 5f },
                                { "sevkTarihi", 12f },
                                { "dosyaNo", 8f },
                                { "poliklinik", 14f },
                                { "saat", 6f },
                                { "yapilanIslem", 20f },
                                { "doktor", 12f },
                                { "miktar", 6f },
                                { "birimFiyat", 8f },
                                { "siraNo", 6f },
                                { "toplamTutar", 8f },
                                { "taburcu", 6f },
                                { "hastaAd", 10f },
                                { "hastaSoyad", 10f }
                            };

                            foreach (DataGridViewColumn col in dataGridView1.Columns)
                            {
                                float fw;
                                if (fillWeights.TryGetValue(col.Name, out fw)) col.FillWeight = fw;
                                else col.FillWeight = 8f;

                                // enable wrapping for longer text
                                if (col.Name.Equals("yapilanIslem", StringComparison.OrdinalIgnoreCase) || col.Name.Equals("doktor", StringComparison.OrdinalIgnoreCase))
                                    col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                                else
                                    col.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                            }

                            dataGridView1.AllowUserToResizeColumns = true;
                            dataGridView1.ScrollBars = ScrollBars.Both;
                            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                            dataGridView1.MultiSelect = false;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorgulama sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
            radioButton3.Checked = true;
            dateTimePicker1.Value = DateTime.Today.AddMonths(-1);
            dateTimePicker2.Value = DateTime.Today;
        }

        
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Yazdırılacak veri yok.", "Bilgi");
                return;
            }

            using (PrintDialog dlg = new PrintDialog())
            {
                dlg.Document = printDoc;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    printRowIndex = 0;
                    printDoc.Print();
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }
    }
}
