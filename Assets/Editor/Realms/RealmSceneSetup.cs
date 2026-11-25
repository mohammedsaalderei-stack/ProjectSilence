using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public static class RealmSceneSetup
{
    const string Scenes="Assets/Scenes/";
    const string Folder="Assets/Realms";
    static string source;
    static void Delete(GameObject g){if(g!=null)UnityEngine.Object.DestroyImmediate(g);}
    static void Save(){EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
    static GameObject Marker(string name,Vector3 p){var g=new GameObject(name);g.transform.position=p;return g;}
    static void Portal(string name,Vector3 p,Vector3 size,string scene,string marker)
    {
        var go=Marker(name,p);var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=size;
        var travel=go.AddComponent<RealmSceneExit>();travel.destinationScene=scene;travel.arrivalMarker=marker;
    }
    static void Player(Vector3 p)
    {
        var player=GameObject.Find("Musical Mage Player");var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p;cc.enabled=true;player.SetActive(true);
        if(player.GetComponent<RealmPlayerSpawn>()==null)player.AddComponent<RealmPlayerSpawn>();
        var follow=Camera.main.GetComponent<MusicalMageFollowCamera>();follow.target=player.transform;follow.enabled=true;follow.SnapToTarget();
    }
    static void Lighting(bool night)
    {
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(l.type==LightType.Directional||l.type==LightType.Spot)Delete(l.gameObject);
        foreach(var v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))v.enabled=false;
        var root=new GameObject(night?"Forest Night Lighting":"Kingdom Day Lighting");
        var key=new GameObject(night?"Moon - Readable Forest":"Sun - Kingdom Day");key.transform.SetParent(root.transform);key.transform.rotation=Quaternion.Euler(night?48:46,-35,0);
        var sun=key.AddComponent<Light>();sun.type=LightType.Directional;sun.lightUnit=LightUnit.Lux;sun.intensity=night?1.15f:3.1f;sun.color=night?new Color(.66f,.78f,1):new Color(1,.95f,.86f);sun.shadows=LightShadows.Soft;sun.shadowStrength=night?.4f:.58f;sun.shadowBias=.025f;sun.shadowNormalBias=.25f;key.AddComponent<UniversalAdditionalLightData>().renderingLayers=uint.MaxValue;RenderSettings.sun=sun;
        var bounce=new GameObject(night?"Night Sky - Soft Visibility Fill":"Day Sky - Soft Fill");bounce.transform.SetParent(root.transform);bounce.transform.rotation=Quaternion.Euler(65,145,0);
        var fill=bounce.AddComponent<Light>();fill.type=LightType.Directional;fill.lightUnit=LightUnit.Lux;fill.intensity=night?.34f:.55f;fill.color=night?new Color(.58f,.72f,1):new Color(.8f,.88f,1);bounce.AddComponent<UniversalAdditionalLightData>().renderingLayers=uint.MaxValue;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=night?new Color(.30f,.38f,.48f):new Color(.45f,.51f,.59f);RenderSettings.ambientEquatorColor=night?new Color(.24f,.29f,.35f):new Color(.37f,.35f,.30f);RenderSettings.ambientGroundColor=night?new Color(.14f,.18f,.24f):new Color(.23f,.21f,.17f);RenderSettings.fog=false;RenderSettings.skybox=null;
        string path=Folder+(night?"/ForestNightProfile.asset":"/KingdomDayProfile.asset");
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
        ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(night?.2f:-.2f);color.contrast.Override(night?2:0);color.saturation.Override(night?-4:2);
        Bloom bloom;if(!profile.TryGet(out bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(night?.4f:.15f);bloom.threshold.Override(night?.85f:1.2f);bloom.scatter.Override(.55f);
        var volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=profile;
        foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=night?new Color(.025f,.045f,.08f):new Color(.54f,.62f,.70f);c.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
        foreach(var c in profile.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(profile);
    }
    static void LoadCopy(string path)
    {
        EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),path,true);
        EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
    }
    [MenuItem("Tools/Realms/Split Menu Kingdom And Forest")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        source=SceneManager.GetActiveScene().path;
        if(source!="Assets/Scenes/SampleScene.unity")throw new InvalidOperationException("Start from the combined SampleScene.");
        if(GameObject.Find("FORREST")==null||GameObject.Find("Royal Kingdom")==null)throw new InvalidOperationException("Realm groups were not found.");
        foreach(var n in new[]{"Menu","Kingdom","Forest"})if(File.Exists(Scenes+n+".unity"))throw new InvalidOperationException(n+" scene already exists; preserve it.");
        Save();Directory.CreateDirectory(Folder);
        File.Copy(source,"C:/Users/hp/Documents/Codex/2026-11-24/can/work/BeforeRealmSplit_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
        LoadCopy(Scenes+"Kingdom.unity");
        var kingdom=GameObject.Find("Royal Kingdom");kingdom.transform.SetParent(null,true);Delete(GameObject.Find("REALMS"));Delete(GameObject.Find("Forest Return - Original Player Position"));
        foreach(var m in UnityEngine.Object.FindObjectsByType<SimpleStartMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))Delete(m.gameObject);
        Lighting(false);var spawn=GameObject.Find("Player Spawn - Kingdom Plaza");Player(spawn.transform.position);
        Marker("Arrival From Forest",new Vector3(125.4f,.1f,0));Portal("East Gate - Travel To Forest",new Vector3(128.1f,1,0),new Vector3(1.1f,3,3.5f),"Forest","Arrival From Kingdom");Save();
        LoadCopy(Scenes+"Forest.unity");
        var forest=GameObject.Find("FORREST");forest.transform.SetParent(null,true);Delete(GameObject.Find("REALMS"));
        foreach(var m in UnityEngine.Object.FindObjectsByType<SimpleStartMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))Delete(m.gameObject);
        Lighting(true);Player(new Vector3(0,.38f,-10.5f));
        Marker("Arrival From Kingdom",new Vector3(0,.36f,-14.2f));Portal("South Grove - Return To Kingdom",new Vector3(0,1.1f,-16.5f),new Vector3(4,3,.7f),"Kingdom","Arrival From Forest");
        var overview=new GameObject("Forest Overview Camera");overview.transform.position=new Vector3(0,30,-28);overview.transform.LookAt(new Vector3(0,0,1));var oc=overview.AddComponent<Camera>();oc.orthographic=true;oc.orthographicSize=13;oc.farClipPlane=150;oc.enabled=false;oc.backgroundColor=new Color(.025f,.045f,.08f);oc.clearFlags=CameraClearFlags.SolidColor;overview.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;Save();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Scenes+"Menu.unity");
        var camera=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";var mc=camera.GetComponent<Camera>();mc.clearFlags=CameraClearFlags.SolidColor;mc.backgroundColor=new Color(.055f,.065f,.085f);mc.cullingMask=0;
        SimpleStartMenuSetup.Build();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scenes+"Menu.unity",true),new EditorBuildSettingsScene(Scenes+"Kingdom.unity",true),new EditorBuildSettingsScene(Scenes+"Forest.unity",true)};Save();
        Debug.Log("REALM_SCENES_READY: Menu -> Kingdom (day); eastern gate -> Forest (visible night); south grove -> Kingdom.");
    }
}
