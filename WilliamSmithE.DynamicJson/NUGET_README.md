# DynamicJson  
A simple, lightweight way to work with JSON as dynamic objects or lists, while still giving you type safety when you need it.

This library converts JSON into `DynamicJsonObject` and `DynamicJsonList`, enabling natural property access while retaining optional mapping to strongly typed POCOs.

---

## ✨ Features

- **`json.ToDynamic()` entry point**  
  Converts JSON into a dynamic object or list that behaves predictably in .NET.

- **Straightforward property access**  
  Case-insensitive lookups with safe null returns for missing fields.

- **Lists integrate naturally with .NET**  
  Dynamic lists support indexing and can be used directly with LINQ.

- **Automatic handling of JSON primitives**  
  Strings, numbers, booleans, and null values map directly to .NET types.

- **Object mapping with `AsType<T>()`**  
  Converts dynamic objects into POCOs using simple reflection-based mapping.

- **Scalar list conversion (`ToScalarList<T>()`)**  
  Extracts arrays of primitives (e.g., strings, ints) into strongly typed lists.

- **Object list conversion (`ToList<T>()`)**  
  Converts arrays of JSON objects into `List<T>` without extra serializer configuration.

- **Clear, predictable error behavior**  
  Missing properties return null; invalid casts are skipped; index errors throw normally.

- **Round-trip JSON support (`ToJson()`)**  
  Modified dynamic objects can be serialized back to JSON cleanly.

- **Minimal, focused API surface**  
  Provides practical capabilities without a large configuration model.

- **Diff / Patch / Merge utilities**  
  Built-in helpers for comparing and combining JSON structures.

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

var dynObj = json.ToDynamic();
```

---

## 🧭 Dynamic Navigation

### Use a dynamic json object like it was a POCO / CLR object:

```csharp
Console.WriteLine(dynObj.id);                       // 67
Console.WriteLine(dynObj.name);                     // John Doe
Console.WriteLine(dynObj.profile.email);            // john@doe.com

var firstRole = dynObj.profile.roles.First();
Console.WriteLine(firstRole.roleName);              // Admin
```

---

## 🔑 Key Sanitization (How Property Names Are Matched)

DynamicJson automatically normalizes all JSON property names using a
simple rule:

**By default: Only letters and digits are kept. All other characters are removed. (A–Z, a–z, 0–9)**

Examples:

| JSON Key           | Sanitized Form |
|-------------------|----------------|
| `First Name`      | `FirstName`    |
| `PROJECT NAME`    | `PROJECTNAME`  |
| `order-id`        | `orderid`      |
| `2024_total$`     | `2024total`    |

This means you can safely access JSON like:

```json
{
  "First Name": "Harry"
  "order-id": 12345
}
```

Using:
```csharp
dynObj.FirstName  // "Harry"
dynObj.OrderId    // 12345
```

### Custom sanitization delegate

You can supply a `Func<char, bool>` delegate that determines which characters are retained:

```csharp
// Example: allow letters, digits, underscores, and hyphens
Func<char, bool> filter = c =>
    char.IsLetterOrDigit(c) || c == '_' || c == '-';

var obj = new DynamicJsonObject(values, filter);

var sanitized = originalKey.Sanitize(filter);
```

### De-duplication of keys
After keys are sanitized, duplicates are automatically renamed by adding a numeric suffix:

-> The first occurrence keeps its name, and any additional collisions become key2, key3, and so on. This ensures every property remains unique without losing any values.

-> The order of properties is preserved as they appear in the original JSON.

Scalar properties:
```csharp
using WilliamSmithE.DynamicJson;

var jsonString = """
{
    "name": "John Doe",
    "age": 30,
    "job-title": "Analyst",
    "jobTitle": "Senior Analyst",
    "skills": ["C#", "JavaScript", "SQL"],
    "address": {
        "street": "123 Main St",
        "city": "Anytown",
        "zip": "12345"
    }
}
""";

var dynObj = jsonString.ToDynamic();

Console.WriteLine(dynObj.JobTitle);             // Analyst
Console.WriteLine(dynObj.JobTitle2);            // Senior Analyst
```

Object / Array properties:
```csharp
using WilliamSmithE.DynamicJson;

