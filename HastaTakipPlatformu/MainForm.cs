using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HastaTakipPlatformu
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // menu item handlers intentionally left unbound here — forms will be added later by you
            // Bind Hasta İşlemleri menu item to open HastaIslemleri
            try
            {
                this.hastaIslemleriToolStripMenuItem.Click += (s, e) => OpenHastaIslemleri();
            }
            catch { }

            // Bind Kullanıcı Tanıtma under Referanslar to open KullaniciTanitma
            try
            {
                this.kullaniciTanıtmaToolStripMenuItem.Click += (s, e) => OpenKullaniciTanitma();
            }
            catch { }

            // Bind Poliklinik Tanıtma under Referanslar to open PoliklinikTanitma
            try
            {
                this.poliklinikTanıtmaToolStripMenuItem.Click += (s, e) => OpenPoliklinikTanitma();
            }
            catch { }

            // Bind Raporlar menu to open Sorgulama
            try
            {
                this.raporlarToolStripMenuItem.Click += (s, e) => OpenSorgulama();
            }
            catch { }
        }

        private void OpenHastaIslemleri()
        {
            // If already open, bring to front
            foreach (Form child in this.MdiChildren)
            {
                if (child is HastaIslemleri)
                {
                    child.BringToFront();
                    return;
                }
            }

            // Open new instance
            HastaIslemleri hiz = new HastaIslemleri();
            hiz.MdiParent = this;
            hiz.StartPosition = FormStartPosition.CenterParent;
            hiz.Show();
        }

        private void OpenKullaniciTanitma()
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is KullaniciTanitma)
                {
                    child.BringToFront();
                    return;
                }
            }

            KullaniciTanitma kt = new KullaniciTanitma();
            kt.MdiParent = this;
            kt.StartPosition = FormStartPosition.CenterParent;
            kt.Show();
        }

        private void OpenPoliklinikTanitma()
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is PoliklinikTanitma)
                {
                    child.BringToFront();
                    return;
                }
            }

            PoliklinikTanitma pt = new PoliklinikTanitma();
            pt.MdiParent = this;
            pt.StartPosition = FormStartPosition.CenterParent;
            pt.Show();
        }

        private void OpenSorgulama()
        {
            foreach (Form child in this.MdiChildren)
            {
                if (child is Sorgulama)
                {
                    child.BringToFront();
                    return;
                }
            }

            Sorgulama s = new Sorgulama();
            s.MdiParent = this;
            s.StartPosition = FormStartPosition.CenterParent;
            s.Show();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Login f = new Login();
            f.MdiParent = this;  
            f.StartPosition = FormStartPosition.CenterScreen;   
            f.Show();
        }
    }
}
