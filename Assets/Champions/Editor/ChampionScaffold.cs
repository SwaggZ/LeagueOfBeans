// Builds a new champion from Jhin's bean rig and registers it everywhere the game needs it.
// Safe to re-run: everything updates in place.
//  - <folder>/<Id>.prefab          gameplay prefab (template's ability script swapped for the champion's)
//  - <folder>/<Id>Preview.prefab   champ-select preview (static: its camera stays off)
//  - <folder>/<id>Mat.mat          body material (put the bean texture here)
//  - SampleScene: CharacterSelection entry, preview instance and a champ-select button
//  - Menu + selection scenes: NetworkGameSession spawn list entry (the server spawns by id from it)
// Per-champion menus (Tools/<Champion>/Build Champion) live next to each champion.
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ChampionScaffold
{
    public class Config
    {
        public string Id;                       // e.g. "Swain" (also the prefab name and spawn id)
        public string Folder;                   // e.g. "Assets/Champions/swain"
        public Type Controller;                 // ability script; needs a public GameObject 'cam' field
        public Color PlaceholderColor = Color.gray;
        public Func<CharacterSelection, UnityAction> SelectMethod; // e.g. cs => cs.Swain
        public string LmbName, LmbDesc, RmbName, RmbDesc, OneName, OneDesc, TwoName, TwoDesc;
    }

    const string TemplatePrefab = "Assets/Champions/jhin/Jhin.prefab";
    const string TemplatePreview = "Assets/Champions/jhin/JhinPreview.prefab";
    const string TemplateMat = "Assets/Champions/jhin/jhinMat.mat";
    const string TemplateId = "Jhin";
    const string SelectionScene = "Assets/Scenes/SampleScene.unity";
    static readonly string[] SessionScenes = { "Assets/Scenes/Menu.unity", SelectionScene, "Assets/Scenes/selection.unity" };

    public static string Build(Config c)
    {
        string gameplayPath = $"{c.Folder}/{c.Id}.prefab";
        string previewPath = $"{c.Folder}/{c.Id}Preview.prefab";
        string matPath = $"{c.Folder}/{char.ToLowerInvariant(c.Id[0])}{c.Id.Substring(1)}Mat.mat";

        var mat = EnsureMaterial(matPath, c.PlaceholderColor);
        var gameplay = BuildGameplay(c, gameplayPath, mat);
        var preview = BuildPreview(c, previewPath, mat, gameplay);
        string sel = RegisterInSelection(c, gameplay, preview);
        string net = string.Join(", ", SessionScenes.Select(s => RegisterNetworkPrefab(s, c.Id, gameplay)));
        return $"{c.Id}: {gameplayPath}, {previewPath} | {sel} | spawn list {net}";
    }

    static Material EnsureMaterial(string path, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        AssetDatabase.CopyAsset(TemplateMat, path);
        mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
        mat.color = color;
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static GameObject InstantiateUnpacked(string path)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        return go;
    }

    // The template's direct "Cube" children are Jhin's cape/hat.
    static void RemoveTemplateAccessories(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            if (go.transform.GetChild(i).name == "Cube") Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    static GameObject BuildGameplay(Config c, string path, Material mat)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var go = InstantiateUnpacked(exists ? path : TemplatePrefab);
        go.name = c.Id;

        var template = go.GetComponent<JhinController>();
        if (template != null) Object.DestroyImmediate(template);
        var ctrl = go.GetComponent(c.Controller);
        if (ctrl == null) ctrl = go.AddComponent(c.Controller);
        RemoveTemplateAccessories(go);

        var camField = c.Controller.GetField("cam");
        var cam = go.GetComponentInChildren<Camera>(true);
        if (camField != null && cam != null) camField.SetValue(ctrl, cam.gameObject);

        var body = go.GetComponent<Renderer>();
        if (body != null) body.sharedMaterial = mat;

        // HUD loadout: drop the template's icons (slots show empty until the champion's art is assigned).
        var hud = go.GetComponent<CooldownUIManager>();
        if (hud != null)
        {
            var so = new SerializedObject(hud);
            var list = so.FindProperty("defaultAbilities");
            for (int i = 0; i < list.arraySize; i++)
            {
                var icon = list.GetArrayElementAtIndex(i).FindPropertyRelative("icon");
                if (icon.objectReferenceValue != null && AssetDatabase.GetAssetPath(icon.objectReferenceValue).Contains("/jhin/"))
                    icon.objectReferenceValue = null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var npc = go.GetComponent<NetworkPlayerController>();
        if (npc != null) npc.abilityController = (MonoBehaviour)ctrl;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildPreview(Config c, string path, Material mat, GameObject gameplay)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var go = InstantiateUnpacked(exists ? path : TemplatePreview);
        go.name = c.Id + "Preview";
        RemoveTemplateAccessories(go);
        var cam = go.transform.Find("Main Camera");
        if (cam != null) cam.gameObject.SetActive(false);
        var body = go.GetComponent<Renderer>();
        if (body != null) body.sharedMaterial = mat;
        var binding = go.GetComponent<PreviewBinding>();
        if (binding != null) binding.gameplayPrefab = gameplay;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static string RegisterNetworkPrefab(string scenePath, string id, GameObject gameplay)
    {
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        int added = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var session in root.GetComponentsInChildren<NetworkGameSession>(true))
            {
                if (session.characterPrefabs.Any(e => e != null && e.id == id)) continue;
                session.characterPrefabs.Add(new NetworkGameSession.CharacterPrefabEntry { id = id, prefab = gameplay });
                EditorUtility.SetDirty(session);
                added++;
            }
        if (added > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        return $"{System.IO.Path.GetFileNameWithoutExtension(scenePath)}+{added}";
    }

    static string RegisterInSelection(Config c, GameObject gameplay, GameObject previewPrefab)
    {
        var scene = EditorSceneManager.GetSceneByPath(SelectionScene);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(SelectionScene, OpenSceneMode.Additive);

        CharacterSelection cs = null;
        foreach (var root in scene.GetRootGameObjects())
            if ((cs = root.GetComponentInChildren<CharacterSelection>(true)) != null) break;
        if (cs == null) { if (openedHere) EditorSceneManager.CloseScene(scene, true); return "CharacterSelection not found"; }

        var templateEntry = cs.characters.FirstOrDefault(e => e != null && e.id == TemplateId);
        var entry = cs.characters.FirstOrDefault(e => e != null && e.id == c.Id);
        if (entry == null) { entry = new CharacterSelection.CharacterEntry(); cs.characters.Add(entry); }

        entry.id = c.Id;
        entry.gameplayPrefab = gameplay;
        entry.lmbName = c.LmbName; entry.lmbDesc = c.LmbDesc;
        entry.rmbName = c.RmbName; entry.rmbDesc = c.RmbDesc;
        entry.oneName = c.OneName; entry.oneDesc = c.OneDesc;
        entry.twoName = c.TwoName; entry.twoDesc = c.TwoDesc;

        GameObject templatePreview = templateEntry != null ? templateEntry.preview : null;
        if (entry.preview == null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(previewPrefab, scene);
            inst.transform.SetParent(templatePreview != null ? templatePreview.transform.parent : cs.transform, false);
            if (templatePreview != null)
            {
                inst.transform.localPosition = templatePreview.transform.localPosition;
                inst.transform.localRotation = templatePreview.transform.localRotation;
                inst.transform.localScale = templatePreview.transform.localScale;
            }
            inst.name = c.Id + "Preview";
            inst.SetActive(false);
            entry.preview = inst;
        }
        // Same layer the preview camera renders.
        if (templatePreview != null)
            foreach (var t in entry.preview.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = templatePreview.layer;

        Button templateButton = null, button = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                if (b.name == TemplateId + "Button") templateButton = b;
                if (b.name == c.Id + "Button") button = b;
            }
        if (button == null && templateButton != null)
        {
            var clone = Object.Instantiate(templateButton.gameObject, templateButton.transform.parent);
            clone.name = c.Id + "Button";
            clone.transform.SetAsLastSibling();
            button = clone.GetComponent<Button>();
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddVoidPersistentListener(button.onClick, c.SelectMethod(cs));
            var label = clone.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null) label.text = c.Id;
        }

        EditorUtility.SetDirty(cs);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string summary = $"selection entry ok, button={(button != null)}, entries={cs.characters.Count}";
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        return summary;
    }
}
#endif
