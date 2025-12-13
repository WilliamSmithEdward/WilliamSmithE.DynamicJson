# DynamicJson  
A simple, lightweight way to work with JSON as dynamic objects or lists, while still giving you type safety when you need it.

This library converts JSON into `SafeDynamicObject` and `SafeDynamicList`, enabling natural property access while retaining optional mapping to strongly typed POCOs.

---

## ✨ Features

- `json.ToDynamic()` → returns a dynamic object or dynamic list  
- Safe property access with case-insensitive matching  
- Lists behave like `IEnumerable` (LINQ ready)  
- Automatic conversion of JSON scalars → .NET primitives  
- Best-effort `AsType<T>()` for mapping to POCO classes  
- Non-throwing on missing properties  
- Round-trip serialization support (`ToJson`)  
- Clean, minimal API surface

---

## 🚀 Getting Started

### Convert JSON → dynamic

```csharp
using WilliamSmithE.DynamicJson;

string json = @"
{
  ""id"": 67,
  ""name"": ""John Doe"",
  ""isActive"": true,
  ""createdDate"": ""2025-01-15T10:45:00Z"",
  ""profile"": {
    ""email"": ""john@doe.com"",
    ""department"": ""Engineering"",
    ""roles"": [
      { ""roleName"": ""Admin"",     ""level"": 5 },
      { ""roleName"": ""Developer"", ""level"": 3 }
    ]
  },
  ""preferences"": {
    ""theme"": ""dark"",
    ""dashboardWidgets"": [ ""inbox"", ""projects"", ""metrics"" ]
  }
}
";

var dyn = json.ToDynamic();
```

---

## 🧭 Dynamic Navigation

```csharp
Console.WriteLine(dyn.id);                       // 67
Console.WriteLine(dyn.name);                     // John Doe
Console.WriteLine(dyn.profile.email);            // john@doe.com

var firstRole = dyn.profile.roles.First();
Console.WriteLine(firstRole.roleName);           // Admin
```

## 🔍 LINQ works naturally

Use the `.AsEnumerable()` extension method to enable LINQ queries on `SafeDynamicList` objects.

> ⚠️ When using `.AsEnumerable(...)` with a dynamic list, cast the source to `SafeDynamicList` so the lambda can be bound correctly by the C# compiler.

Example:

```csharp
string usersJson = """
{
  "users": [
    {
      "name": "Alice",
      "roles": [
        { "roleName": "Admin", "permissions": [ "read", "write", "delete" ] },
        { "roleName": "User",  "permissions": [ "read" ] }
      ]
    },
    {
      "name": "Bob",
      "roles": [
        { "roleName": "Developer", "permissions": [ "read", "commit" ] },
        { "roleName": "User",      "permissions": [ "read" ] }
      ]
    }
  ]
}
""";

var dynObj = usersJson.ToDynamic();

var names =
    ((SafeDynamicList)dynObj.users)
        .AsEnumerable()
        .Where(u =>
            ((SafeDynamicList)u.roles)
                .AsEnumerable()
                .Any(r => r.roleName == "Admin")
        )
        .Select(u => (string)u.name)
        .Distinct()
        .OrderBy(x => x)
        .ToList();

foreach (var name in names)
{
    Console.WriteLine(name);
}
```

## 🎯 Mapping to POCOs

```csharp
public class MyClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

MyClass instance = dyn.AsType<MyClass>();
Console.WriteLine(instance.Id);                  // 67
```

### Nested objects

```csharp
public class Profile
{
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

var profile = dyn.profile.AsType<Profile>();
Console.WriteLine(profile.Department);           // Engineering
```

---

## 🔄 Serializing Back to JSON

```csharp
var profileJson = dyn.profile.ToJson();
Console.WriteLine(profileJson);
```

Or via helper:

```csharp
var jsonOut = DynamicJson.ToJson(dyn.preferences.dashboardWidgets);
Console.WriteLine(jsonOut);
```

---

## 🏗️ Working With Lists

```csharp
foreach (var role in dyn.profile.roles)
{
    Console.WriteLine(role.roleName);
}
```

Mapping:

```csharp
public class Role
{
    public string RoleName { get; set; } = string.Empty;
    public int Level { get; set; }
}

var roles = dyn.profile.roles.ToList<Role>();
```

---

## 📘 Example End-to-End

```csharp
var dyn = json.ToDynamic();

Console.WriteLine(dyn.profile.roles.First().roleName);
// Admin

var user = dyn.AsType<MyClass>();
Console.WriteLine(user.CreatedDate);
// 1/15/2025 10:45:00 AM

string roundTrip = dyn.ToJson();
Console.WriteLine(roundTrip);
```

---

## 📄 License

MIT License. See `LICENSE` file for details.