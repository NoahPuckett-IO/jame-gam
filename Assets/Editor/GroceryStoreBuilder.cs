using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: JameGam > Build Grocery Store. Builds the whole store in the open scene: floor, walls,
// ceiling with flickering lights, five stocked aisles of shelving, a checkout area with cash
// registers and a sliding door full of bright daylight, the placeholder player, and the title menu.
// Re-running replaces the previous build.
//
// The player stays a normal ~2 unit capsule (keeps physics and the controller sane); instead the
// whole store is built at Scale x real size so the player is about the size of a can of beans.
// The pack's models have inconsistent sizes, so every model is measured and scaled to a
// real-world size here rather than trusting its import scale.
//
// Layout (looking from above, +Z is "up the aisles"):
//   West wall + door | checkout area | walkway | Aisle 5 | ... | Aisle 1 | spawn walkway | East wall
static partial class GroceryStoreBuilder
{
    const string MatFolder = "Assets/Materials";
    const string ModelFolder = "Assets/ThirdParty/Supermarket";
    const string KenneyFolder = "Assets/ThirdParty/Kenney";
    const string FoodKitFolder = "Assets/ThirdParty/KenneyFood";
    const string ShelfModel = "shelfMarker"; // also try shelfMarker2/3/4

    // A ~11 cm can scaled to a 2 m player ≈ 18x. Real-world sizes below are in meters.
    const float Scale = 18f;

    const float ShelfHeight = 1.8f * Scale;
    const float TargetAisleLength = 12f * Scale;
    const float WalkwayWidth = 2.2f * Scale;
    const float EndZone = 2.4f * Scale;          // floor between aisle ends and the north/south walls (coolers take 0.85 m)
    const float CheckoutWidth = 11f * Scale;     // extra room on the west side for the registers
    const float WallHeight = 4.5f * Scale;       // also the ceiling height
    const float DoorWidth = 3.2f * Scale;
    const float DoorHeight = 2.6f * Scale;
    const int AisleCount = 5;

    // Stock per aisle (Aisle 1 is next to the spawn). Model name and its real-world longest
    // dimension in meters. "k:" models come from Kenney's Food Kit, the rest from the Supermarket
    // pack. Each shelf level of each section gets one product, cycling through the list.
    static readonly (string model, float size)[][] Stock =
    {
        // Bread & bakery
        new[] { ("Bread", 0.28f), ("k:loaf", 0.28f), ("k:croissant", 0.14f), ("k:loaf-baguette", 0.6f), ("k:muffin", 0.08f),
                ("Bread", 0.28f), ("k:loaf-round", 0.22f), ("k:donut-sprinkles", 0.1f), ("k:peanut-butter", 0.12f),
                ("k:cookie-chocolate", 0.08f), ("k:honey", 0.12f), ("Pasta", 0.22f), ("k:donut", 0.1f), ("coffee", 0.18f) },
        // Canned goods & condiments
        new[] { ("BeanCan", 0.11f), ("k:can", 0.12f), ("k:bottle-ketchup", 0.2f), ("k:can-small", 0.09f), ("BeanCan", 0.11f),
                ("k:bottle-musterd", 0.18f), ("k:bottle-oil", 0.3f), ("Pasta", 0.22f), ("k:soy", 0.18f),
                ("k:shaker-salt", 0.1f), ("k:shaker-pepper", 0.1f) },
        // Dairy & drinks
        new[] { ("Milk", 0.25f), ("k:carton", 0.25f), ("Juice", 0.25f), ("Eggs", 0.3f), ("k:cheese", 0.15f), ("k:soda-bottle", 0.3f),
                ("cheese", 0.15f), ("k:carton-small", 0.15f), ("k:soda-can", 0.12f), ("k:wine-red", 0.3f), ("k:wine-white", 0.3f) },
        // Produce
        new[] { ("Apple", 0.09f), ("k:orange", 0.08f), ("banana", 0.2f), ("k:carrot", 0.2f), ("k:broccoli", 0.15f), ("grapes", 0.15f),
                ("k:pineapple", 0.3f), ("lettuce", 0.2f), ("k:lemon", 0.07f), ("k:corn", 0.2f), ("tomate", 0.09f), ("k:cabbage", 0.2f),
                ("k:pear", 0.1f), ("k:eggplant", 0.2f), ("k:watermelon", 0.35f), ("k:onion", 0.08f), ("k:paprika", 0.1f), ("k:mushroom", 0.06f) },
        // Meat, frozen & snacks
        new[] { ("meat", 0.25f), ("k:meat-ribs", 0.3f), ("ChickenLeg", 0.15f), ("k:whole-ham", 0.25f), ("fish", 0.3f), ("k:bacon-raw", 0.25f),
                ("k:sausage", 0.2f), ("k:turkey", 0.35f), ("IceCream", 0.15f), ("k:pizza-box", 0.35f), ("k:ice-cream", 0.15f),
                ("k:candy-bar", 0.15f), ("k:chocolate", 0.15f), ("k:cake", 0.25f), ("k:pie", 0.25f), ("Soap", 0.2f), ("toaster", 0.3f) },
    };

