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
using System.Drawing.Printing;

namespace HastaTakipPlatformu
{
    public partial class HastaIslemleri : Form
    {
        // printing helpers
        private PrintDocument printDoc;
        private PrintDialog printDialog;
        private PrintPreviewDialog previewDialog;
        private int currentPrintRow = 0;
        private Font headerFont = new Font("Arial", 14, FontStyle.Bold);
        private Font columnFont = new Font("Arial", 10, FontStyle.Bold);
        private Font rowFont = new Font("Arial", 9);

        SqlConnection baglanti = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=SOHATS;Integrated Security=True");
        public HastaIslemleri()
        {
            InitializeComponent();
            // Wire up Select-Delete button handler (Designer didn't attach)
            this.btnSecSil.Click += new EventHandler(this.btnSecSil_Click);

            // Ensure we update Sıra No when poliklinik or date changes
            this.cmbPoliklinik.SelectedIndexChanged += (s, e) => UpdateSiraNoForSelectedPoliklinik();
            this.dtSevkTarihi.ValueChanged += (s, e) => UpdateSiraNoForSelectedPoliklinik();

            // Wire up Taburcu button handler
            this.btnTaburcu.Click += new EventHandler(this.btnTaburcu_Click);

            // Setup printing
            printDoc = new PrintDocument();
            printDialog = new PrintDialog();
            previewDialog = new PrintPreviewDialog();
            printDoc.PrintPage += PrintDoc_PrintPage;

            // Wire preview and print buttons (they exist in Designer)
            try
            {
                this.btnOnizleme.Click += (s, e) =>
                {
                    try
                    {
                        currentPrintRow = 0;
                        previewDialog.Document = printDoc;
                        previewDialog.Width = 900;
                        previewDialog.Height = 700;
                        previewDialog.ShowDialog();
                    }
                    catch (Exception ex) { MessageBox.Show("Önizleme hatası: " + ex.Message); }
                };

                this.btnYazdır.Click += (s, e) =>
                {
                    try
                    {
                        currentPrintRow = 0;
                        printDialog.Document = printDoc;
                        if (printDialog.ShowDialog() == DialogResult.OK)
                        {
                            printDoc.Print();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Yazdırma hatası: " + ex.Message); }
                };
            }
            catch { }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            float left = e.MarginBounds.Left;
            float top = e.MarginBounds.Top;
            float width = e.MarginBounds.Width;
            float lineHeight = rowFont.GetHeight(e.Graphics) + 6;

            // Header
            string hastaBaslik = "Hasta Sevk İşlemleri : " + (string.IsNullOrWhiteSpace(txtHastaAdi.Text) ? "-" : txtHastaAdi.Text + " " + txtSoyadi.Text);
            e.Graphics.DrawString(hastaBaslik, headerFont, Brushes.Black, left, top);
            top += headerFont.GetHeight(e.Graphics) + 10;

            // column headers to print (align with grid)
            string[] cols = new string[] { "Poliklinik", "SıraNo", "Saat", "Yapılan İşlem", "Doktor", "Miktar", "Birim Fiyat", "Toplam Tutar" };
            float[] colWidths = new float[cols.Length];
            // distribute widths proportional
            colWidths[0] = width * 0.18f; // poliklinik
            colWidths[1] = width * 0.06f; // sıra
            colWidths[2] = width * 0.08f; // saat
            colWidths[3] = width * 0.22f; // işlem
            colWidths[4] = width * 0.14f; // doktor
            colWidths[5] = width * 0.06f; // miktar
            colWidths[6] = width * 0.12f; // birim
            colWidths[7] = width * 0.14f; // toplam

            float x = left;
            // draw column headers
            for (int i = 0; i < cols.Length; i++)
            {
                e.Graphics.DrawRectangle(Pens.Black, x, top, colWidths[i], lineHeight);
                e.Graphics.DrawString(cols[i], columnFont, Brushes.Black, new RectangleF(x + 2, top + 2, colWidths[i], lineHeight));
                x += colWidths[i];
            }
            top += lineHeight;

            // print rows
            while (currentPrintRow < dgvİslemler.Rows.Count)
            {
                DataGridViewRow row = dgvİslemler.Rows[currentPrintRow];
                x = left;

                // prepare cell values in same order
                string[] vals = new string[cols.Length];
                vals[0] = Convert.ToString(row.Cells[1].Value ?? ""); // poliklinik
                vals[1] = Convert.ToString(row.Cells[2].Value ?? ""); // sira
                vals[2] = Convert.ToString(row.Cells[3].Value ?? ""); // saat
                vals[3] = Convert.ToString(row.Cells[4].Value ?? ""); // islem
                vals[4] = Convert.ToString(row.Cells[5].Value ?? ""); // doktor
                vals[5] = Convert.ToString(row.Cells[6].Value ?? ""); // miktar
                vals[6] = Convert.ToString(row.Cells[7].Value ?? ""); // birimFiyat
                vals[7] = Convert.ToString(row.Cells[8].Value ?? ""); // toplam

                for (int c = 0; c < vals.Length; c++)
                {
                    e.Graphics.DrawRectangle(Pens.Black, x, top, colWidths[c], lineHeight);
                    e.Graphics.DrawString(vals[c], rowFont, Brushes.Black, new RectangleF(x + 2, top + 2, colWidths[c], lineHeight));
                    x += colWidths[c];
                }

                top += lineHeight;
                currentPrintRow++;

                // check for page end
                if (top + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            // footer: toplam
            top += 10;
            string toplam = "Toplam Tutar: " + lblToplamTutar.Text;
            e.Graphics.DrawString(toplam, columnFont, Brushes.Black, left, top);

            // finished
            e.HasMorePages = false;
            currentPrintRow = 0;
        }

        private void UpdateSiraNoForSelectedPoliklinik()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmbPoliklinik.Text))
                {
                    txtSiraNo.Text = "1";
                    return;
                }

                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(TRY_CAST(siraNo AS INT)),0) FROM sevk WHERE poliklinik=@pol AND CAST(sevkTarihi AS DATE)=@tarih", baglanti))
                {
                    cmd.Parameters.AddWithValue("@pol", cmbPoliklinik.Text);
                    cmd.Parameters.AddWithValue("@tarih", dtSevkTarihi.Value.Date);
                    object maxObj = cmd.ExecuteScalar();
                    int maxVal = 0;
                    if (maxObj != null && int.TryParse(maxObj.ToString(), out maxVal))
                        txtSiraNo.Text = (maxVal + 1).ToString();
                    else
                        txtSiraNo.Text = "1";
                }

                baglanti.Close();
            }
            catch
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }
        }

        private void btnHastaBilgileri_Click(object sender, EventArgs e)
        {
            // If a DosyaNo is entered in the current form, open the details form pre-filled
            if (!string.IsNullOrWhiteSpace(txtDosyaNo.Text))
            {
                using (HastaBilgileri frm = new HastaBilgileri(txtDosyaNo.Text))
                {
                    frm.ShowDialog();
                }
            }
            else
            {
                using (HastaBilgileri frm = new HastaBilgileri())
                {
                    frm.ShowDialog();
                }
            }
        }

        private void txtDosyaNo_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Enter)
            {

                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                SqlCommand komut = new SqlCommand("SELECT * FROM dosyalar WHERE dosyaNo=@p1", baglanti);
                komut.Parameters.AddWithValue("@p1", txtDosyaNo.Text);

                SqlDataReader dr = komut.ExecuteReader();


                if (dr.Read())
                {

                    txtHastaAdi.Text = dr["ad"].ToString();
                    txtSoyadi.Text = dr["soyad"].ToString();


                    txtKurumAdi.Text = "SGK";


                    dr.Close();

                    cmbPoliklinik.Focus();
                }
                else
                {
                    dr.Close(); 


                    DialogResult cevap = MessageBox.Show("Bu numaralı hasta bulunamadı! Yeni kayıt oluşturmak ister misiniz?", "Kayıt Yok", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (cevap == DialogResult.Yes)
                    {

                        btnHastaBilgileri.PerformClick();
                    }
                    else
                    {

                        txtDosyaNo.Text = "";
                        txtDosyaNo.Focus();
                    }
                }


            }
        }

        private void btnEkle_Click(object sender, EventArgs e)
        {
            if (cmbPoliklinik.Text == "" || cmbYapılanİslem.Text == "" || txtBirimFiyat.Text == "")
            {
                MessageBox.Show("Lütfen Poliklinik ve İşlem seçiniz!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string poliklinik = cmbPoliklinik.Text;
                string saat = DateTime.Now.ToShortTimeString();
                string islem = cmbYapılanİslem.Text;
                string doktor = cmbDr.Text;
                int miktar = (int)nudMiktar.Value;
                decimal birimFiyat = decimal.Parse(txtBirimFiyat.Text);
                decimal toplamTutar = miktar * birimFiyat;

                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                using (SqlTransaction tran = baglanti.BeginTransaction())
                {
                    try
                    {
                        // compute next siraNo for this poliklinik and date
                        int nextSira = 1;
                        using (SqlCommand cmdMax = new SqlCommand("SELECT ISNULL(MAX(TRY_CAST(siraNo AS INT)),0) FROM sevk WHERE poliklinik=@pol AND CAST(sevkTarihi AS DATE)=@tarih", baglanti, tran))
                        {
                            cmdMax.Parameters.AddWithValue("@pol", poliklinik);
                            cmdMax.Parameters.AddWithValue("@tarih", dtSevkTarihi.Value.Date);
                            object maxObj = cmdMax.ExecuteScalar();
                            int maxVal = 0;
                            if (maxObj != null && int.TryParse(maxObj.ToString(), out maxVal))
                                nextSira = maxVal + 1;
                        }

                        // if user entered a larger siraNo manually, keep it
                        int userSira;
                        if (int.TryParse(txtSiraNo.Text, out userSira) && userSira > nextSira)
                            nextSira = userSira;

                        // Insert and get identity
                        string sql = "INSERT INTO sevk (dosyaNo, poliklinik, saat, yapilanIslem, doktor, miktar, birimFiyat, siraNo, toplamTutar, sevkTarihi, taburcu) " +
                                     "VALUES (@dosya, @pol, @saat, @islem, @dr, @miktar, @birim, @sira, @toplam, @tarih, @taburcu); SELECT SCOPE_IDENTITY();";

                        using (SqlCommand komut = new SqlCommand(sql, baglanti, tran))
                        {
                            komut.Parameters.AddWithValue("@dosya", txtDosyaNo.Text);
                            komut.Parameters.AddWithValue("@pol", poliklinik);
                            komut.Parameters.AddWithValue("@saat", saat);
                            komut.Parameters.AddWithValue("@islem", islem);
                            komut.Parameters.AddWithValue("@dr", doktor);
                            komut.Parameters.AddWithValue("@miktar", miktar);
                            komut.Parameters.AddWithValue("@birim", birimFiyat);
                            komut.Parameters.AddWithValue("@sira", nextSira.ToString());
                            komut.Parameters.AddWithValue("@toplam", toplamTutar);
                            komut.Parameters.AddWithValue("@tarih", dtSevkTarihi.Value);
                            komut.Parameters.AddWithValue("@taburcu", "Hayır");

                            object result = komut.ExecuteScalar();
                            int newId = 0;
                            if (result != null && int.TryParse(result.ToString(), out newId))
                            {
                                tran.Commit();

                                // update UI
                                txtSiraNo.Text = nextSira.ToString();

                                // Rows.Add order must match designer columns: islemId, Poliklinik, SıraNo, Saat, Yapılan İşlem, Dr. Kodu, Miktar, Birim Fiyat, Toplam Tutar
                                dgvİslemler.Rows.Add(newId, poliklinik, nextSira.ToString(), saat, islem, doktor, miktar, birimFiyat, toplamTutar);

                                GenelToplamHesapla();
                                nudMiktar.Value = 1;
                                MessageBox.Show("İşlem veritabanına kaydedildi.");
                            }
                            else
                            {
                                tran.Rollback();
                                MessageBox.Show("Kayıt oluşturulurken kimlik alınamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
            }
            catch (Exception hata)
            {
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
                MessageBox.Show("Ekleme sırasında hata oluştu: " + hata.Message);
            }
        }
        void GenelToplamHesapla()
        {
            decimal genelToplam = 0;

            // Determine column indices for miktar and birimFiyat by name if possible
            int idxMiktar = -1;
            int idxBirim = -1;
            try
            {
                if (dgvİslemler.Columns.Contains("colMiktar")) idxMiktar = dgvİslemler.Columns["colMiktar"].Index;
                if (dgvİslemler.Columns.Contains("colBirimFiyat")) idxBirim = dgvİslemler.Columns["colBirimFiyat"].Index;
            }
            catch { }

            // Fallback to previous hard-coded indices if column names not available
            if (idxMiktar == -1) idxMiktar = 6; // default where 'miktar' was after reorder
            if (idxBirim == -1) idxBirim = 7;  // default where 'birimFiyat' was after reorder

            for (int i = 0; i < dgvİslemler.Rows.Count; i++)
            {
                try
                {
                    object hucreMiktar = null;
                    object hucreFiyat = null;

                    if (idxMiktar >= 0 && idxMiktar < dgvİslemler.Columns.Count)
                        hucreMiktar = dgvİslemler.Rows[i].Cells[idxMiktar].Value;

                    if (idxBirim >= 0 && idxBirim < dgvİslemler.Columns.Count)
                        hucreFiyat = dgvİslemler.Rows[i].Cells[idxBirim].Value;

                    if (hucreMiktar != null && hucreFiyat != null)
                    {
                        decimal adet;
                        decimal fiyat;

                        if (decimal.TryParse(Convert.ToString(hucreMiktar), out adet) && decimal.TryParse(Convert.ToString(hucreFiyat), out fiyat))
                        {
                            genelToplam += (adet * fiyat);
                        }
                    }
                }
                catch
                {
                    // ignore per-row errors and continue
                }
            }

            lblToplamTutar.Text = genelToplam.ToString() + " TL";
        }

        private void HastaIslemleri_Load(object sender, EventArgs e)
        {

            if (baglanti.State == ConnectionState.Closed) baglanti.Open();

            // ensure islemId column visible and configured
            try
            {
                if (dgvİslemler.Columns.Contains("colIslemId"))
                {
                    var col = dgvİslemler.Columns["colIslemId"];
                    col.Visible = true;
                    col.HeaderText = "İşlem ID";
                    col.ReadOnly = true;
                    col.Width = 80;

                    // Make sure columns fit and the ID column becomes visible
                    dgvİslemler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    // If grid content is wider than view, scroll to the ID column
                    try
                    {
                        int idx = col.Index;
                        if (idx >= 0 && idx < dgvİslemler.Columns.Count)
                        {
                            dgvİslemler.FirstDisplayedScrollingColumnIndex = idx;
                        }
                    }
                    catch { }
                }
            }
            catch { }


            cmbPoliklinik.Items.Clear(); 
                                                
            // robustly select active poliklinikler whether 'durum' is stored as bit/int (1) or string ('True')
            SqlCommand komutPol = new SqlCommand("SELECT poliklinikAdi FROM poliklinik WHERE (TRY_CONVERT(INT,durum) = 1 OR LOWER(CAST(durum AS VARCHAR(10))) = 'true') ORDER BY poliklinikAdi", baglanti);
            SqlDataReader drPol = komutPol.ExecuteReader();
            while (drPol.Read())
            {
                cmbPoliklinik.Items.Add(drPol[0].ToString());
            }
            drPol.Close();


            SqlDataAdapter daIslem = new SqlDataAdapter("SELECT islemAdi, birimFiyat FROM islemler", baglanti);
            DataTable dtIslem = new DataTable();
            daIslem.Fill(dtIslem);

            cmbYapılanİslem.DisplayMember = "islemAdi";   // Ekranda İSİM görünecek (Genel Muayene)
            cmbYapılanİslem.ValueMember = "birimFiyat";   // Arkada FİYAT tutulacak (150.00)
            cmbYapılanİslem.DataSource = dtIslem;         // Kutuyu doldur

            cmbDr.Items.Clear();
            
            SqlCommand komutDr = new SqlCommand("SELECT ad + ' ' + soyad FROM kullanicilar", baglanti);
            SqlDataReader drDr = komutDr.ExecuteReader();
            while (drDr.Read())
            {
                cmbDr.Items.Add(drDr[0].ToString());
            }
            drDr.Close();

            txtSiraNo.Text = "1"; 
            dtSevkTarihi.Value = DateTime.Now; 

            
            baglanti.Close();
        }

        private void cmbYapılanİslem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbYapılanİslem.SelectedValue != null)
            {
                // "Birim Fiyat" kutusunun adını kontrol etmelisin. 
                // Tasarımda o kutunun adı txtBirimFiyat ise:
                txtBirimFiyat.Text = cmbYapılanİslem.SelectedValue.ToString();
            }
        }
        void Listele()
        {
            // 1. Önce listeyi temizleyelim ki üst üste binmesin
            dgvİslemler.Rows.Clear();

            if (baglanti.State == ConnectionState.Closed)
                baglanti.Open();

            // 2. Sadece o dosya numarasına ait kayıtları çeken sorgu
            SqlCommand komut = new SqlCommand("SELECT * FROM sevk WHERE dosyaNo=@p1", baglanti);
            komut.Parameters.AddWithValue("@p1", txtDosyaNo.Text);

            SqlDataReader dr = komut.ExecuteReader();

            while (dr.Read())
            {
               
                // Match column order: islemId, Poliklinik, SıraNo, Saat, Yapılan İşlem, Dr. Kodu, Miktar, Birim Fiyat, Toplam Tutar
                dgvİslemler.Rows.Add(
                    dr["islemId"].ToString(),
                    dr["poliklinik"].ToString(),
                    dr["siraNo"].ToString(),
                    dr["saat"].ToString(),
                    dr["yapilanIslem"].ToString(),
                    dr["doktor"].ToString(),
                    dr["miktar"].ToString(),
                    dr["birimFiyat"].ToString(),
                    dr["toplamTutar"].ToString()
                );
            }
            dr.Close();
            baglanti.Close();

            GenelToplamHesapla();
        }


        private void btnBul_Click(object sender, EventArgs e)
        {
            // 1. Önce dosya numarası girilmiş mi bakalım
            if (txtDosyaNo.Text == "")
            {
                MessageBox.Show("Lütfen bir Dosya No giriniz.");
                return;
            }

            // 2. Önce aşağıdaki listeyi dolduruyoruz (Senin yazdığın metod)
            Listele();


            // Veritabanı bağlantısı kapalıysa aç
            if (baglanti.State == ConnectionState.Closed) baglanti.Open();

            // 'dosyalar' tablosundan o numaralı hastayı bul
            SqlCommand komutHasta = new SqlCommand("SELECT * FROM dosyalar WHERE dosyaNo=@p1", baglanti);
            komutHasta.Parameters.AddWithValue("@p1", txtDosyaNo.Text);

            SqlDataReader drHasta = komutHasta.ExecuteReader();

            if (drHasta.Read()) // Eğer kayıt bulunduysa
            {
                // 4. KUTULARI DOLDURMA (Kutu isimlerini kontrol et!)
                // Tasarım ekranına bak, kutularının adı txtAd ve txtSoyad mı?
                // Değilse (örn: textBox1 ise) burayı ona göre değiştir.

                txtHastaAdi.Text = drHasta["ad"].ToString();       // Veritabanındaki 'ad' sütunu
                txtSoyadi.Text = drHasta["soyad"].ToString(); // Veritabanındaki 'soyad' sütunu

                // Kurum adı sütunu tablonda yoksa bu satırı // ile kapalı tut:
                // txtKurumAdi.Text = drHasta["kurum"].ToString(); 
            }
            else
            {
                MessageBox.Show("Bu numaralı hasta kayıtlı değil!", "Uyarı");
            }

            // Okuyucuyu ve bağlantıyı kapat
            drHasta.Close();
            baglanti.Close();

            // --- ÖNCEKİ İŞLEMLER KUTUSUNU DOLDURMA ---
            cmbOncekiIslemler.Items.Clear();
            cmbOncekiIslemler.Text = "";

            // 'DISTINCT' komutu aynı tarihleri tekrar tekrar getirmesin diye kullanılır
            // Sadece sevk tarihlerini çekiyoruz
            if (baglanti.State == ConnectionState.Closed) baglanti.Open();
            // Bu sorgu saati atar, sadece YIL-AY-GÜN getirir.
            // Böylece aynı gün 50 işlem de olsa listede tek bir tarih çıkar.
            SqlCommand komutTarih = new SqlCommand("SELECT DISTINCT CONVERT(VARCHAR(10), sevkTarihi, 104) FROM sevk WHERE dosyaNo=@p1 ORDER BY 1 DESC", baglanti);
            komutTarih.Parameters.AddWithValue("@p1", txtDosyaNo.Text);

            SqlDataReader drTarih = komutTarih.ExecuteReader();
            while (drTarih.Read())
            {
                cmbOncekiIslemler.Items.Add(drTarih[0].ToString());
            }
            drTarih.Close();
            // -----------------------------------------

        }


        private void btnGit_Click(object sender, EventArgs e)
        {
            if (cmbOncekiIslemler.Text == "")
            {
                MessageBox.Show("Lütfen bir tarih seçiniz.");
                return;
            }

            dgvİslemler.Rows.Clear(); // Listeyi temizle

            if (baglanti.State == ConnectionState.Closed) baglanti.Open();

            // SQL SORGUSU: CAST(sevkTarihi AS DATE) komutu veritabanındaki saati ve saliseyi çöpe atar.
            // Sadece günü kıyaslar. böylece .987 milisaniye sorunu ortadan kalkar!
            string sql = "SELECT * FROM sevk WHERE dosyaNo=@p1 AND CAST(sevkTarihi AS DATE) = @p2";

            SqlCommand komut = new SqlCommand(sql, baglanti);
            komut.Parameters.AddWithValue("@p1", txtDosyaNo.Text);

            // ComboBox'taki tarihi SQL'in anlayacağı tarih formatına çevirip gönderiyoruz
            komut.Parameters.AddWithValue("@p2", DateTime.Parse(cmbOncekiIslemler.Text).Date);

            SqlDataReader dr = komut.ExecuteReader();
            while (dr.Read())
            {
                dgvİslemler.Rows.Add(
                    dr["islemId"].ToString(),
                    dr["poliklinik"].ToString(),
                    dr["siraNo"].ToString(),
                    dr["saat"].ToString(),
                    dr["yapilanIslem"].ToString(),
                    dr["doktor"].ToString(),
                    dr["miktar"].ToString(),
                    dr["birimFiyat"].ToString(),
                    dr["toplamTutar"].ToString()
                );
            }
            dr.Close();
            baglanti.Close();

            GenelToplamHesapla(); // Toplam tutarı da güncelleyelim
        }

        private void btnYenile_Click(object sender, EventArgs e)
        {
            // Open patient entry form to create a new patient
            using (HastaBilgileri frm = new HastaBilgileri())
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    string yeniDosya = frm.SavedDosyaNo;
                    if (!string.IsNullOrWhiteSpace(yeniDosya))
                    {
                        // Set the dosya no and load patient info
                        txtDosyaNo.Text = yeniDosya;

                        if (baglanti.State == ConnectionState.Closed) baglanti.Open();
                        SqlCommand komutHasta = new SqlCommand("SELECT * FROM dosyalar WHERE dosyaNo=@p1", baglanti);
                        komutHasta.Parameters.AddWithValue("@p1", yeniDosya);

                        SqlDataReader drHasta = komutHasta.ExecuteReader();
                        if (drHasta.Read())
                        {
                            txtHastaAdi.Text = drHasta["ad"].ToString();
                            txtSoyadi.Text = drHasta["soyad"].ToString();
                        }
                        drHasta.Close();
                        baglanti.Close();

                        // Refresh list of operations for this patient (if any)
                        Listele();

                        // Reset some controls for new entry
                        txtSiraNo.Text = "1";
                        dtSevkTarihi.Value = DateTime.Now;
                        nudMiktar.Value = 1;
                        lblToplamTutar.Text = "0 TL";

                        cmbPoliklinik.Focus();
                    }
                }
            }
        }

        private void btnCikis_Click(object sender, EventArgs e)
        {
            // Close this form and switch back to the main form instead of exiting the application
            this.Close();

            // Try to activate the MDI parent if present
            Form main = null;
            if (this.MdiParent != null)
            {
                main = this.MdiParent;
            }
            else
            {
                // Look for an open MainForm instance
                foreach (Form f in Application.OpenForms)
                {
                    if (f.GetType().Name == "MainForm" || f is MainForm)
                    {
                        main = f;
                        break;
                    }
                }
            }

            if (main != null)
            {
                try
                {
                    main.Show();
                    main.BringToFront();
                    main.Activate();
                }
                catch
                {
                    // ignore activation errors
                }
            }
            else
            {
                // If no main form is open, create and show one
                try
                {
                    MainForm mf = new MainForm();
                    mf.Show();
                }
                catch
                {
                    // ignore if cannot create
                }
            }
        }

        private void btnSecSil_Click(object sender, EventArgs e)
        {
            if (dgvİslemler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen silmek istediğiniz satır(ları) seçin.", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Seçili satır(ları) veritabanından ve listeden silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Get selected row indices once and sort descending
            var indices = dgvİslemler.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderByDescending(i => i).ToList();

            SqlConnection conn = baglanti;
            try
            {
                if (conn.State == ConnectionState.Closed) conn.Open();

                foreach (int rowIndex in indices)
                {
                    if (rowIndex < 0 || rowIndex >= dgvİslemler.Rows.Count)
                        continue;

                    DataGridViewRow row = dgvİslemler.Rows[rowIndex];

                    // Get islemId if present
                    object idObj = null;
                    if (dgvİslemler.Columns.Contains("colIslemId"))
                        idObj = row.Cells["colIslemId"].Value;
                    else if (dgvİslemler.Columns.Count > 8)
                        idObj = row.Cells[8].Value;

                    int islemId = 0; // initialize to avoid unassigned usage
                    bool hasId = false;
                    if (idObj != null)
                    {
                        hasId = int.TryParse(idObj.ToString(), out islemId);
                    }

                    int affected = 0;

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        if (hasId)
                        {
                            cmd.CommandText = "DELETE FROM sevk WHERE islemId=@id";
                            cmd.Parameters.AddWithValue("@id", islemId);
                            affected = cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            // fallback: best-effort delete using row values
                            string poliklinik = Convert.ToString(dgvİslemler.Columns.Contains("colPoliklinik") ? row.Cells["colPoliklinik"].Value : row.Cells[0].Value ?? "");
                            string siraNo = Convert.ToString(dgvİslemler.Columns.Contains("colSİraNo") ? row.Cells["colSİraNo"].Value : row.Cells[1].Value ?? "");
                            string saat = Convert.ToString(dgvİslemler.Columns.Contains("colSaat") ? row.Cells["colSaat"].Value : row.Cells[2].Value ?? "");
                            string yapilanIslem = Convert.ToString(dgvİslemler.Columns.Contains("colYapılanİslem") ? row.Cells["colYapılanİslem"].Value : row.Cells[3].Value ?? "");
                            string doktor = Convert.ToString(dgvİslemler.Columns.Contains("colDrKodu") ? row.Cells["colDrKodu"].Value : row.Cells[4].Value ?? "");
                            int miktar = 0;
                            decimal birimFiyat = 0;
                            int.TryParse(Convert.ToString(dgvİslemler.Columns.Contains("colMiktar") ? row.Cells["colMiktar"].Value : row.Cells[5].Value ?? "0"), out miktar);
                            decimal.TryParse(Convert.ToString(dgvİslemler.Columns.Contains("colBirimFiyat") ? row.Cells["colBirimFiyat"].Value : row.Cells[6].Value ?? "0"), out birimFiyat);

                            cmd.CommandText = "DELETE TOP (1) FROM sevk WHERE dosyaNo=@dosya AND poliklinik=@pol AND siraNo=@sira AND saat=@saat AND yapilanIslem=@islem AND doktor=@dr AND miktar=@miktar AND birimFiyat=@birim";
                            cmd.Parameters.AddWithValue("@dosya", txtDosyaNo.Text);
                            cmd.Parameters.AddWithValue("@pol", poliklinik);
                            cmd.Parameters.AddWithValue("@sira", siraNo);
                            cmd.Parameters.AddWithValue("@saat", saat);
                            cmd.Parameters.AddWithValue("@islem", yapilanIslem);
                            cmd.Parameters.AddWithValue("@dr", doktor);
                            cmd.Parameters.AddWithValue("@miktar", miktar);
                            cmd.Parameters.AddWithValue("@birim", birimFiyat);

                            affected = cmd.ExecuteNonQuery();
                        }
                    }

                    if (affected > 0)
                    {
                        // remove from grid
                        dgvİslemler.Rows.RemoveAt(rowIndex);
                    }
                    else
                    {
                        MessageBox.Show($"Satır {rowIndex + 1} veritabanından silinemedi.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Silme sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (conn.State == ConnectionState.Open) conn.Close(); } catch { }
            }

            // Recalculate totals
            try { GenelToplamHesapla(); } catch { }
        }

        private void btnTaburcu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDosyaNo.Text))
            {
                MessageBox.Show("Lütfen önce bir Dosya No giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Seçili hastayı taburcu etmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                using (SqlCommand cmd = new SqlCommand("UPDATE sevk SET taburcu=@taburcu WHERE dosyaNo=@dosya", baglanti))
                {
                    cmd.Parameters.AddWithValue("@taburcu", "Evet");
                    cmd.Parameters.AddWithValue("@dosya", txtDosyaNo.Text);

                    int affected = cmd.ExecuteNonQuery();

                    MessageBox.Show(affected + " kayıt taburcu olarak işaretlendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Taburcu işlemi sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (baglanti.State == ConnectionState.Open) baglanti.Close(); } catch { }
            }

            // Refresh the list to show updated values
            try { Listele(); } catch { }
        }

    }
}

