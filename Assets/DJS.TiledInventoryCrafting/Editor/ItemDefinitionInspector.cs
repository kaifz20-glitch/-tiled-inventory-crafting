using DJS.TiledInventoryCrafting;
using UnityEditor;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    [CustomEditor(typeof(ItemDefinition))]
    public class ItemDefinitionInspector : UnityEditor.Editor
    {
        private SerializedProperty id;
        private SerializedProperty displayName;
        private SerializedProperty icon;
        private SerializedProperty maxStack;

        private void OnEnable()
        {
            id = serializedObject.FindProperty("id");
            displayName = serializedObject.FindProperty("displayName");
            icon = serializedObject.FindProperty("icon");
            maxStack = serializedObject.FindProperty("maxStack");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var item = (ItemDefinition)target;

            EditorGUILayout.HelpBox(
                $"Id: {item.Id}\nCategory: {item.Category} · Rarity: {item.Rarity}\n" +
                (item.CanEquip ? $"Equips to: {item.EquippableSlot}\n" : "") +
                (item.Stats.Count > 0 ? $"Damage: {item.GetStat(StatType.Damage)} · Armor: {item.GetStat(StatType.Armor)}\n" : "") +
                "Referenced by save files via its stable Id — keep it unique.",
                MessageType.Info);

            if (icon.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No icon assigned. The UI will draw a rarity-colored placeholder.", MessageType.Warning);
            if (maxStack.intValue < 1)
                EditorGUILayout.HelpBox("Max stack must be at least 1.", MessageType.Error);

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
