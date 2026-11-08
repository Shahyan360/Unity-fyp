# Quaternius character and animation assets

This folder contains the Standard/free downloads used for Lumen's humanoid animation and optional guest/NPC variants:

- `BaseCharacters/`: the two full-body character FBXs included in the free Universal Base Characters download, plus the matching textures/hair files.
- `Animations/UAL1_Standard.fbx`: the Standard/free Universal Animation Library FBX, used as a retargetable Humanoid clip source.

The accompanying source license files identify these Quaternius packs as CC0 1.0. Keep those license files with the assets if you copy them elsewhere. `Assets/Editor/QuaterniusHumanoidSetup.cs` asks Unity to import the FBXs as Humanoid rigs and creates reusable prefabs/controller under `Assets/Resources/LumenPeople/` once Unity has finished importing them. This is deliberately the free Standard subset; it is not the full paid/source pack.

The character art is stylized superhero art, not period-accurate Roman or modern café clothing. Prefer the existing scene characters for café/market roles until a suitable free, properly licensed everyday/period character set is added; do not present the superhero models as historically accurate.
