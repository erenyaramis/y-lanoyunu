# Yılan Oyunu (Snake Game) - WinForms Uygulaması

C# Windows Forms ile geliştirilmiş, menü sistemi, skor tablosu, admin paneli ve özelleştirilebilir ayarlara sahip klasik Yılan oyunu.

## Genel Bakış

Uygulama `Form1` sınıfı üzerine kurulu, tek pencere içinde üç farklı panel (Giriş, Ayarlar, Skor Tablosu) arasında geçiş yaparak çalışan bir oyun. Oyun döngüsü `System.Windows.Forms.Timer` ile yönetiliyor, çizim işlemleri `Paint` olayında yapılıyor ve skorlar yerel bir metin dosyasında (`skorlar.txt`) saklanıyor.

## Bileşenler ve Değişkenler

### Oyun Durumu

| Değişken | Tip | Açıklama |
|---|---|---|
| `yilan` | `List<Point>` | Yılanın gövdesini oluşturan hücre koordinatları listesi (ilk eleman kafa) |
| `yem` | `Point` | Normal yemin konumu |
| `altinYem` | `Point` | Altın (bonus) yemin konumu |
| `yon` | `string` | Anlık hareket yönü: `"YUKARI"`, `"ASAGI"`, `"SOL"`, `"SAG"` |
| `oyunTimer` | `Timer` | Oyun döngüsünü tetikleyen zamanlayıcı |
| `skor` / `enYuksekSkor` | `int` | Anlık skor ve şimdiye kadarki en yüksek skor |
| `oyunBitti`, `oyunBasladiMi`, `oyunDuraklatildi` | `bool` | Oyunun durumunu (bitti/başladı/duraklatıldı) takip eden bayraklar |

### Altın Yem Mantığı

Her **4 normal yem** yendiğinde (`yenilenYemSayisi % 4 == 0`), ekranda 5 saniyeliğine (`5000 / oyunTimer.Interval` tur) bir altın yem belirir. Bu yem yenildiğinde normal yemin **3 katı** puan kazandırır.

### Modifiye (Özelleştirme) Ayarları

| Değişken | Açıklama |
|---|---|
| `aktifYemRengi` | Normal yemin rengi (Kırmızı, Mavi, Sarı, Pembe) |
| `aktifYilanKafaRengi` / `aktifYilanGövdeRengi` | Yılanın kafa ve gövde renkleri (Yeşil, Mavi, Beyaz, Turuncu) |
| `kazanilacakPuan` | Yem başına kazanılan puan (sabit: 10) |
| `duvarModu` | `"Duvara Çarp"` veya `"İçinden Geç"` (wrap-around) |
| `oyunTimer.Interval` | Zorluk seviyesine göre değişen oyun hızı: Kolay (140 ms), Orta (90 ms), Zor (50 ms) |

## Dosya İşlemleri (Yerel Skor Kayıt Sistemi)

Skorlar `skorlar.txt` dosyasında `Puan|İsim` formatında satır satır tutulur.

### `SkorlariLocaldenYukle()`
Uygulama açılışında dosyayı okur, her satırı `|` karakterine göre ayırır ve `(Puan, İsim)` tuple listesine ekler. Eski formatta (sadece puan içeren, isim olmayan) kayıtlarla geriye dönük uyumluluk sağlar — bu durumda isim `"Bilinmeyen Oyuncu"` olarak atanır. Liste puana göre büyükten küçüğe sıralanır.

### `SkoruLocaleKaydet(int yeniSkor, string oyuncuAdi)`
Yeni skoru listeye ekler, sıralar ve **sadece ilk 5 kaydı** tutacak şekilde kırpar. Ardından dosyayı tamamen yeniden yazar (`File.WriteAllLines`).

### `OyuncuIsmiIste(int kazanilanPuan)`
Oyun bittiğinde, eğer skor 0'dan büyükse açılan özel bir `Form` penceresi ile oyuncudan ismini ister. İsim boş bırakılırsa `"İsimsiz Oyuncu"` olarak kaydedilir.

## Arayüz Panelleri

Uygulama, aynı pencere üzerinde üst üste binen üç `Panel` ile çalışır; görünürlük (`Visible`) durumu değiştirilerek geçiş sağlanır.

### 1. Giriş Paneli (`girisPaneli`)
Ana menü. Dört buton içerir:
- **OYUNA BAŞLA** → `BaslaButonu_Click`: oyunu başlatır
- **MODİFİYE ET** → `ModifiyeButonu_Click`: ayarlar paneline geçer
- **SKOR TABLOSU** → `SkorButonu_Click`: skor tablosu paneline geçer
- **ÇIKIŞ** → `CikisButonu_Click`: uygulamayı kapatır

### 2. Ayarlar Paneli (`ayarlarPaneli`)
Dört `ComboBox` ile yem rengi, yılan rengi, zorluk ve duvar kuralı seçimi yapılır. **AYARLARI KAYDET VE DÖN** butonu (`KaydetButonu_Click`) seçimleri ilgili değişkenlere uygular ve giriş paneline döner.

