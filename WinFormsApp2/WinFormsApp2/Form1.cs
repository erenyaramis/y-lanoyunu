using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        // Oyun bileşenleri
        private List<Point> yilan = new List<Point>();
        private Point yem;
        private Point altinYem;
        private string yon = "SAG";
        private System.Windows.Forms.Timer oyunTimer = new System.Windows.Forms.Timer();
        private Random rnd = new Random();

        private const int HUCRE_BOYUTU = 20;
        private int skor = 0;
        private int enYuksekSkor = 0;
        private bool oyunBitti = false;
        private bool oyunBasladiMi = false;
        private bool oyunDuraklatildi = false;

        // Altın Yem Mantığı
        private bool altinYemAktif = false;
        private int yenilenYemSayisi = 0;
        private int altinYemSureSayaci = 0;

        // Modifiye Ayarları
        private Brush aktifYemRengi = Brushes.Red;
        private Brush aktifYilanKafaRengi = Brushes.LimeGreen;
        private Brush aktifYilanGövdeRengi = Brushes.ForestGreen;
        private int kazanilacakPuan = 10; // Sabit olarak 10 puanda bırakıldı
        private string duvarModu = "Duvara Çarp";

        // Local Kayıt Değişkenleri
        private const string DOSYA_ADI = "skorlar.txt";
        private List<(int Puan, string Isim)> enYuksekSkorlarListesi = new List<(int Puan, string Isim)>();

        // Arayüz Panelleri ve Kontrolleri
        private Panel girisPaneli = null!;
        private Panel ayarlarPaneli = null!;
        private Panel skorlarPaneli = null!;
        private ComboBox cmbYemRengi = null!;
        private ComboBox cmbYilanRengi = null!;
        private ComboBox cmbZorluk = null!;
        private ComboBox cmbDuvarModu = null!;
        private Label lblSkorListesi = null!;

        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.ClientSize = new Size(600, 600);
            this.BackColor = Color.Black;

            oyunTimer.Tick += OyunDongusu;

            this.KeyDown += TusaBasildi;
            this.Paint += EkranCiz;

            SkorlariLocaldenYukle();
            PanelleriOlustur();
        }

        // --- DOSYA İŞLEMLERİ ---

        private void SkorlariLocaldenYukle()
        {
            enYuksekSkorlarListesi.Clear();
            try
            {
                if (File.Exists(DOSYA_ADI))
                {
                    string[] satirlar = File.ReadAllLines(DOSYA_ADI);
                    foreach (string satir in satirlar)
                    {
                        string[] parcalar = satir.Split('|');

                        if (parcalar.Length >= 2 && int.TryParse(parcalar[0], out int okunanSkor))
                        {
                            enYuksekSkorlarListesi.Add((okunanSkor, parcalar[1]));
                        }
                        else if (parcalar.Length == 1 && int.TryParse(parcalar[0], out int eskiTekSkor))
                        {
                            enYuksekSkorlarListesi.Add((eskiTekSkor, "Bilinmeyen Oyuncu"));
                        }
                    }
                    enYuksekSkorlarListesi.Sort((a, b) => b.Puan.CompareTo(a.Puan));
                }
            }
            catch (Exception) { }

            if (enYuksekSkorlarListesi.Count > 0)
            {
                enYuksekSkor = enYuksekSkorlarListesi[0].Puan;
            }
            else
            {
                enYuksekSkor = 0;
            }
        }

        private void SkoruLocaleKaydet(int yeniSkor, string oyuncuAdi)
        {
            if (yeniSkor <= 0) return;

            enYuksekSkorlarListesi.Add((yeniSkor, oyuncuAdi));
            enYuksekSkorlarListesi.Sort((a, b) => b.Puan.CompareTo(a.Puan));

            if (enYuksekSkorlarListesi.Count > 5)
            {
                enYuksekSkorlarListesi.RemoveRange(5, enYuksekSkorlarListesi.Count - 5);
            }

            try
            {
                List<string> satirlar = new List<string>();
                foreach (var kayit in enYuksekSkorlarListesi)
                {
                    satirlar.Add($"{kayit.Puan}|{kayit.Isim}");
                }
                File.WriteAllLines(DOSYA_ADI, satirlar);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Skor kaydedilirken hata: " + ex.Message);
            }

            if (enYuksekSkorlarListesi.Count > 0)
            {
                enYuksekSkor = enYuksekSkorlarListesi[0].Puan;
            }
        }

        private string OyuncuIsmiIste(int kazanilanPuan)
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Yeni Skor!",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Width = 300, Text = $"Tebrikler! {kazanilanPuan} puan aldın.\nİsmini gir:", ForeColor = Color.White, Font = new Font("Arial", 12) };
            TextBox textBox = new TextBox() { Left = 20, Top = 65, Width = 290, Font = new Font("Arial", 12) };
            Button confirmation = new Button() { Text = "Kaydet", Left = 200, Top = 100, Width = 110, BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox); prompt.Controls.Add(confirmation); prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            prompt.ShowDialog();
            return string.IsNullOrWhiteSpace(textBox.Text) ? "İsimsiz Oyuncu" : textBox.Text.Trim();
        }

        // --- ARAYÜZ ---

        private void PanelleriOlustur()
        {
            // ================= AYARLAR (MODİFİYE) PANELİ =================
            ayarlarPaneli = new Panel() { Size = new Size(600, 600), Location = new Point(0, 0), BackColor = Color.FromArgb(30, 30, 30), Visible = false };
            Label ayarBaslik = new Label() { Text = "GELİŞMİŞ MODİFİYE", Font = new Font("Arial", 28, FontStyle.Bold), ForeColor = Color.Orange, AutoSize = false, Size = new Size(600, 60), Location = new Point(0, 40), TextAlign = ContentAlignment.MiddleCenter };

            Label lblYem = new Label() { Text = "Yem Rengi:", Font = new Font("Arial", 12), ForeColor = Color.White, Location = new Point(150, 130), AutoSize = true };
            cmbYemRengi = new ComboBox() { Location = new Point(320, 130), Size = new Size(130, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 11) };
            cmbYemRengi.Items.AddRange(new string[] { "Kırmızı", "Mavi", "Sarı", "Pembe" }); cmbYemRengi.SelectedIndex = 0;

            Label lblYilan = new Label() { Text = "Yılan Rengi:", Font = new Font("Arial", 12), ForeColor = Color.White, Location = new Point(150, 180), AutoSize = true };
            cmbYilanRengi = new ComboBox() { Location = new Point(320, 180), Size = new Size(130, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 11) };
            cmbYilanRengi.Items.AddRange(new string[] { "Yeşil", "Mavi", "Beyaz", "Turuncu" }); cmbYilanRengi.SelectedIndex = 0;

            // Puan ayarı kaldırıldı, aşağıdaki elemanlar yukarı kaydırıldı (Y ekseninde -50 piksel)
            Label lblZorluk = new Label() { Text = "Zorluk (Hız):", Font = new Font("Arial", 12), ForeColor = Color.White, Location = new Point(150, 230), AutoSize = true };
            cmbZorluk = new ComboBox() { Location = new Point(320, 230), Size = new Size(130, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 11) };
            cmbZorluk.Items.AddRange(new string[] { "Kolay", "Orta", "Zor" }); cmbZorluk.SelectedIndex = 1;

            Label lblDuvar = new Label() { Text = "Duvar Kuralı:", Font = new Font("Arial", 12), ForeColor = Color.White, Location = new Point(150, 280), AutoSize = true };
            cmbDuvarModu = new ComboBox() { Location = new Point(320, 280), Size = new Size(130, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 11) };
            cmbDuvarModu.Items.AddRange(new string[] { "Duvara Çarp", "İçinden Geç" }); cmbDuvarModu.SelectedIndex = 0;

            Button btnKaydet = new Button() { Text = "AYARLARI KAYDET VE DÖN", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.DodgerBlue, Size = new Size(240, 50), Location = new Point(180, 380), FlatStyle = FlatStyle.Flat };
            btnKaydet.FlatAppearance.BorderSize = 0; btnKaydet.Click += KaydetButonu_Click;

            ayarlarPaneli.Controls.Add(ayarBaslik); ayarlarPaneli.Controls.Add(lblYem); ayarlarPaneli.Controls.Add(cmbYemRengi);
            ayarlarPaneli.Controls.Add(lblYilan); ayarlarPaneli.Controls.Add(cmbYilanRengi);
            ayarlarPaneli.Controls.Add(lblZorluk); ayarlarPaneli.Controls.Add(cmbZorluk);
            ayarlarPaneli.Controls.Add(lblDuvar); ayarlarPaneli.Controls.Add(cmbDuvarModu); ayarlarPaneli.Controls.Add(btnKaydet);

            // ================= SKOR TABLOSU PANELİ =================
            skorlarPaneli = new Panel() { Size = new Size(600, 600), Location = new Point(0, 0), BackColor = Color.FromArgb(25, 25, 35), Visible = false };
            Label skorBaslik = new Label() { Text = "EN YÜKSEK SKORLAR", Font = new Font("Arial", 28, FontStyle.Bold), ForeColor = Color.Gold, AutoSize = false, Size = new Size(600, 60), Location = new Point(0, 50), TextAlign = ContentAlignment.MiddleCenter };
            lblSkorListesi = new Label() { Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Size = new Size(500, 280), Location = new Point(50, 140), TextAlign = ContentAlignment.TopCenter };

            Button btnSkorGeri = new Button() { Text = "ANA MENÜYE DÖN", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.SlateGray, Size = new Size(200, 50), Location = new Point(200, 450), FlatStyle = FlatStyle.Flat };
            btnSkorGeri.FlatAppearance.BorderSize = 0; btnSkorGeri.Click += SkorGeriButonu_Click;

            Button btnAdminGiris = new Button() { Text = "Admin ⚙", Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.Gray, BackColor = Color.FromArgb(40, 40, 50), Size = new Size(80, 28), Location = new Point(500, 15), FlatStyle = FlatStyle.Flat };
            btnAdminGiris.FlatAppearance.BorderSize = 0; btnAdminGiris.Click += AdminGirisButonu_Click;

            skorlarPaneli.Controls.Add(skorBaslik); skorlarPaneli.Controls.Add(lblSkorListesi); skorlarPaneli.Controls.Add(btnSkorGeri); skorlarPaneli.Controls.Add(btnAdminGiris);

            // ================= GİRİŞ PANELİ =================
            girisPaneli = new Panel() { Size = new Size(600, 600), Location = new Point(0, 0), BackColor = Color.FromArgb(20, 20, 20) };
            Label baslikLabel = new Label() { Text = "YILAN OYUNU\nARCADE", Font = new Font("Arial", 36, FontStyle.Bold), ForeColor = Color.LimeGreen, AutoSize = false, Size = new Size(600, 120), Location = new Point(0, 60), TextAlign = ContentAlignment.MiddleCenter };
            Button baslaButonu = new Button() { Text = "OYUNA BAŞLA", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.ForestGreen, Size = new Size(200, 50), Location = new Point(200, 220), FlatStyle = FlatStyle.Flat };
            baslaButonu.FlatAppearance.BorderSize = 0; baslaButonu.Click += BaslaButonu_Click;
            Button modifiyeButonu = new Button() { Text = "MODİFİYE ET", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.DarkOrange, Size = new Size(200, 50), Location = new Point(200, 290), FlatStyle = FlatStyle.Flat };
            modifiyeButonu.FlatAppearance.BorderSize = 0; modifiyeButonu.Click += ModifiyeButonu_Click;
            Button skorButonu = new Button() { Text = "SKOR TABLOSU", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.Black, BackColor = Color.Gold, Size = new Size(200, 50), Location = new Point(200, 360), FlatStyle = FlatStyle.Flat };
            skorButonu.FlatAppearance.BorderSize = 0; skorButonu.Click += SkorButonu_Click;
            Button cikisButonu = new Button() { Text = "ÇIKIŞ", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Firebrick, Size = new Size(200, 50), Location = new Point(200, 430), FlatStyle = FlatStyle.Flat };
            cikisButonu.FlatAppearance.BorderSize = 0; cikisButonu.Click += CikisButonu_Click;

            girisPaneli.Controls.Add(baslikLabel); girisPaneli.Controls.Add(baslaButonu); girisPaneli.Controls.Add(modifiyeButonu); girisPaneli.Controls.Add(skorButonu); girisPaneli.Controls.Add(cikisButonu);
            this.Controls.Add(skorlarPaneli); this.Controls.Add(ayarlarPaneli); this.Controls.Add(girisPaneli);
        }

        // --- TIKLAMA OLAYLARI ---

        private void BaslaButonu_Click(object? sender, EventArgs e) { girisPaneli.Visible = false; oyunBasladiMi = true; OyunuBaslat(); this.Focus(); }
        private void ModifiyeButonu_Click(object? sender, EventArgs e) { girisPaneli.Visible = false; ayarlarPaneli.Visible = true; }
        private void SkorGeriButonu_Click(object? sender, EventArgs e) { skorlarPaneli.Visible = false; girisPaneli.Visible = true; }
        private void CikisButonu_Click(object? sender, EventArgs e) { Application.Exit(); }

        private void SkorButonu_Click(object? sender, EventArgs e)
        {
            girisPaneli.Visible = false;
            skorlarPaneli.Visible = true;
            SkorListesiniYenile();
        }

        private void SkorListesiniYenile()
        {
            lblSkorListesi.Text = "";
            if (enYuksekSkorlarListesi.Count == 0) lblSkorListesi.Text = "\nHenüz kaydedilmiş skor yok!";
            else
            {
                for (int i = 0; i < enYuksekSkorlarListesi.Count; i++)
                {
                    string oyuncu = enYuksekSkorlarListesi[i].Isim;
                    if (oyuncu.Length > 15) oyuncu = oyuncu.Substring(0, 15) + "...";
                    lblSkorListesi.Text += $"{i + 1}. {oyuncu.PadRight(20)} {enYuksekSkorlarListesi[i].Puan} Puan\n\n";
                }
            }
        }

        private void AdminGirisButonu_Click(object? sender, EventArgs e)
        {
            Form loginForm = new Form()
            {
                Width = 300,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Admin Yetkilendirme",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            Label lblUser = new Label() { Left = 20, Top = 25, Text = "Kullanıcı Adı:", ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold) };
            TextBox txtUser = new TextBox() { Left = 130, Top = 22, Width = 130, Font = new Font("Arial", 10) };

            Label lblPass = new Label() { Left = 20, Top = 65, Text = "Şifre:", ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold) };
            TextBox txtPass = new TextBox() { Left = 130, Top = 62, Width = 130, Font = new Font("Arial", 10), PasswordChar = '*' };

            Button btnLogin = new Button() { Text = "Giriş Yap", Left = 150, Top = 115, Width = 110, Height = 35, BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLogin.FlatAppearance.BorderSize = 0;

            loginForm.Controls.Add(lblUser); loginForm.Controls.Add(txtUser);
            loginForm.Controls.Add(lblPass); loginForm.Controls.Add(txtPass);
            loginForm.Controls.Add(btnLogin);
            loginForm.AcceptButton = btnLogin;

            bool girisBasarili = false;
            btnLogin.Click += (s, ev) =>
            {
                if (txtUser.Text == "admin" && txtPass.Text == "admin123")
                {
                    girisBasarili = true;
                    loginForm.Close();
                }
                else
                {
                    MessageBox.Show("Hatalı Kullanıcı Adı veya Şifre!", "Erişim Reddedildi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            loginForm.ShowDialog();

            if (girisBasarili)
            {
                AdminYonetimPaneliAc();
            }
        }

        private void AdminYonetimPaneliAc()
        {
            Form adminForm = new Form()
            {
                Width = 360,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Yönetici Kontrol Paneli",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(35, 35, 45)
            };

            Label lblBilgi = new Label() { Left = 20, Top = 25, Width = 310, Height = 40, Text = "Sistem yetkileri doğrulandı.\nLocal veri tabanındaki tüm skorları temizleyebilirsiniz.", ForeColor = Color.LightGray, Font = new Font("Arial", 10) };

            Button btnSil = new Button() { Text = "🗑 TÜM SKORLARI KALICI SİL", Left = 20, Top = 85, Width = 300, Height = 45, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Arial", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSil.FlatAppearance.BorderSize = 0;

            btnSil.Click += (s, ev) =>
            {
                DialogResult eminMisin = MessageBox.Show("Tüm oyuncuların kayıtlı skor geçmişi kalıcı olarak silinecektir. Bu işlem geri alınamaz!\n\nEmin misiniz?", "Kritik Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (eminMisin == DialogResult.Yes)
                {
                    try
                    {
                        enYuksekSkorlarListesi.Clear();
                        enYuksekSkor = 0;

                        if (File.Exists(DOSYA_ADI))
                        {
                            File.Delete(DOSYA_ADI);
                        }

                        SkorListesiniYenile();
                        MessageBox.Show("Skor tablosu ve yerel veri dosyası tamamen temizlendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        adminForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Dosya silinirken bir hata meydana geldi: " + ex.Message);
                    }
                }
            };

            adminForm.Controls.Add(lblBilgi);
            adminForm.Controls.Add(btnSil);
            adminForm.ShowDialog();
        }

        private void KaydetButonu_Click(object? sender, EventArgs e)
        {
            // Puan alma satırı kaldırıldı, sabit değer kullanılacak.

            if (cmbDuvarModu.SelectedItem != null) duvarModu = cmbDuvarModu.SelectedItem.ToString() ?? "Duvara Çarp";

            if (cmbZorluk.SelectedItem != null)
            {
                switch (cmbZorluk.SelectedItem.ToString())
                {
                    case "Kolay": oyunTimer.Interval = 140; break;
                    case "Orta": oyunTimer.Interval = 90; break;
                    case "Zor": oyunTimer.Interval = 50; break;
                }
            }

            if (cmbYemRengi.SelectedItem != null)
            {
                switch (cmbYemRengi.SelectedItem.ToString())
                {
                    case "Kırmızı": aktifYemRengi = Brushes.Red; break;
                    case "Mavi": aktifYemRengi = Brushes.DeepSkyBlue; break;
                    case "Sarı": aktifYemRengi = Brushes.Yellow; break;
                    case "Pembe": aktifYemRengi = Brushes.HotPink; break;
                }
            }

            if (cmbYilanRengi.SelectedItem != null)
            {
                switch (cmbYilanRengi.SelectedItem.ToString())
                {
                    case "Yeşil": aktifYilanKafaRengi = Brushes.LimeGreen; aktifYilanGövdeRengi = Brushes.ForestGreen; break;
                    case "Mavi": aktifYilanKafaRengi = Brushes.Cyan; aktifYilanGövdeRengi = Brushes.Teal; break;
                    case "Beyaz": aktifYilanKafaRengi = Brushes.White; aktifYilanGövdeRengi = Brushes.LightGray; break;
                    case "Turuncu": aktifYilanKafaRengi = Brushes.Orange; aktifYilanGövdeRengi = Brushes.DarkOrange; break;
                }
            }

            ayarlarPaneli.Visible = false; girisPaneli.Visible = true;
        }

        // --- OYUN MEKANİKLERİ ---

        private void OyunuBaslat()
        {
            yilan.Clear(); yilan.Add(new Point(300, 300)); yilan.Add(new Point(280, 300)); yilan.Add(new Point(260, 300));
            yon = "SAG"; skor = 0; yenilenYemSayisi = 0; oyunBitti = false; oyunDuraklatildi = false; altinYemAktif = false;
            YemOlustur(); oyunTimer.Start();
        }

        private void YemOlustur()
        {
            int xXor = rnd.Next(0, 600 / HUCRE_BOYUTU) * HUCRE_BOYUTU;
            int yYor = rnd.Next(0, 600 / HUCRE_BOYUTU) * HUCRE_BOYUTU;
            yem = new Point(xXor, yYor);
            if (yilan.Contains(yem)) YemOlustur();
        }

        private void AltinYemOlustur()
        {
            int xXor = rnd.Next(0, 600 / HUCRE_BOYUTU) * HUCRE_BOYUTU;
            int yYor = rnd.Next(0, 600 / HUCRE_BOYUTU) * HUCRE_BOYUTU;
            altinYem = new Point(xXor, yYor);
            if (yilan.Contains(altinYem) || altinYem == yem) AltinYemOlustur();
            altinYemAktif = true; altinYemSureSayaci = 5000 / oyunTimer.Interval;
        }

        private void OyunDongusu(object? sender, EventArgs e)
        {
            if (oyunBitti || !oyunBasladiMi || oyunDuraklatildi) return;

            if (altinYemAktif) { altinYemSureSayaci--; if (altinYemSureSayaci <= 0) altinYemAktif = false; }

            for (int i = yilan.Count - 1; i > 0; i--) yilan[i] = yilan[i - 1];

            Point kafa = yilan[0];
            switch (yon)
            {
                case "YUKARI": kafa.Y -= HUCRE_BOYUTU; break;
                case "ASAGI": kafa.Y += HUCRE_BOYUTU; break;
                case "SOL": kafa.X -= HUCRE_BOYUTU; break;
                case "SAG": kafa.X += HUCRE_BOYUTU; break;
            }
            yilan[0] = kafa;

            if (duvarModu == "İçinden Geç")
            {
                if (yilan[0].X < 0) yilan[0] = new Point(600 - HUCRE_BOYUTU, yilan[0].Y);
                else if (yilan[0].X >= 600) yilan[0] = new Point(0, yilan[0].Y);
                else if (yilan[0].Y < 0) yilan[0] = new Point(yilan[0].X, 600 - HUCRE_BOYUTU);
                else if (yilan[0].Y >= 600) yilan[0] = new Point(yilan[0].X, 0);
            }
            else
            {
                if (kafa.X < 0 || kafa.X >= 600 || kafa.Y < 0 || kafa.Y >= 600) { OyunBitis(); return; }
            }

            KendineCarptiMi(); YemYediMi(); this.Invalidate();
        }

        private void KendineCarptiMi()
        {
            Point kafa = yilan[0];
            for (int i = 1; i < yilan.Count; i++) if (kafa == yilan[i]) OyunBitis();
        }

        private void YemYediMi()
        {
            if (yilan[0] == yem)
            {
                skor += kazanilacakPuan; yenilenYemSayisi++;
                yilan.Add(new Point(yilan[yilan.Count - 1].X, yilan[yilan.Count - 1].Y));
                if (yenilenYemSayisi % 4 == 0 && !altinYemAktif) AltinYemOlustur();
                YemOlustur();
            }
            if (altinYemAktif && yilan[0] == altinYem)
            {
                skor += (kazanilacakPuan * 3);
                yilan.Add(new Point(yilan[yilan.Count - 1].X, yilan[yilan.Count - 1].Y));
                altinYemAktif = false;
            }
        }

        private void OyunBitis()
        {
            oyunTimer.Stop();
            oyunBitti = true;
            oyunBasladiMi = false;

            if (skor > 0)
            {
                string isim = PromptWindowName(skor);
                SkoruLocaleKaydet(skor, isim);
            }

            DialogResult sonuc = MessageBox.Show($"Oyun Bitti!\nSkorunuz: {skor}\nEn Yüksek Skor: {enYuksekSkor}\n\nAna menüye dönmek ister misiniz?", "Game Over", MessageBoxButtons.YesNo);

            if (sonuc == DialogResult.Yes) girisPaneli.Visible = true;
            else { oyunBasladiMi = true; OyunuBaslat(); this.Focus(); }
        }

        private string PromptWindowName(int currentScore)
        {
            return OyuncuIsmiIste(currentScore);
        }

        private void TusaBasildi(object? sender, KeyEventArgs e)
        {
            if (oyunBasladiMi && !oyunBitti && (e.KeyCode == Keys.Space || e.KeyCode == Keys.P))
            {
                oyunDuraklatildi = !oyunDuraklatildi; this.Invalidate(); return;
            }
            if (!oyunBasladiMi || oyunDuraklatildi) return;

            switch (e.KeyCode)
            {
                case Keys.Up: if (yon != "ASAGI") yon = "YUKARI"; break;
                case Keys.Down: if (yon != "YUKARI") yon = "ASAGI"; break;
                case Keys.Left: if (yon != "SAG") yon = "SOL"; break;
                case Keys.Right: if (yon != "SOL") yon = "SAG"; break;
            }
        }

        private void EkranCiz(object? sender, PaintEventArgs e)
        {
            if (!oyunBasladiMi) return;
            Graphics g = e.Graphics;

            g.FillEllipse(aktifYemRengi, new Rectangle(yem.X, yem.Y, HUCRE_BOYUTU, HUCRE_BOYUTU));
            if (altinYemAktif) g.FillEllipse(Brushes.Gold, new Rectangle(altinYem.X, altinYem.Y, HUCRE_BOYUTU, HUCRE_BOYUTU));

            for (int i = 0; i < yilan.Count; i++)
            {
                Brush yilanRengi = (i == 0) ? aktifYilanKafaRengi : aktifYilanGövdeRengi;
                g.FillRectangle(yilanRengi, new Rectangle(yilan[i].X, yilan[i].Y, HUCRE_BOYUTU, HUCRE_BOYUTU));
                g.DrawRectangle(Pens.Black, new Rectangle(yilan[i].X, yilan[i].Y, HUCRE_BOYUTU, HUCRE_BOYUTU));
            }

            string skorMetni = $"Skor: {skor}   |   En Yüksek Skor: {enYuksekSkor}";
            using (Font skorFont = new Font("Arial", 12, FontStyle.Bold)) { g.DrawString(skorMetni, skorFont, Brushes.White, new Point(10, 10)); }

            if (oyunDuraklatildi)
            {
                string duraklatMetni = "OYUN DURAKLATILDI\nDevam etmek için SPACE'e basın.";
                using (Font durFont = new Font("Arial", 20, FontStyle.Bold))
                {
                    SizeF boyut = g.MeasureString(duraklatMetni, durFont);
                    g.DrawString(duraklatMetni, durFont, Brushes.Yellow, new Point((600 - (int)boyut.Width) / 2, (600 - (int)boyut.Height) / 2));
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e) { }
    }
}