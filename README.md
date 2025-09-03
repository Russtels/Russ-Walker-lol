# C# Pixel-Based Orbwalker & Utility Suite for League of Legends

![Language](https://img.shields.io/badge/Language-C%23-blueviolet)
![Framework](https://img.shields.io/badge/Framework-.NET%208-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)

> [!NOTE] 
> I will try to update this every patch and add every champion to the game , i will keep this free but if u use it consider buying me a coffee 

<img src="https://raw.githubusercontent.com/Russtels/QR/blob/main/binance%20(1).jpg" alt="App Screenshot" width="">


This project is an advanced external utility for League of Legends, written entirely in C#. It leverages a hybrid approach of pixel scanning and the official Live Client Data API to provide high-performance, responsive gameplay assistance. The primary goal is to achieve perfect orbwalking (kiting) mechanics, supplemented by other useful automated features.

## ✨ Features

* **🚶‍♂️ High-Performance Orbwalker:**
    * Executes a flawless attack-move sequence (kiting) by calculating attack windup, attack speed, and ping buffer.
    * Prioritizes champions and maintains movement towards the cursor between auto-attacks.

* **🚜 Automated Last Hitting (Auto-Farm):**
    * A dedicated mode (activated by a hotkey) to automatically perform last hits on minions.
    * Uses pixel detection to identify minions with low health that are ready to be last-hit.

* **🛡️ Instant Anti-CC Module:**
    * Monitors the screen for crowd control status icons (e.g., Stun).
    * Automatically uses Cleanse or Mercurial Scimitar to instantly remove CC.
    * Scans all item slots to find the correct item and uses the corresponding hotkey.
    * Features a priority system (e.g., Mercurial Scimitar before Cleanse).

* **🖥️ Real-time In-Game Overlay:**
    * Displays crucial real-time stats like Attack Speed, Attack Range, and Windup timing.
    * Shows the status of all active modules (Orbwalker, Anti-CC, Cleanse Ready, etc.).

## ⚙️ How It Works

The tool operates on two main principles:

1.  **Live Client Data API:** It connects to the local game client's API (`https://127.0.0.1:2999`) to fetch reliable, real-time data such as the player's current Attack Speed, Attack Range, and Champion Name. This data is used for precise timing calculations.

2.  **High-Performance Pixel Scanning:** For information not available via the API (like enemy positions, minion health, or CC status), the tool takes rapid, small screenshots of specific screen areas. It then uses a highly optimized, multi-threaded process to search for predefined pixel patterns that identify targets or status effects.

These two systems work together in a multi-threaded architecture to ensure that screen analysis does not interfere with the low-latency requirements of the orbwalker.

## 📋 Requirements

* **.NET 8 SDK**
* **Windows Operating System** (due to the use of Windows API for input simulation and screen reading).
* League of Legends running in **Borderless** or **Windowed** mode for reliable screen captures.

## 🛠️ Setup & Configuration

1.  Clone the repository.
2.  Open the `.sln` file in Visual Studio.
3.  Build the project (NuGet packages should restore automatically).
4.  Create an `assets` folder in the output directory (`bin/Debug/...`) and place the reference images for the Anti-CC module there.
5.  All major settings, hotkeys, pixel patterns, and UI coordinates can be configured in the `Values.cs` file.

## 📚 Libraries Used

This project relies on a few key NuGet packages:

* **[GameOverlay.Net](https://github.com/michel-pi/GameOverlay.Net):** Used to render the DirectX-based in-game overlay for displaying real-time information.
* **[Hazdryx.Drawing (FastBitmap)](https://github.com/Hazdryx/FastBitmap):** A critical performance library that provides high-speed, direct memory access to bitmap data for ultra-fast pixel scanning.
* **[Newtonsoft.Json](https://www.newtonsoft.com/json):** Used for parsing the JSON responses from the League of Legends Live Client Data API.
* **[System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common/):** Provides access to fundamental GDI+ graphics functionality, including the `Bitmap` and `Graphics` classes.

## ⚠️ Disclaimer

This project is for educational and experimental purposes only. Using external tools that interact with or automate gameplay is against the Riot Games Terms of Service. Use of this software may result in a permanent ban of your game account. The author is not responsible for any consequences that may arise from its use. **Use at your own risk.**

this is a proyect based on : [MagicOrbwalker](https://github.com/sajmonekk191/MagicOrbwalker.git) and made by [sajmonekk191](https://github.com/sajmonekk191/)
