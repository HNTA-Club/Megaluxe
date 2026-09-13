"""
ScreenHighscore Theme XML Generator for Vocaluxe / Megaluxe
============================================================
Generates the ScreenHighscore.xml layout file across supported themes:
- Output/Themes/Vocaluxe 2024/Screens/ScreenHighscore.xml
- Output/Themes/Idiot 2024/Screens/ScreenHighscore.xml
- Output/Themes/Idiot 2025/Screens/ScreenHighscore.xml

Usage:
    python Tools/generate_highscore_xml.py
"""

import os
from dataclasses import dataclass

def create_text(name, x, y, h, align='Left', text='', max_w='0', style='Bold', color='TextColor', font='Outline'):
    return f'''    <Text Name="{name}">
      <X>{x}</X>
      <Y>{y}</Y>
      <Z>-1</Z>
      <H>{h}</H>
      <MaxW>{max_w}</MaxW>
      <Color Name="{color}" />
      <SelColor Name="TextSelColor" />
      <Align>{align}</Align>
      <ResizeAlign>Center</ResizeAlign>
      <Style>{style}</Style>
      <Font>{font}</Font>
      <Text>{text}</Text>
    </Text>'''

def create_static(name, x, y, w, h, color='BackgroundColorAlpha'):
    return f'''    <Static Name="{name}">
      <Skin />
      <Color Name="{color}" />
      <Rect>
        <X>{x}</X>
        <Y>{y}</Y>
        <W>{w}</W>
        <H>{h}</H>
        <Z>0</Z>
      </Rect>
    </Static>'''

def create_particle(name, x, y, w, h):
    return f'''    <ParticleEffect Name="{name}">
      <Skin>Star</Skin>
      <Rect>
        <X>{x}</X>
        <Y>{y}</Y>
        <W>{w}</W>
        <H>{h}</H>
        <Z>-1</Z>
      </Rect>
      <Color Name="StaticColor" />
      <Type>Star</Type>
      <Size>25</Size>
      <MaxNumber>100</MaxNumber>
    </ParticleEffect>'''

# =============================================================================
# Layout Engine & Geometry
# =============================================================================
@dataclass
class Rect:
    x: float
    y: float
    w: float
    h: float

    @property
    def right(self) -> float:
        return self.x + self.w

    @property
    def bottom(self) -> float:
        return self.y + self.h

    def inset(self, dx: float = 0, dy: float = 0) -> 'Rect':
        return Rect(self.x + dx, self.y + dy, self.w - 2 * dx, self.h - 2 * dy)

# 1. Screen & Primary Cards
SCREEN_W, SCREEN_H = 1920, 1080
MARGIN_X           = 40
TOP_Y              = 140
PANEL_GAP          = 40

MENU_BAR   = Rect(x=0, y=60, w=SCREEN_W, h=67.5)
LEFT_CARD  = Rect(x=MARGIN_X, y=TOP_Y, w=980, h=890)

RIGHT_X    = LEFT_CARD.right + PANEL_GAP                   # 1060
RIGHT_W    = SCREEN_W - RIGHT_X - MARGIN_X                 # 820
LORE_CARD  = Rect(x=RIGHT_X, y=TOP_Y, w=RIGHT_W, h=450)
CHART_CARD = Rect(x=RIGHT_X, y=LORE_CARD.bottom + 30, w=RIGHT_W, h=410)

# 2. Left Pane: Unified Master Leaderboard (12 Rows, 5 Columns)
NUM_LEADERBOARD_ROWS = 12
LEADERBOARD_START_Y  = LEFT_CARD.y + 70                    # 210
LEADERBOARD_PITCH    = 65
COL_RANK             = LEFT_CARD.x + 30                    # 70
COL_NAME             = COL_RANK + 65                       # 135 (max_w=310)
COL_SCORE            = LEFT_CARD.x + 530                   # 570 (align Right)
COL_TAG              = LEFT_CARD.x + 560                   # 600 (max_w=140)
COL_DATE             = LEFT_CARD.right - 30                # 990 (align Right, max_w=240)

# 3. Top-Right Pane: Song Lore (2 Columns + Fact Footer)
LORE_COL1_X = LORE_CARD.x + 40                             # 1100
LORE_COL2_X = LORE_CARD.x + 410                            # 1470

# 4. Bottom-Right Pane: Club Visualization Panel (8 Rows)
NUM_CHART_ROWS   = 8
CHART_BAR_INSET  = 25
CHART_BARS       = CHART_CARD.inset(dx=CHART_BAR_INSET)    # X=1085, W=770
CHART_START_Y    = CHART_CARD.y + 70                       # 690
CHART_ROW_PITCH  = 40
CHART_BAR_H      = 32
CHART_TEXT_H     = 25
CHART_TEXT_PAD_Y = (CHART_BAR_H - CHART_TEXT_H) // 2       # 3px centering offset -> Y=693
CHART_NAME_X     = CHART_BARS.x + 10                       # 1095
CHART_VAL_X      = CHART_BARS.right - 10                   # 1845 (align Right)

# =============================================================================
# Assembly
# =============================================================================
statics = [
    create_static('StaticMenuBar', MENU_BAR.x, MENU_BAR.y, MENU_BAR.w, MENU_BAR.h),
    create_static('StaticCardCurrent', LEFT_CARD.x, LEFT_CARD.y, LEFT_CARD.w, LEFT_CARD.h),
    create_static('StaticCardLore', LORE_CARD.x, LORE_CARD.y, LORE_CARD.w, LORE_CARD.h),
    create_static('StaticCardHighlight', CHART_CARD.x, CHART_CARD.y, CHART_CARD.w, CHART_CARD.h),
]

