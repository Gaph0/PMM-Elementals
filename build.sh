#!/bin/bash
# Builds Assemblies/PMM_Elementals.dll with csc against RimWorld + workshop mod references.
# Depends on Project Momo being built first (ProjectMomo.dll is referenced).
set -e
cd "$(dirname "$0")"

shopt -s nullglob
SRC=(Source/Elementals/*.cs)
if [ ${#SRC[@]} -eq 0 ]; then
  echo "No C# sources in Source/Elementals yet (phase 0 scaffold) — nothing to build."
  exit 0
fi

PM="$HOME/Desktop/Project Momo"
if [ ! -f "$PM/Assemblies/ProjectMomo.dll" ]; then
  echo "ProjectMomo.dll not found — building Project Momo first..."
  "$PM/build.sh"
fi

WS=~/.steam/steam/steamapps/workshop/content/294100
M=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed
H=$WS/2009463077/Current/Assemblies   # Harmony

csc -nologo -target:library \
  "${SRC[@]}" -out:Assemblies/PMM_Elementals.dll \
  -r:"$M/Assembly-CSharp.dll" -r:"$M/UnityEngine.CoreModule.dll" \
  -r:"$M/UnityEngine.IMGUIModule.dll" -r:"$M/UnityEngine.TextRenderingModule.dll" \
  -r:"$M/netstandard.dll" \
  -r:"$H/0Harmony.dll" \
  -r:"$PM/Assemblies/ProjectMomo.dll"

echo "Built Assemblies/PMM_Elementals.dll"
