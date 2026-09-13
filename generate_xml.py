import xml.etree.ElementTree as ET
import os

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

statics = []
statics.append(create_static('StaticMenuBar', 0, 60, 1920, 67.5))
statics.append(create_static('StaticCardCurrent', 60, 140, 870, 890))
statics.append(create_static('StaticCardLore', 990, 140, 870, 450))
statics.append(create_static('StaticCardHighlight', 990, 620, 870, 410))

texts = []

# Top Menu Header
texts.append(create_text('TextTitle', 90, 75, 40.5, 'Left', 'TR_SCREENHIGHSCORE_HIGHSCORE'))
texts.append(create_text('TextSongName', 960, 75, 40.5, 'Center'))
texts.append(create_text('TextSongMode', 1830, 75, 40.5, 'Right'))

# Left Pane - Unified Master Leaderboard (13 rows)
texts.append(create_text('TextLeaderboardTitle', 90, 155, 34, 'Left', text='TR_SCREENHIGHSCORE_LEADERBOARD', color='TextColor'))
texts.append(create_text('TextLeaderboardSubTitle', 890, 162, 24, 'Right', text='TR_SCREENHIGHSCORE_SUBTITLE_ALLTIME'))
for i in range(1, 14):
    y = 205 + (i-1)*54
    texts.append(create_text(f'TextLeaderboardRank{i}', 90, y, 30, 'Left'))
    texts.append(create_text(f'TextLeaderboardName{i}', 155, y, 30, 'Left', max_w='340'))
    texts.append(create_text(f'TextLeaderboardScore{i}', 620, y, 30, 'Right'))
    texts.append(create_text(f'TextLeaderboardTag{i}', 645, y, 26, 'Left', max_w='155'))
    texts.append(create_text(f'TextLeaderboardDate{i}', 890, y, 26, 'Right'))

# Card 2 (Top-Right): Song Lore (Side-by-Side Key Metrics with Units)
texts.append(create_text('TextLoreTitle', 1020, 160, 38, 'Left', color='TextColor'))

# Left Column (Performances)
texts.append(create_text('TextLoreStat1_Num', 1040, 215, 64, 'Left', color='TextColor', max_w='380'))
texts.append(create_text('TextLoreStat1', 1040, 290, 28, 'Left', max_w='380'))
texts.append(create_text('TextLoreStat4', 1040, 330, 26, 'Left', max_w='380'))

# Right Column (Record & Date)
texts.append(create_text('TextLoreStat2_Num', 1460, 215, 64, 'Left', color='TextColor', max_w='410'))
texts.append(create_text('TextLoreStat2', 1460, 290, 28, 'Left', max_w='410'))
texts.append(create_text('TextLoreStat3', 1460, 330, 26, 'Left', max_w='410'))

# Fun Fact / Highlight Line (Bottom of Song Lore Card)
texts.append(create_text('TextLoreFact', 1040, 485, 32, 'Left', max_w='780', color='TextColor'))

# Card 4 (Bottom-Right): Visualization Panel
texts.append(create_text('TextHighlightTitle', 1020, 640, 38, 'Left', color='TextColor'))
texts.append(create_text('TextHighlightBody', 1425, 750, 44, 'Center', max_w='800'))
for i in range(1, 6):
    y = 700 + (i-1)*62
    texts.append(create_text(f'TextChartName{i}', 1030, y, 32, 'Left', max_w='500'))
    texts.append(create_text(f'TextChartValue{i}', 1820, y, 32, 'Right'))

particles = []
# Particle Effects for Leaderboard (1..13)
for i in range(1, 14):
    y = 205 + (i-1)*54
    particles.append(create_particle(f'ParticleEffectLeaderboard{i}', 90, y-4, 810, 38))

xml_content = f'''<?xml version='1.0' encoding='utf-8'?>
<Screen xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Informations>
    <ScreenName>ScreenHighscore</ScreenName>
    <ScreenVersion>5</ScreenVersion>
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

themes = ['Vocaluxe 2024', 'Idiot 2024', 'Idiot 2025']
for theme in themes:
    path = f'Output/Themes/{theme}/Screens/ScreenHighscore.xml'
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(xml_content)
    print(f"Updated {path}")
