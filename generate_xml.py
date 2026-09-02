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

texts = []

# Headers
texts.append(create_text('TextTitle', 90, 75, 40.5, 'Left', 'TR_SCREENHIGHSCORE_HIGHSCORE'))
texts.append(create_text('TextSongName', 960, 75, 40.5, 'Center'))
texts.append(create_text('TextSongMode', 1830, 75, 40.5, 'Right'))

# Quadrant 1: CURRENT PERFORMANCE (MASSIVE)
texts.append(create_text('TextCurrentTitle', 120, 140, 45, 'Left', color='SelectedColor'))
for i in range(1, 5):
    y = 250 + (i-1)*80
    texts.append(create_text(f'TextCurrentName{i}', 150, y, 75, 'Left', max_w='500'))
    texts.append(create_text(f'TextCurrentScore{i}', 850, y, 75, 'Right'))
    texts.append(create_text(f'TextCurrentRecord{i}', 870, y, 40, 'Left', color='SelectedColor'))

# Quadrant 2: SEASON LEADERBOARD
texts.append(create_text('TextSeasonTitle', 1020, 140, 40, 'Left', color='SelectedColor'))
texts.append(create_text('TextSeasonSubTitle', 1020, 185, 26, 'Left'))
for i in range(1, 6):
    y = 225 + (i-1)*65
    texts.append(create_text(f'TextSeasonRank{i}', 1020, y, 38, 'Left'))
    texts.append(create_text(f'TextSeasonScore{i}', 1180, y, 38, 'Right'))
    texts.append(create_text(f'TextSeasonName{i}', 1200, y, 38, 'Left', max_w='300'))
    texts.append(create_text(f'TextSeasonDiff{i}', 1520, y, 35, 'Left'))
    texts.append(create_text(f'TextSeasonDate{i}', 1830, y, 35, 'Right'))

# Quadrant 3: SONG LORE
texts.append(create_text('TextLoreTitle', 120, 650, 45, 'Left', color='SelectedColor'))
for i in range(1, 5):
    y = 720 + (i-1)*50
    texts.append(create_text(f'TextLoreStat{i}', 150, y, 38, 'Left', max_w='750'))

# Quadrant 4: CLUB HIGHLIGHTS
texts.append(create_text('TextHighlightTitle', 1020, 650, 45, 'Left', color='SelectedColor'))
texts.append(create_text('TextHighlightBody', 1050, 720, 50, 'Left', max_w='750'))

particles = []
for i in range(1, 5):
    y = 250 + (i-1)*80
    particles.append(create_particle(f'ParticleEffectNew{i}', 150, y-10, 700, 90))

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
    <Static Name="StaticMenuBar">
      <Skin />
      <Color Name="BackgroundColorAlpha" />
      <Rect>
        <X>0</X>
        <Y>60</Y>
        <W>1920</W>
        <H>67.5</H>
        <Z>0</Z>
      </Rect>
    </Static>
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
