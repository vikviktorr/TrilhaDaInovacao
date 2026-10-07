#!/usr/bin/env bash
# Baixa os pacotes 3D da Kenney (licença CC0) e copia para Assets só os
# modelos usados nos cenários. Os zips completos ficam em Terceiros/ (fora do git).
set -euo pipefail
cd "$(dirname "$0")/.."

ORIGEM=Terceiros/kenney
DESTINO=Assets/TrilhaDaInovacao/Modelos/Kenney
mkdir -p "$ORIGEM" "$DESTINO"

declare -A PACOTES=(
  [furniture-kit]=https://kenney.nl/media/pages/assets/furniture-kit/440e0608a4-1677580847/kenney_furniture-kit.zip
  [space-kit]=https://kenney.nl/media/pages/assets/space-kit/20874c75ac-1677698978/kenney_space-kit.zip
  [nature-kit]=https://kenney.nl/media/pages/assets/nature-kit/37ac38a37b-1677698939/kenney_nature-kit.zip
  [city-kit-commercial]=https://kenney.nl/media/pages/assets/city-kit-commercial/a742d900eb-1753115042/kenney_city-kit-commercial_2.1.zip
  [factory-kit]=https://kenney.nl/media/pages/assets/factory-kit/edaac9d4f6-1777639602/kenney_factory-kit_3.0.zip
  [mini-characters]=https://kenney.nl/media/pages/assets/mini-characters/bfc7e272b4-1774770718/kenney_mini-characters.zip
)

declare -A MODELOS=(
  [furniture-kit]="desk tableCoffee deskCorner chairDesk chair computerScreen computerKeyboard computerMouse laptop bookcaseOpen bookcaseClosedWide pottedPlant plantSmall1 plantSmall2 loungeSofa loungeDesignChair lampSquareFloor lampRoundTable radio televisionVintage cardboardBoxClosed cardboardBoxOpen sideTable trashcan books speaker coatRackStanding"
  [space-kit]="satelliteDish_large satelliteDish_detailed desk_computer desk_computerScreen desk_chair rocket_baseA rocket_fuelA rocket_topA rocket_finsA structure_detailed structure supports_high machine_generator machine_generatorLarge machine_wireless pipe_straight pipe_corner barrels platform_large"
  [nature-kit]="plant_bush plant_bushLarge plant_bushDetailed plant_bushSmall grass grass_large tree_palmTall tree_palmDetailedTall tree_palmBend rock_smallA rock_smallC rock_largeA flower_yellowA"
  [city-kit-commercial]="building-a building-b building-c building-d building-e building-f building-g building-h building-skyscraper-a building-skyscraper-b building-skyscraper-c low-detail-building-a low-detail-building-b low-detail-building-c low-detail-building-d low-detail-building-e low-detail-building-wide-a low-detail-building-wide-b"
  [factory-kit]="robot-arm-a machine machine-window screen-panel-wide screen-hanging-wide box-small box-large"
  [mini-characters]="character-female-a character-female-c character-male-b character-male-d"
)

for pacote in "${!PACOTES[@]}"; do
  url=${PACOTES[$pacote]}
  pasta="$ORIGEM/$(basename "$url" .zip)"
  if [ ! -d "$pasta" ]; then
    echo "Baixando $pacote..."
    curl -sfL -A 'Mozilla/5.0' -o "$pasta.zip" "$url"
    mkdir -p "$pasta" && unzip -qo "$pasta.zip" -d "$pasta" && rm "$pasta.zip"
  fi

  fbx="$pasta/Models/FBX format"
  alvo="$DESTINO/$pacote"
  mkdir -p "$alvo"
  for m in ${MODELOS[$pacote]}; do
    if [ -f "$fbx/$m.fbx" ]; then cp "$fbx/$m.fbx" "$alvo/"; else echo "  aviso: $pacote/$m não existe"; fi
  done
  # Alguns pacotes usam uma textura de paleta compartilhada.
  for tex in "$fbx/Textures" "$pasta/Models/Textures"; do
    [ -d "$tex" ] && mkdir -p "$alvo/Textures" && cp "$tex"/*.png "$alvo/Textures/" 2>/dev/null || true
  done
  cp "$pasta/License.txt" "$alvo/LICENSE-Kenney-CC0.txt" 2>/dev/null || true
done

echo "Modelos em $DESTINO: $(find "$DESTINO" -name '*.fbx' | wc -l) arquivos FBX."
