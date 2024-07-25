# UEParser - Parser of Dead by Daylight game assets

<img src="https://www.dbd-info.com/images/Logo/DBDInfoLogo.png" align="right" alt="DBDInfo Logo" width="192">

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
![Commit Activity](https://img.shields.io/github/commit-activity/m/Masusder/UEParser..svg)
![Last Commit](https://img.shields.io/github/last-commit/Masusder/UEParser.svg)

UEParser is an Avalonia-based application developed as a part of [DBDInfo](https://dbd-info.com/) project. It's designed focusing specifically on Dead by Daylight game.<br/>
This tool is designed to fully utilize the way Dead by Daylight actually works, with main focus on **automated and bulk retrieval of data from the game assets**, while maintaining organization by build version.

This project wouldn't be possible without great team behind [CUE4Parse](https://github.com/FabianFG/CUE4Parse) library.

## Table of Contents
- [Features](#features)
- [Screenshots](#screenshots)
- [Installation](#installation)

## Features

- Bulk extraction of assets including asset properties, meshes, textures, UI elements, and animations.
- Parsing of game assets into pre-defined, type-safe schemas suitable for various use cases, such as serving game data through an API.
- Automated upload of extracted data to cloud storage*.
- Extraction of encrypted game APIs.<br/>

**This is personal feature which allows to serve game's data through network. Just ignore.*

## Screenshots
<img src="https://i.imgur.com/doRx4dQ.png" alt="UEParser Presentation">

## Installation

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Avalonia](https://avaloniaui.net/)

### Quick Start

1. Clone the repository:

   ```sh
   git clone https://github.com/Masusder/UEParser.git
