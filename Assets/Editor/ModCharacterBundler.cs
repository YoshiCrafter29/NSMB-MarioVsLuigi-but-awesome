using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ModCharacterBundler : Editor
{
    [MenuItem("Modding/Build Character")]
    static void BuildCharacter()
    {
        string charPath = EditorUtility.OpenFolderPanel("Select your character's folder", Application.dataPath, "mario");
        if (string.IsNullOrEmpty(charPath)) return;
        string savePath = EditorUtility.SaveFilePanel("Save your AssetsBundle....", charPath, Path.GetFileName(charPath), "");
        if (string.IsNullOrEmpty(savePath)) return;
        Debug.Log(charPath);
        Debug.Log(savePath);
        BuildPipeline.BuildAssetBundles(charPath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }
}