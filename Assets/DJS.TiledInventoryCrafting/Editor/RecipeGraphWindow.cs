using System.Collections.Generic;
using System.IO;
using System.Linq;
using TiledInventory;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TiledInventory.EditorTools
{
    /// <summary>
    /// Phase 2 visual recipe editor. Define recipes by dragging nodes instead of editing
    /// ScriptableObjects: an <see cref="ItemSourceNode"/> (an item + count) connects into a
    /// <see cref="RecipeNode"/>'s input; the recipe node holds craft parameters and outputs.
    /// A live preview shows totals and warnings; "Export" writes production-ready
    /// <see cref="RecipeDefinition"/> assets.
    ///
    /// The whole editor is themed to match the runtime UI (see <see cref="RecipeGraphTheme"/>):
    /// it picks up the palette of an <see cref="InventoryCraftingUI"/> in the open scene, or
    /// falls back to the default <see cref="RarityPalette"/>.
    /// </summary>
    public class RecipeGraphWindow : EditorWindow
    {
        private RecipeGraphView graphView;
        private Label previewLabel;

        [MenuItem("Tools/Tiled Inventory/Recipe Graph Editor")]
        public static void Open()
        {
            var window = GetWindow<RecipeGraphWindow>();
            window.titleContent = new GUIContent("Recipe Graph");
            window.minSize = new Vector2(700f, 450f);
            window.Show();
        }

        public RecipeGraphView GraphView => graphView;
        public string PreviewText => previewLabel?.text;

        private void OnEnable()
        {
            BuildWindow();
        }

        private void BuildWindow()
        {
            RecipeGraphTheme.FromScene();
            rootVisualElement.Clear();

            var toolbar = new IMGUIContainer(DrawToolbar);
            rootVisualElement.Add(toolbar);

            graphView = new RecipeGraphView { name = "RecipeGraph" };
            graphView.style.flexGrow = 1f;
            graphView.OnGraphChanged += RefreshPreview;
            rootVisualElement.Add(graphView);

            // themed preview bar: accent top border, accent header, live summary below
            var preview = new VisualElement();
            preview.style.backgroundColor = RecipeGraphTheme.Palette.panelBackground;
            preview.style.borderTopWidth = 2f;
            preview.style.borderTopColor = RecipeGraphTheme.Palette.accent;
            preview.style.paddingTop = 6f;
            preview.style.paddingBottom = 6f;
            preview.style.paddingLeft = 10f;
            preview.style.paddingRight = 10f;

            var header = new Label("Recipe Preview");
            header.style.color = RecipeGraphTheme.Palette.accent;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 12f;
            header.style.marginBottom = 2f;
            preview.Add(header);

            previewLabel = new Label("No recipes yet — add an item node and a recipe node, then connect them.");
            previewLabel.enableRichText = true;
            previewLabel.style.color = RecipeGraphTheme.Palette.textPrimary;
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            preview.Add(previewLabel);

            rootVisualElement.Add(preview);

            RefreshPreview();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New Item Node", EditorStyles.toolbarButton))
                graphView.AddItemSourceNode(new Vector2(80f, 80f));
            if (GUILayout.Button("New Recipe Node", EditorStyles.toolbarButton))
                graphView.AddRecipeNode(new Vector2(420f, 80f));
            GUILayout.Space(8f);
            if (GUILayout.Button("Remove Selected", EditorStyles.toolbarButton))
                graphView.RemoveSelection();
            GUILayout.FlexibleSpace();
            GUILayout.Label("drag ports to connect · scroll to zoom · drag canvas to pan", EditorStyles.miniLabel);
            if (GUILayout.Button("Export Recipes…", EditorStyles.toolbarButton))
                ExportRecipes();
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshPreview()
        {
            if (previewLabel == null || graphView == null) return;
            var recipes = graphView.GetRecipeNodes();
            if (recipes.Count == 0)
            {
                previewLabel.style.color = RecipeGraphTheme.Palette.textSecondary;
                previewLabel.text = "No recipes yet — add an item node and a recipe node, then connect them.";
                return;
            }
            previewLabel.style.color = RecipeGraphTheme.Palette.textPrimary;
            var lines = new List<string>();
            foreach (var node in recipes)
            {
                var inputs = node.GetInputStacks();
                var outputs = node.GetOutputStacks();
                string summary = inputs.Count == 0
                    ? "(no inputs)"
                    : string.Join(" + ", inputs.Select(s => $"{s.count} {s.item?.name ?? "?"}"));
                string outs = outputs.Count == 0
                    ? "(no outputs)"
                    : string.Join(" + ", outputs.Select(s => $"{s.count} {s.item?.name ?? "?"}"));
                string flags = "";
                if (inputs.Count == 0) flags += " <color=#FF6B6B>⚠ no inputs</color>";
                if (outputs.Count == 0) flags += " <color=#FF6B6B>⚠ no outputs</color>";
                if (node.FailureChance > 0f) flags += $" · fail {node.FailureChance:P0}";
                if (node.Cooldown > 0f) flags += $" · cd {node.Cooldown}s";
                if (node.GoldCost > 0f || node.XpCost > 0f) flags += $" · {node.GoldCost}g/{node.XpCost}xp";
                lines.Add($"{node.RecipeName}: {summary} → {outs}  ({node.CraftTime}s, lvl {node.Level}){flags}");
            }
            previewLabel.text = string.Join("\n", lines);
        }

        // ------------------------------------------------------------------ export

        private void ExportRecipes()
        {
            var nodes = graphView.GetRecipeNodes();
            if (nodes.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Recipes", "There are no recipe nodes to export.", "OK");
                return;
            }

            string folder = EditorUtility.SaveFolderPanel("Export Recipes", DemoContentBuilder.RecipesFolder, "");
            if (string.IsNullOrEmpty(folder)) return;

            string relative = ToRelativePath(folder);
            if (string.IsNullOrEmpty(relative))
            {
                EditorUtility.DisplayDialog("Export Recipes", "Choose a folder inside the project (Assets/...).", "OK");
                return;
            }
            DemoContentBuilder.EnsureFolders();
            EnsureFolder(relative);

            int created = 0;
            foreach (var node in nodes)
            {
                var recipe = node.BuildDefinition();
                if (recipe == null)
                {
                    EditorUtility.DisplayDialog("Export Recipes", $"Recipe '{node.RecipeName}' is missing required fields.", "OK");
                    continue;
                }
                string name = Sanitize(node.RecipeName);
                string path = $"{relative}/{name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (existing != null)
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(recipe, path);
                EditorUtility.SetDirty(recipe);
                created++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[TiledInventory] Exported {created} recipe asset(s) to {relative}.");
            RefreshPreview();
        }

        private static string ToRelativePath(string absolute)
        {
            string full = Path.GetFullPath(absolute).Replace('\\', '/');
            string assets = Path.GetFullPath("Assets/").Replace('\\', '/');
            if (!full.StartsWith(assets)) return null;
            return "Assets/" + full.Substring(assets.Length);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string Sanitize(string name)
        {
            string clean = string.IsNullOrEmpty(name) ? "Recipe" : name;
            foreach (char c in Path.GetInvalidFileNameChars())
                clean = clean.Replace(c, '_');
            return clean;
        }
    }

    // ================================================================== graph view

    public class RecipeGraphView : GraphView
    {
        public event System.Action OnGraphChanged;

        public RecipeGraphView()
        {
            // NOTE: extension methods must use an explicit receiver here — implicit `this`
            // lookup does not resolve them inside this assembly (same pattern as Unity's ShaderGraph).
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.SetupZoom(0.25f, 2.5f);

            style.backgroundColor = RecipeGraphTheme.Canvas;

            // themed canvas: replaces the default GridBackground so the grid lines and
            // canvas color follow the runtime palette (no USS assets required).
            Insert(0, new ThemedGridBackground(this, RecipeGraphTheme.Canvas, RecipeGraphTheme.GridLine, RecipeGraphTheme.GridMajor));

            graphViewChanged = OnGraphViewChanged;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            if (changes.edgesToCreate != null)
                foreach (var edge in changes.edgesToCreate)
                {
                    RecipeGraphTheme.StyleEdge(edge);
                    (edge.input?.node as RecipeNode)?.SyncInputPort();
                }
            if (changes.elementsToRemove != null)
                foreach (var element in changes.elementsToRemove)
                {
                    if (element is Edge edge && edge.input?.node is RecipeNode recipeNode)
                        recipeNode.SyncInputPort();
                    if (element is ItemSourceNode || element is RecipeNode)
                        continue;
                }
            OnGraphChanged?.Invoke();
            return changes;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;
                compatible.Add(port);
            });
            return compatible;
        }

        public void AddItemSourceNode(Vector2 position)
        {
            var node = new ItemSourceNode();
            node.SetPosition(new Rect(position, new Vector2(220f, 120f)));
            node.ContentChanged += () => OnGraphChanged?.Invoke();
            AddElement(node);
            OnGraphChanged?.Invoke();
        }

        public void AddRecipeNode(Vector2 position)
        {
            var node = new RecipeNode();
            node.SetPosition(new Rect(position, new Vector2(300f, 280f)));
            node.ContentChanged += () => OnGraphChanged?.Invoke();
            AddElement(node);
            OnGraphChanged?.Invoke();
        }

        public void RemoveSelection()
        {
            var selected = selection.OfType<GraphElement>().ToList();
            foreach (var element in selected)
            {
                if (element is Edge edge)
                {
                    RemoveElement(edge);
                    (edge.input?.node as RecipeNode)?.SyncInputPort();
                }
                else
                {
                    RemoveElement(element);
                }
            }
            OnGraphChanged?.Invoke();
        }

        public List<RecipeNode> GetRecipeNodes()
        {
            return nodes.OfType<RecipeNode>().ToList();
        }

        public List<ItemSourceNode> GetItemSourceNodes()
        {
            return nodes.OfType<ItemSourceNode>().ToList();
        }

        public List<Edge> GetEdges() => edges.ToList();

        /// <summary>Item sources feeding a recipe node's input port.</summary>
        public List<ItemSourceNode> GetSourcesFor(RecipeNode recipeNode)
        {
            var sources = new List<ItemSourceNode>();
            foreach (var edge in edges.ToList())
            {
                if (edge.input?.node == recipeNode && edge.output?.node is ItemSourceNode source)
                    sources.Add(source);
            }
            return sources;
        }
    }

    // ================================================================== item node

    public class ItemSourceNode : Node
    {
        private readonly ObjectField itemField = new ObjectField();
        private readonly IntegerField countField = new IntegerField { value = 1 };
        public readonly Port OutputPort;

        public ItemSourceNode()
        {
            title = "Item";
            capabilities |= Capabilities.Movable | Capabilities.Selectable | Capabilities.Deletable;

            RecipeGraphTheme.StyleNode(this,
                Color.Lerp(RecipeGraphTheme.Palette.panelHeaderTop, RecipeGraphTheme.Palette.rare, 0.25f),
                RecipeGraphTheme.Palette.rare);

            itemField.objectType = typeof(ItemDefinition);
            itemField.label = "Item";
            itemField.RegisterValueChangedCallback(_ => OnContentChanged());
            countField.label = "Count";
            countField.RegisterValueChangedCallback(_ => OnContentChanged());

            contentContainer.Add(itemField);
            contentContainer.Add(countField);

            OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(object));
            OutputPort.portName = "out";
            RecipeGraphTheme.StylePort(OutputPort, RecipeGraphTheme.Palette.accent);
            outputContainer.Add(OutputPort);

            RefreshExpandedState();
        }

        public ItemDefinition Item => itemField.value as ItemDefinition;
        public int Count => Mathf.Max(1, countField.value);

        public event System.Action ContentChanged;

        private void OnContentChanged()
        {
            OutputPort.portName = $"{Item?.name ?? "?"} x{Count}";
            RefreshPorts();
            // underline follows the item's rarity color when one is assigned
            var item = Item;
            RecipeGraphTheme.SetAccent(this, item != null ? RarityColors.Get(item.Rarity) : RecipeGraphTheme.Palette.rare);
            ContentChanged?.Invoke();
        }
    }

    // ================================================================== recipe node

    public class RecipeNode : Node
    {
        private readonly TextField nameField = new TextField { value = "New Recipe" };
        private readonly FloatField craftTimeField = new FloatField { value = 2f, label = "Craft time (s)" };
        private readonly IntegerField levelField = new IntegerField { value = 1, label = "Level req" };
        private readonly IntegerField goldField = new IntegerField { value = 0, label = "Gold cost" };
        private readonly IntegerField xpField = new IntegerField { value = 0, label = "XP cost" };
        private readonly FloatField cooldownField = new FloatField { value = 0f, label = "Cooldown (s)" };
        private readonly FloatField failureField = new FloatField { value = 0f, label = "Failure chance" };
        private readonly VisualElement outputsContainer = new VisualElement();
        private readonly List<OutputRow> outputRows = new List<OutputRow>();
        public readonly Port InputPort;

        public event System.Action ContentChanged;

        public RecipeNode()
        {
            title = "Recipe";
            capabilities |= Capabilities.Movable | Capabilities.Selectable | Capabilities.Deletable;

            RecipeGraphTheme.StyleNode(this, RecipeGraphTheme.Palette.panelHeaderTop, RecipeGraphTheme.Palette.accent);

            nameField.RegisterValueChangedCallback(_ =>
            {
                title = string.IsNullOrEmpty(nameField.value) ? "Recipe" : nameField.value;
                ContentChanged?.Invoke();
            });
            craftTimeField.RegisterValueChangedCallback(_ => ContentChanged?.Invoke());
            levelField.RegisterValueChangedCallback(_ => ContentChanged?.Invoke());
            goldField.RegisterValueChangedCallback(_ => ContentChanged?.Invoke());
            xpField.RegisterValueChangedCallback(_ => ContentChanged?.Invoke());
            cooldownField.RegisterValueChangedCallback(_ => ContentChanged?.Invoke());
            failureField.RegisterValueChangedCallback(_ => ContentChanged?.Invoke());

            contentContainer.Add(nameField);
            contentContainer.Add(craftTimeField);
            contentContainer.Add(levelField);
            contentContainer.Add(goldField);
            contentContainer.Add(xpField);
            contentContainer.Add(cooldownField);
            failureField.tooltip = "Base chance (0–1) the craft fails. Reduced by the crafter's skill.";
            contentContainer.Add(failureField);

            var outputsLabel = new Label("Outputs");
            outputsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            outputsLabel.style.color = RecipeGraphTheme.Palette.textSecondary;
            outputsLabel.style.paddingTop = 4f;
            contentContainer.Add(outputsLabel);
            contentContainer.Add(outputsContainer);

            var addOutput = new Button(() => AddOutputRow()) { text = "+ Output" };
            addOutput.style.marginTop = 4f;
            addOutput.style.backgroundColor = RecipeGraphTheme.Palette.buttonNormal;
            addOutput.style.color = RecipeGraphTheme.Palette.textPrimary;
            addOutput.style.borderTopLeftRadius = 4f;
            addOutput.style.borderTopRightRadius = 4f;
            addOutput.style.borderBottomLeftRadius = 4f;
            addOutput.style.borderBottomRightRadius = 4f;
            addOutput.style.paddingTop = 3f;
            addOutput.style.paddingBottom = 3f;
            contentContainer.Add(addOutput);

            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
            InputPort.portName = "inputs";
            RecipeGraphTheme.StylePort(InputPort, RecipeGraphTheme.Palette.success);
            inputContainer.Add(InputPort);

            AddOutputRow();
            RefreshExpandedState();
        }

        private void AddOutputRow()
        {
            var row = new OutputRow();
            outputRows.Add(row);
            outputsContainer.Add(row.Root);
            row.OnChanged += () => ContentChanged?.Invoke();
        }

        public string RecipeName => string.IsNullOrEmpty(nameField.value) ? "Recipe" : nameField.value;
        public float CraftTime => Mathf.Max(0f, craftTimeField.value);
        public int Level => Mathf.Max(1, levelField.value);
        public int GoldCost => Mathf.Max(0, goldField.value);
        public int XpCost => Mathf.Max(0, xpField.value);
        public float Cooldown => Mathf.Max(0f, cooldownField.value);
        public float FailureChance => Mathf.Clamp01(failureField.value);

        /// <summary>Refresh the input port label from connected item sources.</summary>
        public void SyncInputPort()
        {
            var graph = GetFirstAncestorOfType<RecipeGraphView>();
            if (graph == null) return;
            var sources = graph.GetSourcesFor(this);
            InputPort.portName = sources.Count == 0
                ? "inputs (connect items)"
                : string.Join(" + ", sources.Select(s => $"{s.Count} {s.Item?.name ?? "?"}"));
            RefreshPorts();
            ContentChanged?.Invoke();
        }

        public List<ItemStack> GetInputStacks()
        {
            var graph = GetFirstAncestorOfType<RecipeGraphView>();
            var stacks = new List<ItemStack>();
            if (graph == null) return stacks;
            foreach (var source in graph.GetSourcesFor(this))
                if (source.Item != null)
                    stacks.Add(new ItemStack(source.Item, source.Count));
            return stacks;
        }

        public List<ItemStack> GetOutputStacks()
        {
            var stacks = new List<ItemStack>();
            foreach (var row in outputRows)
            {
                if (row.Root.parent == null) continue; // removed via its ✕ button
                if (row.Item != null)
                    stacks.Add(new ItemStack(row.Item, row.Count));
            }
            return stacks;
        }

        /// <summary>Build a production RecipeDefinition asset from this node.</summary>
        public RecipeDefinition BuildDefinition()
        {
            var inputs = GetInputStacks();
            var outputs = GetOutputStacks();
            if (outputs.Count == 0) return null;
            if (inputs.Count == 0) return null;

            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            var so = new SerializedObject(recipe);
            so.FindProperty("id").stringValue = System.Guid.NewGuid().ToString("N").Substring(0, 12);
            so.FindProperty("displayName").stringValue = RecipeName;
            so.FindProperty("craftTime").floatValue = CraftTime;
            so.FindProperty("levelRequirement").intValue = Level;
            so.FindProperty("goldCost").intValue = GoldCost;
            so.FindProperty("xpCost").intValue = XpCost;
            so.FindProperty("cooldownSeconds").floatValue = Cooldown;
            so.FindProperty("failureChance").floatValue = FailureChance;

            WriteStacks(so.FindProperty("inputs"), inputs);
            WriteStacks(so.FindProperty("outputs"), outputs);
            so.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }

        private static void WriteStacks(SerializedProperty prop, List<ItemStack> stacks)
        {
            prop.ClearArray();
            for (int i = 0; i < stacks.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("item").objectReferenceValue = stacks[i].item;
                el.FindPropertyRelative("count").intValue = stacks[i].count;
            }
        }

        private class OutputRow
        {
            public readonly VisualElement Root = new VisualElement();
            private readonly ObjectField itemField = new ObjectField { objectType = typeof(ItemDefinition) };
            private readonly IntegerField countField = new IntegerField { value = 1 };

            public event System.Action OnChanged;

            public ItemDefinition Item => itemField.value as ItemDefinition;
            public int Count => Mathf.Max(1, countField.value);

            public OutputRow()
            {
                var layout = new VisualElement();
                layout.style.flexDirection = FlexDirection.Row;
                layout.style.marginBottom = 2f;

                itemField.style.flexGrow = 1f;
                countField.style.width = 52f;
                var remove = new Button(() => Root.RemoveFromHierarchy()) { text = "✕" };
                remove.style.width = 22f;

                itemField.RegisterValueChangedCallback(_ => OnChanged?.Invoke());
                countField.RegisterValueChangedCallback(_ => OnChanged?.Invoke());

                layout.Add(itemField);
                layout.Add(countField);
                layout.Add(remove);
                Root.Add(layout);
            }
        }
    }

    // ================================================================== theme

    /// <summary>
    /// Resolves the colors the graph editor uses from a <see cref="RarityPalette"/> so the
    /// editor matches the runtime UI theme. Picks up the palette of an
    /// <see cref="InventoryCraftingUI"/> in the open scene (call <see cref="FromScene"/>),
    /// otherwise falls back to the default palette.
    /// </summary>
    internal static class RecipeGraphTheme
    {
        public static RarityPalette Palette { get; private set; } = new RarityPalette();

        public static Color Canvas => Color.Lerp(Palette.panelBackground, Color.black, 0.45f);
        public static Color GridLine => new Color(0.55f, 0.63f, 0.75f, 0.05f);
        public static Color GridMajor => new Color(0.55f, 0.63f, 0.75f, 0.12f);
        public static Color NodeBody => Palette.panelBackground;
        public static Color NodeBorder => new Color(0.34f, 0.40f, 0.52f, 0.55f);
        public static Color HeaderText => new Color(0.97f, 0.98f, 1f, 1f);
        public static Color PortPill => Palette.slotBackground;
        public static Color PortText => Palette.textPrimary;

        public static void FromScene()
        {
            var ui = Object.FindObjectOfType<InventoryCraftingUI>();
            if (ui != null && ui.Palette != null)
                Palette = ui.Palette;
        }

        /// <summary>Apply the themed node shell: rounded body, gradient-style header with an accent underline.</summary>
        public static void StyleNode(Node node, Color header, Color accent)
        {
            node.style.backgroundColor = NodeBody;
            node.style.borderTopLeftRadius = 10f;
            node.style.borderTopRightRadius = 10f;
            node.style.borderBottomLeftRadius = 10f;
            node.style.borderBottomRightRadius = 10f;
            node.style.borderTopWidth = 1f;
            node.style.borderBottomWidth = 1f;
            node.style.borderLeftWidth = 1f;
            node.style.borderRightWidth = 1f;
            node.style.borderTopColor = NodeBorder;
            node.style.borderBottomColor = NodeBorder;
            node.style.borderLeftColor = NodeBorder;
            node.style.borderRightColor = NodeBorder;

            var title = node.titleContainer;
            title.style.backgroundColor = header;
            title.style.borderTopLeftRadius = 9f;
            title.style.borderTopRightRadius = 9f;
            title.style.borderBottomWidth = 2f;
            title.style.borderBottomColor = accent;
            title.style.paddingTop = 5f;
            title.style.paddingBottom = 5f;
            title.style.paddingLeft = 10f;
            title.style.paddingRight = 10f;

            var label = title.Q<Label>();
            if (label != null)
            {
                label.style.color = HeaderText;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.fontSize = 13f;
            }
        }

        /// <summary>Recolor just the header underline (used to reflect an item's rarity).</summary>
        public static void SetAccent(Node node, Color color)
        {
            node.titleContainer.style.borderBottomColor = color;
        }

        /// <summary>Style a port as a rounded pill with a colored connector dot.</summary>
        public static void StylePort(Port port, Color color)
        {
            port.style.backgroundColor = PortPill;
            port.style.borderTopLeftRadius = 10f;
            port.style.borderTopRightRadius = 10f;
            port.style.borderBottomLeftRadius = 10f;
            port.style.borderBottomRightRadius = 10f;
            port.style.paddingLeft = 6f;
            port.style.paddingRight = 6f;

            var connector = port.Q<VisualElement>("connector");
            if (connector != null)
            {
                connector.style.backgroundColor = color;
                connector.style.borderTopColor = color;
                connector.style.borderBottomColor = color;
                connector.style.borderLeftColor = color;
                connector.style.borderRightColor = color;
            }

            var label = port.Q<Label>();
            if (label != null)
            {
                label.style.color = PortText;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.fontSize = 11f;
            }
        }

        /// <summary>Theme a connection edge (applied to every newly created edge).</summary>
        public static void StyleEdge(Edge edge)
        {
            var ec = edge.edgeControl;
            if (ec == null) return;
            ec.edgeWidth = 3;
            ec.inputColor = Palette.accent;
            ec.outputColor = Palette.accent;
            ec.fromCapColor = Palette.accent;
            ec.toCapColor = Palette.accent;
        }
    }

    // ================================================================== themed grid

    /// <summary>
    /// Replaces the default <see cref="GridBackground"/> with a grid drawn from the theme
    /// palette (no USS assets needed). Minor lines every 20 world units, major every 100;
    /// spacing and offset follow the graph view's pan/zoom.
    /// </summary>
    internal class ThemedGridBackground : VisualElement
    {
        private const float MinorSpacing = 20f;
        private readonly GraphView graphView;
        private readonly Color lineColor;
        private readonly Color majorColor;

        public ThemedGridBackground(GraphView graphView, Color background, Color line, Color major)
        {
            this.graphView = graphView;
            lineColor = line;
            majorColor = major;

            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            style.backgroundColor = background;

            generateVisualContent = OnGenerateVisualContent;
            graphView.viewTransformChanged += OnViewTransformChanged;
            RegisterCallback<DetachFromPanelEvent>(_ => graphView.viewTransformChanged -= OnViewTransformChanged);
        }

        private void OnViewTransformChanged(GraphView _) => MarkDirtyRepaint();

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            if (painter == null) return;
            var rect = contentRect;
            if (rect.width <= 1f || rect.height <= 1f) return;

            var t = graphView.contentViewContainer.transform;
            float scale = Mathf.Clamp(t.scale.x, 0.1f, 8f);
            float minor = MinorSpacing * scale;
            float major = minor * 5f;

            float xMinor = Mathf.Repeat(t.position.x, minor);
            float yMinor = Mathf.Repeat(t.position.y, minor);
            float xMajor = Mathf.Repeat(t.position.x, major);
            float yMajor = Mathf.Repeat(t.position.y, major);

            painter.lineCap = LineCap.Round;

            // minor grid
            for (float x = rect.xMin + xMinor; x <= rect.xMax; x += minor)
                StrokeLine(painter, x, rect.yMin, x, rect.yMax, lineColor, 1f);
            for (float y = rect.yMin + yMinor; y <= rect.yMax; y += minor)
                StrokeLine(painter, rect.xMin, y, rect.xMax, y, lineColor, 1f);

            // major grid
            for (float x = rect.xMin + xMajor; x <= rect.xMax; x += major)
                StrokeLine(painter, x, rect.yMin, x, rect.yMax, majorColor, 1.25f);
            for (float y = rect.yMin + yMajor; y <= rect.yMax; y += major)
                StrokeLine(painter, rect.xMin, y, rect.xMax, y, majorColor, 1.25f);
        }

        private static void StrokeLine(Painter2D painter, float x1, float y1, float x2, float y2, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x1, y1));
            painter.LineTo(new Vector2(x2, y2));
            painter.Stroke();
        }
    }
}
