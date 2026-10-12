using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClassroomUpgrade
{
    // ---- Tweak these if something looks wrong ----
    const string PackFolder = "Assets/Cute_Magic_Stylized_LowPoly_Free_01";
    const float ChairScale = 1.2f;
    const float ChairYaw   = 0f;     // use 180 if chairs face backwards
    const float BookScale  = 1.5f;
    const float BoardScale = 1.5f;
    const float BoardYaw   = 180f;   // use 0 if the board faces the wall

    [MenuItem("Tools/Upgrade Classroom Props")]
    static void Upgrade()
    {
        GameObject props = GameObject.Find("props");
        if (props == null) { Debug.LogError("No 'props' object found."); return; }

        List<GameObject> chairs = FindPrefabs("chair", null);
        List<GameObject> books  = FindPrefabs("book_", "1c");
        List<GameObject> boards = FindPrefabs("board_", null);

        if (chairs.Count == 0) { Debug.LogError("No chair prefab found in the pack."); return; }
        if (books.Count == 0)  { Debug.LogError("No book prefab found in the pack."); return; }
        if (boards.Count == 0) { Debug.LogError("No board prefab found in the pack."); return; }

        // Remove old chairs, books and standing board so the script can be re-run
        for (int i = props.transform.childCount - 1; i >= 0; i--)
        {
            Transform c = props.transform.GetChild(i);
            if (c.name.StartsWith("Student_Chair") || c.name.StartsWith("Book_On_Desk") || c.name == "Standing_Board")
                Undo.DestroyObjectImmediate(c.gameObject);
        }

        for (int n = 1; n <= 12; n++)
        {
            Transform desk = props.transform.Find("Student_Desk_" + n);
            if (desk == null) continue;
            Vector3 dp = desk.localPosition;
            float deskTop = dp.y + desk.localScale.y / 2f;

            // Chair behind the desk
            GameObject chair = Spawn(chairs[0], props.transform, "Student_Chair_" + n,
                new Vector3(dp.x, 0f, dp.z - 0.9f), ChairYaw, ChairScale);

            // Book on the desk, slightly rotated
            GameObject bookPrefab = books[(n - 1) % books.Count];
            float side = (n % 2 == 0) ? 0.4f : -0.4f;
            float yaw = ((n * 37) % 40) - 20;
            Spawn(bookPrefab, props.transform, "Book_On_Desk_" + n,
                new Vector3(dp.x + side, deskTop, dp.z), yaw, BookScale);
        }

        // Standing chalkboard to the left of the main board (left = negative X)
        GameObject boardPrefab = boards[0];
        foreach (GameObject b in boards)
            if (b.name.ToLower().Contains("2c")) { boardPrefab = b; break; }
        Spawn(boardPrefab, props.transform, "Standing_Board",
            new Vector3(-6f, 0f, 5.2f), BoardYaw, BoardScale);

        EditorSceneManager.MarkSceneDirty(props.scene);
        Debug.Log("Classroom upgraded. Chair: " + chairs[0].name + " | Board: " + boardPrefab.name);
    }

    static GameObject Spawn(GameObject prefab, Transform parent, string name, Vector3 pos, float yaw, float scale)
    {
        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        g.name = name;
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        g.transform.localScale = Vector3.one * scale;
        Undo.RegisterCreatedObjectUndo(g, "Create " + name);
        return g;
    }

    static List<GameObject> FindPrefabs(string contains, string exclude)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PackFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string n = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            if (!n.Contains(contains)) continue;
            if (exclude != null && n.Contains(exclude)) continue;
            list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
        return list;
    }
}