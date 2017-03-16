using UnityEngine;
using UnityEditor;
using System.Collections;

class LevelToolsWindow : EditorWindow
{
    public Transform srcObj;
    public Transform dstObj;
    public WorldOffsets offsets;

    [MenuItem("Window/Boats 'N' Oats Level Tools")]
    public static void ShowWindow()
    {
        GetWindow(typeof(LevelToolsWindow));
    }

    void OnGUI()
    {
        titleContent.text = "Level Tools";

        GUILayout.Label("World Cloner");
        srcObj = (Transform)EditorGUILayout.ObjectField("Source", srcObj, typeof(Transform), true);
        dstObj = (Transform)EditorGUILayout.ObjectField("Destination", dstObj, typeof(Transform), true);
        offsets = (WorldOffsets)EditorGUILayout.ObjectField("World Offsets", offsets, typeof(WorldOffsets), true);
        bool copyObject = GUILayout.Button(new GUIContent("Copy Object", "Copy a single Source object into Destination, replacing Desert prefabs with Wet equivalents where possible."));
        bool copyChildren = GUILayout.Button(new GUIContent("Copy Children", "Copy children of Source to Destination, replacing Desert prefabs with Wet equivalents where possible."));

        if (copyObject)
        {
            CopyObject(srcObj);
        }

        if (copyChildren)
        {
            CopyChildren();
        }
    }

    void CopyObject(Transform obj)
    {
        // Get prefab name
        var prefab = PrefabUtility.GetPrefabParent(obj);
        var prefabName = AssetDatabase.GetAssetPath(prefab);

        // Check if there's a wet version
        var wetName = prefabName.Replace("Desert", "Wet");
        var wetPrefab = AssetDatabase.LoadAssetAtPath<Object>(wetName);

        if (wetPrefab != null)
        {
            Instantiate(wetPrefab, obj.position + offsets.offset, obj.rotation, dstObj);
        }
        else
        {
            Instantiate(obj, obj.position + offsets.offset, obj.rotation, dstObj);
        }
    }

    void CopyChildren()
    {
        for (int i = 0; i < srcObj.childCount; ++i)
        {
            var child = srcObj.GetChild(i);
            CopyObject(child);
        }
    }
}