texts = []

# Top Menu Header
texts.append(create_text('TextTitle', 90, 75, 40.5, 'Left', 'TR_SCREENHIGHSCORE_HIGHSCORE'))
texts.append(create_text('TextSongName', SCREEN_W // 2, 75, 40.5, 'Center'))
texts.append(create_text('TextSongMode', 1830, 75, 40.5, 'Right'))

# Left Pane: Master Leaderboard
texts.append(create_text('TextLeaderboardTitle', COL_RANK, LEFT_CARD.y + 15, 34, 'Left', text='TR_SCREENHIGHSCORE_LEADERBOARD', color='TextColor'))
texts.append(create_text('TextLeaderboardSubTitle', COL_DATE, LEFT_CARD.y + 22, 24, 'Right', text='TR_SCREENHIGHSCORE_SUBTITLE_ALLTIME'))

for i in range(1, NUM_LEADERBOARD_ROWS + 1):
    y = LEADERBOARD_START_Y + (i - 1) * LEADERBOARD_PITCH
    texts.append(create_text(f'TextLeaderboardRank{i}', COL_RANK, y, 38, 'Left'))
    texts.append(create_text(f'TextLeaderboardName{i}', COL_NAME, y, 38, 'Left', max_w='310'))
    texts.append(create_text(f'TextLeaderboardScore{i}', COL_SCORE, y, 38, 'Right'))
    texts.append(create_text(f'TextLeaderboardTag{i}', COL_TAG, y + 5, 28, 'Left', max_w='140'))
    texts.append(create_text(f'TextLeaderboardDate{i}', COL_DATE, y + 4, 30, 'Right', max_w='240'))

# Top-Right Pane: Song Lore
texts.append(create_text('TextLoreTitle', LORE_CARD.x + 30, LORE_CARD.y + 20, 38, 'Left', color='TextColor'))

# Left Column: Performance History & Lore
texts.append(create_text('TextLoreStat1_Num', LORE_COL1_X, 210, 64, 'Left', color='TextColor', max_w='740'))
texts.append(create_text('TextLoreStat1', LORE_COL1_X, 285, 32, 'Left', max_w='740'))
texts.append(create_text('TextLoreStat4', LORE_COL1_X, 335, 30, 'Left', max_w='740'))
texts.append(create_text('TextLoreStat3', LORE_COL1_X, 385, 30, 'Left', max_w='740'))

# Right Column: Reserved for difficulty metric (placeholders)
texts.append(create_text('TextLoreStat2_Num', LORE_COL2_X, 210, 64, 'Left', color='TextColor', max_w='370'))
texts.append(create_text('TextLoreStat2', LORE_COL2_X, 285, 32, 'Left', max_w='370'))

# Fun Fact / Highlight Line
texts.append(create_text('TextLoreFact', LORE_COL1_X, 480, 34, 'Left', max_w='740', color='TextColor'))

# Bottom-Right Pane: Club Visualization Panel
texts.append(create_text('TextHighlightTitle', CHART_CARD.x + 30, CHART_CARD.y + 20, 36, 'Left', color='TextColor'))
texts.append(create_text('TextHighlightBody', CHART_CARD.x + CHART_CARD.w // 2, 750, 44, 'Center', max_w='760'))

for i in range(1, NUM_CHART_ROWS + 1):
    y = CHART_START_Y + (i - 1) * CHART_ROW_PITCH + CHART_TEXT_PAD_Y
    texts.append(create_text(f'TextChartName{i}', CHART_NAME_X, y, CHART_TEXT_H, 'Left', max_w='580'))
    texts.append(create_text(f'TextChartValue{i}', CHART_VAL_X, y, CHART_TEXT_H, 'Right'))

particles = []
# Particle Effects for Leaderboard (1..12)
for i in range(1, NUM_LEADERBOARD_ROWS + 1):
    y = LEADERBOARD_START_Y + (i - 1) * LEADERBOARD_PITCH
    particles.append(create_particle(f'ParticleEffectLeaderboard{i}', LEFT_CARD.x + 25, y - 4, LEFT_CARD.w - 50, 48))

xml_content = f'''<?xml version='1.0' encoding='utf-8'?>
<!-- ========================================================================= -->
<!-- NOTICE: Automatically generated by Tools/generate_highscore_xml.py        -->
<!-- Do not edit coordinates manually; adjust the layout in the generator.    -->
<!-- ========================================================================= -->
<Screen xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Informations>
    <ScreenName>ScreenHighscore</ScreenName>
    <ScreenVersion>11</ScreenVersion>
  </Informations>
  <Backgrounds>
    <Background Name="Background1">
      <Type>Video</Type>
      <SlideShowTextures />
      <Video>BG_Video</Video>
      <Skin>BG_Default</Skin>
      <Color Name="BackgroundColor" />
    </Background>
  </Backgrounds>
  <Statics>
{chr(10).join(statics)}
  </Statics>
  <Texts>
{chr(10).join(texts)}
  </Texts>
  <Buttons />
  <SongMenus />
  <Lyrics />
  <SelectSlides />
  <SingNotes />
  <NameSelections />
  <Equalizers />
  <Playlists />
  <ParticleEffects>
{chr(10).join(particles)}
  </ParticleEffects>
</Screen>
'''

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
themes = ['Vocaluxe 2024', 'Idiot 2024', 'Idiot 2025']
for theme in themes:
    path = os.path.join(REPO_ROOT, 'Output', 'Themes', theme, 'Screens', 'ScreenHighscore.xml')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(xml_content)
    print(f"Updated {path}")
