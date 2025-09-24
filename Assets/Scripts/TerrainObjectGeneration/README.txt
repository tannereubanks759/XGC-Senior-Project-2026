# Terrain Object Spawner + NavMesh Rebuild (Unity)

A small Unity utility that:
- Spawns trees, grass, and generic objects on a Terrain with practical rules (slope, ocean level, terrain layers, spacing, keep-out volumes).
- Rebuilds NavMesh after spawning by temporarily disabling water/ocean objects so they don’t block baking, then re-enables them.

Works in **Edit Mode** and **Play Mode**. Includes a hotkey, context menu actions, and readable debug logs.

---

## Quick Start (≈60 seconds)

1. **Unity version:** Unity 6 (URP or Built-in).
2. **Scene:** Open a scene with a `Terrain`.
3. **Add component:** Attach `TerrainObjectSpawner` to an empty GameObject.
4. **Inspector setup:**
   - **Terrain:** assign your Terrain.
   - **waterObjects:** add your ocean/water GameObjects to disable during baking.
   - **navSurfaces (optional):** assign `NavMeshSurface` components or leave empty to auto-find.
5. **Prefabs:** assign `treePrefabs`, `grassPrefabs`, and `prefabs`.
6. **Run:** Press Play or use the component context menu **Spawn All**.
   - If **rebuildNavMeshAfterSpawn** is enabled, water disables → NavMesh builds → water re-enables.

---

## Why This Is Useful

- Placement respects ocean level, terrain slope, grass and path paint, and keep-out volumes.
- Prevents water meshes from forming “invisible walls” during NavMesh baking.
- Spacing logic avoids clutter and considers baked terrain trees.
- Editor-friendly workflow with `ExecuteAlways`, a hotkey, and verbose logs.

---

## Key Features

- **Three spawn passes**
  - **Trees:** upright; requires grass layer; respects tree clearance.
  - **Grass:** aligns to ground normal; optional self-spacing within the pass.
  - **Generic objects:** optional normal alignment; participates in global spacing.
- **Terrain layer filters**
  - **Grass layer:** threshold gate for trees and grass.
  - **Path layer:** avoidance with optional ring sampling padding.
- **Keep-out zones:** supply colliders via `noSpawnVolumes`.
- **Collision bubble:** optional Physics overlap check.
- **Randomization:** uniform scale range and random Y rotation.
- **NavMesh rebuild:** disables `waterObjects`, builds all `NavMeshSurface`s, then re-enables water.

---

## How It Works (High Level)

1. **Setup:** caches Terrain, alphamaps, terrain layer indices, baked terrain tree positions, and finds `NavMeshSurface`s if needed.
2. **Spawn passes:** for each pass, picks random terrain points and validates:
   - inside terrain bounds
   - above ocean level
   - slope within `maxSlopeDegrees` (if enabled)
   - grass requirement for trees/grass
   - path avoidance with optional ring padding
   - outside keep-out volumes
   - spacing vs baked terrain trees and previously placed items
   - optional Physics overlap
3. **Placement:** optional normal alignment, random rotation/scale, instantiate under `parentForSpawned`.
4. **NavMesh rebuild:** disables `waterObjects`, builds each `NavMeshSurface` (auto-finds if list empty), re-enables water.

---

## Usage Tips

- Paint a **Grass** terrain layer where trees/grass are allowed, and a **Path** layer to avoid.
- Use `noSpawnVolumes` for towns/POIs to keep clear.
- Tune `minDistanceToTrees` and `minSpacingBetweenObjects` for readable distribution.
- Press **F7** in Play Mode to re-spawn quickly.
- In Edit Mode, use the component context menu (**Spawn All** or **Rebuild NavMesh**).

---

## Performance Notes

- Spawning uses a capped attempts loop (`maxAttemptsPerObject`) to prevent stalls.
- NavMesh rebuild calls `RemoveData()` then `BuildNavMesh()` for a clean bake.
- Heavy scenes: increase `playmodeDelayFrames` so `SetActive` changes settle before baking.
- Consider pooling and batching for very large counts.



