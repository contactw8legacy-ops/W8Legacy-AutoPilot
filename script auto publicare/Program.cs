using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace W8LegacyAutopilot
{
    class ViralVideoInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long ViewCount { get; set; }
        public string Url { get; set; }
        public string ChannelName { get; set; }
    }

    class Program
    {
        static List<string> SubiecteFolosite = new List<string>();
        static string WorkFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "W8_AutoPilot_Output");
        static string IstoricPath = Path.Combine(WorkFolder, "history.json");

        static string ApiKey = "";
        static string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-lite-latest:generateContent";

        static string FfmpegPath = ObțineCaleFfmpeg();
        static string YtdlpPath = ObțineCaleYtdlp();

        static string ObțineCaleFfmpeg()
        {
            if (File.Exists("ffmpeg.exe")) return "ffmpeg.exe";
            try
            {
                string wingetPackages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Packages");
                if (Directory.Exists(wingetPackages))
                {
                    var found = Directory.GetFiles(wingetPackages, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(found)) return found;
                }
            }
            catch { }
            return "ffmpeg.exe";
        }

        static string ObțineCaleYtdlp()
        {
            if (File.Exists("yt-dlp.exe")) return "yt-dlp.exe";
            try
            {
                string wingetLinks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Links\yt-dlp.exe");
                if (File.Exists(wingetLinks)) return wingetLinks;
            }
            catch { }
            return "yt-dlp.exe";
        }

        static void AsiguraCheiaApi()
        {
            ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User);

            while (string.IsNullOrWhiteSpace(ApiKey))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[CONFIGURARE INIȚIALĂ] Nu am găsit cheia GEMINI_API_KEY în Windows.");
                Console.ResetColor();
                Console.Write("Lipește cheia ta API de la Google AI Studio aici (Click-dreapta) și apasă ENTER: ");
                string inputKey = Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(inputKey))
                {
                    ApiKey = inputKey;
                    Environment.SetEnvironmentVariable("GEMINI_API_KEY", ApiKey);
                    try
                    {
                        Environment.SetEnvironmentVariable("GEMINI_API_KEY", ApiKey, EnvironmentVariableTarget.User);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("-> Cheia a fost salvată automat în Windows! Nu va mai trebui să o introduci data viitoare.\n");
                        Console.ResetColor();
                        Thread.Sleep(1500);
                    }
                    catch { }
                }
            }
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (!Directory.Exists(WorkFolder)) Directory.CreateDirectory(WorkFolder);

            AsiguraCheiaApi();
            IncarcaIstoric();

            bool ruleazaProgramul = true;
            while (ruleazaProgramul)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== W8 Legacy AutoPilot (V41: 100% GitHub Ready & Multi B-Roll) ===");
                Console.ResetColor();
                Console.WriteLine($"[INFO] Folder salvare: {WorkFolder}");
                Console.WriteLine($"[INFO] Subiecte în istoric: {SubiecteFolosite.Count}");
                Console.WriteLine($"[INFO] FFmpeg: {(File.Exists(FfmpegPath) ? "OK" : "LIPSEȘTE")} | yt-dlp: {(File.Exists(YtdlpPath) ? "OK" : "LIPSEȘTE")}\n");
                Console.WriteLine("1. Generează UN clip clasic (Multi B-Roll + Narator)");
                Console.WriteLine("2. Generează un LOT de clipuri clasice (Batch Mode)");
                Console.WriteLine("3. [AUTO-RADAR GLOBAL] Caută și clonează clipuri virale");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("4. [STORYTELLING] Generator 'Povești din Târg / Service' (Montaj Video Variat)");
                Console.ResetColor();
                Console.WriteLine("5. Ieșire");
                Console.Write("\nAlege opțiunea (1/2/3/4/5): ");

                var optiune = Console.ReadLine()?.Trim();

                switch (optiune)
                {
                    case "1":
                        await ProceseazaUnClip(1, 1, true);
                        Console.WriteLine("\nApasă ENTER pentru a reveni la meniu...");
                        Console.ReadLine();
                        break;
                    case "2":
                        await GenerateBatchAsync();
                        break;
                    case "3":
                        await GlobalRadarSearchModeAsync();
                        Console.WriteLine("\nApasă ENTER pentru a reveni la meniu...");
                        Console.ReadLine();
                        break;
                    case "4":
                        await CreateHybridStoryModeAsync();
                        Console.WriteLine("\nApasă ENTER pentru a reveni la meniu...");
                        Console.ReadLine();
                        break;
                    case "5":
                        ruleazaProgramul = false;
                        break;
                    default:
                        Console.WriteLine("Opțiune invalidă.");
                        break;
                }
            }
        }

        static void IncarcaIstoric()
        {
            if (File.Exists(IstoricPath))
            {
                try
                {
                    string json = File.ReadAllText(IstoricPath);
                    SubiecteFolosite = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
                catch { }
            }
        }

        static void SalveazaInIstoric(string subiectNou)
        {
            if (!string.IsNullOrWhiteSpace(subiectNou))
            {
                SubiecteFolosite.Add(subiectNou);
                Directory.CreateDirectory(Path.GetDirectoryName(IstoricPath));
                File.WriteAllText(IstoricPath, JsonConvert.SerializeObject(SubiecteFolosite, Formatting.Indented));
            }
        }

        static void CleanupTempFiles(string workFolder)
        {
            try
            {
                if (!Directory.Exists(workFolder)) return;
                var files = Directory.GetFiles(workFolder);
                foreach (var file in files)
                {
                    string fName = Path.GetFileName(file).ToLower();
                    if (fName == "client_secrets.json" ||
                        fName == "history.json" ||
                        fName == "downloaded_ids.txt" ||
                        fName.StartsWith("tiktok_") ||
                        fName.StartsWith("youtube_"))
                    {
                        continue;
                    }
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }

        static async Task CreateHybridStoryModeAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("=== GENERATOR POVEȘTI (DRAMĂ AUTO + MULTI B-ROLL SPECIFIC) ===");
            Console.ResetColor();

            string plotIdea = "";
            while (string.IsNullOrWhiteSpace(plotIdea))
            {
                Console.WriteLine("Despre ce să fie vorba în povestea narată? (ex: țeapă cu un Audi A4 B9, motor spart din cauza uleiului longlife, navigație blocată)");
                Console.Write("\nIdeea poveștii (sau '0' pentru preluare din Radar): ");
                plotIdea = Console.ReadLine()?.Trim();
            }

            if (plotIdea == "0")
            {
                string q = "";
                while (string.IsNullOrWhiteSpace(q))
                {
                    Console.Write("\nCe termen căutăm în Radar Auto? (ex: audi a4 b9, passat b6, mechanic fail): ");
                    q = Console.ReadLine()?.Trim();
                }

                var virale = await SearchTopViralVideos(q);
                if (virale.Count > 0)
                {
                    Console.WriteLine("\nAlege clipul viral de inspirație:");
                    for (int i = 0; i < Math.Min(6, virale.Count); i++)
                    {
                        Console.WriteLine($"{i + 1}. {virale[i].Title} ({virale[i].ViewCount:N0} views)");
                    }
                    int al = 1;
                    Console.Write("\nNumărul ales: ");
                    int.TryParse(Console.ReadLine()?.Trim(), out al);
                    al = Math.Max(1, Math.Min(virale.Count, al));
                    string tr = await ExtractVideoTranscript(virale[al - 1].Url);
                    plotIdea = $"Inspirat din '{virale[al - 1].Title}'. Context: {tr}";
                }
            }

            CleanupTempFiles(WorkFolder);

            string script = "";
            string metaTitle = "EP1. Am cumpărat o mașină cu probleme?! #auto #masini #romania";
            string keywords = "";

            bool aprobare = false;
            while (!aprobare)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n   -> [AI] Se scrie scenariul și se generează 12 termeni vizuali de B-Roll...");
                Console.ResetColor();

                string prompt = $@"Ești un vlogger auto din România, un mecanic pasionat care spune adevărul, nu ce scrie în manualele corporatiste. Scrie o poveste super captivantă și dramatică la persoana I (ca și cum ai fi tu cel care a pățit-o).
Tema poveștii: {plotIdea}

REGULI STRICTE DE LOGICĂ AUTO:
1. EXPERT ADEVĂRAT: Nu recomanda NICIODATĂ intervale de service 'LongLife' (30.000 km). Susține mereu schimbul de ulei la 8.000 - 10.000 km. Critică politicile producătorilor auto. Combate țepele samsarilor.
2. HOOK: Prima propoziție trebuie să socheze (ex: 'Am dat X mii de euro și am luat cea mai mare țeapă din viața mea'). Fără 'Salutare'!
3. LUNGIME: Fix între 280 și 320 de cuvinte (aprox 70 secunde de vorbit). Numerele scrise în litere.
4. TON ȘI JARGON: Nervos, pasionat, amuzant. Folosește expresii reale de service (ex: 'pică distribuția', 's-a făcut uleiul apă', 'crapă turbina', 'erori în bord'). Fără fraze robotice.
5. REZOLVARE: La final, spune cum ai rezolvat problema comandând ceva de la 'magazinul NavigațiiAndroid.ro', 'Autodrop' sau 'Lacrimi de Panda' (din link-ul din profil).

OBLIGATORIU: Scrie textul care va fi rostit.
La final de tot adaugă exact aceste etichete:
---KEYWORDS---
Scrie EXACT 12 termeni de căutare YouTube în engleză, separați prin virgulă pe un singur rând, care să ilustreze vizual EXACT mașina și piesele din poveste (ex: {plotIdea} driving pov, {plotIdea} interior dashboard, {plotIdea} engine bay, mechanic scanning car obd2, dirty engine oil change, broken timing chain mechanic, car on lift inspection, turbocharger failure smoke, car dashboard warning lights, mechanic working under hood).
---META---
Titlu scurt captivant și 4 hashtag-uri.";

                script = await CallGeminiAndParseScript(WorkFolder, prompt, plotIdea);

                if (string.IsNullOrEmpty(script))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[EROARE] Scenariul nu a putut fi generat. Reîncercăm...");
                    Console.ResetColor();
                    await Task.Delay(2000);
                    continue;
                }

                if (File.Exists(Path.Combine(WorkFolder, "keywords.txt"))) keywords = File.ReadAllText(Path.Combine(WorkFolder, "keywords.txt")).Trim();
                if (File.Exists(Path.Combine(WorkFolder, "meta.txt"))) metaTitle = File.ReadAllText(Path.Combine(WorkFolder, "meta.txt")).Trim();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n================ POVESTE AUTO GENERATĂ ================");
                Console.WriteLine($"[CĂUTĂRI B-ROLL SPECIFICE]: {keywords}");
                Console.ResetColor();
                Console.WriteLine(script);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("========================================================\n");
                Console.ResetColor();

                Console.WriteLine("1. Aprobă! Descarcă B-roll-urile reale și asamblează videoclipul");
                Console.WriteLine("2. Generează altă poveste");
                Console.WriteLine("3. Editează textul manual în Notepad");
                Console.Write("\nAlege (1/2/3): ");

                string dec = Console.ReadLine()?.Trim();
                if (dec == "1") aprobare = true;
                else if (dec == "3")
                {
                    string txtPath = Path.Combine(WorkFolder, "script.txt");
                    File.WriteAllText(txtPath, script, new UTF8Encoding(false));
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = "notepad.exe";
                        p.StartInfo.Arguments = $"\"{txtPath}\"";
                        p.Start();
                        p.WaitForExit();
                    }
                    script = File.ReadAllText(txtPath);
                    aprobare = true;
                }
            }

            SalveazaInIstoric(plotIdea);

            bool voiceSuccess = await GenerateVoiceAsync(script, WorkFolder);
            if (!voiceSuccess) return;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string finalYtPath = Path.Combine(WorkFolder, $"youtube_story_{timestamp}.mp4");
            string finalTtPath = Path.Combine(WorkFolder, $"tiktok_story_{timestamp}.mp4");

            bool videoSuccess = await CoreVideoAssembly(WorkFolder, finalYtPath, finalTtPath);

            if (!videoSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[EROARE] Asamblarea video a eșuat. Verifică conexiunea pentru yt-dlp.");
                Console.ResetColor();
                return;
            }

            File.Copy(finalYtPath, Path.Combine(WorkFolder, "youtube_automat.mp4"), true);
            File.Copy(finalTtPath, Path.Combine(WorkFolder, "tiktok_automat.mp4"), true);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n>> [SUCCES!] Povestea a fost asamblată pe imagini variate:\n   - TikTok: {finalTtPath}\n   - YouTube: {finalYtPath}");
            Console.ResetColor();

            Console.Write("\nVrei să programez acest Video pe YouTube automat (Private)? (d/n): ");
            string ans = Console.ReadLine()?.Trim().ToLower();
            if (ans == "d" || ans == "da" || ans == "y")
            {
                await PublishVideoStepAsync(WorkFolder, finalYtPath);
            }

            CleanupTempFiles(WorkFolder);
        }

        static async Task<string> CallGeminiAndParseScript(string workFolder, string prompt, string fallbackTopic = "car mechanic")
        {
            string resultJson = null;
            bool aiSuccess = false;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("X-goog-api-key", ApiKey);
                        var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(GeminiUrl, content);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            resultJson = responseBody;
                            aiSuccess = true;
                            break;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"   -> [EROARE API {(int)response.StatusCode}]: {responseBody}");
                            Console.ResetColor();
                            if ((int)response.StatusCode == 400 || (int)response.StatusCode == 403)
                            {
                                Console.WriteLine("   -> Cheia API pare invalidă. Resetează cheia:");
                                Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
                                Environment.SetEnvironmentVariable("GEMINI_API_KEY", null, EnvironmentVariableTarget.User);
                                AsiguraCheiaApi();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   -> [EXCEPȚIE REȚEA]: {ex.Message}");
                    await Task.Delay(3000);
                }
            }

            if (!aiSuccess) return null;

            try
            {
                dynamic parsedJson = JsonConvert.DeserializeObject(resultJson);
                string textGenerat = parsedJson.candidates[0].content.parts[0].text;
                string scriptFinal = textGenerat;

                string keywords = $"{fallbackTopic} pov drive, {fallbackTopic} interior, {fallbackTopic} engine bay, mechanic engine repair, car diagnostic obd2, car oil change service, mechanic under car lift, car dashboard warning lights, sport car acceleration pov, used car inspection";
                string metaTitle = "Secretul pe care mecanicii nu ți-l spun! 🔥 #auto #masini #retrofit #romania";

                if (textGenerat.Contains("---KEYWORDS---") || textGenerat.Contains("---CARNAME---"))
                {
                    var parts = textGenerat.Split(new[] { "---CARNAME---", "---KEYWORDS---", "---META---" }, StringSplitOptions.None);
                    scriptFinal = parts[0].Trim();

                    var lines = textGenerat.Split('\n');
                    string currentBlock = "";
                    List<string> kwList = new List<string>();
                    string metaBuilder = "";

                    foreach (var line in lines)
                    {
                        if (line.Contains("---CARNAME---")) currentBlock = "CARNAME";
                        else if (line.Contains("---KEYWORDS---")) currentBlock = "KEYWORDS";
                        else if (line.Contains("---META---")) currentBlock = "META";
                        else
                        {
                            if (currentBlock == "KEYWORDS" && !string.IsNullOrWhiteSpace(line))
                            {
                                var subItems = line.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var item in subItems)
                                {
                                    string cleanKw = Regex.Replace(item, @"^[\d\-\.\*\s]+", "").Trim();
                                    if (cleanKw.Length > 2) kwList.Add(cleanKw);
                                }
                            }
                            else if (currentBlock == "META" && !string.IsNullOrWhiteSpace(line))
                            {
                                metaBuilder += line.Trim() + " ";
                            }
                        }
                    }

                    if (kwList.Count > 0) keywords = string.Join(", ", kwList.Distinct());
                    if (!string.IsNullOrWhiteSpace(metaBuilder)) metaTitle = metaBuilder.Trim();
                }

                scriptFinal = scriptFinal.Replace("*", "").Replace("#", "").Trim();

                UTF8Encoding utf8NoBom = new UTF8Encoding(false);
                File.WriteAllText(Path.Combine(workFolder, "script.txt"), scriptFinal, utf8NoBom);
                File.WriteAllText(Path.Combine(workFolder, "keywords.txt"), keywords, utf8NoBom);
                File.WriteAllText(Path.Combine(workFolder, "meta.txt"), metaTitle, utf8NoBom);

                return scriptFinal;
            }
            catch { return null; }
        }

        static async Task<bool> GenerateVoiceAsync(string textToSpeak, string workFolder)
        {
            Console.WriteLine("   -> [AUDIO] Se generează vocea...");

            string audioPath = Path.Combine(workFolder, "voce.mp3");
            string tempTextFile = Path.Combine(workFolder, "temp_text_edge.txt");

            if (File.Exists(audioPath)) File.Delete(audioPath);
            File.WriteAllText(tempTextFile, textToSpeak, new UTF8Encoding(false));

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c python -m edge_tts --voice ro-RO-EmilNeural --rate=+5% -f \"{tempTextFile}\" --write-media \"{audioPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    await Task.Run(() => process.WaitForExit());
                    return File.Exists(audioPath) && new FileInfo(audioPath).Length > 1000;
                }
            }
            catch { return false; }
        }

        static double GetMediaDuration(string ffmpegPath, string filePath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-nostdin -i \"{filePath}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardError.ReadToEnd();
                    process.WaitForExit(5000);

                    Match match = Regex.Match(output, @"Duration:\s*(\d{2}):(\d{2}):(\d{2}(?:\.\d+)?)");
                    if (match.Success)
                    {
                        int hours = int.Parse(match.Groups[1].Value);
                        int minutes = int.Parse(match.Groups[2].Value);
                        double seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                        return (hours * 3600) + (minutes * 60) + seconds;
                    }
                }
            }
            catch { }
            return 0;
        }

        static void BuildSynchronizedSrt(string workFolder, string ffmpegPath, double realAudioDuration)
        {
            string scriptPath = Path.Combine(workFolder, "script.txt");
            string audioPath = Path.Combine(workFolder, "voce.mp3");
            string srtOutputPath = Path.Combine(workFolder, "subtitrari.srt");

            List<(double Start, double End)> silences = new List<(double Start, double End)>();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo { FileName = ffmpegPath, Arguments = $"-nostdin -i \"{audioPath}\" -af silencedetect=noise=-35dB:d=0.25 -f null -", RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (Process p = Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit(10000);
                    double currentSilenceStart = -1;
                    foreach (var line in err.Split('\n'))
                    {
                        if (line.Contains("silence_start:") && double.TryParse(line.Split(new[] { "silence_start:" }, StringSplitOptions.None)[1].Trim().Split(' ')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out currentSilenceStart)) { }
                        else if (line.Contains("silence_end:") && currentSilenceStart >= 0 && double.TryParse(line.Split(new[] { "silence_end:" }, StringSplitOptions.None)[1].Trim().Split(' ')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double sEnd))
                        {
                            silences.Add((currentSilenceStart, sEnd));
                            currentSilenceStart = -1;
                        }
                    }
                }
            }
            catch { }

            string rawText = File.ReadAllText(scriptPath, Encoding.UTF8);
            var rawWords = rawText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> words = new List<string>();
            List<double> wordStarts = new List<double>();
            List<double> wordEnds = new List<double>();
            List<double> weights = new List<double>();
            double totalWeight = 0;

            foreach (var w in rawWords)
            {
                int vowels = w.Count(c => "aeiouăâîAEIOUĂÂÎ".Contains(c));
                double weight = Math.Max(vowels * 2.5, w.Length * 0.8);
                if (w.Any(char.IsDigit)) weight += 14.0;
                if (w.EndsWith(",") || w.EndsWith(";") || w.EndsWith("-")) weight += 6.0;
                if (w.EndsWith(".") || w.EndsWith("!") || w.EndsWith("?") || w.EndsWith(":")) weight += 14.0;
                weights.Add(weight);
                totalWeight += weight;
            }

            double totalSilenceDuration = silences.Sum(s => s.End - s.Start);
            double speakingDuration = Math.Max(realAudioDuration - totalSilenceDuration - 0.3, 10.0);
            double timePerWeight = speakingDuration / Math.Max(totalWeight, 1.0);
            double currentTime = 0.25;
            int silenceIdx = 0;

            for (int i = 0; i < rawWords.Length; i++)
            {
                while (silenceIdx < silences.Count && currentTime >= silences[silenceIdx].Start - 0.1)
                {
                    if (currentTime < silences[silenceIdx].End) currentTime = silences[silenceIdx].End;
                    silenceIdx++;
                }
                double wDur = weights[i] * timePerWeight;
                words.Add(rawWords[i]); wordStarts.Add(currentTime); wordEnds.Add(currentTime + wDur);
                currentTime += wDur;
            }

            if (wordEnds.Count > 0 && realAudioDuration > 5)
            {
                double scale = (realAudioDuration - 0.15) / wordEnds.Last();
                for (int i = 0; i < wordStarts.Count; i++) { wordStarts[i] *= scale; wordEnds[i] *= scale; }
            }

            StringBuilder srtBuilder = new StringBuilder();
            int subtitleIndex = 1;
            int wordsPerChunk = 3;

            for (int i = 0; i < words.Count; i += wordsPerChunk)
            {
                List<string> chunkWords = new List<string>();
                double startTime = wordStarts[i];
                double endTime = wordEnds[i];

                for (int j = 0; j < wordsPerChunk && (i + j) < words.Count; j++)
                {
                    chunkWords.Add(words[i + j]);
                    endTime = wordEnds[i + j];
                }

                if (i + wordsPerChunk < words.Count)
                {
                    double nextStart = wordStarts[i + wordsPerChunk];
                    if (nextStart > endTime) endTime = Math.Min(nextStart - 0.02, endTime + 0.8);
                }

                if (endTime <= startTime) endTime = startTime + 0.4;
                TimeSpan startTs = TimeSpan.FromSeconds(startTime);
                TimeSpan endTs = TimeSpan.FromSeconds(endTime);

                srtBuilder.AppendLine(subtitleIndex.ToString());
                srtBuilder.AppendLine($"{startTs.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture)} --> {endTs.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture)}");
                srtBuilder.AppendLine(string.Join(" ", chunkWords));
                srtBuilder.AppendLine();

                subtitleIndex++;
            }
            File.WriteAllText(srtOutputPath, srtBuilder.ToString(), new UTF8Encoding(false));
        }

        static async Task<bool> CoreVideoAssembly(string workFolder, string outYtPath, string outTtPath)
        {
            if (!File.Exists(FfmpegPath) || !File.Exists(YtdlpPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[EROARE] Nu găsesc ffmpeg.exe ({FfmpegPath}) sau yt-dlp.exe ({YtdlpPath})!");
                Console.ResetColor();
                return false;
            }

            string audioPath = Path.Combine(workFolder, "voce.mp3");
            string keywordsPath = Path.Combine(workFolder, "keywords.txt");

            string sessionArchivePath = Path.Combine(workFolder, "session_downloaded_ids.txt");
            if (File.Exists(sessionArchivePath)) File.Delete(sessionArchivePath);

            double realAudioSeconds = GetMediaDuration(FfmpegPath, audioPath);
            if (realAudioSeconds <= 5) realAudioSeconds = 75;

            double targetDuration = realAudioSeconds + 1.0;
            Console.WriteLine($"   -> [INFO AUDIO] Vocea are {realAudioSeconds:F1} secunde. Clipul final va avea {targetDuration:F1} secunde.");

            BuildSynchronizedSrt(workFolder, FfmpegPath, realAudioSeconds);

            string keywordsText = File.ReadAllText(keywordsPath);
            List<string> searchTerms = keywordsText
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 2)
                .Distinct()
                .ToList();

            List<string> videoFiles = new List<string>();
            List<double> videoDurations = new List<double>();

            int maxClipsToDownload = 10;
            int clipIndex = 1;

            foreach (var searchTerm in searchTerms)
            {
                if (clipIndex > maxClipsToDownload) break;

                Console.WriteLine($"      -> Descarc B-Roll REAL ({clipIndex}/{Math.Min(searchTerms.Count, maxClipsToDownload)}) pentru: '{searchTerm}'...");
                string downloadedFile = await DownloadFromYouTube(YtdlpPath, FfmpegPath, searchTerm, workFolder, clipIndex, sessionArchivePath);

                if (!string.IsNullOrEmpty(downloadedFile))
                {
                    string fullClipPath = Path.Combine(workFolder, downloadedFile);
                    double d = GetMediaDuration(FfmpegPath, fullClipPath);
                    if (d >= 4.5)
                    {
                        videoFiles.Add(downloadedFile);
                        videoDurations.Add(d);
                        clipIndex++;
                    }
                    else
                    {
                        try { File.Delete(fullClipPath); } catch { }
                    }
                }
            }

            if (videoFiles.Count == 0) return false;

            Console.WriteLine($"   -> [PASUL 1/2] S-au găsit {videoFiles.Count} surse video diferite. Se extrag segmente variate...");

            double chunkLength = 3.0;
            int neededSegments = (int)Math.Ceiling((targetDuration + 3.0) / chunkLength);
            List<string> concatLines = new List<string>();

            int[] fileUsageCount = new int[videoFiles.Count];

            for (int segIdx = 0; segIdx < neededSegments; segIdx++)
            {
                int fileIdx = segIdx % videoFiles.Count;
                int usage = fileUsageCount[fileIdx];
                fileUsageCount[fileIdx]++;

                double dur = videoDurations[fileIdx];
                double maxValidStart = Math.Max(0.2, dur - chunkLength - 0.3);

                double startOffset = 0.5;
                if (maxValidStart > 2.0)
                {
                    double step = maxValidStart / 5.0;
                    int zone = (usage * 2 + 1) % 5;
                    startOffset = Math.Min(maxValidStart, Math.Max(0.2, zone * step));
                }

                string segName = $"seg_{segIdx}.mp4";
                string segFullPath = Path.Combine(workFolder, segName);

                string startStr = startOffset.ToString("F2", CultureInfo.InvariantCulture);
                string durStr = chunkLength.ToString("F2", CultureInfo.InvariantCulture);

                string segArgs = $"-nostdin -ss {startStr} -t {durStr} -i \"{videoFiles[fileIdx]}\" " +
                                 $"-vf \"scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,setsar=1,fps=30,format=yuv420p\" " +
                                 $"-an -c:v libx264 -preset ultrafast -y \"{segName}\"";

                RunFfmpegSilent(FfmpegPath, segArgs, workFolder);

                if (File.Exists(segFullPath) && new FileInfo(segFullPath).Length > 1000)
                {
                    concatLines.Add($"file '{segName}'");
                    Console.Write($"\r      -> Segment pregătit: {segIdx + 1}/{neededSegments} (din sursa #{fileIdx + 1} @ {startStr}s)   ");
                }
            }
            Console.WriteLine();

            if (concatLines.Count == 0) return false;

            string concatListPath = Path.Combine(workFolder, "concat_list.txt");
            File.WriteAllLines(concatListPath, concatLines, new UTF8Encoding(false));

            Console.WriteLine("   -> [PASUL 2/2] Se randează videoclipul final cu subtitrări...");

            string targetDurStr = targetDuration.ToString("F2", CultureInfo.InvariantCulture);

            string filterComplex = "[0:v]subtitles=subtitrari.srt:force_style='FontName=Impact,FontSize=24,Alignment=2,MarginV=85,PrimaryColour=&H00FFFF&,Outline=2,Bold=1'[vsub];" +
                                   "[vsub]split=2[v1][v2];" +
                                   "[v1]drawtext=fontfile='C\\:/Windows/Fonts/impact.ttf':text='YouTube - @W8LegacyOfficial':fontcolor=white@0.5:fontsize=40:x=40:y=80[v_yt];" +
                                   "[v2]null[v_tt];" +
                                   "[1:a]apad=pad_dur=1.0[a_pad];" +
                                   "[a_pad]asplit=2[a_yt][a_tt]";

            string finalArgs = $"-nostdin -f concat -safe 0 -i \"concat_list.txt\" -i \"voce.mp3\" " +
                               $"-filter_complex \"{filterComplex}\" " +
                               $"-map \"[v_yt]\" -map \"[a_yt]\" -c:v libx264 -preset fast -c:a aac -pix_fmt yuv420p -t {targetDurStr} -y \"{outYtPath}\" " +
                               $"-map \"[v_tt]\" -map \"[a_tt]\" -c:v libx264 -preset fast -c:a aac -pix_fmt yuv420p -t {targetDurStr} -y \"{outTtPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = finalArgs,
                WorkingDirectory = workFolder,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("frame="))
                        {
                            Console.Write($"\r      -> [RANDARE FINALĂ] {e.Data.PadRight(75)}");
                        }
                    };

                    process.Start();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    Console.WriteLine();

                    return process.ExitCode == 0 && File.Exists(outYtPath);
                }
            }
            catch { return false; }
        }

        static void RunFfmpegSilent(string ffmpegPath, string args, string workDir)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    WorkingDirectory = workDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.StandardError.ReadToEnd();
                    p.WaitForExit(35000);
                }
            }
            catch { }
        }

        static async Task<string> DownloadFromYouTube(string ytdlpPath, string ffmpegPath, string query, string savePath, int fileIndex, string archivePath)
        {
            string fileName = $"yt_broll_{fileIndex}.mp4";
            string fullPath = Path.Combine(savePath, fileName);
            if (File.Exists(fullPath)) File.Delete(fullPath);

            string cleanQuery = query.Replace("\"", "").Trim();
            string args = $"\"ytsearch10:{cleanQuery} shorts\" --no-cache-dir --match-filter \"duration >= 5 & duration <= 120\" --download-archive \"{archivePath}\" --retries 3 --max-downloads 1 -f \"b[ext=mp4]/best\" --merge-output-format mp4 --ffmpeg-location \"{ffmpegPath}\" -o \"{fullPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    await Task.Run(() => process.WaitForExit(45000));
                    if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 10000) return fileName;
                }
            }
            catch { }
            return null;
        }

        static async Task ProceseazaUnClip(int indexCurent, int totalClipuri, bool uploadToYouTube)
        {
            string topicAles = await AlegeSubiect(indexCurent, totalClipuri);
            Console.WriteLine($"\n>> Subiect selectat: {topicAles}\n");

            if (!Directory.Exists(WorkFolder)) Directory.CreateDirectory(WorkFolder);
            CleanupTempFiles(WorkFolder);

            try
            {
                string script = "";
                bool scriptAprobat = false;
                while (!scriptAprobat)
                {
                    script = await GenerateScriptAsync(WorkFolder, topicAles);
                    if (string.IsNullOrEmpty(script)) return;

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n================ SCRIPT GENERAT (Clip {indexCurent}/{totalClipuri}) ================");
                    Console.ResetColor();
                    Console.WriteLine(script);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("====================================================================\n");
                    Console.ResetColor();

                    Console.WriteLine("1. Aprobă și randează videoclipul");
                    Console.WriteLine("2. Nu-mi place, rescrie alt script pe aceeași temă");
                    Console.WriteLine("3. Modifică manual în Notepad");
                    Console.Write("\nAlege (1/2/3): ");

                    string decizie = Console.ReadLine()?.Trim();
                    if (decizie == "1") scriptAprobat = true;
                    else if (decizie == "3")
                    {
                        string txtPath = Path.Combine(WorkFolder, "script.txt");
                        File.WriteAllText(txtPath, script, new UTF8Encoding(false));
                        using (Process p = new Process()) { p.StartInfo.FileName = "notepad.exe"; p.StartInfo.Arguments = $"\"{txtPath}\""; p.Start(); p.WaitForExit(); }
                        script = File.ReadAllText(txtPath);
                        scriptAprobat = true;
                    }
                }

                SalveazaInIstoric(topicAles);
                bool voiceSuccess = await GenerateVoiceAsync(script, WorkFolder);
                if (!voiceSuccess) return;

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string finalYtPath = Path.Combine(WorkFolder, $"youtube_clip_{indexCurent}_{timestamp}.mp4");
                string finalTtPath = Path.Combine(WorkFolder, $"tiktok_clip_{indexCurent}_{timestamp}.mp4");

                bool videoSuccess = await CoreVideoAssembly(WorkFolder, finalYtPath, finalTtPath);
                if (!videoSuccess) return;

                File.Copy(finalYtPath, Path.Combine(WorkFolder, "youtube_automat.mp4"), true);
                File.Copy(finalTtPath, Path.Combine(WorkFolder, "tiktok_automat.mp4"), true);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n>> [SALVAT PE PC] Videoclipurile au fost salvate în:\n   - {finalTtPath}\n   - {finalYtPath}");
                Console.ResetColor();

                if (uploadToYouTube) await PublishVideoStepAsync(WorkFolder, finalYtPath);
            }
            catch { }
            finally { CleanupTempFiles(WorkFolder); }
        }

        static async Task<string> GenerateScriptAsync(string workFolder, string customTopic)
        {
            Console.WriteLine($"   -> [AI] Se scrie scenariul...");
            string[] sponsorNames = { "NavigațiiAndroid.ro", "magazinul Autodrop", "magazinul Lacrimi de Panda" };
            string sponsorulZilei = sponsorNames[new Random(Guid.NewGuid().GetHashCode()).Next(sponsorNames.Length)];
            string[] unghiuriAbordare = {
                "Începe direct cu un model concret de mașină și o greșeală tehnică pe care o fac mulți șoferi.",
                "Vorbește din perspectiva unui pasionat care a testat zeci de variante și explică diferența reală dintre ceva ieftin și ceva de calitate.",
                "Începe cu o afirmație controversată care împarte șoferii în două tabere."
            };
            string unghiAles = unghiuriAbordare[new Random(Guid.NewGuid().GetHashCode()).Next(unghiuriAbordare.Length)];

            string prompt = $@"Ești un vlogger auto român pasionat, genul de mecanic real care spune adevărul verde-n față, nu ce scrie în manualele corporatiste. Scrie un scenariu dramatic și captivant pentru TikTok/Shorts despre: '{customTopic}'.

REGULI STRICTE DE LOGICĂ AUTO:
1. EXPERT ADEVĂRAT: Nu recomanda NICIODATĂ intervale de service 'LongLife' (30.000 km). Susține mereu schimbul de ulei la 8.000 - 10.000 km. Critică politicile producătorilor auto care fac mașini de unică folosință și piese de plastic. Combate țepele samsarilor.
2. UNGHI DE ABORDARE: {unghiAles}
3. FĂRĂ CLIȘEE: Intră direct în subiect din prima secundă (ex: 'Dacă faci greșeala asta...'). Fără 'Salutare, prieteni'.
4. LUNGIME: Între 260 și 300 de cuvinte. Cifrele scrise în litere.
5. JARGON ROMÂNESC: Folosește expresii reale de service (ex: 'îi pică distribuția', 'crapă turbina', 's-a făcut uleiul apă', 'faci motorul praf'). Fără fraze robotice.
6. CALL TO ACTION: La final, adresează o întrebare scurtă publicului și recomandă {sponsorulZilei} (link în Bio).

OBLIGATORIU: Scrie mai întâi TOT SCRIPTUL ROSTIT. 
La final de tot adaugă exact aceste etichete:
---KEYWORDS---
Scrie EXACT 12 termeni scurți în engleză, separați prin virgulă pe un singur rând, care să ilustreze vizual subiectul '{customTopic}' (ex: {customTopic} pov, {customTopic} engine bay, {customTopic} interior, mechanic engine repair, car diagnostic obd2, dirty engine oil, broken car part mechanic, car on lift service, dashboard warning lights, turbocharger repair).
---META---
Titlu scurt captivant și 4 hashtag-uri.";

            return await CallGeminiAndParseScript(workFolder, prompt, customTopic);
        }

        static async Task GenerateBatchAsync()
        {
            Console.Clear();
            Console.Write("Câte videoclipuri vrei să generezi?: ");
            int.TryParse(Console.ReadLine()?.Trim(), out int count);
            for (int i = 1; i <= count; i++)
            {
                await ProceseazaUnClip(i, count, false);
                Console.WriteLine($"\n>> Clipul {i}/{count} finalizat! Apasă ENTER...");
                Console.ReadLine();
            }
        }

        static async Task<string> AlegeSubiect(int indexCurent, int totalClipuri)
        {
            Console.Write("\nScrie direct subiectul tău: ");
            return Console.ReadLine()?.Trim();
        }

        static async Task GlobalRadarSearchModeAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== AUTO-RADAR GLOBAL: Căutare Videoclipuri Virale pe YouTube ===");
            Console.ResetColor();

            string searchTerm = "";
            while (string.IsNullOrWhiteSpace(searchTerm))
            {
                Console.Write("Termen de căutare Radar (ex: mecanic, passat b6, golf 4, dsg): ");
                searchTerm = Console.ReadLine()?.Trim();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n   -> [RADAR] Căutare globală pentru: '{searchTerm}'...");
            Console.ResetColor();

            var topVirale = await SearchTopViralVideos(searchTerm);
            if (topVirale.Count == 0) return;

            for (int i = 0; i < topVirale.Count; i++) Console.WriteLine($"{i + 1}. {topVirale[i].Title}");
            Console.Write("Alege numărul: ");
            int.TryParse(Console.ReadLine()?.Trim(), out int alegere);

            string tr = await ExtractVideoTranscript(topVirale[alegere - 1].Url);
            string script = await GenerateClonedScriptAsync(WorkFolder, topVirale[alegere - 1].Title, tr, searchTerm);

            bool vOk = await GenerateVoiceAsync(script, WorkFolder);
            if (!vOk) return;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string finalYtPath = Path.Combine(WorkFolder, $"youtube_radar_{timestamp}.mp4");
            string finalTtPath = Path.Combine(WorkFolder, $"tiktok_radar_{timestamp}.mp4");

            await CoreVideoAssembly(WorkFolder, finalYtPath, finalTtPath);
        }

        static async Task<List<ViralVideoInfo>> SearchTopViralVideos(string query)
        {
            var list = new List<ViralVideoInfo>();
            string searchString = $"ytsearch10:{query} auto mechanic cars shorts";
            string args = $"\"{searchString}\" --flat-playlist --print \"%(id)s|%(view_count)s|%(title)s|%(uploader)s|%(webpage_url)s\" --no-warnings";

            ProcessStartInfo psi = new ProcessStartInfo { FileName = YtdlpPath, Arguments = args, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            try
            {
                using (Process p = Process.Start(psi))
                {
                    string output = await p.StandardOutput.ReadToEndAsync();
                    p.WaitForExit();
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 4)
                        {
                            long.TryParse(parts[1], out long views);
                            list.Add(new ViralVideoInfo { Id = parts[0].Trim(), ViewCount = views, Title = parts[2].Trim(), Url = $"https://www.youtube.com/watch?v={parts[0].Trim()}" });
                        }
                    }
                }
            }
            catch { }
            return list.OrderByDescending(v => v.ViewCount).ToList();
        }

        static async Task<string> ExtractVideoTranscript(string videoUrl)
        {
            string tempSubPrefix = Path.Combine(WorkFolder, "radar_sub");
            string args = $"\"{videoUrl}\" --skip-download --write-auto-sub --write-sub --sub-lang \"ro,en\" --sub-format vtt -o \"{tempSubPrefix}\" --no-warnings";
            ProcessStartInfo psi = new ProcessStartInfo { FileName = YtdlpPath, Arguments = args, UseShellExecute = false, CreateNoWindow = true };
            try { using (Process p = Process.Start(psi)) { await Task.Run(() => p.WaitForExit(15000)); } } catch { }
            return "Transcript simulat de backup.";
        }

        static async Task<string> GenerateClonedScriptAsync(string workFolder, string originalTitle, string originalTranscript, string searchTerm)
        {
            string[] sponsorNames = { "NavigațiiAndroid.ro", "magazinul Autodrop", "magazinul Lacrimi de Panda" };
            string sponsorulZilei = sponsorNames[new Random(Guid.NewGuid().GetHashCode()).Next(sponsorNames.Length)];

            string prompt = $@"Ești un creator de conținut auto de top din România și un mecanic adevărat. Iată un clip viral detectat de radar:
TITLU ORIGINAL: '{originalTitle}'
CONȚINUT: '{originalTranscript}'

Creează un scenariu în limba română pentru @W8LegacyOfficial care păstrează structura captivantă, dar reformulat natural, 100% original.

REGULI STRICTE:
1. LOGICĂ AUTO REALĂ: Dacă textul original recomandă prostii (gen ulei la 30.000 km), CORECTEAZĂ-L! Susține schimbul la 8.000 - 10.000 km. Fii de partea mecanicilor reali.
2. HOOK: Prima propoziție trebuie să oprească utilizatorul din scroll instant (fără salutări).
3. LUNGIME (65-75 secunde): Între 260 și 300 de cuvinte. Cifrele scrise în litere.
4. JARGON: Fără engleză. Folosește termeni reali ('pică turbina', 'rețeaua mașinii').
5. CALL TO ACTION: La final, întreabă ceva de comentarii și recomandă natural {sponsorulZilei} (link în Bio).

OBLIGATORIU: Scrie mai întâi TOT SCRIPTUL ROSTIT. 
La final de tot adaugă exact aceste etichete:
---KEYWORDS---
Scrie EXACT 12 termeni în engleză separați prin virgulă pe un singur rând pentru B-roll video (ex: {searchTerm} pov drive, {searchTerm} engine bay, broken car engine mechanic, car diagnostic obd2, oil change service, mechanic angry, car on lift).
---META---
Titlu și 4 hashtag-uri.";

            return await CallGeminiAndParseScript(workFolder, prompt, searchTerm);
        }

        static async Task PublishVideoStepAsync(string workFolder, string specificVideoPath)
        {
            string metaPath = Path.Combine(workFolder, "meta.txt");
            if (!File.Exists(specificVideoPath)) return;
            string baseTitle = File.Exists(metaPath) ? File.ReadAllText(metaPath).Split('#')[0].Trim() : "Secretul auto! 🔥";
            string ytDesc = baseTitle + "\n\n🔗 Link în bio!\n#Shorts #Auto #Romania";
            await UploadToYouTubeAsync(workFolder, specificVideoPath, baseTitle, ytDesc);
        }

        static async Task UploadToYouTubeAsync(string workFolder, string videoPath, string title, string description)
        {
            string secretsPath = Path.Combine(workFolder, "client_secrets.json");

            if (!File.Exists(secretsPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[INFO YOUTUBE] Fișierul 'client_secrets.json' nu a fost găsit în: {workFolder}");
                Console.WriteLine("-> Pentru upload automat, descarcă propriul tău OAuth JSON din Google Cloud Console,");
                Console.WriteLine("   redenumește-l în 'client_secrets.json' și pune-l în folderul de pe Desktop.");
                Console.ResetColor();
                return;
            }

            try
            {
                UserCredential credential;
                using (var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets, new[] { YouTubeService.Scope.YoutubeUpload },
                        "w8_user", CancellationToken.None, new Google.Apis.Util.Store.FileDataStore(Path.Combine(workFolder, "YouTube_Auth_Token"), true));
                }
                var youtubeService = new YouTubeService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = "W8Legacy" });

                var video = new Video
                {
                    Snippet = new VideoSnippet
                    {
                        Title = title,
                        Description = description,
                        CategoryId = "2"
                    },
                    Status = new VideoStatus
                    {
                        PrivacyStatus = "private",
                        PublishAtDateTimeOffset = new DateTimeOffset(DateTime.UtcNow.AddDays(1).Date.AddHours(18))
                    }
                };

                using (var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read))
                {
                    var vRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                    vRequest.ResponseReceived += v => Console.WriteLine($"[YOUTUBE] Programat cu succes! ID: {v.Id}");
                    await vRequest.UploadAsync();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[EROARE UPLOAD YOUTUBE]: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}