var jsonString = """
{
    "name": "John Doe",
    "skills": ["C#", "JavaScript", "SQL"],
    "Skills": ["Excel", "PowerBI", "Tableau"],
    "Skills": ["SqlServer", "Kubernetes", "AWS"],
    "Credentials": {
        "username": "johndoe",
        "password": "securepassword123"
    },
    "Credentials": {
        "apiKey": "ABCD"
    }
}
""";

var dyn = jsonString.ToDynamic();

Console.WriteLine(string.Join(", ", dyn.Skills));                                   // C#, JavaScript, SQL
Console.WriteLine(string.Join(", ", dyn.Skills2));                                  // Excel, PowerBI, Tableau
Console.WriteLine(string.Join(", ", dyn.Skills3));                                  // SqlServer, Kubernetes, AWS

Console.WriteLine(dyn.Credentials.Username + " | " + dyn.Credentials.Password);     // johndoe | securepassword123
Console.WriteLine(dyn.Credentials2.ApiKey);                                         // ABCD
```

---

## 🔢 Value Type Handling in DynamicJson

DynamicJson automatically maps JSON primitives and CLR value types into appropriate .NET types.

### Type Mapping

| JSON / CLR Value      | Resulting DynamicJson Type | Notes |
|-----------------------|----------------------------|-------|
| `123`                 | `long` or `double`         | Integers stay `long`; large/float-like values become `double`. |
| `19.99`               | `double` or `decimal`      | Cast inside LINQ projections. |
| `\"2025-12-13T00:00Z\"` | `DateTime`               | ISO-like strings auto-parse to `DateTime`. |
| `true` / `false`      | `bool`                     | Direct mapping. |
| `null`                | `null`                     | Preserved. |

---

## 🔍 LINQ works naturally

Use the `.AsEnumerable()` extension method to enable LINQ queries on `DynamicJsonList` objects.

