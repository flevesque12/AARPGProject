using UnityEngine;
using UnityEditor;

public class CreateFirewallSkill
{
    [MenuItem("Tools/Skills/Create Firewall Skill")]
    public static void Execute()
    {
        // Create the SkillData asset
        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillName = "Firewall";
        skill.description = "Conjures a blazing wall of fire that burns enemies who pass through it.";
        skill.school = SkillSchool.Ignis;
        skill.skillType = SkillType.PersistentZone;
        skill.staminaCost = 25f;
        skill.cooldown = 6f;
        skill.baseDamage = 10f;
        skill.range = 8f;
        skill.radius = 3f;
        skill.castTime = 0f;
        skill.duration = 5f;
        skill.tickInterval = 0.5f;
        skill.projectileSpeed = 0f;
        skill.projectileSize = 0f;
        skill.skillColor = new Color(1f, 0.4f, 0f, 1f);

        // Assign the VFX_Firewall prefab as the zone prefab
        GameObject firewallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MainProject/Prefabs/VFX/VFX_Firewall.prefab");
        if (firewallPrefab != null)
        {
            skill.zonePrefab = firewallPrefab;
            Debug.Log("Assigned VFX_Firewall prefab to zonePrefab");
        }
        else
        {
            Debug.LogWarning("Could not find VFX_Firewall.prefab!");
        }

        // Save the asset
        string assetPath = "Assets/_MainProject/Data/Skills/Ignis_Firewall.asset";
        AssetDatabase.CreateAsset(skill, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Created Firewall skill asset at: " + assetPath);

        // Now assign it to the Player's SkillCaster slot
        // Find the Player in the scene
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player not found in scene!");
            return;
        }

        SkillCaster caster = player.GetComponent<SkillCaster>();
        if (caster == null)
        {
            Debug.LogError("SkillCaster not found on Player!");
            return;
        }

        // Use SerializedObject to assign to a skill slot
        SerializedObject so = new SerializedObject(caster);
        SerializedProperty slotsProperty = so.FindProperty("_slots");

        if (slotsProperty == null || !slotsProperty.isArray)
        {
            Debug.LogError("Could not find _slots property on SkillCaster!");
            return;
        }

        // Find the first empty slot, or use slot index 0 if all are filled
        int targetSlot = -1;
        for (int i = 0; i < slotsProperty.arraySize; i++)
        {
            SerializedProperty element = slotsProperty.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue == null)
            {
                targetSlot = i;
                break;
            }
        }

        if (targetSlot == -1)
        {
            // All slots filled, add to the last slot
            targetSlot = slotsProperty.arraySize - 1;
            Debug.Log("All skill slots occupied. Replacing slot " + (targetSlot + 1));
        }

        slotsProperty.GetArrayElementAtIndex(targetSlot).objectReferenceValue = skill;
        so.ApplyModifiedProperties();

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"Assigned Firewall skill to Player's skill slot {targetSlot + 1} (key {targetSlot + 1})");
    }
}
