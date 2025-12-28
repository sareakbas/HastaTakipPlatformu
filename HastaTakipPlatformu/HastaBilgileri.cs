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
    public partial class HastaBilgileri : Form
    {
     
        SqlConnection baglanti = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=SOHATS;Integrated Security=True");

        public HastaBilgileri()
        {
            InitializeComponent();
        }

        // New constructor to load an existing patient by dosyaNo
        public HastaBilgileri(string dosyaNo) : this()
        {
            if (!string.IsNullOrWhiteSpace(dosyaNo))
            {
                LoadPatient(dosyaNo);
            }
        }

        // Expose the saved DosyaNo so the caller form can read it after dialog closes
        public string SavedDosyaNo
        {
            get { return txtDosyaNo.Text; }
        }

        private void LoadPatient(string dosyaNo)
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                SqlCommand komut = new SqlCommand("SELECT * FROM dosyalar WHERE dosyaNo=@p1", baglanti);
                komut.Parameters.AddWithValue("@p1", dosyaNo);

                SqlDataReader dr = komut.ExecuteReader();

                if (dr.Read())
                {
                    txtDosyaNo.Text = dr["dosyaNo"].ToString();
                    txtTC.Text = dr["tcKimlikNo"].ToString();
                    txtAd.Text = dr["ad"].ToString();
                    txtSoyad.Text = dr["soyad"].ToString();

                    // dogumTarihi may be null in DB
                    object dtObj = dr["dogumTarihi"];
                    DateTime dt;
                    if (dtObj != DBNull.Value && DateTime.TryParse(dtObj.ToString(), out dt))
                        dtDogumTarihi.Value = dt;
                    else
                        dtDogumTarihi.Value = DateTime.Now;

                    cbCinsiyet.Text = dr["cinsiyet"].ToString();
                    cbKanGrubu.Text = dr["kanGrubu"].ToString();
                    txtAdres.Text = dr["adres"].ToString();
                    txtTelNo.Text = dr["tel"].ToString();
                }

                dr.Close();
                baglanti.Close();
            }
            catch (Exception ex)
            {
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
                MessageBox.Show("Hasta bilgileri yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnKaydet_Click(object sender, EventArgs e)
        {
            
            if (txtDosyaNo.Text == "" || txtAd.Text == "" || txtSoyad.Text == "")
            {
                MessageBox.Show("Lütfen Dosya No, Ad ve Soyad alanlarını doldurunuz!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                string sorgu = "INSERT INTO dosyalar " +
                    "(dosyaNo, tcKimlikNo, ad, soyad, dogumTarihi, cinsiyet, kanGrubu, adres, tel) " +
                    "VALUES " +
                    "(@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";

                SqlCommand komut = new SqlCommand(sorgu, baglanti);

            
                komut.Parameters.AddWithValue("@p1", txtDosyaNo.Text);
                komut.Parameters.AddWithValue("@p2", txtTC.Text);
                komut.Parameters.AddWithValue("@p3", txtAd.Text);
                komut.Parameters.AddWithValue("@p4", txtSoyad.Text);
                komut.Parameters.AddWithValue("@p5", dtDogumTarihi.Value);
                komut.Parameters.AddWithValue("@p6", cbCinsiyet.Text);
                komut.Parameters.AddWithValue("@p7", cbKanGrubu.Text);
                komut.Parameters.AddWithValue("@p8", txtAdres.Text);

                
                komut.Parameters.AddWithValue("@p9", txtTelNo.Text);

                
                komut.ExecuteNonQuery();
                baglanti.Close();

                MessageBox.Show("Hasta başarıyla kaydedildi!", "Süper", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Close dialog and notify caller that a new patient was added
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception hata)
            {
                MessageBox.Show("Hata çıktı: " + hata.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
            }
        }

        
        private void btnYeni_Click(object sender, EventArgs e)
        {
            foreach (Control item in this.Controls)
            {
                if (item is TextBox) item.Text = "";
                if (item is ComboBox) item.Text = "";
            }
            dtDogumTarihi.Value = DateTime.Now;
        }

        private void btnCikis_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void dtDogumTarihi_ValueChanged(object sender, EventArgs e)
        {

        }

        private void btnYenile_Click(object sender, EventArgs e)
        {
           
            foreach (Control item in this.Controls)
            {
                
                if (item is TextBox)
                {
                    item.Text = "";
                }

               
                if (item is ComboBox)
                {
                    item.Text = "";
                }
            }

            dtDogumTarihi.Value = DateTime.Now;

            txtDosyaNo.Focus();
        }

        private void btnCikis_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}