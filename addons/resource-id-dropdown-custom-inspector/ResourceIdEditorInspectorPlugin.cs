#if TOOLS
using System;
using Godot;
using System.Reflection;
using System.Linq;
#endif

namespace OverfortGames.SteamNetGodot
{
#if TOOLS
    [Tool]
    public partial class ResourceIdEditorInspectorPlugin : EditorInspectorPlugin
    {

        // Custom logic to override the Inspector for classes with ResourceIdDropdownAttribute
        public override bool _CanHandle(GodotObject obj)
        {
            // Check if any field has the custom ResourceIdDropdownAttribute and is of type string
            var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            return fields.Any(f => f.GetCustomAttribute<ResourceIdDropdownAttribute>() != null && f.FieldType == typeof(string));
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
        {
            var field = @object.GetType().GetField(name);
            if (field != null && field.GetCustomAttribute<ResourceIdDropdownAttribute>() != null && type == Variant.Type.String)
            {
                // Get all const fields from ResourceId
                var resourceIdType = typeof(ResourceId);
                var constFields = resourceIdType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                    .Select(fi => (string)fi.GetRawConstantValue())
                    .ToArray();

                // Create container for label and dropdown
                var container = new HBoxContainer();
                container.OffsetLeft = 0;
                container.OffsetRight = 0;
                container.OffsetTop = 0;
                container.OffsetBottom = 0;
                container.CustomMinimumSize = new Vector2(300, 0); // Adjust as needed
                                                                   // Create a label with the field name
                var label = new Label();
                label.Text = CapitalizeFirstLetter(name); // Set the label text to the field name
                label.CustomMinimumSize = new Vector2(100, 0); // Adjust as needed for label width
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                // Create a dropdown for these const values
                var dropdown = new OptionButton();
                dropdown.SizeFlagsHorizontal = label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                dropdown.Name = name;
                for (int i = 0; i < constFields.Length; i++)
                {
                    dropdown.AddItem(constFields[i], i);
                }

                // Set current selection
                string currentValue = (string)field.GetValue(@object);
                int selectedIndex = Array.IndexOf(constFields, currentValue);
                if (selectedIndex >= 0)
                {
                    dropdown.Select(selectedIndex);
                }

                dropdown.ItemSelected += (x) => OnItemSelected(x, @object, field, constFields);

                // Add label and dropdown to the container
                Control spaceBeforeLabel = new Control();
                spaceBeforeLabel.CustomMinimumSize = new Vector2(5, 0);

                container.AddChild(spaceBeforeLabel);
                container.AddChild(label);
                container.AddChild(dropdown);

                // Add the container to the inspector
                AddCustomControl(container);

                return true; // Indicate that the property was handled
            }

            return false; // Indicate that the property was not handled
        }

        private void OnItemSelected(long index, GodotObject obj, FieldInfo field, string[] constValues)
        {
            // Update the field with the selected constant value
            field.SetValue(obj, constValues[index]);

            Resource objAsResource = obj as Resource;
            if (objAsResource != null)
                ResourceSaver.Save(objAsResource);
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
#endif
}