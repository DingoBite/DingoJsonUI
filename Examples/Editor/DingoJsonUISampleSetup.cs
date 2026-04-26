using System.Collections.Generic;
using System.Reflection;
using UImGui.Renderer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DingoJsonUISampleSetup
{
    private const string MenuPath = "DingoJsonUI/Sample/Ensure Scene Setup";
    private const string RenderFeatureName = "DingoJsonUI Render ImGui";

    [MenuItem(MenuPath)]
    public static void EnsureSceneSetup()
    {
        var renderFeature = EnsureRenderFeature();
        if (renderFeature == null)
            return;

        var uImGuis = Object.FindObjectsByType<UImGui.UImGui>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var uImGui in uImGuis)
        {
            var camera = uImGui.GetComponent<Camera>() ?? Camera.main;
            var serializedObject = new SerializedObject(uImGui);
            serializedObject.FindProperty("_camera").objectReferenceValue = camera;
            serializedObject.FindProperty("_renderFeature").objectReferenceValue = renderFeature;
            serializedObject.FindProperty("_shaders").objectReferenceValue = LoadPackageAsset("Resources/DefaultShader.asset");
            serializedObject.FindProperty("_style").objectReferenceValue = LoadPackageAsset("Resources/DefaultStyle.asset");
            serializedObject.FindProperty("_cursorShapes").objectReferenceValue = LoadPackageAsset("Resources/DefaultCursorShape.asset");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uImGui);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"DingoJsonUI sample scene setup complete. Render feature: {renderFeature.name}");
    }

    private static RenderImGui EnsureRenderFeature()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline)
        {
            Debug.LogWarning("DingoJsonUI sample uses UImGui. The current render pipeline is not URP, so no RenderImGui feature was added.");
            return null;
        }

        var rendererData = GetDefaultRendererData(pipeline);
        if (rendererData == null)
        {
            Debug.LogWarning("DingoJsonUI sample could not find a URP renderer data asset.");
            return null;
        }

        var features = GetRendererFeatures(rendererData);
        if (features == null)
        {
            Debug.LogWarning("DingoJsonUI sample could not access URP renderer features.");
            return null;
        }

        for (var i = 0; i < features.Count; i++)
        {
            if (features[i] is RenderImGui existing)
                return existing;
        }

        var feature = ScriptableObject.CreateInstance<RenderImGui>();
        feature.name = RenderFeatureName;
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        features.Add(feature);

        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssetIfDirty(rendererData);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(rendererData));
        return feature;
    }

    private static ScriptableRendererData GetDefaultRendererData(UniversalRenderPipelineAsset pipeline)
    {
        var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(pipeline) is not ScriptableRendererData[] rendererDataList)
            return null;

        for (var i = 0; i < rendererDataList.Length; i++)
        {
            if (rendererDataList[i] != null)
                return rendererDataList[i];
        }

        return null;
    }

    private static List<ScriptableRendererFeature> GetRendererFeatures(ScriptableRendererData rendererData)
    {
        var field = typeof(ScriptableRendererData).GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(rendererData) as List<ScriptableRendererFeature>;
    }

    private static Object LoadPackageAsset(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Object>($"Packages/com.psydack.uimgui/{relativePath}");
    }
}
