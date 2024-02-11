using ShieldVSExtension.Common.Models;

namespace ShieldVSExtension.Common.Helpers;

internal class JsonHelper
{
    public static string Serialize<T>(T obj, bool formatting = true)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.JsonSerializerSettings
        {
            Formatting = formatting ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None
        });
    }

    public static T Deserialize<T>(string source)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(source);
    }

    public static ShieldConfiguration Parse(string source)
    {
        return Deserialize<ShieldConfiguration>(source);
    }

    public static string Stringify(ShieldConfiguration configuration, bool formatting = true)
    {
        return Serialize(configuration, formatting);
    }
}