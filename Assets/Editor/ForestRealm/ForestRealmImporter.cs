using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

// Imports the actual modeled Blender environment. No backdrop/image-plane replacement.
public static class ForestRealmImporter
{
    const string Folder = "Assets/ForestRealm";
    const string RootName = "Forest Realm - Imported Map";
    const string Report = "C:/Users/hp/Documents/Codex/2026-11-24/can/work/unity_collision_validation.json";
    static JObject manifest;
    static PhysicsMaterial stonePhysics;
    static Transform safety;
    static float Stream(float x) { return 4.8f + .65f * Mathf.Sin((x + 9f) * .55f); }
    static Vector3 Vec(JToken j) { return new Vector3((float)j[0], (float)j[1], (float)j[2]); }
    static string SafeName(string s) { return string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_')); }

    [MenuItem("Tools/Forest Realm/Import Map With Collisions")]
    public static void Import()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") throw new InvalidOperationException("Open SampleScene before importing this map.");
        if (GameObject.Find(RootName) != null) throw new InvalidOperationException("Map is already imported. Use Validate Collisions to check it.");
        manifest = JObject.Parse(File.ReadAllText(Folder + "/forest_manifest.json"));
        Directory.CreateDirectory(Folder + "/Materials");
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "/Models/ForestRealm.fbx");
        importer.isReadable = true;
        importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
        importer.addCollider = false; importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Models/ForestRealm.fbx");
        if (model == null) throw new InvalidOperationException("FBX did not import.");
        var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Import Forest Realm");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Blender Map - Editable Geometry";
        visual.transform.SetParent(root.transform, false);
        // FBX's right-handed conversion negates X and Z on Unity import. Undo both with a rotation.
        visual.transform.localRotation = Quaternion.Euler(0, 180, 0) * model.transform.localRotation;
        PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var materials = BuildMaterials();
        var objectMats = ((JArray)manifest["objects"]).ToDictionary(o => (string)o["name"], o => (JArray)o["materials"]);
        foreach (var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            JArray slots;
            if (objectMats.TryGetValue(r.name, out slots))
            {
                var assigned = slots.Select(t => materials.ContainsKey((string)t) ? materials[(string)t] : materials.Values.First()).ToArray();
                if (assigned.Length > 0) r.sharedMaterials = assigned.Take(r.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();
            }
            r.gameObject.isStatic = true;
            if (r.name.Contains("leaves") || r.name.Contains("Leaves") || r.name.Contains("canopy") || r.name.Contains("Canopy"))
                r.shadowCastingMode = ShadowCastingMode.TwoSided;
        }
        stonePhysics=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Folder+"/Materials/ForestCollision.asset");
        if(stonePhysics==null){stonePhysics=new PhysicsMaterial("Forest Stone - Low Snag"){dynamicFriction=.35f,staticFriction=.35f,bounciness=0};AssetDatabase.CreateAsset(stonePhysics,Folder+"/Materials/ForestCollision.asset");}
        int solidCount = 0;
        foreach (var mf in visual.GetComponentsInChildren<MeshFilter>())
        {
            if (!Solid(mf.transform)) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
            mc.convex = false; mc.isTrigger = false; mc.sharedMaterial = stonePhysics; solidCount++;
        }
        var barriers = new GameObject("Collision Safety - River and Map Borders");
        barriers.transform.SetParent(root.transform, false); safety = barriers.transform;
        Border("West map edge", new Vector3(-15.7f, 3, 0), new Vector3(.6f, 8, 28));
        Border("East map edge", new Vector3(15.7f, 3, 0), new Vector3(.6f, 8, 28));
        Border("South map edge", new Vector3(0, 3, -13.7f), new Vector3(32, 8, .6f));
        Border("North map edge", new Vector3(0, 3, 13.7f), new Vector3(32, 8, .6f));
        // Omit river-bank barriers only at the bridge's protected passage.
        var deck=visual.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Bridge transverse arch stone.004").bounds;
        float bridgeX=deck.center.x,bridgeZ=deck.center.z;
        for (int side = -1; side <= 1; side += 2)
        {
            Bank(-16,bridgeX-1.1f,side);Bank(bridgeX+1.1f,-3.5f,side);
        }
        Segment("River closed eastern end", new Vector3(-3.45f, 3, Stream(-3.5f) - 1.18f), new Vector3(-3.45f, 3, Stream(-3.5f) + 1.18f), .42f);
        foreach (float x in new [] {bridgeX-1.13f,bridgeX+1.13f})
            Border("Bridge invisible safety railing", new Vector3(x, 3, bridgeZ), new Vector3(.26f, 8, 3.9f));
        AddStairApproach(root);
        // Explicit spawn marker for an existing/future 3D player, without adding a controller.
        var spawn = new GameObject("Player Spawn - Clearing"); spawn.transform.SetParent(root.transform); spawn.transform.position = new Vector3(0, .35f, -1.5f);
        SetupView();
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        var results = ValidatePhysics(root);
        results["solidMeshColliders"] = solidCount;
        results["invisibleSafetyBarriers"] = safety.GetComponentsInChildren<BoxCollider>().Length;
        File.WriteAllText(Report, results.ToString());
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/ForestRealm_WithCollisions.prefab");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(new Vector3(0, 2, 1), Quaternion.Euler(43, -22, 0), 28f);
        AssetDatabase.SaveAssets();
        Debug.Log("FOREST_IMPORT_COMPLETE " + results.ToString(Newtonsoft.Json.Formatting.None));
    }

    static Dictionary<string, Material> BuildMaterials()
    {
        var result = new Dictionary<string, Material>();
        foreach (JObject item in manifest["materials"])
        {
            string name = (string)item["name"];
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = name;
            var c = (JArray)item["color"];
            // Blender material values are scene-linear; Unity's material color is authored in sRGB.
            Color col = new Color((float)c[0], (float)c[1], (float)c[2], 1).gamma;
            mat.SetColor("_BaseColor", col); mat.SetFloat("_Metallic", (float)item["metallic"]);
            mat.SetFloat("_Smoothness", (float)item["smoothness"]); mat.SetFloat("_Cull", 0);
            var em = (JArray)item["emission"];
            Color emission = new Color((float)em[0], (float)em[1], (float)em[2]);
            if (emission.maxColorComponent > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
            string tex = (string)item["texture"];
            if (!string.IsNullOrEmpty(tex)) { mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex)); mat.SetColor("_BaseColor", Color.white); }
            string path=Folder+"/Materials/"+SafeName(name)+".mat";
            var previous=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(previous!=null){EditorUtility.CopySerialized(mat,previous);UnityEngine.Object.DestroyImmediate(mat);mat=previous;}
            else AssetDatabase.CreateAsset(mat,path);
            result[name]=mat;
        }
        return result;
    }

    static bool Solid(Transform t)
    {
        string n = t.name;
        if(n.StartsWith("Individually dressed path flag")||n.StartsWith("Bridge and dwelling approach"))return true;
        bool In(string group) { for(var p=t.parent;p!=null;p=p.parent) if(p.name==group) return true; return false; }
        if (In("Characters")) return false;
        if (In("Water")) return n.StartsWith("River bank rounded stone");
        if (In("Terrain")) return true;
        if (In("Ground_Vegetation")) return n.StartsWith("Mossy woodland boulder") || n.StartsWith("Faceted violet");
        if (In("Trees")) return n.Contains("tapered trunk") || n.StartsWith("Buttress root");
        if (In("Paths")) return true;
        if (In("Tree_Dwelling")) return n.StartsWith("Elder dwelling") || n.StartsWith("Buttress root") || n.StartsWith("Arched oak door") || n.StartsWith("Welcome doorstep");
        if (In("Ruined_Gateway")) return n.StartsWith("Raised gateway terrace") || n.StartsWith("Broad approach stair") || n.StartsWith("Pier ") || n.StartsWith("Projecting pier") || n.StartsWith("Arch ") || n.StartsWith("Broken adjoining wall") || n.StartsWith("Moss capped wall") || n.StartsWith("Shaded path") || n.StartsWith("Faceted violet");
        if (In("Crystal_Shrine")) return n.StartsWith("Shrine recessed") || n.StartsWith("Shrine ") && n.Contains("chunky ring") || n.StartsWith("Faceted violet");
        if (In("Musical_Props")) return n.StartsWith("Musicians resting stone") || n.StartsWith("Music folio stone stand") || n.StartsWith("Abandoned instrument case") || n.StartsWith("Harp ") && !n.StartsWith("Harp string") || n.StartsWith("Cart wheel rim") || n.StartsWith("Wheel axle") || n.StartsWith("Wheel spoke");
        return false;
    }
    static BoxCollider Border(string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name); go.transform.SetParent(safety, false); go.transform.position = center; go.isStatic = true;
        var b = go.AddComponent<BoxCollider>(); b.size = size; b.sharedMaterial = stonePhysics; b.isTrigger = false; return b;
    }
    static void Segment(string name, Vector3 a, Vector3 b, float width)
    {
        var wall = Border(name, (a+b)/2, new Vector3(Vector3.Distance(a,b)+.12f, 8, width));
        wall.transform.rotation = Quaternion.FromToRotation(Vector3.right, (b-a).normalized);
    }
    static void Bank(float start, float end, int side)
    {
        int steps = Mathf.CeilToInt((end-start)/.32f);
        for (int i=0;i<steps;i++)
        {
            float x = Mathf.Lerp(start,end,(float)i/steps), xx = Mathf.Lerp(start,end,(float)(i+1)/steps);
            Segment("River bank " + (side<0?"south":"north") + " " + i, new Vector3(x,3,Stream(x)+side*1.18f), new Vector3(xx,3,Stream(xx)+side*1.18f), .4f);
        }
    }
    static void AddStairApproach(GameObject root)
    {
        Physics.SyncTransforms();
        var step=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Broad approach stair 00").bounds;
        float front=step.min.z,start=front-.95f,y=0;RaycastHit hit;
        if(Physics.Raycast(new Vector3(step.center.x,8,start),Vector3.down,out hit,20))y=hit.point.y;
        if(step.max.y-y<.34f)return;
        float left=step.min.x,right=step.max.x,end=front+.10f;
        var mesh=new Mesh();mesh.name="Safe approach to raised gateway";
        mesh.vertices=new[]{new Vector3(left,y,start),new Vector3(right,y,start),new Vector3(left,step.max.y,end),new Vector3(right,step.max.y,end)};
        mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/Models/GatewayApproachCollision.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
        var go=new GameObject("Invisible approach to raised gateway");go.transform.SetParent(safety,false);
        var col=go.AddComponent<MeshCollider>();col.sharedMesh=mesh;col.sharedMaterial=stonePhysics;
    }
    [MenuItem("Tools/Forest Realm/Replace With Saved Blender Map")]
    public static void ReplaceSaved()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/ForestRealm.fbx")==null)throw new InvalidOperationException("Export the edited Blender map first.");
        var old=GameObject.Find(RootName);
        if(old!=null)Undo.DestroyObjectImmediate(old);
        Import();
    }
    static void SetupView()
    {
        var cam = Camera.main;
        if (cam == null) { var go = new GameObject("Main Camera"); go.tag="MainCamera";cam=go.AddComponent<Camera>();go.AddComponent<AudioListener>(); }
        JObject c = (JObject)manifest["camera"];
        cam.transform.position=Vec(c["position"]);cam.transform.rotation=Quaternion.LookRotation(Vec(c["forward"]),Vec(c["up"]));
        cam.orthographic=true;cam.orthographicSize=(float)c["orthoSize"];cam.nearClipPlane=.1f;cam.farClipPlane=150;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.075f,.135f,.15f);
        var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
        var sun=UnityEngine.Object.FindObjectsByType<Light>().FirstOrDefault(l=>l.type==LightType.Directional);
        if(sun!=null) {sun.transform.rotation=Quaternion.Euler(47,-35,0);sun.intensity=1.2f;sun.color=new Color(1,.85f,.65f);sun.shadows=LightShadows.Soft;}
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.44f,.58f,.63f);RenderSettings.ambientEquatorColor=new Color(.26f,.34f,.31f);RenderSettings.ambientGroundColor=new Color(.13f,.16f,.12f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.12f,.22f,.24f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.009f;
        var lighting=new GameObject("Forest Accent Lights");lighting.transform.SetParent(GameObject.Find(RootName).transform);
        Point("Door Lantern",new Vector3(-7.73f,2.3f,5.8f),new Color(1,.53f,.13f),4,5,lighting.transform);
        Point("Crystal Shrine Glow",new Vector3(-8,1.7f,-4),new Color(.62f,.16f,1),2.6f,5,lighting.transform);
    }
    static void Point(string n, Vector3 pos, Color color,float power,float range,Transform parent)
    {var go=new GameObject(n);go.transform.SetParent(parent);go.transform.position=pos;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=power;l.range=range;}

    [MenuItem("Tools/Forest Realm/Validate Collisions")]
    public static void ValidateMenu()
    {
        var root=GameObject.Find(RootName);if(root==null)throw new InvalidOperationException("Import the map first.");
        var r=ValidatePhysics(root);File.WriteAllText(Report,r.ToString());Debug.Log(r.ToString());
    }
    [MenuItem("Tools/Forest Realm/Finalize Existing Import")]
    public static void FinalizeExisting()
    {
        var root=GameObject.Find(RootName);if(root==null)throw new InvalidOperationException("Import the map first.");
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/ForestRealm.fbx");
        root.transform.Find("Blender Map - Editable Geometry").localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
        manifest=JObject.Parse(File.ReadAllText(Folder+"/forest_manifest.json"));
        var mapping=((JArray)manifest["objects"]).ToDictionary(o=>(string)o["name"],o=>(JArray)o["materials"]);
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            JArray slots;if(!mapping.TryGetValue(r.name,out slots))continue;
            var mats=slots.Select(t=>AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/"+SafeName((string)t)+".mat")).ToArray();
            int count=r.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
            if(mats.Length>=count&&mats.All(m=>m!=null))r.sharedMaterials=mats.Take(count).ToArray();
        }
        string oldPath=Folder+"/Materials/ForestCollision.physicsMaterial";
        if(AssetDatabase.LoadMainAssetAtPath(oldPath)!=null)AssetDatabase.MoveAsset(oldPath,Folder+"/Materials/ForestCollision.asset");
        stonePhysics=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Folder+"/Materials/ForestCollision.asset");
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if(!Solid(mf.transform)||mf.GetComponent<Collider>()!=null)continue;
            var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;c.sharedMaterial=stonePhysics;
        }
        var result=ValidatePhysics(root);
        result["solidMeshColliders"]=root.GetComponentsInChildren<MeshCollider>().Length;
        result["invisibleSafetyBarriers"]=root.GetComponentsInChildren<BoxCollider>().Length;
        File.WriteAllText(Report,result.ToString());
        PrefabUtility.SaveAsPrefabAsset(root,Folder+"/ForestRealm_WithCollisions.prefab");
        var scene=root.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();Selection.activeGameObject=root;
        Debug.Log("FOREST_IMPORT_COMPLETE "+result.ToString(Newtonsoft.Json.Formatting.None));
    }
    static JObject ValidatePhysics(GameObject root)
    {
        Physics.SyncTransforms();
        var report=new JObject();var failures=new JArray();var drops=new JArray();
        var bridge=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Bridge transverse arch stone.004").bounds.center;
        var stepCenter=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Broad approach stair 02").bounds.center;
        var testScene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var c in root.GetComponentsInChildren<Collider>())
            {
                if(!c.enabled||c.isTrigger)continue;
                var go=new GameObject("Test "+c.name);SceneManager.MoveGameObjectToScene(go,testScene);
                go.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);go.transform.localScale=c.transform.lossyScale;
                var mc=c as MeshCollider;var bc=c as BoxCollider;
                if(mc!=null){var copy=go.AddComponent<MeshCollider>();copy.sharedMesh=mc.sharedMesh;copy.convex=false;}
                else if(bc!=null){var copy=go.AddComponent<BoxCollider>();copy.center=bc.center;copy.size=bc.size;}
            }
            var bodies=new List<Rigidbody>();var labels=new List<string>();var starts=new List<Vector3>();
            Vector3[] positions={new Vector3(0,3,-1.5f),new Vector3(bridge.x,3,bridge.z),new Vector3(stepCenter.x,3,stepCenter.z),new Vector3(14.1f,1.5f,-10),new Vector3(-14.1f,1.5f,-10),new Vector3(0,1.5f,-12.1f),new Vector3(0,1.5f,12.1f),new Vector3(-11,1.5f,Stream(-11)-2.6f),new Vector3(-4,1.5f,Stream(-4)+2.6f)};
            Vector3[] velocities={Vector3.zero,Vector3.zero,Vector3.zero,Vector3.right*5,Vector3.left*5,Vector3.back*5,Vector3.forward*5,Vector3.forward*5,Vector3.back*5};
            string[] names={"Clearing floor","Bridge walk surface","Gateway stairs","East edge","West edge","South edge","North edge","South river bank","North river bank"};
            for(int i=0;i<positions.Length;i++)
            {
                var go=new GameObject(names[i]);SceneManager.MoveGameObjectToScene(go,testScene);go.transform.position=positions[i];
                var cap=go.AddComponent<CapsuleCollider>();cap.radius=.32f;cap.height=1.75f;
                var rb=go.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearVelocity=velocities[i];
                bodies.Add(rb);labels.Add(names[i]);starts.Add(positions[i]);
            }
            var physics=testScene.GetPhysicsScene();
            for(int step=0;step<150;step++)physics.Simulate(.02f);
            for(int i=0;i<bodies.Count;i++)
            {
                var p=bodies[i].position;bool ok=p.y>-.1f&&Mathf.Abs(p.x)<15.5f&&Mathf.Abs(p.z)<13.5f;
                if(i==7)ok&=p.z<Stream(p.x)-1.0f;
                if(i==8)ok&=p.z>Stream(p.x)+1.0f;
                drops.Add(new JObject{{"test",labels[i]},{"passed",ok},{"position",p.ToString("F3")}});
                if(!ok)failures.Add(labels[i]);
            }
            // Sweep a player-sized body through the bridge opening above its walking surface.
            var hits=new RaycastHit[32];
            int hitCount=physics.CapsuleCast(new Vector3(bridge.x,1.3f,bridge.z-2.4f),new Vector3(bridge.x,2.4f,bridge.z-2.4f),.30f,Vector3.forward,hits,4.8f,~0,QueryTriggerInteraction.Ignore);
            bool bridgeOpen=true;
            for(int i=0;i<hitCount;i++)if(hits[i].collider.name.StartsWith("Test River bank")||hits[i].collider.name.StartsWith("Test Bridge invisible"))bridgeOpen=false;
            report["bridgePassageClearOfSafetyWalls"]=bridgeOpen;if(!bridgeOpen)failures.Add("Bridge passage blocked");
        }
        finally {EditorSceneManager.ClosePreviewScene(testScene);}
        report["capsuleHeight"]=1.75;report["capsuleRadius"]=.32;report["simulationSeconds"]=3;
        report["physicsTests"]=drops;report["failures"]=failures;report["passed"]=failures.Count==0;
        var current=JObject.Parse(File.ReadAllText(Folder+"/forest_manifest.json"));
        var expected=((JArray)current["objects"]).ToDictionary(o=>(string)o["name"],o=>o);
        var alignment=new JArray();int matched=0;
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            JToken obj;if(!expected.TryGetValue(r.name,out obj)||obj["center"]==null)continue;
            float error=Vector3.Distance(r.bounds.center,Vec(obj["center"]));
            if(error>.03f)alignment.Add(new JObject{{"name",r.name},{"meters",error}});else matched++;
        }
        report["sourceSha256"]=current["sourceSha256"];
        report["objectsMatchingEditedBlenderPositions"]=matched;report["positionMismatches"]=alignment;
        return report;
    }
}
