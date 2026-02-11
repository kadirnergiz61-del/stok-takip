# StokTakipPortable (3 Depo - Ürün - Stok)

## PC'ye hiçbir program kurmadan çalıştırma
Bu mümkün ama **önceden derlenmiş tek bir .exe** dosyası gerekir.

Bu ZIP, kaynak kod + hazır proje içerir.
EXE üretmek için bir kere (sadece derleme makinesinde) şu 2 yoldan birini kullan:

### Yol A (en kolay): GitHub Actions ile EXE üret (PC'ye kurulum yok)
1) GitHub'da yeni repo aç.
2) Bu projeyi repoya yükle.
3) `.github/workflows/build.yml` dosyası zaten var.
4) Actions çalışınca `artifact` olarak `StokTakipPortable.exe` indireceksin.
5) İndirdiğin EXE'yi istediğin bilgisayarda **kurulumsuz** çalıştır.

### Yol B: Kendi PC'nde derle (derleyen PC'ye .NET SDK gerekir)
- `dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true`
çıkan EXE: `bin\Release\net8.0-windows\win-x64\publish\StokTakipPortable.exe`

## Veri dosyası
Program, EXE'nin yanında `data.json` dosyasına kaydeder (kolay yedek).
