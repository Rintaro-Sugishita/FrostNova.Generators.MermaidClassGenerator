using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase.Json
{

    internal class AccessibilityConverter : JsonConverter<Accessibility>
    {
        public override Accessibility Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return GetAccessibility(str);
        }

        public override void Write(Utf8JsonWriter writer, Accessibility value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(GetText(value));
        }


        static Accessibility GetAccessibility(string? text)
        {
            if (Enum.TryParse<Accessibility>(text, ignoreCase: true, out var res))
            {
                return res;
            }
            if (string.Equals(text, "private protected", StringComparison.OrdinalIgnoreCase))
            {
                return Accessibility.ProtectedAndInternal;
            }
            else if (string.Equals(text, "protected internal", StringComparison.OrdinalIgnoreCase))
            {
                return Accessibility.ProtectedOrInternal;
            }
            return Accessibility.NotApplicable;

        }

        static string GetText(Accessibility accessibility) 
        {
            switch (accessibility)
            {
                case Accessibility.NotApplicable:
                    return "";                   
                case Accessibility.ProtectedAndInternal:
                    return "private protected";
                case Accessibility.ProtectedOrInternal:
                    return "protected internal";
                case Accessibility.Private:
                case Accessibility.Protected:
                case Accessibility.Internal:
                case Accessibility.Public:
                    return accessibility.ToString().ToLower();
                default:
                    return "";
            }

        }

    }
}
