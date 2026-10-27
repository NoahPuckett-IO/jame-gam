using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    // far away from the store, so nothing of it shows through and nothing in it reaches the store
    static readonly Vector3 TutorialOrigin = new Vector3(0, 0, 3000);

    static Material TutorialMaterial(string name, Color color, float smooth = 0.25f, float glow = 0f)
    {
        var m = GetMaterial(name, color);
        m.color = color;
        m.SetFloat("_Smoothness", smooth);
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white * glow);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // a long white room in a row of stations, each closed off by a glass gate until its lesson is done:
    //   walk -> jump up two steps -> dash over a pit -> carry a box to a ledge -> climb a ladder -> ride the tray
    //   slide down over a second pit -> glide across the ice and stop on the pad
    static void BuildTutorialRoom(Transform store)
    {
        var root = new GameObject("Tutorial Room").transform;
        root.SetParent(store);
        root.position = TutorialOrigin;
        Vector3 P(float x, float y, float z) => TutorialOrigin + new Vector3(x, y, z);

        const float W = 22f, H = 14f, L = 198f, D = 6f;
        var white = TutorialMaterial("Tutorial White", new Color(0.95f, 0.95f, 0.96f), 0.3f);
        var trim = TutorialMaterial("Tutorial Trim", new Color(0.86f, 0.87f, 0.9f), 0.2f);
        var panel = TutorialMaterial("Tutorial Light Panel", Color.white, 0.5f, 1.6f);
        var iceFloor = TutorialMaterial("Tutorial Ice", new Color(0.74f, 0.88f, 1f), 0.92f);
        var accent = TutorialMaterial("Tutorial Accent", new Color(0.2f, 0.55f, 0.95f), 0.4f);
        var glass = StoreAtmosphere.Glass("Tutorial Glass", new Color(0.62f, 0.85f, 1f, 0.32f));
        var ink = new Color(0.06f, 0.2f, 0.36f);

        // the floor is solid down to the pit floor, with gaps where the pits are
        void Slab(string name, float z0, float z1, Material m) => Box(name, root, P(0, -D / 2, (z0 + z1) / 2), new Vector3(W, D, z1 - z0), m);
        Slab("Floor", -1f, 40f, white);
        Slab("Floor", 52f, 102f, white);
        Slab("Floor", 122f, 136f, white);
        Slab("Ice Floor", 136f, 186f, iceFloor);
        Slab("Floor", 186f, L, white);
        Box("Pit Floor", root, P(0, -D - 0.5f, L / 2), new Vector3(W, 1f, L + 2f), trim);
        foreach (var (z0, z1) in new[] { (40f, 52f), (102f, 122f) })
        {
            var pit = new GameObject("Pit").AddComponent<BoxCollider>();
            pit.transform.SetParent(root);
            pit.transform.position = P(0, -D + 1.5f, (z0 + z1) / 2);
            pit.isTrigger = true;
            pit.size = new Vector3(W, 2f, z1 - z0);
            pit.gameObject.AddComponent<Hazard>();
        }

        Box("Wall West", root, P(-W / 2 - 0.5f, (H - D) / 2, L / 2), new Vector3(1f, H + D, L + 2f), white);
        Box("Wall East", root, P(W / 2 + 0.5f, (H - D) / 2, L / 2), new Vector3(1f, H + D, L + 2f), white);
        Box("Wall South", root, P(0, (H - D) / 2, -1.5f), new Vector3(W + 2f, H + D, 1f), white);
        Box("Wall North", root, P(0, (H - D) / 2, L + 0.5f), new Vector3(W + 2f, H + D, 1f), white);
        Box("Ceiling", root, P(0, H + 0.5f, L / 2), new Vector3(W + 2f, 1f, L + 2f), white);
        foreach (int s in new[] { -1, 1 })
            Object.DestroyImmediate(Box2("Skirting", root, P(s * (W / 2 - 0.06f), 0.25f, L / 2), new Vector3(0.12f, 0.5f, L), trim).GetComponent<Collider>());

        // light panels in the ceiling with a light under every other one
        for (int i = 0; i < 17; i++)
        {
            float z = 6f + i * 12f;
            Object.DestroyImmediate(Box2("Light Panel", root, P(0, H - 0.03f, z), new Vector3(7f, 0.06f, 3f), panel).GetComponent<Collider>());
            if (i % 2 == 1) continue;
            var light = new GameObject("Room Light").AddComponent<Light>();
            light.transform.SetParent(root);
            light.transform.position = P(0, H - 1.5f, z);
            light.type = LightType.Point;
            light.range = 30f;
            light.intensity = 70f;
            light.color = new Color(1f, 0.98f, 0.95f);
            light.shadows = LightShadows.None;
        }

        // gates: a glass pane under a white header with the next station's name on it
        Transform Gate(float z, string label)
        {
            Box("Header", root, P(0, 7f + (H - 7f) / 2, z), new Vector3(W, H - 7f, 1f), trim);
            MakeText(root, P(0, 10.5f, z - 0.53f), 0f, label, 12f, 2.4f, ink, lit: true);
            var gate = new GameObject("Gate").transform;
            gate.SetParent(root);
            gate.position = P(0, 0, z);
            var pane = Box2("Glass", gate, P(0, 3.5f, z), new Vector3(W, 7f, 0.3f), glass);
            pane.isStatic = false;
            foreach (int s in new[] { -1, 1 })
                Object.DestroyImmediate(Box2("Frame", gate, P(s * (W / 2 - 0.2f), 3.5f, z), new Vector3(0.4f, 7f, 0.5f), accent).GetComponent<Collider>());
            Object.DestroyImmediate(Box2("Frame", gate, P(0, 0.1f, z), new Vector3(W, 0.2f, 0.5f), accent).GetComponent<Collider>());
            foreach (var t in gate.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            return gate;
        }
        var gates = new[]
        {
            Gate(20f, "JUMP"), Gate(36f, "DASH"), Gate(60f, "CARRY"), Gate(84f, "LADDER + SLIDE"), Gate(132f, "ICE"), Gate(188f, "EXIT"),
        };

        // jump: two steps the full width of the room
        Box("Step", root, P(0, 0.65f, 26f), new Vector3(W, 1.3f, 4f), trim);
        Box("Step", root, P(0, 1.3f, 30f), new Vector3(W, 2.6f, 4f), white);

        // carry: a box, and a ledge too high to jump
        var box = CarryBox(root, P(-3f, 0, 66f), 1.5f);
        const float ledge = 2.7f;
        Box("Ledge", root, P(0, ledge / 2, 76f), new Vector3(W, ledge, 8f), trim);
        Object.DestroyImmediate(Box2("Ledge Edge", root, P(0, ledge - 0.05f, 72.05f), new Vector3(W, 0.1f, 0.1f), accent).GetComponent<Collider>());

        // ladder up the platform, slide off its far side over the second pit
        const float deck = 8f;
        Box("Platform", root, P(0, deck / 2, 97f), new Vector3(W, deck, 10f), white);
        KitLadder(root, P(0, 0, 92f), Vector3.back, 0f, deck, P(0, deck, 94.2f));
        Slide(root, P(0, deck, 101.6f), P(0, 0.4f, 124f), 4.5f, exitSpeed: 9f);

        // ice: a rink with pillars to glide round and a pad to stop on
        IceZoneBox(root, P(0, 1.5f, 161f), new Vector3(W, 3f, 50f));
        foreach (var (x, z) in new[] { (-5f, 146f), (5f, 156f), (-3.5f, 167f) })
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(root);
            pillar.transform.position = P(x, H / 2, z);
            pillar.transform.localScale = new Vector3(2.4f, H / 2, 2.4f);
            pillar.GetComponent<Renderer>().sharedMaterial = white;
            pillar.isStatic = true;
        }
        Object.DestroyImmediate(Box2("Stop Pad", root, P(0, 0.012f, 179f), new Vector3(8f, 0.02f, 6f), accent).GetComponent<Collider>());

        var start = Waypoint(root, "Tutorial Start", P(0, 1, 4f), 0);
        Transform Respawn(string name, float y, float z) => Waypoint(root, name, P(0, y, z), 0);
        Bounds Area(float y, float z, float depth, float width = W) => new Bounds(P(0, y, z), new Vector3(width, 4f, depth));

        var lessons = new List<TutorialRoom.Lesson>
        {
            new TutorialRoom.Lesson { icon = Glyph("walk"), title = "WALK", goal = TutorialRoom.Goal.Reach,
                body = "Use WASD to walk and move the mouse to look around. Walk up to the glass.",
                keys = new[] { "WASD", "MOUSE" }, area = Area(2f, 16f, 6f), gate = gates[0] },
            new TutorialRoom.Lesson { icon = Glyph("jump"), title = "JUMP", goal = TutorialRoom.Goal.Reach,
                body = "Press SPACE to jump. Hop up onto both steps.",
                keys = new[] { "SPACE" }, area = Area(4.6f, 30f, 4f), gate = gates[1], respawn = Respawn("Respawn Jump", 1f, 22f) },
            new TutorialRoom.Lesson { icon = Glyph("bolt"), title = "DASH", goal = TutorialRoom.Goal.Reach,
                body = "Press SHIFT to dash. The gap is too far to jump, so take a running jump and dash in mid-air. Dashing recharges after a few seconds.",
                keys = new[] { "SHIFT", "SPACE" }, area = Area(2f, 56f, 4f), gate = gates[2], respawn = Respawn("Respawn Dash", 1f, 37.5f) },
            new TutorialRoom.Lesson { icon = Glyph("hand"), title = "PICK UP", goal = TutorialRoom.Goal.PickUp,
                body = "Walk up to the box and press E to pick it up.",
                keys = new[] { "E" }, respawn = Respawn("Respawn Carry", 1f, 62f) },
            new TutorialRoom.Lesson { icon = Glyph("box"), title = "STAND ON IT", goal = TutorialRoom.Goal.Reach,
                body = "That ledge is too high to jump. Press E again to put the box down against it, then hop onto the box and jump up. Press R if the box gets stuck.",
                keys = new[] { "E", "SPACE", "R" }, area = Area(ledge + 2f, 76.5f, 7f), gate = gates[3] },
            new TutorialRoom.Lesson { icon = Glyph("stairs"), title = "CLIMB", goal = TutorialRoom.Goal.Reach,
                body = "Walk into the ladder to grab it. Hold W to climb and S to climb down. Jump to let go.",
                keys = new[] { "W", "S", "SPACE" }, area = Area(deck + 2f, 97.5f, 9f), respawn = Respawn("Respawn Ladder", 1f, 87f) },
            new TutorialRoom.Lesson { icon = Glyph("ski"), title = "SLIDE", goal = TutorialRoom.Goal.Reach,
                body = "Walk onto the baking tray. Henry lies back and rides it all the way down to the floor.",
                keys = new[] { "W" }, area = Area(2f, 128f, 8f), gate = gates[4], respawn = Respawn("Respawn Slide", deck + 1f, 96f) },
            new TutorialRoom.Lesson { icon = Glyph("frozen"), title = "ICE", goal = TutorialRoom.Goal.StopOn,
                body = "Ice is slippery! Henry keeps sliding after you let go and turns slowly. Glide round the pillars and come to a stop on the blue square.",
                keys = new[] { "WASD" }, area = new Bounds(P(0, 1.5f, 179f), new Vector3(8f, 3f, 6f)), gate = gates[5], respawn = Respawn("Respawn Ice", 1f, 134f) },
        };
        root.gameObject.AddComponent<TutorialRoom>().Setup(start, box != null ? box.GetComponent<Carryable>() : null, lessons.ToArray(), Glyph("check"));
    }
}