> ⚠️ When using `.AsEnumerable(...)` with a dynamic list, cast the source to `DynamicJsonList` so the lambda can be bound correctly by the C# compiler.

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
    ((DynamicJsonList)dynObj.users)
        .AsEnumerable()
        .Where(u =>
            ((DynamicJsonList)u.roles)
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

⚠️ Casting Disclaimer:  
    
Because `AsEnumerable()` produces `IEnumerable<dynamic>`, **LINQ cannot infer the numeric type automatically**.  
  
This means:
  
- **You must cast inside projection lambdas** (e.g., for `Sum`, `Average`, `Max`, etc.).
- **Without casting**, LINQ will default to the `int` overload, which can cause runtime binder errors.

### Accessing Value Types

```csharp
double price = (double)dynItem.Price;
long qty = (long)dynItem.Qty;
bool active = (bool)dynUser.IsActive;
DateTime ts = (DateTime)dynRecord.Timestamp;
```

## 🎯 Mapping to POCOs

DynamicJson maps JSON to CLR objects using sanitized, case-insensitive
property matching.

This means JSON like:

```json
{
  "Created Date": "1/1/2025"
}

OR

{
  "Created-Date": "1/1/2025"
}
```

Will correctly populate a POCO property named:

```csharp
public DateTime CreatedDate { get; set; }
```
### Example POCO Mapping

```csharp
public class MyClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

MyClass instance = dynObj.AsType<MyClass>();
Console.WriteLine(instance.Id);                  // 67
```

### Nested objects

```csharp
public class Profile
{
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

var profile = dynObj.profile.AsType<Profile>();
Console.WriteLine(profile.Department);           // Engineering
```

---

## 🔄 Serializing Back to JSON

```csharp
var profileJson = dynObj.profile.ToJson();
Console.WriteLine(profileJson);
```

Or via helper:

```csharp
var jsonOut = DynamicJson.ToJson(dynObj.preferences.dashboardWidgets);
Console.WriteLine(jsonOut);
```

---

## 🏗️ Working With Lists

```csharp
foreach (var role in dynObj.profile.roles)
{
    Console.WriteLine(role.roleName);
}
```
Indexing into a `DynamicJsonList` behaves like a normal .NET list:

```csharp
Console.WriteLine(dynObj.profile.roles[0].roleName); // valid

Console.WriteLine(dynObj.profile.roles[5]); 
// throws IndexOutOfRangeException with a clear message
```

Mapping to POCOs:

```csharp
public class Role
{
    public string RoleName { get; set; } = string.Empty;
    public int Level { get; set; }
}

var roles = dynObj.profile.roles.ToList<Role>();
```

`.ToScalarList()`:

```csharp
using WilliamSmithE.DynamicJson;

var dyn = """
{
  "Users": [
    { "Name": "Alice", "Age": 30, "Locations": ["Boston", "Chicago"] },
    { "Name": "Bob",   "Age": 25, "Locations": ["New York", "Los Angeles"] }
  ]
}
""".ToDynamic();

Console.WriteLine((
    (List<string>)dyn                   // Cast to List<string>
        .Users                          // Access Users array
        .First()                        // Get the first user
        .Locations                      // Access Locations array
        .ToScalarList<string>())        // Convert to List<string>    
    .Skip(1)                            // Get the second location
    .First());                          // Output: Chicago
```

---

## 📘 Example End-to-End

```csharp
using WilliamSmithE.DynamicJson;

// JSON comes from outside your system (HTTP, file, DB, etc.)
var customerJson = """
{
  "CustomerId": 42,
  "Name": "Jane Doe",
  "Email": "jane@example.com"
}
""";

var cartItemsJson = """
[
  { "Sku": "ABC123", "Qty": 1, "Price": 19.99 },
  { "Sku": "XYZ789", "Qty": 2, "Price": 5.00 }
]
""";

// 1) Convert JSON → dynamic JSON objects
dynamic customer = customerJson.ToDynamic();
var cartItems = (DynamicJsonList)cartItemsJson.ToDynamic();

customer.Name = "John Doe";
customer.Email = "john@example.com";

// Work with value types dynamically
var dynamicTotal = cartItems
    .AsEnumerable()
    .Sum(x => (long)x.Qty * (double)x.Price);

Console.WriteLine($"Dynamic cart total: {dynamicTotal}");

// 2) Build outbound payload as a CLR anonymous object
var payload = new
{
    customer = Raw.ToRawObject(customer),
    items = Raw.ToRawObject(cartItems),
    total = dynamicTotal,
    timestamp = DateTime.UtcNow
};

payload.customer.Name = "James Doe";

// 3) Convert entire payload → dynamic JSON
dynamic dyn = payload.ToDynamic();

// 4) Use the result dynamically
Console.WriteLine((string)dyn.customer.Name);      // "John Doe"
Console.WriteLine((double)dyn.total);              // 29.99 → double
Console.WriteLine((string)dyn.items[0].Sku);       // "ABC123"

// 5) Modify before sending
dyn.customer.Email = "billing@" + dyn.customer.Email;

// 6) Serialize back for HTTP call
var finalJson = DynamicJson.ToJson(dyn);

Console.WriteLine("Final outbound JSON:");
Console.WriteLine(finalJson);

// Dynamic cart total: 29.99
// John Doe
// 29.99
// ABC123
// Final outbound JSON:
// {
//     "customer": {
//         "CustomerId": 42,
//         "Name": "John Doe",
//         "Email": "billing@john@example.com"
//     },
//     "items": [
//         {
//             "Sku": "ABC123",
//             "Qty": 1,
//             "Price": 19.99
//         },
//         {
//             "Sku": "XYZ789",
//             "Qty": 2,
//             "Price": 5
//         }
//     ],
//     "total": 29.99,
//     "timestamp": "2025-12-13T09:47:40.4611875Z"
// }
```

---

## 🧩 Dynamic JSON Diff & Patch

### What “Diff” Does

Diff compares two JSON values and produces a minimal change object that describes only what is different between them. It does not return the entire JSON structure. This represents the smallest set of updates needed to turn the first object into the second.

Example:

```csharp
using WilliamSmithE.DynamicJson;

dynamic before = """
{
  "Name": "Alice",
  "Age": 30,
  "City": "Boston"
}
""".ToDynamic();

dynamic after = """
{
  "Name": "Alicia",
  "Age": 31,
  "City": "Boston"
}
""".ToDynamic();

// Compute the minimal diff between the two JSON values
dynamic patch = DynamicJson.DiffDynamic(before, after);

Console.WriteLine(DynamicJson.ToJson(patch));

// Output:
// {
//   "Name": "Alicia",
//   "Age": 31
// }
```

### What “Patch” Does

Patch takes an original JSON value and a diff, and applies those changes to produce an updated JSON value.

Example:

```csharp
using WilliamSmithE.DynamicJson;

dynamic before = """
{
  "Name": "Alice",
  "Age": 30,
  "City": "Boston"
}
""".ToDynamic();

dynamic after = """
{
  "Name": "Alicia",
  "Age": 31,
  "City": "Boston"
}
""".ToDynamic();

// First compute the diff
dynamic patch = DynamicJson.DiffDynamic(before, after);

// Apply the diff to the original
dynamic patched = DynamicJson.ApplyPatchDynamic(before, patch);

Console.WriteLine(DynamicJson.ToJson(patched));

// Output:
// {
//   "Name": "Alicia",
//   "Age": 31,
//   "City": "Boston"
// }
```

---

## 🔀 Merging Dynamic JSON Objects

Merge combines two JSON values into a single result by overlaying the fields from the second value onto the first. Unlike ApplyPatch, which applies only changes, merge performs a full union of both JSON structures.

Example:

```csharp
using WilliamSmithE.DynamicJson;

dynamic left = """
{
  "Name": "Alice",
  "Address": { "City": "Boston" },
  "Tags": ["user"]
}
""".ToDynamic();

dynamic right = """
{
  "Age": 30,
  "Address": { "Zip": "02110" },
  "Tags": ["admin"]
}
""".ToDynamic();

dynamic merged = DynamicJson.MergeDynamic(left, right);

Console.WriteLine(DynamicJson.ToJson(merged));

// Output:
// {
//   "Name": "Alice",
//   "Address": { "City": "Boston", "Zip": "02110" },
//   "Tags": ["admin"],
//   "Age": 30
// }

dynamic mergedConcat = DynamicJson.MergeDynamic(left, right, concatArrays: true);

Console.WriteLine(DynamicJson.ToJson(mergedConcat));

// Output with concatArrays = true:
// {
//   "Name": "Alice",
//   "Address": { "City": "Boston", "Zip": "02110" },
//   "Tags": ["user", "admin"],
//   "Age": 30
// }
```

---

## 🧬 Cloning a DynamicJson Object / List

The `Clone` method creates a deep copy of the `DynamicJson` object, including all nested structures. This allows you to work with a copy of the data without affecting the original object.

Example:

```csharp
using WilliamSmithE.DynamicJson;

dynamic original = """
{
  "Name": "Alice",
  "Age": 30,
  "City": "Boston"
}
""".ToDynamic();

dynamic copy = original.Clone();

copy.Name = "Alicia";

Console.WriteLine(original.Name);   // Output: Alice
Console.WriteLine(copy.Name);       // Output: Alicia
```

---

## 🛤️ JsonPath: A Structural Identifier for JSON Locations

JsonPath is a value type that represents a specific location inside a JSON structure.
It is designed to be composable, comparable, hashable, and enumerable.

Unlike string paths, a JsonPath is:

- Built structurally

- Compared structurally

- Safe to use as a dictionary key

- Independent of any particular JSON instance

```csharp
using WilliamSmithE.DynamicJson;

var p1 = JsonPath.Root.Property("user").Property("orders").Index(0).Property("id");
var p2 = JsonPath.Root.Property("user").Property("orders").Index(1).Property("id");
var p3 = JsonPath.Root.Property("user").Property("orders").Index(0).Property("id");

Console.WriteLine(p1);                 // /user/orders[0]/id
Console.WriteLine(p2);                 // /user/orders[1]/id
Console.WriteLine(p1 == p3);           // True

var dict = new Dictionary<JsonPath, string>
{
    [p1] = "Order0",
    [p2] = "Order1"
};

Console.WriteLine(dict[p3]);           // Order0

foreach (var seg in p1)
{
    Console.WriteLine(seg.Kind == JsonPath.SegmentKind.Property
        ? seg.PropertyName
        : $"[{seg.ArrayIndex}]");
}

// Expected Output:
// /user/orders[0]/id
// /user/orders[1]/id
// True
// Order0
// user
// orders
// [0]
// id
```

---

## 📄 License

MIT License. See `LICENSE` file for details.