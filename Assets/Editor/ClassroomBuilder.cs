using UnityEditor;
using UnityEngine;

public static class ClassroomBuilder
{
    [MenuItem("Tools/Build Classroom Furniture")]
    static void Build()
    {
        GameObject props = GameObject.Find("props");
        if (props == null) { Debug.LogError("No 'props' object found"); return; }

        for (int i = props.transform.childCount - 1; i >= 0; i--)
        {
            Transform c = props.transform.GetChild(i);
            if (c.name.StartsWith("Student_Desk") || c.name.StartsWith("Student_Chair"))
                Undo.DestroyObjectImmediate(c.gameObject);
        }

        float[] xs = { -5.5f, -2f, 2f, 5.5f };
        float[] zs = { 2f, -0.5f, -3f };
        int n = 1;
        foreach (float z in zs)
            foreach (float x in xs)
            {
                Make("Student_Desk_" + n, new Vector3(x, 0.4f, z), new Vector3(1.6f, 0.8f, 0.8f), props.transform);
                Make("Student_Chair_" + n, new Vector3(x, 0.25f, z - 0.9f), new Vector3(0.6f, 0.5f, 0.6f), props.transform);
                n++;
            }
        Debug.Log("Classroom furniture built.");
    }

    static void Make(string name, Vector3 pos, Vector3 scale, Transform parent)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(g, "Create " + name);
    }
}