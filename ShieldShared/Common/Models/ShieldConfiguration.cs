using System.Collections.Generic;

namespace ShieldVSExtension.Common.Models;

internal class ShieldConfiguration
{
    public string Name { get; set; }
    public string Preset { get; set; }
    public string ProjectToken { get; set; }
    public string ProtectionSecret { get; set; }
    public bool Enabled { get; set; }
    public string RunConfiguration { get; set; }
    public Dictionary<string, bool?> Protections { get; set; } = [];
}