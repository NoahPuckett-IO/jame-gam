using UnityEditor;
using UnityEngine;

// the finishing touches round the store made from the Kenney kits (CC0): traffic cones by the spills and the fallen
// sign, the customer service desk and the staff door by the entrance, and speakers up on the walls
static partial class GroceryStoreBuilder
{
    const string FactoryFolder = "Assets/ThirdParty/KenneyConveyor";

    static GameObject KitModel(string path) => Usable(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);

    // a Kenney model scaled to a real-world height in metres, turned to yaw, with box colliders, standing at floorAt
    static GameObject KitProp(string path, Transform parent, float realHeight, Vector3 floorAt, float yaw, bool collide = true)
    {
        var prefab = KitModel(path);
        if (prefab == null) return null;
        var go = Spawn(prefab, parent, yaw, 1);
        go.transform.localScale *= realHeight * Scale / GetBounds(go).size.y;
        PlaceOnFloor(go, floorAt);
        if (collide) AddBoxColliders(go);
        return go;
    }

    // an orange safety cone, about knee height on a person
    static GameObject TrafficCone(Transform parent, Vector3 floorAt, float yaw)
    {
        var cone = KitProp($"{FactoryFolder}/cone.fbx", parent, 0.34f, floorAt, yaw);
        if (cone != null) cone.name = "Traffic Cone";
        return cone;
    }

    static void DressEntrance(Transform store, float left, float depth)
    {
        var root = new GameObject("Entrance Extras").transform;
        root.SetParent(store);

        // customer service: a desk in the corner south of the doors facing into the store, its computer, a chair and a
        // filing cupboard behind it against the wall, and a sign hung over it
        var deskAt = new Vector3(left + 0.2f * CheckoutWidth, 0, -depth / 2 + 1.25f * Scale);
        var desk = KitProp($"{KenneyFolder}/desk.fbx", root, 0.78f, deskAt, 0f);
        if (desk != null)
        {
            desk.name = "Service Desk";
            var top = GetBounds(desk);
            KitProp($"{KenneyFolder}/computerScreen.fbx", root, 0.42f, new Vector3(top.center.x - top.size.x * 0.2f, top.max.y, top.center.z + top.size.z * 0.1f), 0f);
            KitProp($"{KenneyFolder}/computerKeyboard.fbx", root, 0.03f, new Vector3(top.center.x - top.size.x * 0.2f, top.max.y, top.center.z - top.size.z * 0.15f), 0f, collide: false);
            KitProp($"{KenneyFolder}/chairDesk.fbx", root, 1f, new Vector3(top.center.x - top.size.x * 0.15f, 0, top.min.z - 0.35f * Scale), 180f);
            KitProp($"{KenneyFolder}/bookcaseClosedDoors.fbx", root, 1.5f, new Vector3(top.center.x + top.size.x * 0.33f, 0, -depth / 2 + 0.3f * Scale), 180f);
            BacklitSign(root, new Vector3(top.center.x, WallHeight - 1.6f * Scale, top.center.z), Vector3.back, 3.2f * Scale, 0.55f * Scale,
                "CUSTOMER SERVICE", new Color(0.12f, 0.45f, 0.6f), twoSided: true, iconLeft: "hand", iconRight: "store");
            var wire = GetMaterial("Wire", new Color(0.2f, 0.2f, 0.2f));
            float signTop = WallHeight - 1.6f * Scale + 0.3f * Scale;
            foreach (int s in new[] { -1, 1 })
                Box("Wire", root, new Vector3(top.center.x + s * 1.2f * Scale, (signTop + WallHeight) / 2, top.center.z),
                    new Vector3(0.01f * Scale, WallHeight - signTop, 0.01f * Scale), wire);
        }

        // the staff door in the back wall behind the checkouts, with its plaque
        var doorAt = new Vector3(left + 0.5f * CheckoutWidth, 0, depth / 2 - 0.05f * Scale);
        var door = KitProp($"{KenneyFolder}/doorwayFront.fbx", root, 2.2f, doorAt, 180f);
        if (door != null)
        {
            door.name = "Staff Door";
            var b = GetBounds(door);
            door.transform.position += Vector3.forward * (depth / 2 - b.max.z);
            b = GetBounds(door);
            var plaque = GetMaterial("Staff Plaque", new Color(0.12f, 0.13f, 0.15f));
            Box("Staff Plaque", root, new Vector3(b.center.x, b.max.y + 0.22f * Scale, depth / 2 - 0.01f * Scale), new Vector3(1.1f * Scale, 0.26f * Scale, 0.02f * Scale), plaque);
            MakeText(root, new Vector3(b.center.x, b.max.y + 0.22f * Scale, depth / 2 - 0.025f * Scale), 0f, "EMPLOYEES ONLY", 0.95f * Scale, 0.17f * Scale, new Color(0.95f, 0.95f, 0.92f), lit: true);
        }

        // music speakers high on the walls, each turned to face into the store and set back against its wall
        float high = WallHeight - 0.9f * Scale;
        foreach (var (wall, inward) in new[]
        {
            (new Vector3(left, high, 3.6f * Scale), Vector3.right),
            (new Vector3(left, high, -3.6f * Scale), Vector3.right),
            (new Vector3(left + 0.45f * CheckoutWidth, high, depth / 2), Vector3.back),
            (new Vector3(left + 0.45f * CheckoutWidth, high, -depth / 2), Vector3.forward),
        })
        {
            // the Kenney furniture faces -z, so it's turned to put that side towards the room
            var sp = KitProp($"{KenneyFolder}/speakerSmall.fbx", root, 0.45f, wall, Mathf.Atan2(-inward.x, -inward.z) * Mathf.Rad2Deg, collide: false);
            if (sp == null) continue;
            sp.name = "Wall Speaker";
            var b = GetBounds(sp);
            float back = Vector3.Dot(b.center, inward) - Vector3.Dot(b.extents, new Vector3(Mathf.Abs(inward.x), 0, Mathf.Abs(inward.z)));
            sp.transform.position += inward * (Vector3.Dot(wall, inward) - back);
        }
    }
}
