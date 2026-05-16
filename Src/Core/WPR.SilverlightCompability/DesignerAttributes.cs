// Designer-hint attributes from the Silverlight Toolkit / WP7 control templates.
// They affect Blend/designer behaviour only — runtime stores and ignores them.
// We shim them as empty Attribute subclasses so that JIT and reflection over
// patched control assemblies (Microsoft.Phone.Controls.dll, etc.) don't blow up
// when these attribute types appear in custom-attribute metadata.

using System;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Shim for <c>System.Windows.StyleTypedPropertyAttribute</c>. Decorates a
    /// control class to advertise that one of its style-typed dependency properties
    /// should target a specific element type. Designer hint only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class StyleTypedPropertyAttribute : Attribute
    {
        public string? Property { get; set; }
        public Type? StyleTargetType { get; set; }
    }

    /// <summary>
    /// Shim for <c>System.Windows.TemplatePartAttribute</c>. Marks a part name
    /// the control template is expected to expose. Designer hint only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class TemplatePartAttribute : Attribute
    {
        public string? Name { get; set; }
        public Type? Type { get; set; }
    }

    /// <summary>
    /// Shim for <c>System.Windows.TemplateVisualStateAttribute</c>. Marks a visual
    /// state the control template participates in. Designer hint only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class TemplateVisualStateAttribute : Attribute
    {
        public string? Name { get; set; }
        public string? GroupName { get; set; }
    }
}

namespace WPR.SilverlightCompability.Markup
{
    /// <summary>
    /// Shim for <c>System.Windows.Markup.XmlnsDefinitionAttribute</c>. Assembly-level
    /// declaration that maps an xmlns URI to a CLR namespace. Designer/XAML loader
    /// hint — our XamlReader uses its own resolution, so this attribute is inert.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsDefinitionAttribute : Attribute
    {
        public string XmlNamespace { get; }
        public string ClrNamespace { get; }
        public string? AssemblyName { get; set; }

        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
            XmlNamespace = xmlNamespace;
            ClrNamespace = clrNamespace;
        }
    }

    /// <summary>
    /// Shim for <c>System.Windows.Markup.XmlnsPrefixAttribute</c>. Assembly-level
    /// hint mapping an xmlns URI to a recommended prefix. Designer hint only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsPrefixAttribute : Attribute
    {
        public string XmlNamespace { get; }
        public string Prefix { get; }

        public XmlnsPrefixAttribute(string xmlNamespace, string prefix)
        {
            XmlNamespace = xmlNamespace;
            Prefix = prefix;
        }
    }
}
