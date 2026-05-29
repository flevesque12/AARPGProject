using UnityEngine;
using UnityEditor;

public class CreateDirectionArrowSetup
{
    public static void Execute()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("Player introuvable"); return; }

        // Supprimer un éventuel ancien DirectionArrow
        var existing = player.transform.Find("DirectionArrow");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_MainProject/Materials/DirectionArrow.mat");

        // Parent vide — positionné au niveau des pieds (comme HeroModel)
        var arrowRoot = new GameObject("DirectionArrow");
        arrowRoot.transform.SetParent(player.transform, false);
        arrowRoot.transform.localPosition = new Vector3(0f, -1f, 0f);

        // Bras gauche — pointe vers +Z (direction mouvement), base évasée vers -Z
        // Tip convergence à z≈0.65, base à (-0.30, 0, 0.30)
        var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = "ArmLeft";
        left.transform.SetParent(arrowRoot.transform, false);
        left.transform.localPosition = new Vector3(-0.15f, 0.02f, 0.475f);
        left.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
        left.transform.localScale = new Vector3(0.07f, 0.04f, 0.46f);
        left.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(left.GetComponent<BoxCollider>());

        // Bras droit — symétrique
        var right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = "ArmRight";
        right.transform.SetParent(arrowRoot.transform, false);
        right.transform.localPosition = new Vector3(0.15f, 0.02f, 0.475f);
        right.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
        right.transform.localScale = new Vector3(0.07f, 0.04f, 0.46f);
        right.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(right.GetComponent<BoxCollider>());

        // Script de contrôle
        arrowRoot.AddComponent<MovementDirectionArrow>();

        // Démarrer à scale 0 (invisible au repos)
        arrowRoot.transform.localScale = Vector3.zero;

        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("DirectionArrow créé avec succès !");
    }
}
