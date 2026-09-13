# Developer Tools

This folder contains developer scripts and utilities for maintaining assets and screen layout configurations in Megaluxe.

## Theme Screen Generators

### `generate_highscore_xml.py`
Generates the declarative `ScreenHighscore.xml` file across themes (`Vocaluxe 2024`, `Idiot 2024`, `Idiot 2025`).

#### Why a generator is used
The redesigned scoreboard dashboard features a 12-row by 5-column Master Leaderboard table and an 8-row Seasonal Bar Chart panel with accompanying particle emitters and statics. Generating these structured XML elements guarantees exact mathematical alignment (column offsets, pitches, text heights, particle coordinates) across all 3 themes simultaneously.

#### Usage
From anywhere in the repository:
```bash
python Tools/generate_highscore_xml.py
```
Outputs:
- `Output/Themes/Vocaluxe 2024/Screens/ScreenHighscore.xml`
- `Output/Themes/Idiot 2024/Screens/ScreenHighscore.xml`
- `Output/Themes/Idiot 2025/Screens/ScreenHighscore.xml`
