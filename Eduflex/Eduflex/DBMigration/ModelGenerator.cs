using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace DBMigration;

public static class ModelGenerator
{
    public static async Task BuildDbModelsAsync(IDatabaseService databaseService)
    {
        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
        string outputDir = Path.Combine(rootDir, "ShareService", "Models");
        Directory.CreateDirectory(outputDir);

        var collections = await databaseService.GetCollectionNamesAsync();
        if (collections == null || !collections.Any())
        {
            Console.WriteLine("⚠️ No collections found in the database.");
            return;
        }
        Console.WriteLine($"📊 Found {collections.Count} collections: {string.Join(", ", collections)}");

        foreach (var collectionName in collections)
        {
            try
            {
                var collection = await databaseService.GetCollection(collectionName);
                if (collection == null)
                {
                    Console.WriteLine($"⚠️ Collection '{collectionName}' not found or inaccessible.");
                    continue;
                }

                string path = Path.Combine(outputDir, $"{collectionName}Model.cs");
                string classContent = GenerateCSharpClass(collectionName, collection.ToBsonDocument());

                bool shouldWrite = true;
                if (File.Exists(path))
                {
                    string existingContent = await File.ReadAllTextAsync(path);
                    if (existingContent == classContent)
                    {
                        Console.WriteLine($"✅ Model unchanged: {Path.GetFileName(path)}");
                        shouldWrite = false;
                    }
                    else
                    {
                        Console.WriteLine($"⚡ Updating model: {Path.GetFileName(path)}");
                    }
                }
                else
                {
                    Console.WriteLine($"✅ Creating new model: {Path.GetFileName(path)}");
                }

                if (shouldWrite)
                {
                    await File.WriteAllTextAsync(path, classContent);
                    Console.WriteLine($"✅ Generated: {Path.GetFileName(path)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing collection {collectionName}: {ex.Message}");
            }
        }
    }

    private static string GenerateCSharpClass(string name, BsonDocument doc)
    {
        var props = new List<string>();
        if (doc?.Elements == null)
        {
            return string.Empty;
        }

        foreach (var element in doc.Elements)
        {
            if (element.Name == "_id")
            {
                props.Add($@"    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {{ get; set; }}");
            }
            else
            {
                string clrType = MapBsonTypeToCSharp(element.Value.BsonType);
                props.Add($@"    [BsonElement(""{element.Name}"")]
    public {clrType} {ToPascalCase(element.Name)} {{ get; set; }}");
            }
        }

        return
$@"using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eduflex.Shared.Models;

public class {ToPascalCase(name)}
{{
{string.Join(Environment.NewLine + Environment.NewLine, props)}
}}";
    }

    private static string MapBsonTypeToCSharp(BsonValue value)
    {
        // Prefer value-based inference (arrays, etc.)
        switch (value.BsonType)
        {
            case BsonType.ObjectId: return "string";          // store as string with [BsonRepresentation]
            case BsonType.String: return "string";
            case BsonType.Int32: return "int";
            case BsonType.Int64: return "long";
            case BsonType.Double: return "double";
            case BsonType.Boolean: return "bool";
            case BsonType.DateTime: return "DateTime";
            case BsonType.Decimal128: return "decimal";
            case BsonType.Document: return "BsonDocument";    // or a nested POCO if you generate one
            case BsonType.Array:
                var arr = value.AsBsonArray;
                var elemType = arr.Count > 0 ? MapBsonTypeToCSharp(arr[0]) : "object";
                return elemType.EndsWith("[]", StringComparison.Ordinal) ? elemType : $"{elemType}[]";
            case BsonType.Binary: return "byte[]";
            case BsonType.Null: return "string";          // fallback
            default: return "string";          // safe default
        }
    }

    private static string ToPascalCase(string fieldName)
    {
        // split on non-alphanumerics and join in PascalCase
        var parts = Regex.Split(fieldName ?? string.Empty, @"[^A-Za-z0-9]+")
                         .Where(p => p.Length > 0)
                         .Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1));
        var name = string.Concat(parts);
        if (string.IsNullOrEmpty(name)) name = "Field";
        if (char.IsDigit(name[0])) name = "_" + name;
        // escape C# keywords
        if (new[] { "Class", "Namespace", "Event", "String", "Int", "Long", "Decimal", "Object", "Params", "Operator", "Base", "This" }
            .Contains(name, StringComparer.Ordinal)) name = "@" + name;
        return name;
    }
}
