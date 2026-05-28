using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

public class FixHeroModelHeight
{
    public static void Execute()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Fix] Player introuvable."); return; }

        Transform heroModel = player.transform.Find("HeroModel");
        if (heroModel == null) { Debug.LogError("[Fix] HeroModel introuvable."); return; }

        // Le NavMeshAgent élève le Player de baseOffset au runtime.
        // HeroModel doit être décalé de -baseOffset en local pour que ses pieds
        // tombent au Y=0 mondial quand le Player est à Y=baseOffset.
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        float baseOffset = agent != null ? agent.baseOffset : 1.0f;
        float targetLocalY = -baseOffset;

        heroModel.localPosition = new Vector3(0f, targetLocalY, 0f);

        Debug.Log($"[Fix] baseOffset={baseOffset:F3} → HeroModel localY={targetLocalY:F3}");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, scene.path);
        Debug.Log($"[Fix] Scène sauvegardée ({saved}) : {scene.path}");
    }
}
