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
    public partial class Login : Form
    {
        SqlConnection baglanti = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=SOHATS;Integrated Security=True");
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {

        }

        private void btnGiris_Click(object sender, EventArgs e)
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed)
                    baglanti.Open();

                string sql = "SELECT * FROM kullanicilar WHERE ad=@kadi AND sifre=@sifre";
                SqlCommand komut = new SqlCommand(sql, baglanti);

                komut.Parameters.AddWithValue("@kadi", txtKullaniciAdi.Text);
                komut.Parameters.AddWithValue("@sifre", txtSifre.Text);

                SqlDataReader dr = komut.ExecuteReader();

                // DÜZELTME BURADA: Sadece tek bir if (dr.Read()) yeterli.
                if (dr.Read())
                {

                    // 1. Veritabanından kullanıcının UNVAN bilgisini çekiyoruz
                    // Veritabanında unvan NULL (boş) ise program patlamasın diye kontrol ediyoruz
                    string gelenUnvan = "";
                    if (dr["unvan"] != DBNull.Value)
                    {
                        gelenUnvan = dr["unvan"].ToString();
                    }

                    // 2. Unvana göre yönlendirme yapıyoruz
                    if (gelenUnvan == "Doktor")
                    {
                        // --- DOKTOR GİRİŞİ ---
                        // Çözüm Gezgininde 'MainForm' gördüm, doktor için ana formu açtırabilirsin
                        // Veya doktor için özel bir formun varsa onun adını yaz (Örn: DoktorEkrani)

                        MainForm frmDoktor = new MainForm();
                        // Eğer MDI (iç içe form) kullanıyorsan şu satırı aç:
                        // frmDoktor.MdiParent = this.MdiParent; 
                        frmDoktor.Show();
                    }
                    else if (gelenUnvan == "Sekreter")
                    {
                        // --- SEKRETER GİRİŞİ ---
                        // Sekreter hasta işlemleri yapacağı için senin eski kodunu buraya koyuyoruz

                        HastaIslemleri frmSekreter = new HastaIslemleri();
                        frmSekreter.MdiParent = this.MdiParent; // Senin kodunda vardı, korudum
                        frmSekreter.Show();
                    }
                    else
                    {
                        MessageBox.Show("Hata: Bu kullanıcının unvanı (Doktor/Sekreter) tanımlanmamış!");
                        return; // Hata varsa login ekranı kapanmasın
                    }

                    // Giriş başarılı olduğu için Login ekranını kapatıyoruz
                    this.Close();
                }

                dr.Close();
                baglanti.Close();
            }
            catch (Exception hata)
            {
                MessageBox.Show("Bir hata oluştu: " + hata.Message);
            }
        }

        private void btnTemizle_Click(object sender, EventArgs e)
        {

        }

        private void btnCıkıs_Click(object sender, EventArgs e)
        {

        }
    }
}
