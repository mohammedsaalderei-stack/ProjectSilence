using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class FinaleSetup
{
    const string Models = "Assets/_Game/Characters/Models/";
    const string Animators = "Assets/_Game/Characters/Animators/";
    const string Textures = "Assets/_Game/Characters/Textures/";
    const string Materials = "Assets/_Game/Characters/Materials/";
    const string BattleScene = "Assets/_Game/Scenes/Battle.unity";
    const int TextureSize = 1024;

    class Spec
    {
        public string idle, attack, cast;
        public float idleSpeed = 1f;
    }

    static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
    {
        { "Maestro", new Spec { idle = "Attack", idleSpeed = 0f, attack = "Attack", cast = "Attack" } },
        { "Harmony", new Spec { idle = "Charged_Spell_Cast", idleSpeed = 0f, attack = "Charged_Spell_Cast", cast = "Charged_Spell_Cast" } },
        { "Forte", new Spec { idle = "Combat_Stance", attack = "Charged_Slash", cast = "Charged_Slash" } },
        { "Lyra", new Spec { idle = "Archery_Shot", idleSpeed = 0f, attack = "Archery_Shot", cast = "Archery_Shot" } },
        { "ArchcantorVale", new Spec { idle = "mage_soell_cast_3", idleSpeed = 0f, attack = "Charged_Ground_Slam", cast = "mage_soell_cast_3" } },
        { "StoneGolem", new Spec { idle = "Charged_Ground_Slam", idleSpeed = 0f, attack = "Charged_Ground_Slam", cast = "Charged_Ground_Slam" } },
        { "CrystalWolf", new Spec { idle = "wolf_idle_fight", attack = "wolf_jump_start", cast = "wolf_jump_start" } },
    };

    [MenuItem("Silence/Set Up Party and Finale")]
    public static void SetUp()
    {
        Directory.CreateDirectory(Animators);
        Directory.CreateDirectory(Textures);
        Directory.CreateDirectory(Materials);
        foreach (var pair in Specs) MakeController(pair.Key, pair.Value);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene(BattleScene);
        BattleManager battle = Object.FindAnyObjectByType<BattleManager>(FindObjectsInactive.Include);
        List<Unit> all = Object.FindObjectsByType<Unit>(FindObjectsInactive.Include).ToList();

        Unit maestro = all.First(u => u.isPlayer && u.unitName == "Maestro");
        Unit harmony = all.First(u => u.isPlayer && u.unitName == "Harmony");
        Unit forte = all.FirstOrDefault(u => u.isPlayer && u.unitName == "Forte") ?? Clone(harmony, "Forte");
        Unit lyra = all.FirstOrDefault(u => u.isPlayer && u.unitName == "Lyra") ?? Clone(harmony, "Lyra");

        Hero(maestro, "Maestro", new Vector3(-2.3f, 0f, 3.4f), 100, 40, 14, 7, 13, 12, new Color(1f, 0.8f, 0.3f), true);
        Hero(harmony, "Harmony", new Vector3(-4.6f, 0f, -0.6f), 90, 60, 9, 6, 10, 16, new Color(0.55f, 0.85f, 1f), true);
        Hero(forte, "Forte", new Vector3(-2.2f, 0f, 0.4f), 160, 10, 17, 12, 7, 5, new Color(1f, 0.55f, 0.35f), true);
        Hero(lyra, "Lyra", new Vector3(-4.9f, 0f, 2.6f), 95, 25, 15, 5, 15, 10, new Color(0.7f, 1f, 0.6f), false);
        battle.party = new List<Unit> { forte, maestro, harmony, lyra };

        Transform lyraModel = lyra.transform.Find("Lyra Model"), maestroModel = maestro.transform.Find("Maestro Model");
        if (lyraModel != null && maestroModel != null) lyraModel.rotation = Quaternion.AngleAxis(90f, Vector3.up) * maestroModel.rotation;

        foreach (Unit enemy in all.Where(u => !u.isPlayer))
        {
            if (enemy.unitName.StartsWith("Hollow Wolf") || enemy.unitName.StartsWith("Hushed Wolf")) SwapModel(enemy, "CrystalWolf");
            else if (enemy.unitName.StartsWith("Mute Golem")) SwapModel(enemy, "StoneGolem");
        }

        GameObject golemGroup = battle.enemyGroups[1];
        GameObject finalGroup = battle.enemyGroups.Length > 2 ? battle.enemyGroups[2] : null;
        if (finalGroup == null)
        {
            finalGroup = Object.Instantiate(golemGroup, golemGroup.transform.parent);
            finalGroup.name = "Enemy Group - Finale";
            battle.enemyGroups = battle.enemyGroups.Concat(new[] { finalGroup }).ToArray();
        }
        foreach (Unit unit in finalGroup.GetComponentsInChildren<Unit>(true))
        {
            if (unit.unitName.StartsWith("Mute Golem") || unit.unitName == "Archcantor Vale")
            {
                unit.unitName = "Archcantor Vale";
                unit.gameObject.name = "Archcantor Vale";
                unit.maxHP = 520; unit.maxMP = 0; unit.attack = 18; unit.defense = 12; unit.speed = 8; unit.magic = 20;
                unit.castsSpells = true;
                unit.chordEvery = 3;
                unit.spellColor = new Color(0.75f, 0.35f, 1f);
                SwapModel(unit, "ArchcantorVale", 0.85f);
                AttachWeapon(unit, "ArchcantorVale");
            }
            else
            {
                unit.unitName = "Hushed Wolf";
                unit.gameObject.name = "Hushed Wolf";
                unit.maxHP = 60; unit.attack = 13; unit.defense = 5; unit.speed = 14;
                SwapModel(unit, "CrystalWolf");
            }
            EditorUtility.SetDirty(unit);
        }
        finalGroup.SetActive(false);

        EditorUtility.SetDirty(battle);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FINALE setup done: party " + string.Join(", ", battle.party.Select(u => u.unitName)) + " | enemy groups " + battle.enemyGroups.Length);
    }

    static Unit Clone(Unit template, string name)
    {
        GameObject copy = Object.Instantiate(template.gameObject, template.transform.parent);
        copy.name = name;
        return copy.GetComponent<Unit>();
    }

    static void Hero(Unit unit, string name, Vector3 position, int hp, int mp, int atk, int def, int spd, int magic, Color spell, bool melee)
    {
        unit.unitName = name;
        unit.gameObject.name = name;
        unit.transform.position = position;
        unit.maxHP = hp; unit.maxMP = mp; unit.attack = atk; unit.defense = def; unit.speed = spd; unit.magic = magic;
        unit.spellColor = spell;
        UnitMotion motion = unit.GetComponent<UnitMotion>() ?? unit.gameObject.AddComponent<UnitMotion>();
        motion.runToTarget = melee;
        SwapModel(unit, name);
        AttachWeapon(unit, name);
        EditorUtility.SetDirty(unit);
        EditorUtility.SetDirty(motion);
    }

    static void SwapModel(Unit unit, string modelName, float heightScale = 1f)
    {
        Transform root = unit.transform;
        Transform old = root.Cast<Transform>().FirstOrDefault(c => c.GetComponentInChildren<Renderer>(true) != null);
        float height = 1.9f;
        Quaternion facing = Quaternion.identity;
        if (old != null)
        {
            if (old.name == modelName + " Model")
            {
                UseController(old.gameObject, modelName);
                UseCompressedTextures(old.gameObject, modelName);
                return;
            }
            height = Measure(old.gameObject).size.y;
            facing = old.localRotation;
            Object.DestroyImmediate(old.gameObject);
        }
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + modelName + ".glb");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.gameObject.scene);
        model.name = modelName + " Model";
        model.transform.SetParent(root, false);
        model.transform.localRotation = facing;
        model.transform.SetAsFirstSibling();
        float scale = height * heightScale / Mathf.Max(0.01f, Measure(model).size.y);
        model.transform.localScale = model.transform.localScale * scale;
        model.transform.position += Vector3.up * (root.position.y - Measure(model).min.y);
        UseController(model, modelName);
        UseCompressedTextures(model, modelName);
        Debug.Log("FINALE " + unit.unitName + " now uses " + modelName + " (height " + (height * heightScale).ToString("F2") + ")");
    }

    static void UseController(GameObject model, string modelName)
    {
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Animators + modelName + ".controller");
        animator.applyRootMotion = false;
        EditorUtility.SetDirty(animator);
    }


    class WeaponSpec
    {
        public string file, hand;
        public float length;
        public float grip;
        public bool tipAtTop;
        public float spin;
    }

    static readonly Dictionary<string, WeaponSpec> WeaponSpecs = new Dictionary<string, WeaponSpec>
    {
        { "Maestro", new WeaponSpec { file = "Rapier", hand = "mixamorig:RightHand", length = 0.55f, grip = 0.11f, tipAtTop = false } },
        { "Harmony", new WeaponSpec { file = "HolyStaff", hand = "mixamorig:RightHand", length = 0.95f, grip = 0.42f, tipAtTop = true } },
        { "Forte", new WeaponSpec { file = "Greatsword", hand = "mixamorig:RightHand", length = 0.75f, grip = 0.14f, tipAtTop = false } },
        { "Lyra", new WeaponSpec { file = "RecurveBow", hand = "mixamorig:LeftHand", length = 0.66f, grip = 0.5f, tipAtTop = true } },
        { "ArchcantorVale", new WeaponSpec { file = "SorcererStaff", hand = "mixamorig:RightHand", length = 0.9f, grip = 0.42f, tipAtTop = true } },
    };

    static void AttachWeapon(Unit unit, string modelName)
    {
        if (!WeaponSpecs.TryGetValue(modelName, out WeaponSpec spec)) return;
        Transform model = unit.transform.Find(modelName + " Model");
        Transform hand = model != null ? model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == spec.hand) : null;
        if (hand == null || hand.parent == null) { Debug.LogWarning("FINALE no hand bone " + spec.hand + " on " + modelName); return; }
        Transform old = hand.Find("Weapon");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Characters/Weapons/" + spec.file + ".glb");
        if (asset == null) { Debug.LogWarning("FINALE missing weapon " + spec.file); return; }
        var weapon = (GameObject)PrefabUtility.InstantiatePrefab(asset, unit.gameObject.scene);
        weapon.name = "Weapon";
        weapon.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        weapon.transform.localScale = Vector3.one;

        Bounds b = Measure(weapon);
        float length = b.size.y;
        float handleEnd = spec.tipAtTop ? b.min.y : b.max.y;
        var gripLocal = new Vector3(b.center.x, handleEnd + (spec.tipAtTop ? 1f : -1f) * spec.grip * length, b.center.z);

        Vector3 arm = (hand.position - hand.parent.position).normalized;
        Vector3 tip = Vector3.ProjectOnPlane(model.forward, arm).normalized;
        Vector3 width = Vector3.ProjectOnPlane(arm, tip).normalized;
        Quaternion turn = Quaternion.LookRotation(Vector3.Cross(width, tip), tip);
        if (!spec.tipAtTop) turn *= Quaternion.Euler(0f, 0f, 180f);
        turn *= Quaternion.AngleAxis(spec.spin, Vector3.up);

        float height = Measure(model.gameObject).size.y;
        float scale = height * spec.length / Mathf.Max(0.001f, length);
        Vector3 fist = hand.position + arm * Vector3.Distance(hand.position, hand.parent.position) * 0.3f;
        weapon.transform.rotation = turn;
        weapon.transform.localScale = Vector3.one * scale;
        weapon.transform.position = fist - turn * (gripLocal * scale);
        weapon.transform.SetParent(hand, true);
        foreach (Collider c in weapon.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        UseCompressedTextures(weapon, spec.file);
        Debug.Log("FINALE " + unit.unitName + " holds " + spec.file + " (" + (length * scale).ToString("F2") + " m)");
    }


    static void UseCompressedTextures(GameObject model, string fileName)
    {
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = r.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = CompressedCopy(materials[i], fileName + "_" + i);
            r.sharedMaterials = materials;
            EditorUtility.SetDirty(r);
        }
    }

    static Material CompressedCopy(Material source, string name)
    {
        if (source == null || !AssetDatabase.GetAssetPath(source).EndsWith(".glb")) return source;
        string path = Materials + name + ".mat";
        Material copy = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (copy != null) return copy;
        copy = new Material(source) { name = name };
        foreach (string property in source.GetTexturePropertyNames())
        {
            if (source.GetTexture(property) is Texture2D texture && AssetDatabase.GetAssetPath(texture).EndsWith(".glb"))
                copy.SetTexture(property, SaveTexture(texture, name + "_" + property));
        }
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    static Texture2D SaveTexture(Texture2D source, string name)
    {
        string path = Textures + name + ".png";
        bool srgb = GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
        if (!File.Exists(path))
        {
            float shrink = Mathf.Min(1f, (float)TextureSize / Mathf.Max(source.width, source.height));
            int w = Mathf.Max(4, Mathf.RoundToInt(source.width * shrink)), h = Mathf.Max(4, Mathf.RoundToInt(source.height * shrink));
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var pixels = new Texture2D(w, h, TextureFormat.RGBA32, false, !srgb);
            pixels.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.sRGBTexture != srgb || importer.maxTextureSize != TextureSize || importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.sRGBTexture = srgb;
            importer.maxTextureSize = TextureSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Bounds Measure(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    static void MakeController(string modelName, Spec spec)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(Models + modelName + ".glb").OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview")).ToArray();
        AnimationClip Clip(string wanted) => clips.FirstOrDefault(c => c.name == wanted) ?? clips.FirstOrDefault(c => c.name.Contains(wanted));
        Debug.Log("FINALE clips in " + modelName + ": " + string.Join(", ", clips.Select(c => c.name + (c.legacy ? " (legacy!)" : "") + " " + c.length.ToString("F1") + "s")));

        string path = Animators + modelName + ".controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idle = machine.AddState("Idle", new Vector3(300f, 0f, 0f));
        idle.motion = Clip(spec.idle);
        idle.speed = spec.idleSpeed;
        machine.defaultState = idle;
        AnimatorState run = machine.AddState("Run", new Vector3(300f, 120f, 0f));
        run.motion = Clip("Running");
        AnimatorState attack = machine.AddState("Attack", new Vector3(550f, -80f, 0f));
        attack.motion = Clip(spec.attack);
        AnimatorState cast = machine.AddState("Cast", new Vector3(550f, 60f, 0f));
        cast.motion = Clip(spec.cast);

        AnimatorStateTransition toRun = idle.AddTransition(run);
        toRun.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
        toRun.hasExitTime = false;
        toRun.duration = 0.1f;
        AnimatorStateTransition stopRun = run.AddTransition(idle);
        stopRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
        stopRun.hasExitTime = false;
        stopRun.duration = 0.12f;
        foreach (var (state, trigger) in new[] { (attack, "Attack"), (cast, "Cast") })
        {
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = false;
            AnimatorStateTransition back = state.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.92f;
            back.duration = 0.15f;
        }
        EditorUtility.SetDirty(controller);
    }
}
