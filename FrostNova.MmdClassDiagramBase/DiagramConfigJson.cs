using FrostNova.MmdClassDiagramGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase
{
    [JsonSourceGenerationOptions(WriteIndented =true)]
    [JsonSerializable(typeof(DiagramConfig))]
    public partial class DiagramConfigJson : JsonSerializerContext
    {
        public static string Serialize(DiagramConfig config)
        {
            //var sb = new StringBuilder();
            //sb.Append("{");

            //Append(sb, "Mode", config.Mode, comma: true);
            //Append(sb, "OutputMode", config.OutputMode, comma: true);
            //AppendArray(sb, "ExcludeNameSpaces", config.ExcludeNameSpaces, comma: true);
            //Append(sb, "OutputPath", config.OutputPath, comma: true);
            //Append(sb, "OutputType", config.OutputType, comma: true);
            //AppendArray(sb, "ActiveBuildConfigurations", config.ActiveBuildConfigurations, comma: true);

            //Append(sb, "MethodAccessibility", config.MethodAccessibility, comma: true);
            //Append(sb, "PropertyAccessibility", config.PropertyAccessibility, comma: true);
            //Append(sb, "FieldAccessibility", config.FieldAccessibility, comma: true);

            //AppendArray(sb, "RootAttributes", config.RootAttributes, comma: false);

            //sb.Append("}");
            //return sb.ToString();
           return JsonSerializer.Serialize(config, typeof(DiagramConfig), new DiagramConfigJson());

        }

        private static void Append(StringBuilder sb, string name, string value, bool comma)
        {
            sb.Append($"\"{name}\":\"{Escape(value)}\"");
            if (comma) sb.Append(",");
        }
        private static void AppendArray(StringBuilder sb, string name, string[] values, bool comma)
        {
            sb.Append($"\"{name}\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append($"\"{Escape(values[i])}\"");
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");
            if (comma) sb.Append(",");
        }

        private static string Escape(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"");


        public static DiagramConfig Deserialize(string json)
        {
            //var config = new DiagramConfig();

            //var dict = ParseJsonObject(json);

            //if (dict.TryGetValue("Mode", out var mode)) config.Mode = mode;
            //if (dict.TryGetValue("OutputMode", out var outputMode)) config.OutputMode = outputMode;
            //if (dict.TryGetValue("ExcludeNameSpaces", out var excludeNs))
            //    config.ExcludeNameSpaces = ParseJsonArray(excludeNs);
            //if (dict.TryGetValue("OutputPath", out var outputPath)) config.OutputPath = outputPath;
            //if (dict.TryGetValue("OutputType", out var outputType)) config.OutputType = outputType;
            //if (dict.TryGetValue("ActiveBuildConfigurations", out var configs))
            //    config.ActiveBuildConfigurations = ParseJsonArray(configs);

            //if (dict.TryGetValue("MethodAccessibility", out var methodAcc)) config.MethodAccessibility = methodAcc;
            //if (dict.TryGetValue("PropertyAccessibility", out var propAcc)) config.PropertyAccessibility = propAcc;
            //if (dict.TryGetValue("FieldAccessibility", out var fieldAcc)) config.FieldAccessibility = fieldAcc;

            //if (dict.TryGetValue("RootAttributes", out var rootAttrs))
            //    config.RootAttributes = ParseJsonArray(rootAttrs);

            //return config;
            return JsonSerializer.Deserialize(json, typeof(DiagramConfig), new DiagramConfigJson()) as DiagramConfig ?? new DiagramConfig();
        }
        private static Dictionary<string, string> ParseJsonObject(string json)
        {
            // とても単純化したパーサー（"key":"value" or "key":[...]のみ対応）
            // 空白と{}はトリムされている想定

            var dict = new Dictionary<string, string>();

            int i = 0;
            int length = json.Length;

            // { と } を削除
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length) break;

                // "key"
                var key = ParseJsonString(json, ref i);

                SkipWhitespace(json, ref i);
                if (json[i] != ':') throw new Exception("Expected ':' after key");
                i++; // skip :

                SkipWhitespace(json, ref i);

                if (json[i] == '"')
                {
                    var val = ParseJsonString(json, ref i);
                    dict[key] = val;
                }


                else if (json[i] == '[')
                {
                    int start = i;
                    int bracketCount = 0;
                    do
                    {
                        if (json[i] == '[') bracketCount++;
                        else if (json[i] == ']') bracketCount--;
                        i++;
                    } while (bracketCount > 0 && i < json.Length);

                    var val = json.Substring(start, i - start);
                    dict[key] = val;
                }
                else
                {
                    throw new Exception("Unexpected JSON value type");
                }

                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == ',') i++;
            }

            return dict;
        }

        private static string[] ParseJsonArray(string json)
        {
            // jsonは"[\"val1\",\"val2\"]"の形式を想定
            var list = new List<string>();
            int i = 0;
            json = json.Trim();

            if (!json.StartsWith("[") || !json.EndsWith("]"))
                throw new Exception("Invalid JSON array");

            i++; // skip '['

            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] == ']') break;

                var val = ParseJsonString(json, ref i);
                list.Add(val);

                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == ',') i++;
            }
            return list.ToArray();

        }

        private static string ParseJsonString(string json, ref int i)
        {
            if (json[i] != '"') throw new Exception("Expected '\"'");
            i++; // skip '"'
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"')
                {
                    i++;
                    break;
                }
                if (c == '\\')
                {
                    i++;
                    if (i >= json.Length) break;
                    c = json[i];
                    if (c == '"' || c == '\\')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append('\\').Append(c);
                    }
                    i++;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }
        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }
        }
    }

}
