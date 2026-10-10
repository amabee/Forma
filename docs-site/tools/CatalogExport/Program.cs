using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

var controls = typeof(Control).Assembly.GetTypes()
    .Where(type => !type.IsAbstract && typeof(Control).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
    .Select(type => {
        var control = (Control)Activator.CreateInstance(type)!;
        var result = new {
            name = type.Name, kind = control.ControlType, tray = control is INonvisualControl,
            inspector = InspectorCatalog.ForKind(control.ControlType).ToArray(),
            model = type.GetProperties().Where(property => property.CanRead && property.GetIndexParameters().Length == 0
                && property.Name is not ("Children" or "Parent" or "Id"))
                .GroupBy(property => property.Name).Select(group => group.First()).Select(property => new {
                    name = property.Name, type = property.PropertyType.Name, writable = property.SetMethod?.IsPublic == true,
                    value = property.GetValue(control)
                }).ToArray(),
            events = type.GetEvents().Select(@event => @event.Name).Distinct().ToArray(),
            methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsSpecialName && method.DeclaringType != typeof(object))
                .Select(method => new { name = method.Name, parameters = method.GetParameters().Select(parameter => parameter.Name).ToArray() })
                .ToArray()
        };
        if (control is IDisposable disposable) disposable.Dispose();
        return result;
    }).OrderBy(control => control.name).ToArray();
File.WriteAllText(args[0], JsonSerializer.Serialize(controls, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
var output = Path.GetFullPath(args[0]);
var repository = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(output)))!;
var sources = Directory.GetFiles(Path.Combine(repository, "src/Forma.Core/Controls"), "*.cs")
    .Concat(new[] { "src/Forma.Core/Form.cs", "src/Forma.Builder/Services/InspectorCatalog.cs", "src/Forma.Builder/Models/Appearance.cs" }
        .Select(source => Path.Combine(repository, source)))
    .OrderBy(source => source, StringComparer.Ordinal)
    .ToDictionary(source => Path.GetRelativePath(repository, source).Replace('\\', '/'),
        source => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(File.ReadAllText(source).Replace("\r\n", "\n")))));
File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "catalog-sources.json"), JsonSerializer.Serialize(sources, new JsonSerializerOptions { WriteIndented = true }));
