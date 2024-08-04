# UEParser - Parser of Dead by Daylight game assets

<img src="https://www.dbd-info.com/images/Logo/DBDInfoLogo.png" align="right" alt="DBDInfo Logo" width="192">

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
![Commit Activity](https://img.shields.io/github/commit-activity/m/Masusder/UEParser.svg)
![Last Commit](https://img.shields.io/github/last-commit/Masusder/UEParser.svg)

UEParser is an Avalonia-based application developed as a part of [DBDInfo](https://dbd-info.com/) project. 
It is specifically designed to work with Dead by Daylight game, mainly focusing on **automated and bulk retrieval of data from the game assets**, while maintaining organization by build version.

This project wouldn't be possible without great team behind [CUE4Parse](https://github.com/FabianFG/CUE4Parse) library.<br/>

------------------------------------------

## Notice

This project is not associated with or endorsed by Behaviour Interactive.

## Table of Contents
- [Features](#features)
- [Screenshots](#screenshots)
- [Installation](#installation)
- [License](#license)

## Features

- Bulk extraction of assets including asset properties, meshes, textures, UI elements, and animations.
- Parsing of game assets into pre-defined, type-safe schemas suitable for various use cases, such as serving game data through an API.
- Automated upload of extracted data to cloud storage, and other cloud-related functions..*.
- Extraction of encrypted game APIs.<br/>

**This feature allows serving game data over a network. It can be ignored if not needed.*

## Screenshots
<div align="center"><i>Main window of the UEParser</i></div>
<br/>
<img src="/UEParser/Resources/UEParserMainWindow.png" style="border-radius:50%" alt="UEParser Presentation">

<div align="center"><i>Example of what UEParser allows to do - 3D Model served through the cloud</i></div>
<br/>
<img src="/UEParser/Resources/UEParserUseCasePresentation.png" alt="3D Model Presentation">

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
