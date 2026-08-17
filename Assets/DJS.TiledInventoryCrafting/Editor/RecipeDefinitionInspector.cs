using System.Text;
using DJS.TiledInventoryCrafting;
using UnityEditor;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    [CustomEditor(typeof(RecipeDefinition))]
    public class RecipeDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var recipe = (RecipeDefinition)target;

            var sb = new StringBuilder();
            sb.AppendLine($"Summary: {recipe.GetSummary()}");
            sb.AppendLine($"Time: {recipe.CraftTime}s · Level: {recipe.LevelRequirement}");
            if (recipe.GoldCost > 0 || recipe.XpCost > 0 || recipe.SpecialCosts.Count > 0)
                sb.AppendLine($"Costs: {recipe.GoldCost} gold, {recipe.XpCost} XP, {recipe.SpecialCosts.Count} special");
            sb.AppendLine($"Failure: {recipe.FailureChance:P0} (skill-reduced) · Cooldown: {recipe.CooldownSeconds}s");
            EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.Info);

            if (recipe.Outputs.Count == 0)
                EditorGUILayout.HelpBox("This recipe produces nothing — add at least one output.", MessageType.Error);
            if (recipe.Inputs.Count == 0 && recipe.SpecialCosts.Count == 0)
                EditorGUILayout.HelpBox("This recipe costs nothing. It will craft for free.", MessageType.Warning);
            foreach (var input in recipe.Inputs)
                if (input.item == null)
                {
                    EditorGUILayout.HelpBox("An input references a missing item.", MessageType.Error);
                    break;
                }
            foreach (var output in recipe.Outputs)
                if (output.item == null)
                {
                    EditorGUILayout.HelpBox("An output references a missing item.", MessageType.Error);
                    break;
                }
            if (recipe.FailureChance > 0f && recipe.LevelRequirement <= 0)
                EditorGUILayout.HelpBox("Failure chance is set but there is no level requirement to gate it.", MessageType.Warning);

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
