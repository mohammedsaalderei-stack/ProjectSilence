using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public static class ForestRealmFinishing
{
    const string Dir="Assets/ForestRealm/Finishing";
    static Transform root;
    static Material forest,earth,stone;
    static System.Random rng;
    static float R(float a,float b){return a+(float)rng.NextDouble()*(b-a);}
    static Material Material(string name,Color color)
    {
        string path=Dir+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);return m;
    }
    static string Asset(string name)
    {
        return AssetDatabase.FindAssets(name+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)==name);
    }
    static GameObject Prop(string asset,Vector3 pos,float height,Material material=null)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Asset(asset)));
        go.transform.SetParent(root,false);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.Euler(0,R(0,360),0);
        var rr=go.GetComponentsInChildren<Renderer>();Bounds b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
        go.transform.localScale*=height/Mathf.Max(.05f,b.size.y);
        b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
        go.transform.position+=pos-new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var r in rr){if(material!=null)r.sharedMaterials=r.sharedMaterials.Select(m=>material).ToArray();r.gameObject.isStatic=true;}
        return go;
    }
    static BoxCollider Wall(string name,Vector3 pos,Vector3 size)
    {var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=pos;var c=go.AddComponent<BoxCollider>();c.size=size;return c;}
    [MenuItem("Tools/Forest Realm/Finish Twilight Surroundings")]
    public static void Build()
    {
        if(GameObject.Find("Forest Realm - Twilight Extension")!=null)throw new System.InvalidOperationException("Twilight extension already exists.");
        if(GameObject.Find("Forest Realm - Imported Map")==null)throw new System.InvalidOperationException("Open the imported forest scene.");
        Directory.CreateDirectory(Dir);rng=new System.Random(1771);
        root=new GameObject("Forest Realm - Twilight Extension").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Finish woodland map");
        forest=Material("Shadowed Forest Foliage",new Color(.105f,.19f,.16f));earth=Material("Dark Woodland Earth",new Color(.10f,.14f,.115f));stone=Material("Moss Shadow Rock",new Color(.19f,.23f,.21f));
        // Four-meter scenic collar conceals the diorama cuts without expanding the playable map greatly.
        var v=new List<Vector3>();var t=new List<int>();
        Vector2[] inner={new Vector2(-16,-14),new Vector2(16,-14),new Vector2(16,14),new Vector2(-16,14)};
        Vector2[] outer={new Vector2(-20,-18),new Vector2(20,-18),new Vector2(20,18),new Vector2(-20,18)};
        for(int j=0;j<4;j++)
        {
            int k=v.Count,n=(j+1)%4;
            v.Add(new Vector3(inner[j].x,.20f,inner[j].y));v.Add(new Vector3(inner[n].x,.20f,inner[n].y));
            v.Add(new Vector3(outer[j].x,.05f,outer[j].y));v.Add(new Vector3(outer[n].x,.05f,outer[n].y));
            t.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});
        }
        var me=new Mesh();me.SetVertices(v);me.SetTriangles(t,0);me.RecalculateNormals();me.RecalculateBounds();AssetDatabase.CreateAsset(me,Dir+"/WoodlandCollar.asset");
        var ground=new GameObject("Small forest continuation - scenic ground");ground.transform.SetParent(root);ground.AddComponent<MeshFilter>().sharedMesh=me;ground.AddComponent<MeshRenderer>().sharedMaterial=earth;
        var entry=GameObject.CreatePrimitive(PrimitiveType.Cube);entry.name="Entrance grove - solid ground";entry.transform.SetParent(root);entry.transform.position=new Vector3(0,-.25f,-15.45f);entry.transform.localScale=new Vector3(12,1.05f,4.5f);entry.GetComponent<Renderer>().sharedMaterial=earth;
        // Widen the entrance slightly into a small enclosed arrival grove.
        var old=GameObject.Find("South map edge");if(old!=null)old.SetActive(false);
        Wall("South border west shoulder",new Vector3(-10.7f,3,-13.7f),new Vector3(10.6f,8,.6f));
        Wall("South border east shoulder",new Vector3(10.7f,3,-13.7f),new Vector3(10.6f,8,.6f));
        Wall("Entrance grove west border",new Vector3(-5.7f,3,-15.5f),new Vector3(.6f,8,4.2f));
        Wall("Entrance grove east border",new Vector3(5.7f,3,-15.5f),new Vector3(.6f,8,4.2f));
        Wall("Entrance grove southern border",new Vector3(0,3,-17.35f),new Vector3(12,8,.6f));
        var flags=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Individually dressed path flag")).Take(30).ToArray();
        for(int j=0;j<5;j++)for(int i=0;i<4;i++)
        {
            var src=flags[rng.Next(flags.Length)];var go=Object.Instantiate(src.gameObject,root);go.name="Entrance continuation flagstone";
            go.transform.rotation=src.transform.rotation;go.transform.localScale=src.transform.lossyScale;
            var rr=go.GetComponent<Renderer>();var target=new Vector3(-2.5f+i*.75f+R(-.13f,.13f),.33f,-13.5f-j*.65f+R(-.1f,.1f));go.transform.position+=target-rr.bounds.center;
        }
        for(int j=0;j<4;j++)for(int i=0;i<11;i++)
        {
            float x,z;
            if(j<2){x=-19+i*3.8f+R(-.5f,.5f);z=(j==0?-16.5f:16.2f)+R(-.6f,.6f);if(j==0&&Mathf.Abs(x)<6.8f)continue;}
            else{x=(j==2?-18.1f:18.1f)+R(-.5f,.5f);z=-13+i*2.6f+R(-.5f,.5f);}
            Prop(i%2==0?"UNS_Spruce_01":"UNS_Spruce_02",new Vector3(x,.14f,z),R(5.5f,8.7f),forest);
            Prop("UNS_Standard_Rock_0"+(1+i%5),new Vector3(x+R(-.7f,.7f),.12f,z+R(-.6f,.6f)),R(.65f,1.45f),stone);
        }
        foreach(var p in new[]{new Vector3(-4,.28f,-15),new Vector3(4,.28f,-15.6f),new Vector3(-3.8f,.28f,-16.4f),new Vector3(4.1f,.28f,-13.2f)})
        {Prop("UNS_Bush",p,R(.65f,.95f),forest);Prop("UNS_Mushroom_Patch",p+Vector3.forward*.6f,.32f);}
        Prop("UNS_Log",new Vector3(4.2f,.28f,-15.1f),.65f);
        foreach(var l in Object.FindObjectsByType<Light>())
        {if(l.type==LightType.Directional){l.intensity=.85f;l.color=new Color(.7f,.8f,1);l.shadows=LightShadows.Soft;}else if(l.name=="Door Lantern"){l.intensity=5;l.range=5;}}
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.32f,.42f,.48f);RenderSettings.ambientEquatorColor=new Color(.22f,.29f,.31f);RenderSettings.ambientGroundColor=new Color(.11f,.14f,.15f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.025f,.055f,.07f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.018f;
        Camera.main.backgroundColor=new Color(.009f,.018f,.026f);Camera.main.allowHDR=true;
        Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline!=null){pipeline.supportsHDR=true;EditorUtility.SetDirty(pipeline);}
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Enchanted Twilight";
        var bloom=profile.Add<Bloom>();bloom.intensity.Override(1.0f);bloom.threshold.Override(.85f);bloom.scatter.Override(.65f);
        var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
        var colors=profile.Add<ColorAdjustments>();colors.postExposure.Override(.2f);colors.contrast.Override(12);colors.saturation.Override(-5);
        var vignette=profile.Add<Vignette>();vignette.intensity.Override(.28f);vignette.smoothness.Override(.65f);vignette.color.Override(new Color(.005f,.012f,.018f));
        AssetDatabase.CreateAsset(profile,Dir+"/EnchantedTwilight.asset");
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        var volume=root.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=profile;
        var clusters=new List<Vector3>();var gemstoneMats=new Dictionary<Material,Material>();
        foreach(var r in Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Faceted violet crystal")))
        {
            if(!clusters.Any(p=>Vector3.Distance(p,r.bounds.center)<2.0f))clusters.Add(r.bounds.center);
            r.sharedMaterials=r.sharedMaterials.Select(m=>{
                if(!gemstoneMats.ContainsKey(m)){var copy=new Material(m);copy.name=m.name+" - Twilight Glow";copy.EnableKeyword("_EMISSION");copy.SetColor("_EmissionColor",m.GetColor("_BaseColor").linear*3.0f);AssetDatabase.CreateAsset(copy,Dir+"/"+copy.name+".mat");gemstoneMats[m]=copy;}return gemstoneMats[m];}).ToArray();
        }
        foreach(var p in clusters)
        {var go=new GameObject("Violet crystal pool of light");go.transform.SetParent(root);go.transform.position=p+Vector3.up*.3f;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.57f,.24f,1);l.intensity=3.8f;l.range=4.4f;}
        foreach(var m in gemstoneMats.Values){m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(m);}
        LateAfternoonLighting.Apply();
        AssetDatabase.SaveAssets();Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("TWILIGHT_FINISH_COMPLETE: compact entrance extension, installed nature assets, dark forest perimeter, crystal bloom.");
    }
    [MenuItem("Tools/Forest Realm/Validate Twilight Extension")]
    public static void Validate()
    {
        var volume=GameObject.Find("Forest Realm - Twilight Extension").GetComponent<Volume>();
        foreach(var component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
        EditorUtility.SetDirty(volume.sharedProfile);
        foreach(var r in Object.FindObjectsByType<MeshRenderer>())
        {
            if(!r.name.StartsWith("Faceted violet crystal"))continue;
            foreach(var m in r.sharedMaterials)
            {m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(m);}
        }
        var preview=EditorSceneManager.NewPreviewScene();var results=new List<string>();
        try
        {
            foreach(var c in Object.FindObjectsByType<Collider>())
            {
                if(!c.enabled||c.isTrigger||c.gameObject.scene!=SceneManager.GetActiveScene())continue;
                var go=new GameObject("Test "+c.name);SceneManager.MoveGameObjectToScene(go,preview);go.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);go.transform.localScale=c.transform.lossyScale;
                var b=c as BoxCollider;var m=c as MeshCollider;
                if(b!=null){var copy=go.AddComponent<BoxCollider>();copy.center=b.center;copy.size=b.size;}
                else if(m!=null){var copy=go.AddComponent<MeshCollider>();copy.sharedMesh=m.sharedMesh;}
            }
            Vector3[] positions={new Vector3(0,3,-15.4f),new Vector3(0,1.6f,-16),new Vector3(-4.7f,1.6f,-15.5f),new Vector3(4.7f,1.6f,-15.5f),new Vector3(0,3,-1.5f),new Vector3(-11,1.5f,2.0f)};
            Vector3[] velocities={Vector3.zero,Vector3.back*5,Vector3.left*5,Vector3.right*5,Vector3.zero,Vector3.forward*5};
            var bodies=new List<Rigidbody>();
            for(int i=0;i<positions.Length;i++){var go=new GameObject("Player capsule "+i);SceneManager.MoveGameObjectToScene(go,preview);go.transform.position=positions[i];var cap=go.AddComponent<CapsuleCollider>();cap.height=1.75f;cap.radius=.32f;var rb=go.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation;rb.linearVelocity=velocities[i];bodies.Add(rb);}
            Physics.SyncTransforms();var physics=preview.GetPhysicsScene();for(int i=0;i<150;i++)physics.Simulate(.02f);
            for(int i=0;i<bodies.Count;i++){var p=bodies[i].position;bool ok=p.y>-.1f&&p.z>-17.1f&&Mathf.Abs(p.x)<15.5f;if(i==2||i==3)ok&=Mathf.Abs(p.x)<5.45f;if(i==5)ok&=p.z<3;results.Add(i+":"+(ok?"PASS":"FAIL")+" "+p.ToString("F3"));}
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        File.WriteAllLines("C:/Users/hp/Documents/Codex/2026-11-24/can/work/twilight_validation.txt",results);
        PrefabUtility.SaveAsPrefabAsset(GameObject.Find("Forest Realm - Twilight Extension"),Dir+"/TwilightExtension.prefab");
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Debug.Log("TWILIGHT_VALIDATION "+string.Join("; ",results));
    }
}
