import xml.etree.ElementTree as ET
import os

def create_text(name, x, y, h, align='Left', text='', max_w='0', style='Bold', color='TextColor'):
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
      <Font>Outline</Font>
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
statics.append(create_static('StaticCardCurrent', 60, 140, 870, 450))
statics.append(create_static('StaticCardSeason', 990, 140, 870, 450))
statics.append(create_static('StaticCardLore', 60, 620, 870, 410))
statics.append(create_static('StaticCardHighlight', 990, 620, 870, 410))

texts = []

# Top Menu Header
texts.append(create_text('TextTitle', 90, 75, 40.5, 'Left', 'TR_SCREENHIGHSCORE_HIGHSCORE'))
texts.append(create_text('TextSongName', 960, 75, 40.5, 'Center'))
texts.append(create_text('TextSongMode', 1830, 75, 40.5, 'Right'))

# Card 1 (Top-Left): Current Performance / All-Time Top (Up to 6 rows)
texts.append(create_text('TextCurrentTitle', 90, 160, 38, 'Left', color='SelectedColor'))
for i in range(1, 7):
    y = 210 + (i-1)*52
    texts.append(create_text(f'TextCurrentName{i}', 110, y, 48, 'Left', max_w='460'))
    texts.append(create_text(f'TextCurrentScore{i}', 780, y, 48, 'Right'))
    texts.append(create_text(f'TextCurrentRecord{i}', 800, y, 32, 'Left', color='SelectedColor'))

# Card 2 (Top-Right): Season Leaderboard
texts.append(create_text('TextSeasonTitle', 1020, 160, 38, 'Left', color='SelectedColor'))
texts.append(create_text('TextSeasonSubTitle', 1830, 168, 26, 'Right'))
for i in range(1, 6):
    y = 225 + (i-1)*65
    texts.append(create_text(f'TextSeasonRank{i}', 1020, y, 36, 'Left'))
    texts.append(create_text(f'TextSeasonScore{i}', 1180, y, 36, 'Right'))
    texts.append(create_text(f'TextSeasonName{i}', 1200, y, 36, 'Left', max_w='280'))
    texts.append(create_text(f'TextSeasonDiff{i}', 1520, y, 32, 'Left'))
    texts.append(create_text(f'TextSeasonDate{i}', 1830, y, 32, 'Right'))

# Card 3 (Bottom-Left): Song Lore (Side-by-Side Key Metrics with Units)
texts.append(create_text('TextLoreTitle', 90, 640, 38, 'Left', color='SelectedColor'))

# Left Column (Performances)
texts.append(create_text('TextLoreStat1_Num', 110, 700, 70, 'Left', color='TextColor', max_w='380'))
texts.append(create_text('TextLoreStat1', 110, 785, 28, 'Left', max_w='380'))
texts.append(create_text('TextLoreStat4', 110, 825, 26, 'Left', max_w='380'))

# Right Column (Record & Date)
texts.append(create_text('TextLoreStat2_Num', 500, 700, 70, 'Left', color='TextColor', max_w='410'))
texts.append(create_text('TextLoreStat2', 500, 785, 28, 'Left', max_w='410'))
texts.append(create_text('TextLoreStat3', 500, 825, 26, 'Left', max_w='410'))

# Card 4 (Bottom-Right): Club Highlight (Hero Metric Card)
texts.append(create_text('TextHighlightTitle', 1020, 640, 38, 'Left', color='SelectedColor'))
texts.append(create_text('TextHighlightBody', 1425, 750, 44, 'Center', max_w='800'))

particles = []
for i in range(1, 7):
    y = 210 + (i-1)*52
    particles.append(create_particle(f'ParticleEffectNew{i}', 110, y-5, 700, 50))

xml_content = f'''<?xml version='1.0' encoding='utf-8'?>
<Screen xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Informations>
    <ScreenName>ScreenHighscore</ScreenName>
    <ScreenVersion>4</ScreenVersion>
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
