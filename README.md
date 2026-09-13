# P-Helper

A lightweight, automated companion utility for G-Helper. P-Helper seamlessly switches your performance profile based on the active application.

## Features

* **App-Specific Profiles:** Assign distinct G-Helper performance profiles (Silent, Balanced, Turbo) to individual games or programs.
* **Default Profile:** Set a fallback system profile that automatically activates when no monitored applications are running.
* **Smart "Add App":** Quickly add programs by picking from a list of currently running processes, or browse your disk for a specific `.exe` file.
* **Batch Folder Scan:** Effortlessly scan entire directories to find and add multiple executables at once. You can also define and save your custom default folders for faster future scans.
* **System Tray Integration:** Control the application rapidly straight from your taskbar's system tray context menu.
* **Pause Monitoring:** Temporarily suspend automatic profile switching for a specific duration or indefinitely when you need manual control.
* **Run on Startup:** Launch the app automatically and silently in the background when Windows starts.

## Prerequisites

* **[G-Helper](https://github.com/seerge/g-helper):** Must be installed and running on your system.
* **[.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download):** Required to run the program (will be auto-prompted to install if missing). *Note: Since G-Helper also relies on .NET 10 Desktop Runtime, you may already have it installed.*

## Installation & Usage

1. Download the latest version from the [Releases](https://github.com/dario/p-helper/releases) page.
2. Run `PHelper.exe`.
3. Add your favorite games or demanding applications, select your preferred profiles, and let the app handle the background switching automatically.

## Acknowledgments

Maintained by [converdario](https://github.com/converdario).  
Based on the [original project](https://github.com/LunarstarFurry/GHelper-Auto-Profile-Switcher) by [LunarstarFurry](https://github.com/LunarstarFurry).

## License

This project is licensed under the MIT License - see the `LICENSE` file for details.