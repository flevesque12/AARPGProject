using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateIgnisAssets
{
    public static void Execute()
    {
        EnsureFolders();
        CreateSkillAssets();
        CreateVFXPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ignis] 4 SkillData assets + 4 VFX prefabs créés avec succès.");
    }

    // ========================================
    // DOSSIERS
    // ========================================

    static void EnsureFolders()
    {
        string[] folders = {
            "Assets/_MainProject/Data",
            "Assets/_MainProject/Data/Skills",
            "Assets/_MainProject/Prefabs",
            "Assets/_MainProject/Prefabs/VFX",
            "Assets/_MainProject/Prefabs/VFX/Ignis",
        };
        foreach (string path in folders)
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string child  = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }

    // ========================================
    // SCRIPTABLE OBJECTS
    // ========================================

    static void CreateSkillAssets()
    {
        Color ignis = new Color(1f, 0.35f, 0.05f);

        // ── Trait de Braise ──────────────────────────────
        var braise = MakeSkill(
            "Ignis_TraitDeBraise",
            "Trait de Braise",
            "Projectile de braise rapide qui traverse les ennemis sur sa trajectoire.",
            SkillSchool.Ignis,
            SkillType.Projectile,
            stamina: 15f, cooldown: 0.8f,
            damage: 35f, range: 10f, radius: 0f,
            castTime: 0f, duration: 0f, tickInterval: 0f,
            projSpeed: 20f, projSize: 0.25f,
            color: ignis
        );
        Save(braise, "Ignis_TraitDeBraise");

        // ── Explosion Ignis ───────────────────────────────
        var explosion = MakeSkill(
            "Ignis_Explosion",
            "Explosion Ignis",
            "Déclenche une explosion au point visé après un court délai.",
            SkillSchool.Ignis,
            SkillType.AoE,
            stamina: 25f, cooldown: 2f,
            damage: 55f, range: 8f, radius: 2.5f,
            castTime: 0.35f, duration: 0f, tickInterval: 0f,
            projSpeed: 0f, projSize: 0f,
            color: new Color(1f, 0.5f, 0f)
        );
        Save(explosion, "Ignis_Explosion");

        // ── Mur de Feu ────────────────────────────────────
        var mur = MakeSkill(
            "Ignis_MurDeFeu",
            "Mur de Feu",
            "Invoque une zone de flammes persistante qui brûle les ennemis à l'intérieur.",
            SkillSchool.Ignis,
            SkillType.PersistentZone,
            stamina: 30f, cooldown: 5f,
            damage: 12f, range: 8f, radius: 2f,
            castTime: 0f, duration: 4f, tickInterval: 0.4f,
            projSpeed: 0f, projSize: 0f,
            color: new Color(1f, 0.2f, 0f)
        );
        Save(mur, "Ignis_MurDeFeu");

        // ── Météore Ignis ─────────────────────────────────
        var meteore = MakeSkill(
            "Ignis_Meteore",
            "Météore Ignis",
            "Invoque un météore massif au point visé après un long telegraph.",
            SkillSchool.Ignis,
            SkillType.DelayedAoE,
            stamina: 40f, cooldown: 8f,
            damage: 120f, range: 10f, radius: 3.5f,
            castTime: 1.2f, duration: 0f, tickInterval: 0f,
            projSpeed: 0f, projSize: 0f,
            color: new Color(1f, 0.15f, 0f)
        );
        Save(meteore, "Ignis_Meteore");
    }

    static SkillData MakeSkill(
        string fileName, string skillName, string desc,
        SkillSchool school, SkillType type,
        float stamina, float cooldown,
        float damage, float range, float radius,
        float castTime, float duration, float tickInterval,
        float projSpeed, float projSize,
        Color color)
    {
        var s = ScriptableObject.CreateInstance<SkillData>();
        s.skillName    = skillName;
        s.description  = desc;
        s.school       = school;
        s.skillType    = type;
        s.staminaCost  = stamina;
        s.cooldown     = cooldown;
        s.baseDamage   = damage;
        s.range        = range;
        s.radius       = radius;
        s.castTime     = castTime;
        s.duration     = duration;
        s.tickInterval = tickInterval;
        s.projectileSpeed = projSpeed;
        s.projectileSize  = projSize;
        s.skillColor   = color;
        return s;
    }

    static void Save(SkillData asset, string fileName)
    {
        string path = $"Assets/_MainProject/Data/Skills/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    // ========================================
    // VFX PREFABS (Particle Systems)
    // ========================================

    static void CreateVFXPrefabs()
    {
        CreateProjectileVFX();
        CreateExplosionVFX();
        CreateZoneVFX();
        CreateMeteoreVFX();
        WireVFXToAssets();
    }

    // ── Trait de Braise : boule de feu rapide ──────────
    static void CreateProjectileVFX()
    {
        GameObject root = new GameObject("VFX_Ignis_TraitDeBraise");

        // Traîne de particules
        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop              = true;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
                                    new Color(1f, 0.8f, 0.1f),
                                    new Color(1f, 0.3f, 0f));
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.maxParticles      = 80;

        var emission = ps.emission;
        emission.rateOverTime  = 60f;

        var shape = ps.shape;
        shape.enabled  = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius   = 0.12f;

        SetRendererMaterial(ps, new Color(1f, 0.5f, 0.05f));
        AddColorOverLifetime(ps, new Color(1f, 0.7f, 0.1f, 1f), new Color(0.8f, 0.1f, 0f, 0f));

        // Sphère brillante (core)
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(root.transform, false);
        core.transform.localScale = Vector3.one * 0.25f;
        Object.DestroyImmediate(core.GetComponent<SphereCollider>());
        SetMeshColor(core, new Color(1f, 0.9f, 0.3f));

        SavePrefab(root, "VFX_Ignis_TraitDeBraise");
    }

    // ── Explosion Ignis ────────────────────────────────
    static void CreateExplosionVFX()
    {
        GameObject root = new GameObject("VFX_Ignis_Explosion");

        // Burst principal
        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                    new Color(1f, 0.8f, 0.1f),
                                    new Color(1f, 0.3f, 0f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 120;
        main.stopAction      = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.2f;

        SetRendererMaterial(ps, new Color(1f, 0.4f, 0f));
        AddColorOverLifetime(ps, new Color(1f, 0.6f, 0.1f, 1f), new Color(0.4f, 0.05f, 0f, 0f));
        AddSizeOverLifetime(ps, 1f, 0f);

        // Braises montantes
        GameObject embers = new GameObject("Embers");
        embers.transform.SetParent(root.transform, false);
        ParticleSystem emberPS = embers.AddComponent<ParticleSystem>();
        var eMain = emberPS.main;
        eMain.loop          = false;
        eMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        eMain.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 6f);
        eMain.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        eMain.startColor    = new Color(1f, 0.6f, 0.1f);
        eMain.gravityModifier = new ParticleSystem.MinMaxCurve(-0.5f);
        eMain.simulationSpace = ParticleSystemSimulationSpace.World;
        eMain.maxParticles  = 60;
        eMain.stopAction    = ParticleSystemStopAction.Destroy;
        var eEmission = emberPS.emission;
        eEmission.rateOverTime = 0f;
        eEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        SetRendererMaterial(emberPS, new Color(1f, 0.5f, 0.05f));
        AddColorOverLifetime(emberPS, new Color(1f, 0.5f, 0.1f, 1f), new Color(0.5f, 0.05f, 0f, 0f));

        SavePrefab(root, "VFX_Ignis_Explosion");
    }

    // ── Mur de Feu : flammes persistantes ─────────────
    static void CreateZoneVFX()
    {
        GameObject root = new GameObject("VFX_Ignis_MurDeFeu");

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                    new Color(1f, 0.7f, 0.05f),
                                    new Color(1f, 0.2f, 0f));
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.8f); // monte vers le haut
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 200;

        var emission = ps.emission;
        emission.rateOverTime = 80f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius    = 1.8f;
        shape.donutRadius = 0.3f;

        SetRendererMaterial(ps, new Color(1f, 0.35f, 0f));
        AddColorOverLifetime(ps,
            new Color(1f, 0.7f, 0.1f, 0.9f),
            new Color(0.6f, 0.05f, 0f, 0f));
        AddSizeOverLifetime(ps, 0.3f, 1f);

        SavePrefab(root, "VFX_Ignis_MurDeFeu");
    }

    // ── Météore Ignis : impact massif ─────────────────
    static void CreateMeteoreVFX()
    {
        GameObject root = new GameObject("VFX_Ignis_Meteore");

        // Impact blast
        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                    new Color(1f, 0.9f, 0.2f),
                                    new Color(1f, 0.2f, 0f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 200;
        main.stopAction      = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 150) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.5f;

        SetRendererMaterial(ps, new Color(1f, 0.3f, 0f));
        AddColorOverLifetime(ps,
            new Color(1f, 0.8f, 0.1f, 1f),
            new Color(0.3f, 0.03f, 0f, 0f));
        AddSizeOverLifetime(ps, 1f, 0f);

        // Fragments rocheux
        GameObject rocks = new GameObject("RockFragments");
        rocks.transform.SetParent(root.transform, false);
        ParticleSystem rockPS = rocks.AddComponent<ParticleSystem>();
        var rMain = rockPS.main;
        rMain.loop            = false;
        rMain.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        rMain.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 8f);
        rMain.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        rMain.startColor      = new ParticleSystem.MinMaxGradient(
                                    new Color(0.9f, 0.4f, 0.05f),
                                    new Color(0.5f, 0.1f, 0f));
        rMain.gravityModifier = new ParticleSystem.MinMaxCurve(1.5f);
        rMain.simulationSpace = ParticleSystemSimulationSpace.World;
        rMain.maxParticles    = 60;
        rMain.stopAction      = ParticleSystemStopAction.Destroy;
        var rEmission = rockPS.emission;
        rEmission.rateOverTime = 0f;
        rEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        var rShape = rockPS.shape;
        rShape.enabled   = true;
        rShape.shapeType = ParticleSystemShapeType.Hemisphere;
        rShape.radius    = 0.3f;
        SetRendererMaterial(rockPS, new Color(0.7f, 0.3f, 0.05f));
        AddColorOverLifetime(rockPS,
            new Color(1f, 0.4f, 0.05f, 1f),
            new Color(0.3f, 0.1f, 0.02f, 0f));

        SavePrefab(root, "VFX_Ignis_Meteore");
    }

    // ========================================
    // WIRE VFX → SKILL ASSETS
    // ========================================

    static void WireVFXToAssets()
    {
        // Trait de Braise : projectilePrefab
        var braise = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/_MainProject/Data/Skills/Ignis_TraitDeBraise.asset");
        var braiseVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MainProject/Prefabs/VFX/Ignis/VFX_Ignis_TraitDeBraise.prefab");
        if (braise != null && braiseVFX != null)
        {
            braise.projectilePrefab = braiseVFX;
            EditorUtility.SetDirty(braise);
        }

        // Explosion : impactVFXPrefab
        var expl = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/_MainProject/Data/Skills/Ignis_Explosion.asset");
        var explVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MainProject/Prefabs/VFX/Ignis/VFX_Ignis_Explosion.prefab");
        if (expl != null && explVFX != null)
        {
            expl.impactVFXPrefab = explVFX;
            EditorUtility.SetDirty(expl);
        }

        // Mur de Feu : zonePrefab
        var mur = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/_MainProject/Data/Skills/Ignis_MurDeFeu.asset");
        var murVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MainProject/Prefabs/VFX/Ignis/VFX_Ignis_MurDeFeu.prefab");
        if (mur != null && murVFX != null)
        {
            mur.zonePrefab = murVFX;
            EditorUtility.SetDirty(mur);
        }

        // Météore : impactVFXPrefab
        var met = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/_MainProject/Data/Skills/Ignis_Meteore.asset");
        var metVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MainProject/Prefabs/VFX/Ignis/VFX_Ignis_Meteore.prefab");
        if (met != null && metVFX != null)
        {
            met.impactVFXPrefab = metVFX;
            EditorUtility.SetDirty(met);
        }
    }

    // ========================================
    // UTILS
    // ========================================

    static void SetRendererMaterial(ParticleSystem ps, Color color)
    {
        var rend = ps.GetComponent<ParticleSystemRenderer>();

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);

        // Blending additif — rendu de feu plus lumineux
        if (mat.HasProperty("_BlendMode"))  mat.SetFloat("_BlendMode", 4f); // Additive
        if (mat.HasProperty("_SrcBlend"))   mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))   mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetOverrideTag("RenderType", "Transparent");

        rend.material = mat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
    }

    static void AddColorOverLifetime(ParticleSystem ps, Color start, Color end)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);
    }

    static void AddSizeOverLifetime(ParticleSystem ps, float startSize, float endSize)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = AnimationCurve.Linear(0f, startSize, 1f, endSize);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    static void SetMeshColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        r.sharedMaterial = mat;
    }

    static void SavePrefab(GameObject go, string name)
    {
        string path = $"Assets/_MainProject/Prefabs/VFX/Ignis/{name}.prefab";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), path)))
            AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
