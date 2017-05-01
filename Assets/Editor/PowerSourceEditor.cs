using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PowerSource))]
public class PowerSourceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PowerSource source = (PowerSource)target;
        string buttonText = (source.IsOn ? "Turn Off" : "Turn On");

        if (GUILayout.Button(buttonText))
        {
            source.IsOn = !source.IsOn;
        }
    }
}
