using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
public static class AdventureSetup
{
    const string RootName = "Adventure - Mobs and NPCs";
    const string WolfModel = "Assets/_Game/Prefabs/wolf.glb";
    const string GolemModel = "Assets/_Game/Prefabs/Golem.glb";
    const string GolemController = "Assets/_Game/Animations/Golem.controller";
    static readonly string[] BuildScenes =
    {
        "Assets/Scenes/_Menu.unity", "Assets/Scenes/Kingdom.unity", "Assets/Scenes/Forest.unity", "Assets/_Game/Scenes/Battle.unity"
    };
    [MenuItem("Silence/Place Mobs and NPCs")]
    public static void PlaceAll()
    {
        FinaleSetup.SetUp();
        MusicSlots.Ensure();
        SoundSlots.Ensure();
        UiStyleSetup.StyleAll();
        SetUpKingdom();
        SetUpForest();
        EditorBuildSettings.scenes = BuildScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ADVENTURE setup done");
    }
    public static void PlaceAndBuild()
    {
        PlaceAll();
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-adventureBuildPath");
        if (i >= 0 && i + 1 < args.Length) Build(args[i + 1]);
        EditorApplication.Exit(0);
    }
    [MenuItem("Silence/Build Windows Game")]
    public static void BuildFromMenu()
    {
        string folder = EditorUtility.SaveFolderPanel("Build Project Silence into", "", "Build");
        if (!string.IsNullOrEmpty(folder)) Build(System.IO.Path.Combine(folder, "ProjectSilence.exe"));
    }
    static void Build(string exePath)
    {
        PlayerSettings.bakeCollisionMeshes = true;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = BuildScenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Debug.Log("ADVENTURE build: " + report.summary.result + ", errors " + report.summary.totalErrors + ", " + exePath);
    }
    static void SetUpKingdom()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Kingdom.unity");
        Context c = Begin(0.2f);
        Npc(c, "Sister Agna", new Vector2(100f, -6f), new Color(1f, 0.72f, 0.72f), 1f, true,
            "Maestro! The cathedral bells still ring, so the Silence cannot cross into the city... not yet.",
            "Rest a moment. Let the bells' note mend you and your companions.");
        Npc(c, "Wren", new Vector2(108f, -14f), new Color(0.55f, 0.75f, 1f), 0.75f, false,
            "My song came out wrong this morning. The last note just... wasn't there.",
            "Mama says the Silence eats endings. Can you give my song its ending back, Maestro?");
        Npc(c, "Old Tacet", new Vector2(92f, -12f), new Color(0.85f, 0.68f, 0.45f), 1f, false,
            "Long ago the Archcantor tried to bind every voice in Cantoria into one endless, perfect chord.",
            "Your teacher, the old Maestro, lowered his baton before the last note. The chord was never finished.",
            "That unfinished chord is the Silence. It hungers for the note it never got.");
        Npc(c, "Guard Bram", new Vector2(146f, -5f), new Color(0.62f, 0.68f, 0.78f), 1f, false,
            "Halt! Past this east gate lies the Forest. Something there swallows every sound: birds, wind, even footsteps.",
            "Hollow wolves prowl the forest paths. If one catches you, you must fight. Sprint with {sprint} to get away.",
            "They say a stone giant, the Hollow Colossus, hums at the heart of the woods. Break it and the Silence fades.");
        EditorSceneManager.SaveScene(scene);
    }
    static void SetUpForest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Forest.unity");
        Context c = Begin(0.3f);
        Npc(c, "Lost Bard", new Vector2(-3f, -2f), new Color(0.5f, 0.5f, 0.56f), 1f, false,
            "...Is someone there? I can barely hear you. I can't hear my lute at all any more.",
            "The further east you go, the quieter it gets. Something huge is swallowing the sound of the forest.");
        Npc(c, "Shrine Keeper Iona", new Vector2(11f, -3f), new Color(0.65f, 1f, 0.7f), 1f, true,
            "This old bell-stone still hums, so the Silence keeps its distance here.",
            "Rest, and let it tune you. Beyond the east path the Hollow Colossus waits.",
            "Cast your spells on the beat. Magic cuts through its stone hide.");
        Mob(c, "forest_wolves", "Hollow Wolf pack", 0, 1.8f, false, WolfModel, 0.7f, new Vector2(24f, 0.4f), 1f);
        Mob(c, "forest_golem", "Mute Golem", 1, 1.3f, false, GolemModel, 1.2f, new Vector2(33f, 1f), 0.85f);
        Mob(c, "forest_boss", "Hollow Colossus", 1, 2.6f, true, GolemModel, 1.9f, new Vector2(42f, 2f), 0.8f);
        OverworldMob vale = Mob(c, "forest_final", "Archcantor Vale", 2, 1f, false, GolemModel, 1.3f, new Vector2(43f, 4f), 0f, "Enemy_SorcererKing");
        vale.isFinalBoss = true;
        vale.appearsAfterMob = "forest_boss";
        vale.wanderRadius = 0f;
        vale.noticeRadius = 5f * c.scale;
        vale.introLines = new[]
        {
            "So. The old Maestro's student has finally come, with a little choir of strays.",
            "Your teacher lowered his baton before my chord could end. Every voice in Cantoria, held forever in one perfect note, and he called it a cage.",
            "The Silence is only my chord, waiting to be finished. Stand aside, or become part of it.",
            "Maestro: \"Every song needs an ending, Vale. We came to play yours.\""
        };
        EditorSceneManager.SaveScene(scene);
    }
    class Context
    {
        public Transform root;
        public float groundY;
        public float scale;
        public float playerHeight;
        public float playerWalk, playerSprint;
        public Sprite npcSprite;
        public Vector3 spriteLocalPosition, spriteLocalScale;
    }
    static Context Begin(float groundY)
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        Physics.SyncTransforms();
        MusicalMagePlayer player = Object.FindAnyObjectByType<MusicalMagePlayer>(FindObjectsInactive.Include);
        var c = new Context
        {
            root = new GameObject(RootName).transform,
            groundY = groundY,
            scale = player.transform.lossyScale.y,
            playerWalk = player.walkSpeed,
            playerSprint = player.sprintSpeed,
            npcSprite = FindNpcSprite(),
            spriteLocalPosition = player.spriteRenderer != null ? player.spriteRenderer.transform.localPosition : Vector3.up,
            spriteLocalScale = player.spriteRenderer != null ? player.spriteRenderer.transform.localScale : Vector3.one
        };
        c.playerHeight = player.GetComponent<CharacterController>().height * c.scale;
        Debug.Log("ADVENTURE scene " + EditorSceneManager.GetActiveScene().name + ": player scale " + c.scale + " height " + c.playerHeight
                  + " walk " + c.playerWalk + " sprint " + c.playerSprint + " npc sprite " + (c.npcSprite != null ? c.npcSprite.name : "none"));
        return c;
    }
    static void Npc(Context c, string name, Vector2 wanted, Color tint, float size, bool heals, params string[] lines)
    {
        Vector3 spot = FindSpot(c, wanted, 0.3f * c.scale, c.playerHeight);
        var go = new GameObject("NPC - " + name);
        go.transform.SetParent(c.root, false);
        go.transform.position = spot;
        go.transform.localScale = Vector3.one * c.scale * size;
        NpcTalk talk = go.AddComponent<NpcTalk>();
        talk.npcName = name;
        talk.lines = lines;
        talk.healsParty = heals;
        talk.talkRange = 2.2f * c.scale;
        CapsuleCollider body = go.AddComponent<CapsuleCollider>();
        body.height = 1.75f;
        body.radius = 0.28f;
        body.center = new Vector3(0f, 0.875f, 0f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.transform.localPosition = c.spriteLocalPosition;
        sprite.transform.localScale = c.spriteLocalScale;
        SpriteRenderer renderer = sprite.AddComponent<SpriteRenderer>();
        Sprite own = LoadCharacterSprite("NPC_" + name.Replace(" ", ""), true);
        renderer.sprite = own != null ? own : c.npcSprite;
        renderer.color = own != null ? Color.white : tint;
        renderer.flipX = own == null && name.Length % 2 == 0;
        var label = new GameObject("Name");
        label.transform.SetParent(sprite.transform, false);
        label.transform.localPosition = Vector3.up * 1.35f;
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = name;
        text.fontSize = 48;
        text.characterSize = 0.05f / Mathf.Max(0.01f, c.spriteLocalScale.x);
        text.anchor = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.92f, 0.6f);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        Debug.Log("ADVENTURE npc '" + name + "' at " + spot.ToString("F1"));
    }
    static OverworldMob Mob(Context c, string id, string displayName, int group, float boost, bool boss, string modelPath, float heightVsPlayer, Vector2 wanted, float speedFactor, string spriteName = null)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        float targetHeight = c.playerHeight * heightVsPlayer;
        Vector3 spot = FindSpot(c, wanted, 0.35f * targetHeight, targetHeight);
        var go = new GameObject("Mob - " + displayName + " (" + id + ")");
        go.transform.SetParent(c.root, false);
        go.transform.position = spot;
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Sprite art = LoadCharacterSprite(spriteName ?? (modelPath == WolfModel ? "Enemy_CrystalWolf" : "Enemy_StoneGolem"), false);
        if (art != null)
        {
            var sprite = new GameObject("Sprite");
            sprite.transform.SetParent(go.transform, false);
            sprite.transform.localScale = Vector3.one * (targetHeight * 1.3f / (256f / 120f));
            SpriteRenderer renderer = sprite.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            if (boss) renderer.color = new Color(0.86f, 0.8f, 1f);
        }
        else
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, go.scene);
            model.transform.SetParent(go.transform, false);
            Bounds bounds = MeasureBounds(model);
            float modelScale = targetHeight / Mathf.Max(0.05f, bounds.size.y);
            model.transform.localScale = Vector3.one * modelScale;
            model.transform.localPosition = Vector3.up * (-bounds.min.y * modelScale);
            if (modelPath == GolemModel)
            {
                Animator animator = model.GetComponentInChildren<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GolemController);
            }
        }
        CharacterController body = go.AddComponent<CharacterController>();
        body.height = Mathf.Max(targetHeight, 0.5f);
        body.radius = Mathf.Clamp(targetHeight * 0.3f, 0.2f, body.height * 0.5f);
        body.center = new Vector3(0f, body.height * 0.5f + 0.05f, 0f);
        OverworldMob mob = go.AddComponent<OverworldMob>();
        mob.mobId = id;
        mob.displayName = displayName;
        mob.enemyGroup = group;
        mob.enemyBoost = boost;
        mob.isBoss = boss;
        mob.wanderRadius = (boss ? 1f : 2.5f) * c.scale;
        mob.noticeRadius = (boss ? 4.5f : 5f) * c.scale;
        mob.giveUpRadius = (boss ? 9f : 10f) * c.scale;
        mob.walkSpeed = c.playerWalk * 0.45f * speedFactor;
        mob.chaseSpeed = Mathf.Min(c.playerWalk * 1.15f, c.playerSprint * 0.85f) * speedFactor;
        mob.touchDistance = body.radius + 0.28f * c.scale + 0.35f * c.scale;
        Debug.Log("ADVENTURE mob '" + id + "' at " + spot.ToString("F1") + " height " + targetHeight.ToString("F2") + " chase " + mob.chaseSpeed.ToString("F1"));
        return mob;
    }
    static Sprite LoadCharacterSprite(string fileName, bool pixelArt)
    {
        string path = "Assets/Sprites/Characters/" + fileName + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, 120f))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 120f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = pixelArt ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0.06f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite FindNpcSprite()
    {
        foreach (string path in new[] { "Assets/Sprites/MusicalMage/Walk_S_00.png", "Assets/Sprites/MusicalMage/Sprint_S_00.png" })
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            Debug.Log("ADVENTURE sprite check " + path + ": " + string.Join(", ", all.Select(a => a.GetType().Name + " '" + a.name + "'")));
            Sprite direct = all.OfType<Sprite>().FirstOrDefault() ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null) return direct;
        }
        Sprite[] sprites = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites/MusicalMage" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Sprite>().ToArray();
        foreach (string wanted in new[] { "Idle_S_00", "Walk_S_00", "Sprint_S_00", "_S_00", "_S_" })
        {
            Sprite match = sprites.FirstOrDefault(s => s.name.Contains(wanted));
            if (match != null) return match;
        }
        return sprites.FirstOrDefault();
    }
    static Bounds MeasureBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(model.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        bounds.center -= model.transform.position;
        return bounds;
    }
    static Vector3 FindSpot(Context c, Vector2 wanted, float radius, float height)
    {
        return new Vector3(wanted.x, c.groundY, wanted.y);
    }
    static Vector3 FindSpotInEditor(Context c, Vector2 wanted, float radius, float height)
    {
        for (int ring = 0; ring <= 8; ring++)
        {
            int tries = ring == 0 ? 1 : 12;
            for (int k = 0; k < tries; k++)
            {
                float angle = k * Mathf.PI * 2f / tries;
                Vector3 candidate = new Vector3(wanted.x, 0f, wanted.y) + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring * 0.6f * c.scale;
                RaycastHit[] hits = Physics.RaycastAll(new Vector3(candidate.x, c.groundY + 40f, candidate.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
                RaycastHit[] ground = hits.Where(h => h.normal.y > 0.75f && Mathf.Abs(h.point.y - c.groundY) < 1.5f * c.scale)
                                          .OrderByDescending(h => h.point.y).ToArray();
                if (ground.Length == 0) continue;
                Vector3 point = ground[0].point;
                Vector3 bottom = point + Vector3.up * (radius + 0.15f);
                Vector3 top = point + Vector3.up * Mathf.Max(radius + 0.2f, height - radius);
                if (Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (ring > 0) Debug.Log("ADVENTURE moved spot " + wanted + " by " + (ring * 0.6f * c.scale).ToString("F1") + " m to find room");
                return point;
            }
        }
        Debug.LogWarning("ADVENTURE no free ground near " + wanted + " - placed it anyway, please move it in the scene");
        return new Vector3(wanted.x, c.groundY, wanted.y);
    }
}
