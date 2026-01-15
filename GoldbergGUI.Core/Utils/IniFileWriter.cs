using System.Text;

namespace GoldbergGUI.Core.Utils;

/// <summary>
///     Utility for writing INI configuration files in Goldberg emulator format
/// </summary>
public sealed class IniFileWriter
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = [];

    /// <summary>
    ///     Adds or updates a section with key-value pairs
    /// </summary>
    public void WriteSection(string sectionName, Dictionary<string, string> values)
    {
        if (_sections.TryGetValue(sectionName, out var section))
            foreach (var (key, value) in values)
                section[key] = value;
        else
            _sections[sectionName] = new Dictionary<string, string>(values);
    }

    /// <summary>
    ///     Adds a single key-value pair to a section
    /// </summary>
    public void AddToSection(string sectionName, string key, string value)
    {
        if (!_sections.ContainsKey(sectionName)) _sections[sectionName] = [];

        _sections[sectionName][key] = value;
    }

    /// <summary>
    ///     Generates the complete INI file content
    /// </summary>
    public string Generate()
    {
        var sb = new StringBuilder();

        foreach (var (sectionName, values) in _sections)
        {
            sb.AppendLine($"[{sectionName}]");

            foreach (var (key, value) in values) sb.AppendLine($"{key}={value}");

            // Add blank line between sections
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Clears all sections and data
    /// </summary>
    public void Clear()
    {
        _sections.Clear();
    }
}