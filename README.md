<div align="center">

# 🏎️ W8Legacy AutoPilot
### *End-to-End AI C# Automation Engine for Faceless Automotive Shorts & TikToks*

[![C#](https://img.shields.io/badge/C%23-.NET_8.0-512BD4?style=for-the-badge&logo=csharp&logoColor=white)](https://dotnet.microsoft.com/)
[![Gemini AI](https://img.shields.io/badge/Google_Gemini-Flash_Lite-8E75B2?style=for-the-badge&logo=googlegemini&logoColor=white)](https://aistudio.google.com/)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-Video_Engine-007808?style=for-the-badge&logo=ffmpeg&logoColor=white)](https://ffmpeg.org/)
[![yt-dlp](https://img.shields.io/badge/yt--dlp-B--Roll_Scraper-FF0000?style=for-the-badge&logo=youtube&logoColor=white)](https://github.com/yt-dlp/yt-dlp)
[![Support via Revolut](https://img.shields.io/badge/Support-Revolut_(1--5%24)-0075EB?style=for-the-badge&logo=revolut&logoColor=white)](https://revolut.me/lzr233/5USD)

*Turn raw car models, mechanical failures, or tuning topics into high-retention 9:16 vertical videos completely on autopilot.*

</div>

---

## ⚡ Overview

**W8Legacy AutoPilot** is a zero-fluff C# console pipeline built for automotive content creators. Instead of spending hours writing scripts, hunting for footage, cutting clips in Premiere, and syncing captions, you simply feed the engine a car model or mechanical issue—and it builds the entire Short from scratch.

---

## 🔥 Core Architecture & Capabilities

| Module | Technology | What It Does |
| :--- | :--- | :--- |
| **🧠 Anti-Cliche Scriptwriter** | `Google Gemini API` | Writes raw, authentic automotive scripts and mechanic insights. Strictly avoids robotic AI intros and debunks corporate LongLife 30,000 km oil intervals using real garage jargon. |
| **🎬 Multi B-Roll Engine** | `yt-dlp` | Extracts 12 specific visual queries per script and downloads up to 10 real-world Shorts sources so visuals match the exact engine or car part discussed. |
| **✂️ Non-Repetitive Slicer** | `FFmpeg` | Divides downloaded footage into 5 distinct time-zones and shuffles 3-second cuts at 30 FPS (`1080x1920`), preventing monotonous loops. |
| **🎙️ Neural Voiceover** | `edge-tts` (`ro-RO`) | Generates punchy neural speech (`ro-RO-EmilNeural` at `+5%` speed) paired with FFmpeg `-35dB` silence detection for natural breathing and pacing. |
| **💬 Dynamic Subtitle Sync** | `FFmpeg` + Custom Math | Calculates vowel/punctuation weights and speech pauses to burn bold, yellow `Impact` subtitles (3 words per chunk) directly onto the video stream. |
| **🛡️ Safe OAuth2 Uploader** | `YouTube Data API v3` | Renders dual outputs (watermarked for Shorts, clean for TikTok) and schedules uploads as `Private` for the next day to avoid algorithmic spam flags. |

---

## 🎮 4 Operating Modes

1. **🎯 Single Clip Mode:** Enter any automotive topic, review/edit the generated script live in Notepad, and render + schedule in one continuous flow.
2. **⚙️ Batch Mode (Autopilot):** Queue multiple car topics back-to-back for hands-free bulk video production.
3. **📡 Global Viral Radar:** Scrapes YouTube for top-performing viral Shorts in your niche, extracts their transcripts, and rewrites an original, mechanically accurate version.
4. **🔧 Garage / Car Lot Storytelling:** First-person dramatic stories paired with real-world inspection and repair B-Roll.

---

## 🛠️ Prerequisites & Installation

Ensure the following tools are installed on your Windows machine before running:

1. **.NET SDK (6.0 / 8.0 or newer)**
2. **Python 3.x** with Microsoft Edge TTS: `pip install edge-tts`
3. **FFmpeg & yt-dlp:** `winget install Gyan.FFmpeg` and `winget install yt-dlp.yt-dlp`

---

## 🚀 Quick Start

1. Clone the repository: `git clone https://github.com/contactw8legacy-ops/W8Legacy-AutoPilot.git`
2. Run the engine: `dotnet run`
3. **Gemini API Key:** Grab a free key from [Google AI Studio](https://aistudio.google.com/). Paste it on your first run—the app automatically saves it to your Windows User Environment Variables (`GEMINI_API_KEY`).
4. **YouTube Auto-Upload (Optional):** Place your OAuth 2.0 Desktop credentials file inside `Desktop\W8_AutoPilot_Output\client_secrets.json`.

---

## ☕ Support the Development

If **W8Legacy AutoPilot** saved you hours of manual video editing or helped grow your channel, consider fueling the project with a quick 1-5$ coffee (or some 100-octane fuel) via Revolut! ⛽

<div align="center">

<a href="https://revolut.me/lzr233/5USD" target="_blank">
  <img src="https://img.shields.io/badge/Buy_Me_a_Coffee_(1--5%24)-Revolut-%230075EB?style=for-the-badge&logo=revolut&logoColor=white" height="45" alt="Support via Revolut">
</a>

**Direct link:** [https://revolut.me/lzr233/5USD](https://revolut.me/lzr233/5USD)

</div>
