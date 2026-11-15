# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-02-13

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android/iOS)

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html

## 6.3 → 6.6 (verified 2026-11-16)

Sources: upgrade-guide-unity64 / 65 / 66 at https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/ (6.4, 6.6) and https://docs.unity.com/en-us/engine/6000.5/manual/upgrade-guides/upgrade-guide-unity65 (6.5).

### 6.4
- `OnDisable` is now invoked on descendants' components when a GameObject is destroyed with `Object.Destroy`; workarounds that propagate it cause duplicate calls and must go.
- Asset import: artifact dependencies reimport only when the artifact changes; custom `ScriptedImporter`/`AssetPostprocessor` may need `AssetImportContext.DependsOnSourceAsset` or `DependsOnArtifact`.
- URP Compatibility Mode removed (`URP_COMPATIBILITY_MODE` define gone); custom render passes must use Render Graph.
- `LightShadowCasterMode`: `NonLightmappedOnly` → `ShadowMask`, `Everything` → `DistanceShadowMask`.
- `InstanceID` replaced by an `EntityId` type; `InstanceID` methods deprecated. Never cast `EntityId` to/from int.

### 6.5
- `InstanceID` APIs removed: `go.GetInstanceID()` → `go.GetEntityId()`. `EntityId` is 8 bytes; storing it in an int loses data.
- Obsolete GameObject/Component shortcuts removed: `GameObject.rigidbody` etc. → `GetComponent<T>()`; `AddComponent(string)` → `AddComponent<T>()`.
- Built-In Render Pipeline deprecated (supported through 6.7 LTS); Legacy Render Graph API obsolete (custom SRPs only); dynamic batching deprecated.
- Render Pipeline Core no longer depends on `com.unity.ugui`: add it explicitly if it came in indirectly.
- Texture Read/Write now required for Particle System Shape and Terrain Paint Detail, otherwise the build fails.
- Editor log defaults to `ProjectName/Logs/Editor.log`; use `Application.consoleLogPath`.
- 2D only: `LowLevelPhysics2D` → `PhysicsCore2D` (`Unity.U2D.Physics`). 3D physics: NOT STATED.

### 6.6
- Play Mode: "reload scene only" is the default for new projects (code not reloaded, static state not reset). Existing-project behaviour: NOT SOURCEABLE.
- YAML serialization: word wrapping disabled and "Reduce Version Control Noise" removed — expect unrelated YAML diffs after reserialize.
- Mesh Read/Write now also required for Mesh Collider (plus Particle System Shape, Terrain Detail Mesh), otherwise the build fails.
- `UNITY_64` and `DEVELOPMENT_BUILD` deprecated; both removed in 6.8.
- UI Toolkit: `DefaultEventSystem.LegacyInputProcessor` removed; `UIToolkitInputConfiguration.SetRuntimeInputBackend` is a compile error; legacy UXML Factory/Traits removed.
- Cinemachine 2 auto-upgraded to Cinemachine 3 (different API and data format). Render Pipeline Core no longer depends on Terrain.
- UGUI and Input System breaking changes: NOT STATED in the fetched guides.