    [MenuItem("JameGam/Build Grocery Store")]
    static void Build()
    {
        MakeModelsReadable(); // the builder reads meshes to find shelf boards and true bounds
        var shelfPrefab = LoadModel(ShelfModel);
        if (shelfPrefab == null)
        {
            EditorUtility.DisplayDialog("Build Grocery Store",
                $"Couldn't find {ModelFolder}/{ShelfModel}.fbx. Let Unity finish importing, then try again.", "OK");
            return;
        }

        foreach (var name in new[] { "Store", "Player", "Cube", "Main Menu", "EventSystem" })
        {
            var old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        var wallMat = GetMaterial("Wall", new Color(0.9f, 0.88f, 0.8f));
        var playerMat = GetMaterial("Player", new Color(0.9f, 0.4f, 0.2f));

        var shelf = MeasureShelf(shelfPrefab);
        int units = Mathf.Max(1, Mathf.RoundToInt(TargetAisleLength / shelf.length));
        float aisleLength = units * shelf.length;

        var store = new GameObject("Store");
        Undo.RegisterCreatedObjectUndo(store, "Build Grocery Store");

        // A one-sided run of shelving lines the east wall (half of a gondola, the rest is in the wall),
        // so the first hallway has shelves on both sides. It gets its own strip of floor.
        float wallShelfOut = shelf.doubleSided ? shelf.depth / 2 : shelf.depth;
        float width = CheckoutWidth + AisleCount * shelf.gondolaDepth + (AisleCount + 1) * WalkwayWidth + wallShelfOut;
        float depth = aisleLength + 2 * EndZone;
        float left = -width / 2, right = width / 2;
        float aislesRight = right - wallShelfOut; // east edge of the first hallway
        float wallT = 0.2f * Scale;

        Box("Floor", store.transform, new Vector3(0, -0.5f, 0), new Vector3(width, 1, depth), FloorTiles(width, depth));
        Box("Ceiling", store.transform, new Vector3(0, WallHeight + 0.5f, 0), new Vector3(width + 2 * wallT, 1, depth + 2 * wallT), CeilingTiles(width + 2 * wallT, depth + 2 * wallT));
        Box("Wall N", store.transform, new Vector3(0, WallHeight / 2, (depth + wallT) / 2), new Vector3(width, WallHeight, wallT), wallMat);
        Box("Wall S", store.transform, new Vector3(0, WallHeight / 2, -(depth + wallT) / 2), new Vector3(width, WallHeight, wallT), wallMat);
        Box("Wall E", store.transform, new Vector3(right + wallT / 2, WallHeight / 2, 0), new Vector3(wallT, WallHeight, depth), wallMat);
        BuildWestWallAndDoor(store.transform, left - wallT / 2, wallT, depth, wallMat);

        var aisles = new List<Transform>();
        for (int i = 0; i < AisleCount; i++)
        {
            float x = aislesRight - WalkwayWidth - shelf.gondolaDepth / 2 - i * (shelf.gondolaDepth + WalkwayWidth);
            aisles.Add(BuildAisle(i + 1, store.transform, x, units, aisleLength, shelfPrefab, shelf));
        }
        var wallShelf = BuildAisle(0, store.transform, shelf.doubleSided ? right : right - shelf.depth / 2, units, aisleLength, shelfPrefab, shelf);
        wallShelf.name = "Wall Shelf";

        TalkerColors = new[]
        {
            GetMaterial("Talker Red", new Color(0.9f, 0.12f, 0.1f)), GetMaterial("Talker Yellow", new Color(1f, 0.8f, 0.1f)),
            GetMaterial("Talker Green", new Color(0.15f, 0.7f, 0.3f)), GetMaterial("Talker Orange", new Color(1f, 0.45f, 0.05f)),
        };
        PlaceBoxes(store.transform, aisles, shelf, aisleLength);
        BuildBlockades(store.transform, aisles, shelf, aisleLength, depth);
        for (int i = 0; i < aisles.Count && i < Stock.Length; i++)
            StockAisle(aisles[i], shelf, aisleLength, Stock[i]);
        StockAisle(wallShelf, shelf, aisleLength, Stock[1], sides: new[] { -1 }); // only the side facing the hallway

        BuildEndcaps(store.transform, aisles, shelf, aisleLength);
        BuildCheckout(store.transform, left, depth);
        DressCheckout(store.transform, left, right, depth);

        float coolersFrom = left + CheckoutWidth;
        BuildCoolerRun(store.transform, coolersFrom, right, depth / 2, +1, CoolerDrinks, "DAIRY & DRINKS", new Color(0.1f, 0.35f, 0.85f), 1);
        BuildCoolerRun(store.transform, coolersFrom, right, -depth / 2, -1, CoolerFrozen, "FROZEN FOODS", new Color(0.8f, 0.1f, 0.12f), 2);
        BacklitSign(store.transform, new Vector3(right - 0.05f * Scale, WallHeight - 0.9f * Scale, 0), Vector3.left,
            7f * Scale, 1.0f * Scale, "FRESH MARKET", new Color(0.15f, 0.55f, 0.25f),
            subtitle: "OPEN 24 HOURS  •  " + MenuBuilder.GameTitle.ToUpper(), iconLeft: "store", iconRight: "cart");
        // Aisle 2's sign hangs by one wire; Aisle 4's has come down and lies on the floor.
        for (int i = 0; i < aisles.Count; i++)
            HangAisleSign(store.transform, aisles[i].position.x, -aisleLength / 2, i + 1, AisleNames[i], damage: i == 1 ? 1 : i == 3 ? 2 : 0);

        // Lights down every walkway and over the checkout area.
        var spots = new List<Vector2>();
        float lightStep = 3f * Scale;
        int rows = Mathf.Max(1, Mathf.RoundToInt(depth / lightStep));
        for (int r = 0; r < rows; r++)
        {
            float z = -depth / 2 + (r + 0.5f) * depth / rows;
            for (int w = 0; w <= AisleCount; w++)
                spots.Add(new Vector2(aislesRight - WalkwayWidth / 2 - w * (shelf.gondolaDepth + WalkwayWidth), z));
            spots.Add(new Vector2(left + CheckoutWidth * 0.3f, z));
            spots.Add(new Vector2(left + CheckoutWidth * 0.75f, z));
        }
        StoreAtmosphere.BuildCeilingLights(store.transform, spots, WallHeight, Scale, StoreAtmosphere.FixtureMaterial());
        StoreAtmosphere.ApplySceneLook(Scale);

        // Where Play drops the player: the near end of the rightmost walkway, facing up the aisle.
        var spawn = new GameObject("Spawn Point").transform;
        spawn.SetParent(store.transform);
        spawn.position = new Vector3(aislesRight - WalkwayWidth / 2, 1, -aisleLength / 2 - 0.5f * Scale);

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = spawn.position;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        var controller = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInteractor>();
        Undo.RegisterCreatedObjectUndo(player, "Build Grocery Store");

        // Walkway centers, east to west: 0 is the first hallway.
        var walkways = new List<float>();
        for (int w = 0; w <= AisleCount; w++)
            walkways.Add(aislesRight - WalkwayWidth / 2 - w * (shelf.gondolaDepth + WalkwayWidth));
        BuildLevelOne(store.transform, walkways, aisleLength, depth);

        AddGrime(store.transform, left, right, depth);
        MakeProductsMatte(store.transform);

        MenuBuilder.Build(controller, spawn);

        // The store is a few hundred units across; make sure the far end is not clipped.
        if (Camera.main != null)
        {
            Undo.RecordObject(Camera.main, "Build Grocery Store");
            Camera.main.farClipPlane = 2000f;
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = player;
    }

    // West wall with a big doorway in the middle, sliding glass doors, and bright daylight outside.
    static void BuildWestWallAndDoor(Transform store, float wallX, float wallT, float depth, Material wallMat)
    {
        float sideLen = (depth - DoorWidth) / 2;
        Box("Wall W South", store, new Vector3(wallX, WallHeight / 2, -(DoorWidth + sideLen) / 2), new Vector3(wallT, WallHeight, sideLen), wallMat);
        Box("Wall W North", store, new Vector3(wallX, WallHeight / 2, (DoorWidth + sideLen) / 2), new Vector3(wallT, WallHeight, sideLen), wallMat);
        Box("Wall W Lintel", store, new Vector3(wallX, (WallHeight + DoorHeight) / 2, 0), new Vector3(wallT, WallHeight - DoorHeight, DoorWidth), wallMat);

        var door = new GameObject("Sliding Door").transform;
        door.SetParent(store);
        door.position = new Vector3(wallX, 0, 0);

        var doorPrefab = LoadModel("Door");
        Transform MakePanel(string name, float z)
        {
            Transform panel;
            if (doorPrefab != null)
            {
                panel = FitInto(doorPrefab, door, new Vector3(0.06f * Scale, DoorHeight, DoorWidth / 2));
            }
            else
            {
                panel = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                panel.SetParent(door);
                panel.localScale = new Vector3(0.06f * Scale, DoorHeight, DoorWidth / 2);
                panel.localPosition = new Vector3(0, DoorHeight / 2, 0);
            }
            panel.name = name;
            panel.localPosition = new Vector3(0, panel.localPosition.y, z);
            AddBoxColliders(panel.gameObject);
            return panel;
        }
        var leftPanel = MakePanel("Panel South", -DoorWidth / 4);
        var rightPanel = MakePanel("Panel North", DoorWidth / 4);
        door.gameObject.AddComponent<SlidingDoor>().Setup(leftPanel, rightPanel, DoorWidth / 2, 3f * Scale);

        // Outside: a sidewalk, a glowing "daylight" backdrop for bloom, and a sun spot shining in.
        var outside = new GameObject("Outside").transform;
        outside.SetParent(store);
        Box("Sidewalk", outside, new Vector3(wallX - 2f * Scale, -0.5f, 0), new Vector3(4f * Scale, 1, DoorWidth * 3), GetMaterial("Sidewalk", new Color(0.85f, 0.85f, 0.85f)));
        var glow = StoreAtmosphere.Emissive("Daylight", new Color(1f, 0.98f, 0.94f), 3.2f);
        Box("Daylight", outside, new Vector3(wallX - 4f * Scale, WallHeight / 2, 0), new Vector3(0.5f, WallHeight * 1.5f, DoorWidth * 3), glow);
        Box("Daylight South", outside, new Vector3(wallX - 2f * Scale, WallHeight / 2, -DoorWidth * 1.5f), new Vector3(4f * Scale, WallHeight * 1.5f, 0.5f), glow);
        Box("Daylight North", outside, new Vector3(wallX - 2f * Scale, WallHeight / 2, DoorWidth * 1.5f), new Vector3(4f * Scale, WallHeight * 1.5f, 0.5f), glow);

        var sunGo = new GameObject("Door Sunlight");
        sunGo.transform.SetParent(outside);
        sunGo.transform.position = new Vector3(wallX - 3f * Scale, DoorHeight * 1.1f, 0);
        sunGo.transform.rotation = Quaternion.Euler(22f, 90f, 0f); // into the store, angled down
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Spot;
        sun.color = new Color(1f, 0.97f, 0.92f);
        sun.range = 25f * Scale;
        sun.spotAngle = 75f;
        sun.innerSpotAngle = 35f;
        sun.intensity = 0.45f * (8f * Scale) * (8f * Scale);
        sun.shadows = LightShadows.Soft;
    }

    // Store front, laid out like a real one: checkout lanes running east-west between the aisles
    // and the door (so you walk through a lane to leave), a nested row of carts by the door,
    // and freezers against the north wall.
    static void BuildCheckout(Transform store, float left, float depth)
    {
        var area = new GameObject("Checkout").transform;
        area.SetParent(store);
        float laneX = left + CheckoutWidth * 0.58f;

        // Counters: 2.5 m tall (well over the 1.05 m carts), at most 6 m long, each standing on a
        // black rubber mat. Lanes between them; the middle lane lines up with the door.
        var cashier = LoadModel("cashier");
        var rubber = GetMaterial("RubberMat", new Color(0.05f, 0.05f, 0.055f));
        rubber.SetFloat("_Smoothness", 0.05f);
        float matT = 0.015f * Scale;
        checkoutCounters.Clear();
        if (cashier != null)
            foreach (float z in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            {
                var counter = PlaceProp(cashier, area, 2.5f, 6f, alongX: true, new Vector3(laneX, matT, z * Scale));
                var b = GetBounds(counter);
                Box("Register Mat", area, new Vector3(b.center.x, matT / 2, b.center.z),
                    new Vector3(b.size.x + 0.5f * Scale, matT, b.size.z + 0.7f * Scale), rubber);
                checkoutCounters.Add(counter);
            }

        // Chest freezers are ~0.9 m tall, upright ones ~2 m.
        var freezers = new[] { LoadModel("Frezer"), LoadModel("Frezer2") }.Where(f => f != null).ToArray();
        for (int i = 0; i < freezers.Length; i++)
        {
            var probe = Spawn(freezers[i], null, 0, 1);
            var pb = GetBounds(probe);
            Object.DestroyImmediate(probe);
            float height = pb.size.y > Mathf.Max(pb.size.x, pb.size.z) ? 2f : 0.9f;
            var f = PlaceProp(freezers[i], area, height, 2.2f, alongX: true, new Vector3(left + CheckoutWidth * (0.3f + 0.4f * i), 0, 0));
            var b = GetBounds(f);
            f.transform.position += Vector3.forward * (depth / 2 - 0.1f * Scale - b.max.z);
        }

        // Cart corral: three rows of carts nested nose-to-tail along the west wall, south of the door,
        // plus a few strays left around the store front.
        var cart = LoadModel("ShopingCar");
        if (cart != null)
        {
            for (int row = 0; row < 2; row++) // two long rows, clear of the drinks racks at the lane ends
            {
                float z = -DoorWidth / 2 - 0.6f * Scale;
                for (int i = 0; i < 11; i++)
                {
                    PlaceProp(cart, area, 1.05f, 1.1f, alongX: false, new Vector3(left + (0.45f + row * 0.65f) * Scale, 0, z));
                    z -= 0.38f * Scale; // nested carts overlap most of their length
                }
            }
            var strayRng = new System.Random(4);
            foreach (var spot in new[] { new Vector2(0.2f, -0.6f), new Vector2(0.15f, 3.2f), new Vector2(0.9f, -6.6f), new Vector2(0.6f, 6.4f) })
            {
                var stray = PlaceProp(cart, area, 1.05f, 1.1f, alongX: strayRng.Next(2) == 0,
                    new Vector3(left + CheckoutWidth * spot.x, 0, spot.y * Scale));
                stray.transform.Rotate(0, strayRng.Next(-40, 40), 0, Space.World);
            }
        }
    }

    // Spawns a model `realHeight` meters tall (scaled), shrunk further if its footprint is longer than
    // `maxLength`, with its long side along X or Z, standing on the floor at `at`.
    static GameObject PlaceProp(GameObject prefab, Transform parent, float realHeight, float maxLength, bool alongX, Vector3 at)
    {
        var go = Spawn(prefab, parent, 0, 1);
        var b = GetBounds(go);
        float yaw = (b.size.x >= b.size.z) == alongX ? 0 : 90;
        Object.DestroyImmediate(go);

        go = Spawn(prefab, parent, yaw, 1);
        b = GetBounds(go);
        float s = Mathf.Min(realHeight * Scale / b.size.y, maxLength * Scale / Mathf.Max(b.size.x, b.size.z));
        go.transform.localScale *= s;
        PlaceOnFloor(go, at);
        AddBoxColliders(go);
        return go;
    }

    static Material[] TalkerColors;

    static readonly string[] AisleNames = { "BAKERY", "CANNED GOODS", "DAIRY & DRINKS", "PRODUCE", "MEAT & FROZEN" };

    // Classic supermarket vinyl tile: off-white and pale grey checker with speckles and grout lines.
    // Generated once as a PNG so it can be tweaked in any image editor.
    static Material FloorTiles(float width, float depth)
    {
        const string texPath = MatFolder + "/FloorTiles.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 256, half = size / 2;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            var rng = new System.Random(3);
            var light = new Color(0.86f, 0.86f, 0.83f);
            var dark = new Color(0.62f, 0.64f, 0.66f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool checker = (x < half) ^ (y < half);
                var c = checker ? light : dark;
                float speck = (float)rng.NextDouble();
                if (speck > 0.97f) c *= 0.8f;
                else if (speck < 0.02f) c = Color.Lerp(c, Color.white, 0.5f);
                c *= 0.97f + 0.03f * (float)rng.NextDouble();
                if (x % half < 2 || y % half < 2) c *= 0.7f; // grout
                tex.SetPixel(x, y, c);
            }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
        }

        var mat = GetMaterial("FloorTiles", Color.white);
        var map = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        mat.SetTexture("_BaseMap", map);
        mat.color = Color.white;
        float tile = 0.3f * Scale; // 30 cm tiles; the texture holds 2×2
        mat.SetTextureScale("_BaseMap", new Vector2(width / (2 * tile), depth / (2 * tile)));
        mat.SetFloat("_Smoothness", 0.55f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Hanging sign over the near end of an aisle, readable from both sides.
    // damage: 0 = hanging normally, 1 = one wire snapped so it dangles at an angle,
    // 2 = both wires gone and the sign lies on the floor.
    static void HangAisleSign(Transform store, float x, float z, int number, string label, int damage = 0)
    {
        var sign = new GameObject($"Aisle Sign {number}").transform;
        sign.SetParent(store);
        sign.position = new Vector3(x, WallHeight - 1.1f * Scale, z);

        // Two-sided lightbox: big aisle number in a white circle on the left, the aisle name, and an icon.
        BacklitSign(sign, sign.position, Vector3.back, 1.7f * Scale, 0.6f * Scale, label, new Color(0.12f, 0.35f, 0.62f),
            twoSided: true, subtitle: AisleSubtitles[number - 1], iconRight: AisleIcons[number - 1],
            iconLeft: ""); // empty icon = keep the left slot clear for the number disc below
        var disc = GetMaterial("SignDisc", Color.white);
        foreach (var n in new[] { Vector3.back, Vector3.forward })
        {
            var right = Vector3.Cross(Vector3.up, -n).normalized;
            var at = sign.position + right * (-0.85f * Scale + 0.3f * Scale) + n * 0.05f * Scale;
            var circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circle.name = "Number Disc";
            circle.transform.SetParent(sign);
            circle.transform.position = at;
            circle.transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
            circle.transform.localScale = new Vector3(0.44f * Scale, 0.005f * Scale, 0.44f * Scale);
            circle.GetComponent<Renderer>().sharedMaterial = disc;
            Object.DestroyImmediate(circle.GetComponent<Collider>());
            MakeText(sign, at + n * 0.015f * Scale, Mathf.Atan2(-n.x, -n.z) * Mathf.Rad2Deg, number.ToString(),
                0.3f * Scale, 0.3f * Scale, new Color(0.12f, 0.35f, 0.62f));
        }

        var wireMat = GetMaterial("Wire", new Color(0.2f, 0.2f, 0.2f));
        var top = sign.position + Vector3.up * 0.3f * Scale;
        float wireLen = WallHeight - top.y;
        void Wire(Vector3 at) =>
            Box("Wire", store, at + Vector3.up * wireLen / 2, new Vector3(0.01f * Scale, wireLen, 0.01f * Scale), wireMat);

        if (damage == 0)
        {
            Wire(top + Vector3.left * 0.6f * Scale);
            Wire(top + Vector3.right * 0.6f * Scale);
        }
        else if (damage == 1)
        {
            // Right wire snapped: the sign swings down from its left wire, with the broken stub left hanging.
            var hook = top + Vector3.left * 0.6f * Scale;
            sign.RotateAround(hook, Vector3.forward, -38f); // right end drops
            Wire(hook);
            Box("Snapped Wire", store, top + Vector3.right * 0.6f * Scale + Vector3.up * wireLen * 0.8f,
                new Vector3(0.01f * Scale, wireLen * 0.4f, 0.01f * Scale), wireMat);
        }
        else
        {
            // Fallen: lying almost flat on the floor a little way out from the aisle end, slightly twisted.
            sign.rotation = Quaternion.Euler(80f, 14f, 0f);
            var b = GetBounds(sign.gameObject);
            sign.position += new Vector3(0.4f * Scale - (b.center.x - x), -b.min.y, -0.9f * Scale - (b.center.z - z));
            Box("Snapped Wire", store, top + Vector3.left * 0.6f * Scale + Vector3.up * wireLen * 0.8f,
                new Vector3(0.01f * Scale, wireLen * 0.4f, 0.01f * Scale), wireMat);
            Box("Snapped Wire", store, top + Vector3.right * 0.6f * Scale + Vector3.up * wireLen * 0.85f,
                new Vector3(0.01f * Scale, wireLen * 0.3f, 0.01f * Scale), wireMat);
        }
    }

    static readonly string[] AisleSubtitles = { "BREAD • PASTRY • BREAKFAST", "SOUP • BEANS • SAUCES", "MILK • EGGS • SODA", "FRUIT • VEGETABLES", "MEAT • FISH • SNACKS" };
    static readonly string[] AisleIcons = { "bakery", "soup", "drink", "apple", "fish" };

    // Moves the spawn to the checkout side of the store, facing the lanes, to look at it quickly.
    // Rebuilding the store puts the spawn back.
    [MenuItem("JameGam/Debug/Spawn At Checkout")]
    static void SpawnAtCheckout()
    {
        var spawn = GameObject.Find("Spawn Point");
        var checkout = GameObject.Find("Checkout");
        if (spawn == null || checkout == null) return;
        var b = GetBounds(checkout);
        Undo.RecordObject(spawn.transform, "Spawn At Checkout");
        spawn.transform.SetPositionAndRotation(new Vector3(b.max.x + 1.2f * Scale, 1, b.center.z - 1.2f * Scale), Quaternion.Euler(0, -70, 0));
    }

    [MenuItem("JameGam/Debug/Log Shelf Info")]
    static void LogShelfInfo()
    {
        MakeReadable(ShelfModel);
        var prefab = LoadModel(ShelfModel);
        var info = MeasureShelf(prefab);
        var go = Spawn(prefab, null, info.yaw, info.scale);
        PlaceOnFloor(go, Vector3.zero);
        var b = GetBounds(go);
        var sb = new System.Text.StringBuilder("[ShelfDebug] ");
        sb.Append($"yaw={info.yaw} scale={info.scale} len={info.length} depth={info.depth} front+X={info.frontIsPlusX} double={info.doubleSided} bounds={b}\n");
        foreach (var t in go.GetComponentsInChildren<Transform>())
        {
            var mf = t.GetComponent<MeshFilter>();
            sb.Append($"  node {t.name} pos={t.position} rot={t.eulerAngles} scale={t.lossyScale} mesh={(mf ? mf.sharedMesh.name + " v" + mf.sharedMesh.vertexCount + " readable=" + mf.sharedMesh.isReadable : "-")}\n");
        }
        var up = UpwardTriangles(go);
        sb.Append($"  upward tris={up.Count}\n");
        for (float fx = -0.45f; fx <= 0.451f; fx += 0.15f)
        {
            float x = b.center.x + fx * b.size.x;
            sb.Append($"  x={fx:+0.00;-0.00} surfaces: {string.Join(", ", SurfacesAt(up, x, b.center.z).Select(h => h.ToString("0.0")))}\n");
        }
        Debug.Log(sb.ToString());
        Object.DestroyImmediate(go);
    }

    // Wraps a model in an unrotated holder scaled so the model fills `size` exactly (world axes).
    static Transform FitInto(GameObject prefab, Transform parent, Vector3 size)
    {
        var holder = new GameObject(prefab.name).transform;
        holder.SetParent(parent, false);
        var probe = Spawn(prefab, holder, 0, 1);
        var b = GetBounds(probe);
        // Match the model's thin side to the target's thin side.
        bool modelThinX = b.size.x < b.size.z, targetThinX = size.x < size.z;
        if (modelThinX != targetThinX)
        {
            Object.DestroyImmediate(probe.gameObject);
            probe = Spawn(prefab, holder, 90, 1);
            b = GetBounds(probe);
        }
        probe.transform.position -= b.center - holder.position;
        holder.localScale = new Vector3(size.x / b.size.x, size.y / b.size.y, size.z / b.size.z);
        holder.localPosition += Vector3.up * size.y / 2;
        return holder;
    }

    struct ShelfInfo
    {
        public float yaw;          // rotation that lines the shelf's long side up with world Z
        public float scale;        // uniform scale applied on top of the prefab's own scale
        public float length, depth;
        public bool frontIsPlusX;  // which side the shelves open towards at `yaw`
        public bool doubleSided;   // open on both sides: one unit per gondola instead of two back to back
        public float gondolaDepth;
        public float SideDepth => doubleSided ? depth / 2 : depth;
    }

    // Works out how the shelf model is oriented and sized, and which side is its open front,
    // from a temporary copy's mesh: the closed back shows a lot of surface facing out, the open
    // front only thin board edges.
    static ShelfInfo MeasureShelf(GameObject prefab)
    {
        var info = new ShelfInfo();
        var probe = Spawn(prefab, null, 0, 1);
        var b = GetBounds(probe);
        info.yaw = b.size.x > b.size.z ? 90 : 0;

        Object.DestroyImmediate(probe);
        probe = Spawn(prefab, null, info.yaw, 1);
        info.scale = ShelfHeight / GetBounds(probe).size.y;
        Object.DestroyImmediate(probe);

        probe = Spawn(prefab, null, info.yaw, info.scale);
        b = GetBounds(probe);
        info.length = b.size.z;
        info.depth = b.size.x;

        float plus = 0, minus = 0;
        foreach (var (p0, p1, p2) in WorldTriangles(probe))
        {
            var n = Vector3.Cross(p1 - p0, p2 - p0); // length = 2 × area
            if (n.x > 0) plus += n.x; else minus -= n.x;
        }
        info.frontIsPlusX = plus <= minus;
        info.doubleSided = Mathf.Abs(plus - minus) < 0.25f * Mathf.Max(plus, minus);
        info.gondolaDepth = info.doubleSided ? info.depth : info.depth * 2;
        Object.DestroyImmediate(probe);
        return info;
    }

    static IEnumerable<(Vector3, Vector3, Vector3)> WorldTriangles(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            var m = mf.transform.localToWorldMatrix;
            var v = mesh.vertices;
            var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
                yield return (m.MultiplyPoint3x4(v[t[i]]), m.MultiplyPoint3x4(v[t[i + 1]]), m.MultiplyPoint3x4(v[t[i + 2]]));
        }
    }

    static List<(Vector3, Vector3, Vector3)> UpwardTriangles(GameObject go) =>
        WorldTriangles(go)
            .Where(t => Vector3.Cross(t.Item2 - t.Item1, t.Item3 - t.Item1).normalized.y > 0.7f)
            .ToList();

    // Heights of every upward-facing surface of `tris` directly above/below (x, z), lowest first.
    static List<float> SurfacesAt(List<(Vector3 a, Vector3 b, Vector3 c)> tris, float x, float z)
    {
        var heights = new List<float>();
        foreach (var (a, b, c) in tris)
        {
            // Barycentric test in the XZ plane, then interpolate Y.
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(d) < 1e-6f) continue;
            float w1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d;
            float w2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d;
            float w3 = 1 - w1 - w2;
            if (w1 < 0 || w2 < 0 || w3 < 0) continue;
            heights.Add(w1 * a.y + w2 * b.y + w3 * c.y);
        }
        heights.Sort();
        return heights.Where((y, i) => i == 0 || y - heights[i - 1] > 0.1f).ToList();
    }

    static bool HasSurface(List<(Vector3, Vector3, Vector3)> tris, float x, float z, float y) =>
        SurfacesAt(tris, x, z).Any(h => Mathf.Abs(h - y) < 0.02f * Scale);

    static void MakeReadable(string model)
    {
        var importer = AssetImporter.GetAtPath($"{ModelFolder}/{model}.fbx") as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    static Transform BuildAisle(int number, Transform parent, float x, int units, float aisleLength,
        GameObject prefab, ShelfInfo shelf)
    {
        var aisle = new GameObject($"Aisle {number}").transform;
        aisle.SetParent(parent);
        aisle.position = new Vector3(x, 0, 0);

        // Each side faces outwards into its walkway: +1 = facing +X, -1 = facing -X.
        var sides = shelf.doubleSided ? new[] { 1 } : new[] { 1, -1 };
        foreach (int side in sides)
        {
            float yaw = shelf.yaw + ((side > 0) == shelf.frontIsPlusX ? 0 : 180);
            float cx = shelf.doubleSided ? x : x + side * shelf.depth / 2;
            for (int u = 0; u < units; u++)
            {
                float z = -aisleLength / 2 + (u + 0.5f) * shelf.length;
                var go = Spawn(prefab, aisle, yaw, shelf.scale);
                PlaceOnFloor(go, new Vector3(cx, 0, z));
                AddMeshColliders(go);
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }
        }
        return aisle;
    }

    class ProductType
    {
        public GameObject prefab;
        public float size;     // longest side, scaled world units
        public float yaw;      // lines the product's long side up with the shelf (Z)
        public Vector3 bounds; // size at scale 1 after `yaw`
    }

    static ProductType MeasureProduct(GameObject prefab, float realSize)
    {
        var go = Spawn(prefab, null, 0, 1);
        var b = GetBounds(go);
        float yaw = b.size.x > b.size.z ? 90 : 0;
        Object.DestroyImmediate(go);
        go = Spawn(prefab, null, yaw, 1);
        var size = GetBounds(go).size;
        Object.DestroyImmediate(go);
        return new ProductType { prefab = prefab, size = realSize * Scale, yaw = yaw, bounds = size };
    }

    // Finds every upward-facing shelf surface in the aisle's meshes, then lines products up along
    // each one (except the top of the unit), front edges flush with the shelf front. Each product
    // is shrunk if needed to fit the gap above it and the shelf's depth.
    static void StockAisle(Transform aisle, ShelfInfo shelf, float aisleLength, (string model, float size)[] stock, int[] sides = null)
    {
        var upTris = UpwardTriangles(aisle.gameObject);
        var products = new GameObject("Products").transform;
        products.SetParent(aisle);

        var cache = new Dictionary<string, ProductType>();
        var types = new List<ProductType>();
        foreach (var (model, size) in stock)
        {
            if (!cache.TryGetValue(model, out var t))
            {
                var prefab = LoadModel(model);
                t = prefab != null ? MeasureProduct(prefab, size) : null;
                cache[model] = t;
            }
            if (t != null) types.Add(t);
        }
        if (types.Count == 0) return;

        float x0 = aisle.position.x;
        float gap = 0.012f * Scale;
        float edge = x0; // outer face of the gondola on the current side
        var rng = new System.Random(aisle.GetSiblingIndex() * 31 + 5);
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var railMat = GetMaterial("PriceRail", new Color(0.92f, 0.92f, 0.9f));
        var tagMat = GetMaterial("PriceTag", new Color(1f, 0.85f, 0.2f));

        foreach (int side in sides ?? new[] { 1, -1 })
        {
            edge = x0 + side * shelf.gondolaDepth / 2;
            // Probe well inside the boards (they don't reach the gondola's middle core).
            float xProbe = x0 + side * shelf.gondolaDepth * 0.35f;
            int sections = Mathf.RoundToInt(aisleLength / shelf.length);
            for (int section = 0; section < sections; section++)
            {
                float start = -aisleLength / 2 + section * shelf.length;
                float zc = start + shelf.length / 2;
                var levels = SurfacesAt(upTris, xProbe, zc);
                // The highest board has nothing above it out here; the unit's height caps it.
                if (levels.Count > 0 && levels[levels.Count - 1] < ShelfHeight * 0.95f) levels.Add(ShelfHeight);

                for (int i = 0; i < levels.Count - 1; i++)
                {
                    float y = levels[i];
                    float room = levels[i + 1] - y;
                    if (room < 0.05f * Scale) continue;

                    // Measure how far this board actually reaches: first and last spot from the
                    // outer edge inwards that has a surface at this height.
                    float front = float.NaN, back = float.NaN;
                    for (float f = 0.005f; f < 0.5f; f += 0.01f)
                    {
                        float x = edge - side * f * shelf.gondolaDepth;
                        bool has = HasSurface(upTris, x, zc, y);
                        if (has && float.IsNaN(front)) front = x;
                        if (has) back = x;
                        else if (!float.IsNaN(front)) break;
                    }
                    if (float.IsNaN(front)) continue;
                    float boardDepth = Mathf.Abs(back - front);
                    if (boardDepth < 0.05f * Scale) continue;

                    // Price rail along the board's front edge, with a tag now and then.
                    Box("Price Rail", products, new Vector3(front + side * 0.004f * Scale, y - 0.015f * Scale, zc),
                        new Vector3(0.008f * Scale, 0.035f * Scale, shelf.length * 0.98f), railMat);

                    StockBoard(products, types, rng, Rand, side, y, room, front, boardDepth, start, shelf.length, gap, upTris, tagMat);
                }
            }
        }
    }

    // Fills one board the way a real store looks: runs ("facings") of the same product side by
    // side, one to three rows deep, slightly crooked, with the odd gap where something sold out
    // and the occasional item knocked over.
    static void StockBoard(Transform parent, List<ProductType> types, System.Random rng, System.Func<float, float, float> rand,
        int side, float y, float room, float front, float boardDepth, float start, float length, float gap,
        List<(Vector3, Vector3, Vector3)> upTris, Material tagMat)
    {
        float z = start + gap * 2;
        float end = start + length - gap * 2;
        while (z < end)
        {
            var type = types[rng.Next(types.Count)];
            var bnd = type.bounds;
            float s = Mathf.Min(type.size / Mathf.Max(bnd.x, bnd.y, bnd.z), 0.85f * room / bnd.y, 0.95f * boardDepth / bnd.x);
            float w = bnd.z * s, d = bnd.x * s;
            int rows = Mathf.Clamp(Mathf.FloorToInt(boardDepth / (d + gap)), 1, 4);
            int facings = rng.Next(2, 7);

            if (z + w > end) break;

            // Price tag under the start of each run.
            var tag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tag.name = "Price Tag";
            tag.transform.SetParent(parent);
            tag.transform.position = new Vector3(front + side * 0.009f * Scale, y - 0.015f * Scale, z + w / 2);
            tag.transform.localScale = new Vector3(0.004f * Scale, 0.03f * Scale, Mathf.Min(0.05f * Scale, w));
            tag.GetComponent<Renderer>().sharedMaterial = tagMat;
            Object.DestroyImmediate(tag.GetComponent<Collider>());

            // Now and then a bright "shelf talker" card sticks out into the aisle.
            if (rng.NextDouble() < 0.14)
            {
                var talker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                talker.name = "Shelf Talker";
                talker.transform.SetParent(parent);
                talker.transform.position = new Vector3(front + side * 0.07f * Scale, y - 0.04f * Scale, z + w / 2);
                talker.transform.localScale = new Vector3(0.12f * Scale, 0.09f * Scale, 0.004f * Scale);
                talker.GetComponent<Renderer>().sharedMaterial = TalkerColors[rng.Next(TalkerColors.Length)];
                Object.DestroyImmediate(talker.GetComponent<Collider>());
            }

            for (int k = 0; k < facings && z + w <= end; k++)
            {
                float zc = z + w / 2;
                bool sold = rng.NextDouble() < 0.03;
                for (int r = 0; r < rows && !sold; r++)
                {
                    // Mostly full, with the odd item already taken.
                    if (r > 0 && rng.NextDouble() < 0.06) continue;
                    if (r == 0 && rng.NextDouble() < 0.02) continue;

                    float px = front - side * (d / 2 + r * (d + gap) + rand(0f, gap));
                    if (!HasSurface(upTris, px, zc - w * 0.4f, y) || !HasSurface(upTris, px, zc + w * 0.4f, y)) continue;

                    var p = Spawn(type.prefab, parent, type.yaw + (side > 0 ? 0 : 180), s * rand(0.93f, 1f));
                    bool tipped = rng.NextDouble() < 0.025 && bnd.y * s < w * 3;
                    p.transform.rotation = Quaternion.Euler(tipped ? 90 : 0, rand(-9f, 9f), 0) * p.transform.rotation;
                    PlaceOnFloor(p, new Vector3(px, y, zc));
                    AddBoxColliders(p);
                }
                z += w + gap * rand(0.2f, 0.8f);
            }
            z += gap * rand(0.5f, 1.5f);
        }
    }

    // Overstock cardboard boxes on top of every shelf section, plus a small stack on the floor at
    // the front end of each aisle. Kept under their own root (not the aisle) so StockAisle
    // doesn't mistake box lids for shelf surfaces.
    static void PlaceBoxes(Transform store, List<Transform> aisles, ShelfInfo shelf, float aisleLength)
    {
        var closed = LoadKenney("cardboardBoxClosed");
        var open = LoadKenney("cardboardBoxOpen");
        if (closed == null || open == null) return;

        var root = new GameObject("Boxes").transform;
        root.SetParent(store);
        float boxSize = 0.45f * Scale;
        var rng = new System.Random(1);

        foreach (var aisle in aisles)
        {
            var upTris = UpwardTriangles(aisle.gameObject);
            float x = aisle.position.x;
            int units = Mathf.RoundToInt(aisleLength / shelf.length);

            for (int u = 0; u < units; u++)
            {
                if (rng.NextDouble() < 0.35) continue; // leave some gaps
                float z = -aisleLength / 2 + (u + 0.5f) * shelf.length;
                var tops = SurfacesAt(upTris, x, z);
                float top = tops.Count > 0 ? tops[tops.Count - 1] : ShelfHeight;
                var box = SpawnSized(rng.NextDouble() < 0.7 ? closed : open, root, boxSize, rng.Next(-10, 10));
                PlaceOnFloor(box, new Vector3(x, top, z));
                AddBoxColliders(box);
            }
        }
    }

    // Piles of boxes with groceries spilling out, closing off one end of every aisle: south end of
    // Aisles 1, 3, 5 and north end of 2, 4. From the spawn the only way to the checkout is an S:
    // up walkway 0, round the north end of Aisle 1, down walkway 1, round the south end of Aisle 2...
    static void BuildBlockades(Transform store, List<Transform> aisles, ShelfInfo shelf, float aisleLength, float depth)
    {
        var closed = LoadKenney("cardboardBoxClosed");
        var open = LoadKenney("cardboardBoxOpen");
        if (closed == null || open == null) return;

        var root = new GameObject("Blockades").transform;
        root.SetParent(store);
        var rng = new System.Random(11);
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float box = 0.45f * Scale;

        for (int i = 0; i < aisles.Count; i++)
        {
            bool south = i % 2 == 0;
            float ax = aisles[i].position.x;
            float zNear = south ? -aisleLength / 2 : aisleLength / 2; // aisle end
            float zWall = (south ? -1 : 1) * (depth / 2 - CoolerDepth); // up to the coolers' glass
            float zMid = (zNear + zWall) / 2;
            float spanZ = Mathf.Abs(zWall - zNear);
            var pile = new GameObject($"Blockade {i + 1}").transform;
            pile.SetParent(root);

            // Invisible wall so the gap is truly closed, even if the player jumps onto the pile.
            var blocker = new GameObject("Blocker").AddComponent<BoxCollider>();
            blocker.transform.SetParent(pile);
            blocker.transform.position = new Vector3(ax, WallHeight / 2, zMid);
            blocker.size = new Vector3(shelf.gondolaDepth, WallHeight, spanZ);

            // Stacks of boxes on a grid, uneven heights, a bit askew.
            int nx = Mathf.CeilToInt(shelf.gondolaDepth / box), nz = Mathf.CeilToInt(spanZ / box);
            var openTops = new List<Bounds>();
            for (int gx = 0; gx < nx; gx++)
            for (int gz = 0; gz < nz; gz++)
            {
                float x = ax - shelf.gondolaDepth / 2 + (gx + 0.5f) * shelf.gondolaDepth / nx + Rand(-0.08f, 0.08f) * box;
                float z = zMid - spanZ / 2 + (gz + 0.5f) * spanZ / nz + Rand(-0.08f, 0.08f) * box;
                int height = rng.Next(1, 5);
                float y = 0;
                for (int h = 0; h < height; h++)
                {
                    bool isOpen = h == height - 1 && rng.NextDouble() < 0.5;
                    var b = SpawnSized(isOpen ? open : closed, pile, box * Rand(0.85f, 1.05f), Rand(-25f, 25f));
                    PlaceOnFloor(b, new Vector3(x + Rand(-0.06f, 0.06f) * box, y, z + Rand(-0.06f, 0.06f) * box));
                    AddBoxColliders(b);
                    var bb = GetBounds(b);
                    y = bb.max.y;
                    if (isOpen) openTops.Add(bb);
                }
                // A box that slid off the top and landed on its side next to the stack.
                if (rng.NextDouble() < 0.15)
                {
                    var fallen = SpawnSized(closed, pile, box * 0.9f, Rand(0f, 360f));
                    fallen.transform.Rotate(Rand(60f, 90f), 0, 0, Space.World);
                    PlaceOnFloor(fallen, new Vector3(x + Rand(-1f, 1f) * box, 0, z + Rand(-0.5f, 0.5f) * box));
                    AddBoxColliders(fallen);
                }
            }

            // Groceries from this aisle: poking out of the open boxes and scattered on the floor.
            var stock = Stock[Mathf.Min(i, Stock.Length - 1)];
            var types = stock.Select(s => (prefab: LoadModel(s.model), s.size)).Where(t => t.prefab != null)
                .Select(t => MeasureProduct(t.prefab, t.size)).ToList();
            if (types.Count == 0) continue;

            GameObject Item(Vector3 at, bool tumble)
            {
                var t = types[rng.Next(types.Count)];
                float s = t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z);
                var p = Spawn(t.prefab, pile, t.yaw + Rand(0f, 360f), s);
                if (tumble) p.transform.rotation = Quaternion.Euler(Rand(0f, 360f), Rand(0f, 360f), Rand(0f, 360f)) * p.transform.rotation;
                PlaceOnFloor(p, at);
                AddBoxColliders(p);
                return p;
            }

            foreach (var top in openTops)
                for (int k = rng.Next(2, 5); k > 0; k--)
                    Item(new Vector3(top.center.x + Rand(-0.3f, 0.3f) * top.size.x, top.max.y - top.size.y * Rand(0.2f, 0.5f),
                        top.center.z + Rand(-0.3f, 0.3f) * top.size.z), tumble: true);

            // Spill fans out from the pile into the walkways on both sides.
            for (int k = rng.Next(10, 18); k > 0; k--)
            {
                float sideX = rng.Next(2) == 0 ? -1 : 1;
                float x = ax + sideX * (shelf.gondolaDepth / 2 + Rand(0f, 0.9f) * Scale);
                float z = zMid + Rand(-0.45f, 0.45f) * spanZ;
                Item(new Vector3(x, 0, z), tumble: rng.NextDouble() < 0.7);
            }
        }
    }

    // Instance scaled so its longest side is `size`.
    static GameObject SpawnSized(GameObject prefab, Transform parent, float size, float yaw)
    {
        var go = Spawn(prefab, parent, yaw, 1);
        var b = GetBounds(go);
        go.transform.localScale *= size / Mathf.Max(b.size.x, b.size.y, b.size.z);
        return go;
    }

    static GameObject Spawn(GameObject prefab, Transform parent, float yaw, float scale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0) * prefab.transform.rotation;
        go.transform.localScale = prefab.transform.localScale * scale;
        return go;
    }

    // Moves the object so its bounds are centered on target (x, z) with the bottom at target.y.
    static void PlaceOnFloor(GameObject go, Vector3 target)
    {
        var b = GetBounds(go);
        go.transform.position += new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z);
    }

