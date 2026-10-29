using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Rendering settings that keep the store fast without changing how it looks. The store has ~30,000 renderers but
// only a few hundred mesh/material pairs, so GPU instancing draws each pair in a handful of calls instead of one
// call per object (the SRP Batcher still issues one per object, which is what made busy views lag, worst in WebGL).
// Builders call Apply after building, and the build scripts call it before building.
static class PerformanceSettings
{
    [MenuItem("JameGam/Apply Performance Settings")]
    public static void Apply()
    {
        // instancing only kicks in with the SRP Batcher off; the Batcher takes over any material it can handle
        foreach (var asset in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(asset);
            if (urp == null) continue;
            urp.useSRPBatcher = false;
            // nothing samples the opaque texture (no refraction or scene-colour shaders), so don't copy it every frame
            urp.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssetIfDirty(urp);
        }

        int count = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || m.enableInstancing || !AssetDatabase.GetAssetPath(m).StartsWith("Assets/")) continue;
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                AssetDatabase.SaveAssetIfDirty(m);
                count++;
            }
        Debug.Log($"[Performance] SRP Batcher off, opaque texture off, instancing turned on for {count} materials");
    }

    // Occlusion culling: the shelves are tall and solid, so from inside an aisle most of the store is hidden behind
    // them. Baking the shelves (and outer walls) as occluders lets Unity skip every product, sign and prop it can't
    // see. Nothing changes on screen. Only things that never move or hide are occluders; everything else (products,
    // levels, Henry) is culled against them at runtime. Rebuilding the store clears the bake, and the build menus
    // bake again before building.
    static readonly string[] OccluderGroups = { "Wall Shelf" };
    static readonly string[] OccluderWalls = { "Wall N", "Wall S", "Wall E", "Wall W South", "Wall W North", "Wall W Lintel" };

    [MenuItem("JameGam/Bake Occlusion Culling")]
    public static void BakeOcclusion()
    {
        var store = GameObject.Find("Store");
        if (store == null) { Debug.LogWarning("[Performance] no Store in the open scene, occlusion not baked"); return; }
        const StaticEditorFlags occlusion = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        // start clean: builders mark plenty of things fully static, including pieces that move or switch off
        var scene = store.scene;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var f = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                if ((f & occlusion) != 0) GameObjectUtility.SetStaticEditorFlags(t.gameObject, f & ~occlusion);
            }

        int count = 0;
        foreach (Transform group in store.transform)
        {
            bool shelves = group.name.StartsWith("Aisle ") || OccluderGroups.Contains(group.name);
            bool wall = OccluderWalls.Contains(group.name);
            if (!shelves && !wall) continue;
            // shelf units are the aisle's batching-static children; the products stocked on them are not
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>())
            {
                var f = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if (shelves && (f & StaticEditorFlags.BatchingStatic) == 0) continue;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, f | StaticEditorFlags.OccluderStatic);
                count++;
            }
        }

        StaticOcclusionCulling.smallestOccluder = 3f;
        StaticOcclusionCulling.smallestHole = 0.25f;
        StaticOcclusionCulling.backfaceThreshold = 100f;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        StaticOcclusionCulling.Compute();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Performance] occlusion baked with {count} occluders in {timer.Elapsed.TotalSeconds:0}s, {StaticOcclusionCulling.umbraDataSize / 1024} KB");
    }

    // build menus call this so a store rebuilt since the last bake never ships with stale culling
    public static void EnsureOcclusion()
    {
        if (StaticOcclusionCulling.umbraDataSize == 0 && GameObject.Find("Store") != null) BakeOcclusion();
    }

    // The supermarket pack's food is far denser than it looks (a cheese wedge is 4,000 triangles, a cart 25,000)
    // and the store holds thousands of them. Mesh LODs let Unity draw a lighter copy once a product is only a few
    // pixels on screen. Meshes that colliders use keep a single level.
    [MenuItem("JameGam/Generate Product Mesh LODs")]
    public static void GenerateProductMeshLods()
    {
        var colliderMeshes = new System.Collections.Generic.HashSet<string>(
            Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include)
                .Where(c => c.sharedMesh != null).Select(c => AssetDatabase.GetAssetPath(c.sharedMesh)));
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/Supermarket" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (colliderMeshes.Contains(path) || !(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
            if (importer.generateMeshLods) continue;
            importer.generateMeshLods = true;
            importer.SaveAndReimport();
            count++;
        }
        Debug.Log($"[Performance] Mesh LODs generated for {count} models (skipped {colliderMeshes.Count} used by colliders)");
    }
}

// Materials that live inside model files can't be edited from the Editor, so instancing is switched on as they import.
class InstancedModelMaterials : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPostprocessMaterial(Material material) => material.enableInstancing = true;
}
