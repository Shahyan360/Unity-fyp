# Character correction and review

The transferred meshes reproduce three outstanding defects. The continuation
repairs the imported meshes directly, using Unity, because Blender/native working
files were not transferred. Original FBXs, skeletons and animations are retained.

Use the connected project's existing Unity Pipeline commands from the repository
root. Wait for a completed Editor recompile before building:

```powershell
unity command run_script --file Tools/Art/ReviewCast.cs --entry LumenCastReview.Run --timeout_ms 120000
```

This writes baseline front/side/rear/three-quarter previews and mesh inventory
into ignored `Builds/Finalization-20261022/Cast-Baseline`. It refuses to replace
evidence. `LUMEN_CAST_REVIEW` selects a different new output directory.

`CorrectCast.cs` is a guarded reproduction recipe for the **uncorrected**
transferred baseline. Its normalized YAML hashes are recorded in
`cast-correction-baseline.json`. Never run it indiscriminately on current final
assets: it deliberately refuses changed hashes and repeated corrections.

The original-baseline procedure is:

1. Preserve source and review baseline views.
2. Run `CorrectLumenCast.Run` through `run_script` to tuck the two trousers and
   remove Maya's remaining cap UV islands. Backups stay under ignored Builds.
3. Run `CorrectLumenCast.CloseMayaCrown` to extend the retained weighted crown
   over the former cap seam. It checks the untouched crown baseline first.
4. Run `LumenCastReview.Final` and inspect all four angles of every character.
5. Run `VerifyLumenCast.Run` with the preserved backups to compare weights,
   bind poses, UVs, normals and every facial morph buffer exactly.
6. Build and run animation/conversation acceptance. Static previews alone do
   not verify all walking, pointing, speaking or silent animation poses.

No external Maya mask is needed: cap island bounds are tracked as small JSON
data and mirrored in `finalize_cast.py`. The crown generation in that script
also closes the former cap seam. Original hair/skin/eye texture pixels are not
edited. The final imported correction retains every native facial vertex and
all 175 blendshape channels, removing only cap-index triangles.

For Blender authoring, both `create_tailored_characters.py` and
`finalize_cast.py` use `LUMEN_ART_ROOT`, defaulting to
`Builds/Art/CharacterTailoring`. Export exact anatomy first with the Unity
`LumenCharacterTailoring.ExportReferences` method. The Blender scripts require
those generated Reference JSONs; missing reference files are not fabricated.
Blender regeneration and final Blender-export equivalence still require a
machine with Blender and explicit render/import review.
