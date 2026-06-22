using UnityEngine;
using UnityEditor;

public class CreateFirewallVFX
{
    [MenuItem("Tools/VFX/Create Firewall VFX")]
    public static void Execute()
    {
        // Create root GameObject
        GameObject root = new GameObject("VFX_Firewall");
        root.transform.position = new Vector3(5f, 0f, 0f);

        // Load existing fire texture
        Texture2D fireTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_MainProject/Art/VFX/Textures/FireParticle.png");
        Texture2D smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_MainProject/Art/VFX/Textures/SmokeParticle.png");
        Texture2D sparkTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_MainProject/Art/VFX/Textures/SparkParticle.png");

        // Load existing materials
        Material fireMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_MainProject/Art/VFX/Materials/FireballFire_Mat.mat");
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_MainProject/Art/VFX/Materials/FireballSmoke_Mat.mat");
        Material sparkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_MainProject/Art/VFX/Materials/FireballSpark_Mat.mat");

        // === MAIN FIRE PARTICLES ===
        ParticleSystem mainFire = root.AddComponent<ParticleSystem>();
        ConfigureMainFire(mainFire, fireMat);

        // === INNER FIRE (brighter core) ===
        GameObject innerFireObj = new GameObject("InnerFire");
        innerFireObj.transform.SetParent(root.transform, false);
        ParticleSystem innerFire = innerFireObj.AddComponent<ParticleSystem>();
        ConfigureInnerFire(innerFire, fireMat);

        // === EMBERS / SPARKS ===
        GameObject embersObj = new GameObject("Embers");
        embersObj.transform.SetParent(root.transform, false);
        ParticleSystem embers = embersObj.AddComponent<ParticleSystem>();
        ConfigureEmbers(embers, sparkMat);

        // === SMOKE (top of wall) ===
        GameObject smokeObj = new GameObject("Smoke");
        smokeObj.transform.SetParent(root.transform, false);
        smokeObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        ParticleSystem smoke = smokeObj.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke, smokeMat);

        // === HEAT GLOW (base) ===
        GameObject glowObj = new GameObject("HeatGlow");
        glowObj.transform.SetParent(root.transform, false);
        glowObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        ParticleSystem glow = glowObj.AddComponent<ParticleSystem>();
        ConfigureHeatGlow(glow, fireMat);

        // Save as prefab
        string prefabFolder = "Assets/_MainProject/Prefabs/VFX";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/_MainProject/Prefabs", "VFX");
        }

        string prefabPath = prefabFolder + "/VFX_Firewall.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);

        Selection.activeGameObject = root;
        Debug.Log("Firewall VFX created successfully at: " + prefabPath);
    }

    static void ConfigureMainFire(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f, 1f),
            new Color(1f, 0.3f, 0f, 1f)
        );
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 60f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 0.3f, 0.3f); // Wide wall shape
        shape.position = new Vector3(0f, 0.5f, 0f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0f), 0.4f),
                new GradientColorKey(new Color(0.8f, 0.1f, 0f), 0.8f),
                new GradientColorKey(new Color(0.3f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Configure velocity using SerializedObject to avoid mode mismatch
        ConfigureVelocityOverLifetime(ps, 
            new Vector2(-0.3f, 0.3f), // X range
            new Vector2(2f, 4f),       // Y range (upward)
            new Vector2(-0.3f, 0.3f)   // Z range
        );

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 2f;
        noise.scrollSpeed = 1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) renderer.material = mat;
        renderer.sortingFudge = 0f;
    }

    static void ConfigureInnerFire(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.5f, 1f),
            new Color(1f, 0.7f, 0.2f, 1f)
        );
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5.5f, 0.2f, 0.15f);
        shape.position = new Vector3(0f, 0.3f, 0f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.7f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.5f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ConfigureVelocityOverLifetime(ps,
            new Vector2(-0.2f, 0.2f),
            new Vector2(3f, 5f),
            new Vector2(-0.2f, 0.2f)
        );

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 3f;
        noise.scrollSpeed = 1.5f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) renderer.material = mat;
        renderer.sortingFudge = -1f;
    }

    static void ConfigureEmbers(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.2f, 1f),
            new Color(1f, 0.5f, 0f, 1f)
        );
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 1.5f, 0.3f);
        shape.position = new Vector3(0f, 1f, 0f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ConfigureVelocityOverLifetime(ps,
            new Vector2(-1f, 1f),
            new Vector2(3f, 6f),
            new Vector2(-1f, 1f)
        );

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1f;
        noise.frequency = 2f;
        noise.scrollSpeed = 0.5f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) renderer.material = mat;
    }

    static void ConfigureSmoke(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.3f, 0.3f, 0.3f, 0.4f),
            new Color(0.15f, 0.1f, 0.05f, 0.3f)
        );
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 0.5f, 0.5f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 1.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.4f, 0.2f, 0.05f), 0f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.2f),
                new GradientAlphaKey(0.2f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ConfigureVelocityOverLifetime(ps,
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, 1.5f),
            new Vector2(-0.5f, 0.5f)
        );

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 1f;
        noise.scrollSpeed = 0.3f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) renderer.material = mat;
        renderer.sortingFudge = 1f;
    }

    static void ConfigureHeatGlow(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.3f, 0f, 0.3f),
            new Color(1f, 0.5f, 0f, 0.2f)
        );
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 0.1f, 0.5f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.4f, 0.3f),
                new GradientAlphaKey(0.2f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) renderer.material = mat;
        renderer.sortingFudge = 2f;
    }

    static void ConfigureVelocityOverLifetime(ParticleSystem ps, Vector2 xRange, Vector2 yRange, Vector2 zRange)
    {
        // Use SerializedObject to set velocity over lifetime to avoid mode mismatch errors
        var so = new SerializedObject(ps);
        
        // Enable the module
        var velModule = so.FindProperty("VelocityModule");
        velModule.FindPropertyRelative("enabled").boolValue = true;
        
        // Set all axes to RandomBetweenTwoConstants mode (3)
        var xProp = velModule.FindPropertyRelative("x");
        var yProp = velModule.FindPropertyRelative("y");
        var zProp = velModule.FindPropertyRelative("z");
        
        // Set minMaxState for all to same mode (3 = RandomBetweenTwoConstants for MinMaxCurve)
        xProp.FindPropertyRelative("minMaxState").intValue = 3;
        yProp.FindPropertyRelative("minMaxState").intValue = 3;
        zProp.FindPropertyRelative("minMaxState").intValue = 3;
        
        // Set scalar values
        xProp.FindPropertyRelative("scalar").floatValue = xRange.y;
        yProp.FindPropertyRelative("scalar").floatValue = yRange.y;
        zProp.FindPropertyRelative("scalar").floatValue = zRange.y;
        
        // Set min scalar values
        xProp.FindPropertyRelative("minScalar").floatValue = xRange.x;
        yProp.FindPropertyRelative("minScalar").floatValue = yRange.x;
        zProp.FindPropertyRelative("minScalar").floatValue = zRange.x;
        
        so.ApplyModifiedProperties();
    }
}