    static readonly Dictionary<Mesh, Vector3[]> vertexCache = new Dictionary<Mesh, Vector3[]>();

    // Tight world-space bounds from the actual mesh vertices. Renderer.bounds is a box around a
    // rotated box, which sits lower than the real geometry for anything tilted, so objects
    // "placed on the floor" with it end up hovering.
    static Bounds GetBounds(GameObject go)
    {
        bool any = false;
        var b = new Bounds();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = mf.sharedMesh;
            if (mesh == null || !mesh.isReadable) continue;
            if (!vertexCache.TryGetValue(mesh, out var verts)) vertexCache[mesh] = verts = mesh.vertices;
            var m = mf.transform.localToWorldMatrix;
            foreach (var v in verts)
            {
                var p = m.MultiplyPoint3x4(v);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }
        if (any) return b;

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[GroceryStoreBuilder] {go.name} has no mesh to measure; using its position.", go);
            return new Bounds(go.transform.position, Vector3.one * 0.01f);
        }
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // A model is only usable if it actually imported with geometry. A broken or placeholder file
    // (e.g. a Git LFS pointer that was never downloaded) imports as an empty object.
    static GameObject Usable(GameObject prefab, string path)
    {
        if (prefab == null) return null;
        if (prefab.GetComponentsInChildren<MeshFilter>(true).Any(mf => mf.sharedMesh != null)) return prefab;
        Debug.LogWarning($"[GroceryStoreBuilder] Skipping {path}: it has no mesh. If it's a ~130 byte text file, run 'git lfs pull'.");
        return null;
    }

