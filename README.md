# UEParser - Parser of Dead by Daylight game assets

<img src="https://www.dbd-info.com/images/Logo/DBDInfoLogo.png" align="right" alt="DBDInfo Logo" width="192">

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
![Commit Activity](https://img.shields.io/github/commit-activity/m/Masusder/UEParser.svg)
![Last Commit](https://img.shields.io/github/last-commit/Masusder/UEParser.svg)
[![Discord](https://discordapp.com/api/guilds/637265123144237061/widget.png?style=shield)](https://discord.gg/dbdleaks)

UEParser is an Avalonia-based application developed as a part of [DBDInfo](https://dbd-info.com/) project. 
It is specifically designed to work with Dead by Daylight game, mainly focusing on **automated and bulk retrieval of data from the game assets**, while maintaining organization by build version.

This project wouldn't be possible without great team behind [CUE4Parse](https://github.com/FabianFG/CUE4Parse) library.<br/>

------------------------------------------

## Notice

This project is not associated with or endorsed by Behaviour Interactive. All used assets belong to their respective owners.

## Table of Contents
- [Features](#features)
- [Screenshots](#screenshots)
- [Usage](#usage)
- [Installation](#installation)
- [License](#license)

## Features

- Bulk extraction of assets including asset properties, meshes, textures, UI elements, animations, and audio.
- Parsing of game assets into pre-defined, type-safe schemas suitable for various use cases, such as serving game data through an API.
- Automated upload of extracted data to cloud storage, and other cloud-related functions..*.
- Extraction of encrypted game APIs.<br/>

*..and more in the future*

**This feature allows serving game data over a network. It can be ignored if not needed.*

## Screenshots
<div align="center"><i>Main window of the UEParser</i></div>
<br/>
<img src="/UEParser/Resources/UEParserMainWindow.png" style="border-radius:50%" alt="UEParser Presentation">

<div align="center"><i>Example of what UEParser allows to do - 3D Model served through the cloud</i></div>
<br/>
<img src="/UEParser/Resources/UEParserUseCasePresentation.png" alt="3D Model Presentation">

## Usage
To use the app, you can either compile it yourself or download the latest release.
(Note: when compiling yourself you might need to move some dependencies (such as .dlls) into root directory of the app manually)

Before using UEParser, you must configure some essential settings, with the most important ones being:
- **Current Version** - Version you see during start-up of the game, it always should be that exact version!
- **Comparison Version** - If you've previously initialized the app with a specific game build, this option will be available. It's crucial to configure this setting if it's accessible, as it allows the app to compare changes between builds. Without it, you won't be able to use some of the extractors.
- **Branch** - Current branch of the game (**Live**, **PTB**, QA, Dev, UAT or Stage).
- **Path to Game Directory** - The root directory where Dead by Daylight is installed.
- **Mappings** - Mappings should be downloaded dynamically (during build initialization), but in case that fails you need to provide them manually. They are essential for parsing to work.

Once these settings are configured, you can initialize the app to match the current game build. This step is vital for the app to function correctly. If this is your first time initializing, please note that the process may take a significant amount of time.

## Installation

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Avalonia](https://avaloniaui.net/)

### Quick Start

1. Clone the repository:

   ```sh
   git clone https://github.com/Masusder/UEParser.git

## License
UEParser is licensed under [Apache License 2.0](https://github.com/Masusder/UEParser/blob/master/LICENSE.txt), and licenses of third-party libraries used are listed [here](https://github.com/Masusder/UEParser/blob/master/NOTICE).