### 3. Skor Tablosu Paneli (`skorlarPaneli`)
`SkorListesiniYenile()` metodu ile en yüksek 5 skoru numaralandırarak listeler (oyuncu adı 15 karakteri geçerse kısaltılır). Sağ üstte gizli bir **Admin ⚙** butonu bulunur.

## Admin Paneli

### `AdminGirisButonu_Click`
Kullanıcı adı/şifre isteyen bir giriş formu açar. Doğrulama bilgisi kod içinde sabit olarak tanımlıdır (`admin` / `admin123`). Giriş başarılıysa `AdminYonetimPaneliAc()` çağrılır.

> **Güvenlik Notu:** Kimlik bilgileri kaynak kodda açık metin olarak yer alıyor. Bu yöntem yalnızca basit/yerel bir oyun için kabul edilebilir; gerçek bir dağıtım senaryosunda şifreleme veya harici kimlik doğrulama kullanılmalıdır.

### `AdminYonetimPaneliAc()`
Onay istemiyle birlikte **tüm skorları kalıcı olarak silme** imkânı sunan bir yönetim formu açar. Onaylanırsa hem bellekteki liste hem de `skorlar.txt` dosyası silinir.

## Oyun Mekanikleri

### `OyunuBaslat()`
Yılanı 3 hücreden oluşan başlangıç haliyle sıfırlar, skor ve durum bayraklarını resetler, ilk yemi oluşturur ve zamanlayıcıyı başlatır.

### `YemOlustur()` / `AltinYemOlustur()`
Rastgele bir hücre koordinatı üretir; eğer üretilen konum yılanın üzerine denk geliyorsa (ya da altın yem için mevcut yemle çakışıyorsa) **özyinelemeli olarak** yeniden çağrılır.

### `OyunDongusu(object?, EventArgs)`
Her tik'te çalışan ana döngü:
1. Altın yem süresini azaltır, süre dolunca pasifleştirir.
2. Yılanın her hücresini bir öncekinin konumuna kaydırır (kuyruk takip mantığı).
3. Kafayı geçerli yöne göre hareket ettirir.
4. Duvar moduna göre ya ekranın diğer ucundan çıkar (`İçinden Geç`) ya da sınır dışına çıkınca oyunu bitirir (`Duvara Çarp`).
5. Kendine çarpma ve yem yeme kontrollerini yapar, ekranı yeniden çizdirir.

### `KendineCarptiMi()`
Kafanın koordinatı gövdenin diğer hücrelerinden biriyle eşleşiyorsa oyunu bitirir.

### `YemYediMi()`
Kafa normal yemle çakışırsa: puan eklenir, yılan büyür, her 4. yemde altın yem tetiklenir, yeni yem oluşturulur. Kafa altın yemle çakışırsa: 3 katı puan eklenir, yılan büyür, altın yem pasifleştirilir.

### `OyunBitis()`
Zamanlayıcıyı durdurur, skor pozitifse isim sorup kaydeder, sonuç mesaj kutusu gösterir. Kullanıcı "Evet" derse ana menüye döner, "Hayır" derse oyun yeniden başlar.

### `TusaBasildi(object?, KeyEventArgs)`
Klavye girişini işler:
- **Space / P** → duraklat/devam et
- **Ok tuşları** → yön değiştir (ters yöne anlık dönüş engellenir, örn. sağa giderken doğrudan sola dönülemez)

### `EkranCiz(object?, PaintEventArgs)`
`Paint` olayında çağrılır; yemleri, yılanın her hücresini (kafa farklı renkte), skor metnini ve duraklatma ekranını çizer.

## Akış Şeması (Özet)

```
Giriş Paneli
   ├── OYUNA BAŞLA ──────────► Oyun Döngüsü (Timer) ──► Oyun Bitti ──► İsim Sor ──► Skor Kaydet ──► Giriş/Yeniden Başlat
   ├── MODİFİYE ET ──────────► Ayarlar Paneli ──► Kaydet ──► Giriş Paneli
   ├── SKOR TABLOSU ─────────► Skor Listesi
   │                              └── Admin ⚙ ──► Giriş Doğrula ──► Skorları Sil
   └── ÇIKIŞ ────────────────► Uygulamayı Kapat
```

## Bilinen Sınırlamalar / Geliştirme Önerileri

- Admin kimlik bilgileri kaynak kodda açık şekilde yazılı; üretim ortamı için uygun değil.
- `kazanilacakPuan` arayüzden değiştirilemiyor (kod içinde sabit 10 olarak bırakılmış); ayarlar panelindeki ilgili kontrol kaldırılmış durumda.
- Skor dosyası (`skorlar.txt`) çalışma dizinine yazıldığından, uygulamanın çalıştığı klasöre yazma izni gerektirir.
- Yön değişimi anlık tuş girişiyle sınırlı; çapraz veya eş zamanlı tuş kombinasyonları desteklenmiyor.