    // Every model folder gets Read/Write enabled so GetBounds and the shelf scan can see vertices.
    static void MakeModelsReadable()
    {
        foreach (var folder in new[] { ModelFolder, KenneyFolder, FoodKitFolder })
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
        vertexCache.Clear();
    }

    static void AddMeshColliders(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.GetComponent<Collider>() == null)
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
    }

    // A BoxCollider added next to a MeshFilter sizes itself to that mesh. Some Kenney models are
    // mirrored (negative scale), which BoxCollider can't handle, so those get a convex MeshCollider.
    static void AddBoxColliders(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.GetComponent<Collider>() != null || mf.sharedMesh == null) continue;
            var s = mf.transform.lossyScale;
            if (s.x < 0 || s.y < 0 || s.z < 0)
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
            }
            else mf.gameObject.AddComponent<BoxCollider>();
        }
    }

    static GameObject LoadModel(string name)
    {
        if (name == "BeanCan") return BeanCanBuilder.GetOrCreate();
        string path = name.StartsWith("k:") ? $"{FoodKitFolder}/{name.Substring(2)}.fbx" : $"{ModelFolder}/{name}.fbx";
        return Usable(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
    }

    static GameObject LoadKenney(string name)
    {
        string path = $"{KenneyFolder}/{name}.fbx";
        return Usable(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
    }

    static void Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
    }

    static Material GetMaterial(string name, Color color)
    {
        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder(MatFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
