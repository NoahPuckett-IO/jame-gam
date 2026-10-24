using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    static readonly Vector3 WoodsOrigin = new Vector3(6000, 0, 0);
    static readonly Vector3 ShipOrigin = new Vector3(6000, 0, 600);

    static void BuildIntro(Transform store, PlayerController player, Transform wakeInside, Transform wakeOutside)
    {
        var old = GameObject.Find("Intro Sets");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Intro Sets").transform;
        var rng = new System.Random(11);

        var woods = new GameObject("Woods").transform;
        woods.SetParent(root);
        var o = WoodsOrigin;
        Box("Ground", woods, o + new Vector3(0, -0.5f, 0), new Vector3(400, 1, 400), GetMaterial("Forest Floor", new Color(0.12f, 0.2f, 0.1f)));
        Box("Path", woods, o + new Vector3(0, 0.02f, 0), new Vector3(4.5f, 0.05f, 120), GetMaterial("Dirt Path", new Color(0.3f, 0.22f, 0.14f)));
        string N(string n) => $"Assets/ThirdParty/KenneyNature/{n}.fbx";
        GameObject Nat(string n) => Usable(AssetDatabase.LoadAssetAtPath<GameObject>(N(n)), N(n));
        string[] trees = { "tree_pineTallA", "tree_pineTallB", "tree_pineTallC", "tree_pineRoundA", "tree_pineRoundC", "tree_pineDefaultA", "tree_pineDefaultB", "tree_detailed_dark", "tree_tall_dark", "tree_default_dark" };
        string[] small = { "rock_largeA", "rock_largeC", "rock_smallA", "rock_smallC", "rock_tallA", "stump_old", "stump_round", "log", "log_large", "mushroom_red", "mushroom_redGroup", "mushroom_tanGroup", "grass", "grass_large", "grass_leafs", "plant_bush", "plant_bushLarge", "plant_bushSmall", "flower_purpleA", "flower_redB", "flower_yellowA" };
        for (int i = 0; i < 160; i++)
        {
            float px = ((float)rng.NextDouble() - 0.5f) * 220f;
            float pz = ((float)rng.NextDouble() - 0.5f) * 220f;
            if (Mathf.Abs(px) < 6f) px += Mathf.Sign(px == 0 ? 1 : px) * 8f;
            if (Mathf.Abs(px) < 14f && pz > -34f && pz < 14f) continue;
            var prefab = Nat(trees[rng.Next(trees.Length)]);
            if (prefab == null) continue;
            var t = Spawn(prefab, woods, rng.Next(360), 1);
            var b = GetBounds(t);
            t.transform.localScale *= (13f + (float)rng.NextDouble() * 10f) / Mathf.Max(b.size.y, 0.01f);
            PlaceOnFloor(t, o + new Vector3(px, 0, pz));
        }
        for (int i = 0; i < 220; i++)
        {
            float px = ((float)rng.NextDouble() - 0.5f) * 120f;
            float pz = ((float)rng.NextDouble() - 0.5f) * 140f;
            if (Mathf.Abs(px) < 2.6f) continue;
            var prefab = Nat(small[rng.Next(small.Length)]);
            if (prefab == null) continue;
            var t = Spawn(prefab, woods, rng.Next(360), 1);
            var b = GetBounds(t);
            float target = prefab.name.StartsWith("rock_large") || prefab.name.StartsWith("log") ? 1.6f : prefab.name.StartsWith("rock_tall") ? 3f : 0.7f;
            t.transform.localScale *= target * (0.7f + (float)rng.NextDouble() * 0.7f) / Mathf.Max(b.size.y, 0.01f);
            PlaceOnFloor(t, o + new Vector3(px, 0, pz));
        }
        var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon";
        moon.transform.SetParent(woods);
        moon.transform.position = o + new Vector3(-60, 140, 260);
        moon.transform.localScale = Vector3.one * 34f;
        moon.GetComponent<Renderer>().sharedMaterial = StoreAtmosphere.Emissive("Moon", new Color(0.9f, 0.95f, 1f), 2.5f);
        Object.DestroyImmediate(moon.GetComponent<Collider>());
        var moonLight = new GameObject("Moonlight").AddComponent<Light>();
        moonLight.transform.SetParent(woods);
        moonLight.transform.position = o + new Vector3(-20, 60, 60);
        moonLight.type = LightType.Point;
        moonLight.color = new Color(0.55f, 0.65f, 1f);
        moonLight.range = 220f;
        moonLight.intensity = 0.5f * 220f * 220f / 4f;
        var starMat = StoreAtmosphere.Emissive("Star", Color.white, 3f);
        for (int i = 0; i < 160; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "Star";
            s.transform.SetParent(woods);
            var dir = new Vector3((float)rng.NextDouble() - 0.5f, 0.15f + (float)rng.NextDouble() * 0.8f, (float)rng.NextDouble() - 0.5f).normalized;
            s.transform.position = o + dir * 380f;
            s.transform.localScale = Vector3.one * (0.6f + (float)rng.NextDouble() * 1.4f);
            s.GetComponent<Renderer>().sharedMaterial = starMat;
            Object.DestroyImmediate(s.GetComponent<Collider>());
        }

        var henryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSetup.ModelPath);
        var henry = (GameObject)PrefabUtility.InstantiatePrefab(henryPrefab, woods);
        henry.name = "Intro Henry";
        var hmat = CharacterSetup.HenryMaterial();
        foreach (var r in henry.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterials = Enumerable.Repeat(hmat, r.sharedMaterials.Length).ToArray();
            if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
        }
        var hb = GetBounds(henry);
        henry.transform.localScale *= 2f / hb.size.y;
        var hAnim = henry.GetComponent<Animator>();
        if (hAnim == null) hAnim = henry.AddComponent<Animator>();
        hAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ThirdParty/KenneyAnimChar/Henry.controller");
        hAnim.applyRootMotion = false;
        hAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        henry.AddComponent<HeadLook>();
        AddSounds(henry, hAnim);
        AddGrounder(henry, hAnim, 0f);
        var moonFill = new GameObject("Henry Fill Light").AddComponent<Light>();
        moonFill.transform.SetParent(henry.transform, false);
        moonFill.transform.localPosition = new Vector3(0.8f, 2.6f, 2.2f) / henry.transform.localScale.y;
        moonFill.type = LightType.Point;
        moonFill.color = new Color(0.75f, 0.85f, 1f);
        moonFill.range = 7f;
        moonFill.intensity = 6f;
        moonFill.shadows = LightShadows.None;
        henry.SetActive(false);

        var pathStart = Waypoint(woods, "Path Start", o + new Vector3(0, 0, -13), 0);
        var pathStop = Waypoint(woods, "Path Stop", o + new Vector3(0, 0, -2), 0);
        var hover = Waypoint(woods, "UFO Hover", o + new Vector3(0, 21, 2), 0);
        var camWalk = Waypoint(woods, "Cam Walk", pathStart.position + new Vector3(4.5f, 2.4f, -6f), 0);
        var camLookUp = Waypoint(woods, "Cam Look Up", pathStop.position + new Vector3(5.5f, 0.8f, -3.5f), 0);
        camLookUp.rotation = Quaternion.LookRotation(hover.position - camLookUp.position);
        var camBeam = Waypoint(woods, "Cam Beam", pathStop.position + new Vector3(14f, 5f, -16f), 0);
        camBeam.rotation = Quaternion.LookRotation(pathStop.position + Vector3.up * 8f - camBeam.position);

        var ufo = BuildSaucer(woods, hover.position + Vector3.up * 60f, out var beam);

        var ship = new GameObject("Ship Interior").transform;
        ship.SetParent(root);
        var s0 = ShipOrigin;
        var metal = GetMaterial("Ship Metal", new Color(0.42f, 0.47f, 0.52f));
        metal.SetFloat("_Metallic", 0.6f);
        metal.SetFloat("_Smoothness", 0.5f);
        var dark = GetMaterial("Ship Dark", new Color(0.12f, 0.14f, 0.18f));
        var greenGlow = StoreAtmosphere.Emissive("Ship Green Glow", new Color(0.3f, 1f, 0.5f), 2f);
        Box("Floor", ship, s0 + new Vector3(0, -0.25f, 0), new Vector3(30, 0.5f, 30), dark);
        Box("Ceiling", ship, s0 + new Vector3(0, 10.25f, 0), new Vector3(30, 0.5f, 30), metal);
        Box("Wall Back", ship, s0 + new Vector3(0, 5, 15), new Vector3(30, 10, 0.5f), metal);
        Box("Wall Left", ship, s0 + new Vector3(-15, 5, 0), new Vector3(0.5f, 10, 30), metal);
        Box("Wall Right", ship, s0 + new Vector3(15, 5, 0), new Vector3(0.5f, 10, 30), metal);
        for (int i = -2; i <= 2; i++)
        {
            Box("Light Strip", ship, s0 + new Vector3(i * 5f, 10f, 0), new Vector3(0.6f, 0.1f, 26f), greenGlow);
            Box("Wall Panel Line", ship, s0 + new Vector3(i * 5.5f, 5, 14.7f), new Vector3(0.15f, 10, 0.1f), dark);
        }
        var shipLight = new GameObject("Ship Light").AddComponent<Light>();
        shipLight.transform.SetParent(ship);
        shipLight.transform.position = s0 + new Vector3(0, 8, 0);
        shipLight.type = LightType.Point;
        shipLight.color = new Color(0.6f, 1f, 0.75f);
        shipLight.range = 40f;
        shipLight.intensity = 400f;
        string S(string n) => $"Assets/ThirdParty/KenneySpace/{n}.fbx";
        GameObject Sp(string n) => Usable(AssetDatabase.LoadAssetAtPath<GameObject>(S(n)), S(n));
        void Prop(string n, Vector3 at, float yaw, float height)
        {
            var p = Sp(n);
            if (p == null) return;
            var g = Spawn(p, ship, yaw, 1);
            var b = GetBounds(g);
            g.transform.localScale *= height / Mathf.Max(b.size.y, 0.01f);
            PlaceOnFloor(g, s0 + at);
        }
        Prop("desk_computer", new Vector3(-9, 0, 11), 180, 2.2f);
        Prop("desk_computerScreen", new Vector3(-4, 0, 12), 180, 2.6f);
        Prop("machine_generator", new Vector3(10, 0, 10), 200, 4.5f);
        Prop("machine_barrelLarge", new Vector3(12, 0, 3), 90, 3.5f);
        Prop("machine_wireless", new Vector3(-12, 0, 2), 90, 4f);
        Prop("desk_chair", new Vector3(-9, 0, 8), 20, 1.6f);
        Prop("structure_detailed", new Vector3(0, 0, 13), 0, 9.5f);

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Specimen Pad";
        pad.transform.SetParent(ship);
        pad.transform.position = s0 + new Vector3(-3, 0.1f, 0);
        pad.transform.localScale = new Vector3(4, 0.1f, 4);
        pad.GetComponent<Renderer>().sharedMaterial = greenGlow;
        var henrySpot = Waypoint(ship, "Henry Spot", s0 + new Vector3(-3, 0.2f, 0), 90);
        Transform alien = Waypoint(ship, "Alien", s0 + new Vector3(4.5f, 0, 0), -90);
        var gunPivot = BuildAlien(alien, henrySpot.position + Vector3.up * 1.1f, out var muzzle);
        var zapGo = new GameObject("Shrink Ray");
        zapGo.transform.SetParent(ship);
        var zap = zapGo.AddComponent<LineRenderer>();
        zap.positionCount = 2;
        zap.widthMultiplier = 0.25f;
        zap.sharedMaterial = StoreAtmosphere.Emissive("Shrink Ray", new Color(0.4f, 1f, 0.4f), 6f);
        zap.enabled = false;
        var flash = new GameObject("Zap Flash").AddComponent<Light>();
        flash.transform.SetParent(ship);
        flash.transform.position = s0 + new Vector3(0, 2, 0);
        flash.type = LightType.Point;
        flash.color = new Color(0.4f, 1f, 0.5f);
        flash.range = 20f;
        flash.enabled = false;
        var camShip = Waypoint(ship, "Cam Ship", s0 + new Vector3(0.5f, 2.6f, -9f), 0);
        camShip.rotation = Quaternion.LookRotation(s0 + new Vector3(0.5f, 1.4f, 0) - camShip.position);

        var intro = root.gameObject.AddComponent<IntroCutscene>();
        intro.Setup(hAnim,
            new[] { pathStart, pathStop, ufo, hover, beam, camWalk, camLookUp, camBeam },
            new[] { henrySpot, alien, gunPivot, muzzle, camShip },
            zap, flash, player, wakeInside, wakeOutside, skipHint);
    }

    static Transform BuildAlien(Transform alien, Vector3 aimAt, out Transform muzzle)
    {
        var skin = GetMaterial("Alien Skin", new Color(0.36f, 0.78f, 0.3f));
        skin.SetFloat("_Smoothness", 0.55f);
        var suit = GetMaterial("Alien Suit", new Color(0.72f, 0.75f, 0.82f));
        suit.SetFloat("_Metallic", 0.7f);
        suit.SetFloat("_Smoothness", 0.6f);
        var trim = GetMaterial("Alien Trim", new Color(0.75f, 0.12f, 0.15f));
        trim.SetFloat("_Smoothness", 0.5f);
        var eye = GetMaterial("Alien Eye", new Color(0.02f, 0.02f, 0.03f));
        eye.SetFloat("_Smoothness", 0.95f);
        var white = StoreAtmosphere.Emissive("Alien Eye Shine", Color.white, 1.5f);
        var boot = GetMaterial("Alien Boot", new Color(0.1f, 0.1f, 0.13f));
        var glow = StoreAtmosphere.Emissive("Alien Glow", new Color(0.4f, 1f, 0.45f), 3f);
        var chrome = GetMaterial("Ray Gun Chrome", new Color(0.85f, 0.87f, 0.9f));
        chrome.SetFloat("_Metallic", 0.95f);
        chrome.SetFloat("_Smoothness", 0.85f);
        var teal = GetMaterial("Ray Gun Teal", new Color(0.1f, 0.65f, 0.7f));
        teal.SetFloat("_Smoothness", 0.6f);

        Transform P(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material m)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }
        var S = PrimitiveType.Sphere;
        var C = PrimitiveType.Capsule;
        var Y = PrimitiveType.Cylinder;
        for (int side = -1; side <= 1; side += 2)
        {
            P(S, "Boot", alien, new Vector3(side * 0.22f, 0.11f, 0.06f), new Vector3(0.32f, 0.22f, 0.46f), Vector3.zero, boot);
            P(C, "Leg", alien, new Vector3(side * 0.22f, 0.55f, 0), new Vector3(0.27f, 0.42f, 0.27f), Vector3.zero, suit);
        }
        P(C, "Torso", alien, new Vector3(0, 1.36f, 0), new Vector3(0.66f, 0.5f, 0.48f), Vector3.zero, suit);
        P(Y, "Belt", alien, new Vector3(0, 1.02f, 0), new Vector3(0.62f, 0.04f, 0.46f), Vector3.zero, trim);
        P(Y, "Chest Emblem", alien, new Vector3(0, 1.48f, 0.23f), new Vector3(0.22f, 0.012f, 0.22f), new Vector3(90, 0, 0), glow);
        P(Y, "Collar", alien, new Vector3(0, 1.84f, 0), new Vector3(0.62f, 0.06f, 0.55f), Vector3.zero, trim);
        P(Y, "Neck", alien, new Vector3(0, 1.95f, 0), new Vector3(0.15f, 0.12f, 0.15f), Vector3.zero, skin);
        P(S, "Head", alien, new Vector3(0, 2.48f, 0), new Vector3(0.98f, 0.86f, 0.92f), Vector3.zero, skin);
        P(S, "Jaw", alien, new Vector3(0, 2.15f, 0.06f), new Vector3(0.52f, 0.46f, 0.52f), Vector3.zero, skin);
        P(C, "Mouth", alien, new Vector3(0, 2.1f, 0.31f), new Vector3(0.03f, 0.07f, 0.02f), new Vector3(0, 0, 90), eye);
        for (int side = -1; side <= 1; side += 2)
        {
            P(S, "Eye", alien, new Vector3(side * 0.21f, 2.44f, 0.39f), new Vector3(0.4f, 0.22f, 0.16f), new Vector3(0, -side * 20f, -side * 28f), eye);
            P(S, "Eye Shine", alien, new Vector3(side * 0.16f, 2.49f, 0.46f), Vector3.one * 0.06f, Vector3.zero, white);
            P(Y, "Antenna", alien, new Vector3(side * 0.22f, 3.0f, -0.02f), new Vector3(0.03f, 0.22f, 0.03f), new Vector3(0, 0, -side * 22f), boot);
            P(S, "Antenna Tip", alien, new Vector3(side * 0.31f, 3.22f, -0.02f), Vector3.one * 0.11f, Vector3.zero, glow);
        }
        P(C, "Arm L", alien, new Vector3(-0.42f, 1.42f, 0.02f), new Vector3(0.16f, 0.32f, 0.16f), new Vector3(0, 0, -8), suit);
        P(S, "Hand L", alien, new Vector3(-0.47f, 1.08f, 0.04f), Vector3.one * 0.17f, Vector3.zero, skin);

        var shoulder = new GameObject("Ray Gun").transform;
        shoulder.SetParent(alien, false);
        shoulder.localPosition = new Vector3(0.42f, 1.72f, 0);
        shoulder.rotation = Quaternion.LookRotation(aimAt - shoulder.position);
        P(S, "Shoulder", shoulder, Vector3.zero, Vector3.one * 0.2f, Vector3.zero, suit);
        P(C, "Arm R", shoulder, new Vector3(0, 0, 0.3f), new Vector3(0.16f, 0.3f, 0.16f), new Vector3(90, 0, 0), suit);
        P(S, "Hand R", shoulder, new Vector3(0, -0.02f, 0.62f), Vector3.one * 0.17f, Vector3.zero, skin);

        var gun = new GameObject("Ray Gun Model").transform;
        gun.SetParent(shoulder, false);
        gun.localPosition = new Vector3(0, 0.02f, 0.66f);
        gun.localScale = Vector3.one * 1.35f;
        P(C, "Grip", gun, new Vector3(0, -0.11f, -0.03f), new Vector3(0.08f, 0.1f, 0.09f), new Vector3(18, 0, 0), trim);
        P(S, "Body", gun, new Vector3(0, 0.04f, 0.06f), new Vector3(0.2f, 0.19f, 0.32f), Vector3.zero, chrome);
        P(Y, "Barrel", gun, new Vector3(0, 0.04f, 0.33f), new Vector3(0.06f, 0.17f, 0.06f), new Vector3(90, 0, 0), chrome);
        float[] ring = { 0.2f, 0.16f, 0.12f };
        for (int i = 0; i < ring.Length; i++)
            P(Y, "Fin Ring", gun, new Vector3(0, 0.04f, 0.24f + i * 0.08f), new Vector3(ring[i], 0.012f, ring[i]), new Vector3(90, 0, 0), i % 2 == 0 ? teal : trim);
        P(S, "Tip", gun, new Vector3(0, 0.04f, 0.52f), Vector3.one * 0.085f, Vector3.zero, glow);
        P(C, "Tail Fin", gun, new Vector3(0, 0.15f, -0.08f), new Vector3(0.025f, 0.06f, 0.08f), new Vector3(-40, 0, 0), trim);
        P(C, "Tail Fin", gun, new Vector3(0.1f, 0.0f, -0.08f), new Vector3(0.025f, 0.06f, 0.08f), new Vector3(-40, 0, 90), trim);
        P(C, "Tail Fin", gun, new Vector3(-0.1f, 0.0f, -0.08f), new Vector3(0.025f, 0.06f, 0.08f), new Vector3(-40, 0, -90), trim);

        muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun, false);
        muzzle.localPosition = new Vector3(0, 0.04f, 0.58f);
        return shoulder;
    }

    static Transform BuildSaucer(Transform parent, Vector3 at, out Transform beam)
    {
        var ufo = new GameObject("Flying Saucer").transform;
        ufo.SetParent(parent);
        ufo.position = at;
        var silver = GetMaterial("Saucer Silver", new Color(0.78f, 0.8f, 0.85f));
        silver.SetFloat("_Metallic", 0.85f);
        silver.SetFloat("_Smoothness", 0.75f);
        GameObject Part(PrimitiveType type, string name, Vector3 local, Vector3 scale, Material m, Transform p = null)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(p ?? ufo, false);
            g.transform.localPosition = local;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        Part(PrimitiveType.Sphere, "Hull", Vector3.zero, new Vector3(16, 2.6f, 16), silver);
        Part(PrimitiveType.Cylinder, "Rim", Vector3.zero, new Vector3(17, 0.18f, 17), GetMaterial("Saucer Rim", new Color(0.35f, 0.38f, 0.45f)));
        Part(PrimitiveType.Sphere, "Dome", new Vector3(0, 1.4f, 0), new Vector3(6.5f, 4.2f, 6.5f), StoreAtmosphere.Glass("Saucer Dome", new Color(0.55f, 0.95f, 0.85f, 0.45f)));
        Part(PrimitiveType.Sphere, "Underglow", new Vector3(0, -1.1f, 0), new Vector3(6f, 0.7f, 6f), StoreAtmosphere.Emissive("Saucer Underglow", new Color(0.5f, 1f, 0.6f), 1.4f));
        var ring = new GameObject("Light Ring").transform;
        ring.SetParent(ufo, false);
        ring.gameObject.AddComponent<Spin>().Setup(new Vector3(0, 70, 0));
        var yellow = StoreAtmosphere.Emissive("Saucer Light A", new Color(1f, 0.85f, 0.2f), 4f);
        var red = StoreAtmosphere.Emissive("Saucer Light B", new Color(1f, 0.2f, 0.2f), 4f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2 / 16;
            Part(PrimitiveType.Sphere, "Light", new Vector3(Mathf.Cos(a) * 7.6f, -0.1f, Mathf.Sin(a) * 7.6f), Vector3.one * 0.7f, i % 2 == 0 ? yellow : red, ring);
        }
        var glow = new GameObject("Saucer Glow").AddComponent<Light>();
        glow.transform.SetParent(ufo, false);
        glow.transform.localPosition = new Vector3(0, -3, 0);
        glow.type = LightType.Point;
        glow.color = new Color(0.5f, 1f, 0.6f);
        glow.range = 40f;
        glow.intensity = 55f;

        var beamGo = new GameObject("Tractor Beam", typeof(MeshFilter), typeof(MeshRenderer));
        beamGo.transform.SetParent(ufo, false);
        beamGo.transform.localPosition = new Vector3(0, -1.2f, 0);
        beam = beamGo.transform;
        var cone = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer));
        cone.transform.SetParent(beam, false);
        cone.transform.localRotation = Quaternion.Euler(180, 0, 0);
        cone.transform.localScale = new Vector3(9f, 20f, 9f);
        cone.transform.localPosition = new Vector3(0, -20f, 0);
        cone.transform.localRotation = Quaternion.identity;
        cone.GetComponent<MeshFilter>().sharedMesh = InvertedCone();
        cone.GetComponent<MeshRenderer>().sharedMaterial = StoreAtmosphere.Glass("Tractor Beam", new Color(0.6f, 1f, 0.7f, 0.13f));
        var beamLight = new GameObject("Beam Light").AddComponent<Light>();
        beamLight.transform.SetParent(beam, false);
        beamLight.transform.localPosition = new Vector3(0, -12, 0);
        beamLight.type = LightType.Point;
        beamLight.color = new Color(0.6f, 1f, 0.7f);
        beamLight.range = 24f;
        beamLight.intensity = 50f;
        beamGo.SetActive(false);
        return ufo;
    }

    static Mesh InvertedCone()
    {
        const string path = "Assets/Models/BeamCone.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int seg = 32;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
            var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
            var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
            int b = verts.Count;
            verts.Add(p0); verts.Add(new Vector3(0, 1, 0)); verts.Add(p1);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
        }
        var mesh = new Mesh { name = "Beam Cone" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
