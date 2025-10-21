1.) Derleme ve Çalıştırma
Geliştirici Modu (kaynak koddan)

Proje dizininde:

dotnet build

Çalıştırma (geliştirici modu)

Tahmin üret (predict):
Girdi dosyasındaki (satır başına 1 kelime) her kelime için tahmin üretir. Aday yoksa o satır boş yazılır.

dotnet run -- predict --corpus data/corpus.txt \
--input data/my_misspelled.txt \
--output out/predictions.txt \
[--ext first-letter|tp]


Değerlendirme (eval):
Hatalı ve doğru dosyaları (satır sayıları eşit) vererek accuracy hesaplar. İstenirse tahminleri dosyaya döker.

dotnet run -- eval --corpus data/corpus.txt \
--misspelled data/test-words-misspelled.txt \
--gold data/test-words-correct.txt \
[--ext first-letter|tp] [--out out/eval-preds.txt]


Karşılaştırma (compare):
Üç konfigürasyonu aynı anda ölçer: Baseline, First-Letter, Transpose-First.

dotnet run -- compare --corpus data/corpus.txt \
--misspelled data/test-words-misspelled.txt \
--gold data/test-words-correct.txt \
[--out-base out/base.txt] [--out-first out/first.txt] [--out-tp out/tp.txt]

Yayınlanmış EXE ile (Windows)

İsteyen değerlendirici için tek dosyalık exe üretebilirsiniz:

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true


Çıktı:

.\bin\Release\net8.0\win-x64\publish\SpellingCorrector.exe


Kullanım örnekleri:

$exe = ".\bin\Release\net8.0\win-x64\publish\SpellingCorrector.exe"
& $exe eval --corpus .\data\corpus.txt --misspelled .\data\my-miss.txt --gold .\data\my-gold.txt
& $exe eval --corpus .\data\corpus.txt --misspelled .\data\my-miss.txt --gold .\data\my-gold.txt --ext tp
& $exe predict --corpus .\data\corpus.txt --input .\data\my_misspelled.txt --output .\out\preds.txt --ext first-letter

2.) Parametreler (Kısa Referans)
   Parametre	Açıklama
   --corpus <path>	Sözlük oluşturmak için corpus.txt yolu
   --input <path>	(predict) Hatalı kelimeler (satır başına 1 kelime)
   --output <path>	(predict) Tahminlerin yazılacağı dosya
   --misspelled <path>	(eval/compare) Hatalı kelimeler
   --gold <path>	(eval/compare) Doğru kelimeler (satır sayısı birebir aynı olmalı)
   --out <path>	(eval) İsteğe bağlı, tahminleri dosyaya döker
   --out-base / --out-first / --out-tp	(compare) Üç konfigürasyonun tahmin dosyaları
   --ext first-letter	İlk harfi eşleşen adaylar varsa yalnız onları değerlendir
   --ext tp veya --ext transpose-first	Sözlükte transpozisyon adayları varsa sadece onları değerlendir

Çıktı biçimi:

predict/eval --out dosyasında her satır bir tahmin olacak şekilde yazılır; tahmin yoksa boş satır bırakılır.

3.) Farklı Test Kümesiyle Çalıştırma

Kendi dosyalarınızı hazırlayın:

my_miss.txt: satır başına 1 hatalı kelime

my_gold.txt: aynı sayıda satır, her satırda doğru yazım

Gerekirse kendi corpus.txt dosyanızı kullanın (İngilizce kelime ağırlıklı olmalı).

Komutu kendi yollarınızla çağırın:

dotnet run -- eval --corpus path/to/corpus.txt \
--misspelled path/to/my_miss.txt \
--gold path/to/my_gold.txt \
[--ext tp]


Ya da EXE ile:

& .\bin\Release\net8.0\win-x64\publish\SpellingCorrector.exe `
  eval --corpus D:\data\my_corpus.txt `
--misspelled D:\data\my_miss.txt `
--gold D:\data\my_gold.txt
