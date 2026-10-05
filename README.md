# DynamicJson

[![NuGet version](https://img.shields.io/nuget/v/WilliamSmithE.DynamicJson)](https://www.nuget.org/packages/WilliamSmithE.DynamicJson)
[![Downloads](https://img.shields.io/nuget/dt/WilliamSmithE.DynamicJson)](https://www.nuget.org/packages/WilliamSmithE.DynamicJson)
[![CI](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/actions/workflows/ci.yml)
[![Security](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/actions/workflows/security.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/actions/workflows/security.yml)
[![Malware scan](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/actions/workflows/malware-scan.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/actions/workflows/malware-scan.yml)
[![OpenSSF Scorecard](https://img.shields.io/ossf-scorecard/github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson?label=OpenSSF%20Score)](https://scorecard.dev/viewer/?uri=github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/blob/main/LICENSE.txt)

DynamicJson parses JSON into `DynamicJsonObject` and `DynamicJsonList`, so you can read it with ordinary member access (`dynObj.profile.email`), query it with LINQ, and map it to your own classes when you want static types. It is built on System.Text.Json and targets .NET 8, 9 and 10.

```
dotnet add package WilliamSmithE.DynamicJson
```

---

## Features

- `json.ToDynamic()` turns a JSON object or array into a dynamic object or list.
- Member lookups ignore case, and a member the JSON does not have returns `null` instead of throwing.
- Dynamic lists support indexing, `foreach` and LINQ.
- JSON strings, numbers, booleans and nulls become `string`, `long` or `double`, `bool` and `null`; ISO 8601 date strings become `DateTime`.
- `AsType<T>()` maps an object to a class by reflection, `ToList<T>()` maps an array of objects to a `List<T>`, and `ToScalarList<T>()` extracts an array of strings or numbers. None of them needs serializer configuration.
- Missing members return `null`, a value that `AsType<T>()` or `ToList<T>()` cannot convert throws (`TryAsType<T>()` returns false instead), and a bad index throws `IndexOutOfRangeException`.
- `ToJson()` writes a modified object back to compact JSON.
- The only parsing option is an optional key-sanitization delegate.
- Diff, patch and merge work on whole JSON structures, and `JsonPath` names a location for path-aware diffs and lookups.

---

## Getting Started

### Convert JSON to dynamic

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

`json.ToDynamic()` is the same as `DynamicJson.FromJson(json)`. The root of the JSON must be an object or an array: an object becomes a `DynamicJsonObject`, an array a `DynamicJsonList`. Any other root, such as a number or a string, throws `InvalidOperationException`. An empty or whitespace string throws `ArgumentNullException`, and invalid JSON throws a `JsonException`.

`ToDynamic()` also works on any other .NET object. It serializes the object with System.Text.Json and parses the result, so anonymous types and POCOs become dynamic JSON too.

---

## Dynamic Navigation

Read a dynamic JSON object the way you would read a POCO:

```csharp
Console.WriteLine(dynObj.id);                       // 67
Console.WriteLine(dynObj.name);                     // John Doe
Console.WriteLine(dynObj.profile.email);            // john@doe.com

var firstRole = dynObj.profile.roles.First();
Console.WriteLine(firstRole.roleName);              // Admin
```

---

## Key Sanitization (How Property Names Are Matched)

DynamicJson normalizes every JSON property name with one rule: by default, only letters and digits are kept and every other character is removed. A letter or digit is whatever `char.IsLetterOrDigit` accepts, so accented and non-Latin letters are kept too.

Examples:

| JSON Key           | Sanitized Form |
|-------------------|----------------|
| `First Name`      | `FirstName`    |
| `PROJECT NAME`    | `PROJECTNAME`  |
| `order-id`        | `orderid`      |
| `2024_total$`     | `2024total`    |

So this JSON:

```json
{
  "First Name": "Harry",
  "order-id": 12345
}
```

reads as:

```csharp
Console.WriteLine(dynObj.FirstName);  // Harry
Console.WriteLine(dynObj.OrderId);    // 12345
```

The member name you write is compared with the sanitized keys, ignoring case, and is not sanitized itself. `dynObj.order_id` therefore returns `null`, because the stored key is `orderid`. `TryGetValue`, `Properties`, `ToJson()`, JSON paths and diffs all use the same sanitized keys.

Assigning a member that does not exist adds it under the name exactly as written: after `dynObj.last_name = "Potter"`, `ToJson()` writes a `last_name` key.

### Custom sanitization delegate

A `Func<char, bool>` delegate decides which characters are kept. Pass it to `ToDynamic`, `DynamicJson.FromJson` or the `DynamicJsonObject` constructor, and it applies to every nested object:

```csharp
// Example: allow letters, digits, underscores, and hyphens
Func<char, bool> filter = c =>
    char.IsLetterOrDigit(c) || c == '_' || c == '-';

var dyn = """{ "first_name": "Harry", "order id": 12345 }""".ToDynamic(filter);

Console.WriteLine(dyn.first_name);    // Harry
Console.WriteLine(dyn.orderid);       // 12345

var obj = new DynamicJsonObject(new Dictionary<string, object?> { ["first name"] = "Harry" }, filter);
```

`Clone()` parses again with the default sanitizer, so a clone of an object built with a custom filter loses the extra characters (`first_name` becomes `firstname`).

### De-duplication of keys

When two keys sanitize to the same name, the first keeps it and the later ones get a numeric suffix: `key2`, `key3`, and so on. Every value is kept, and the properties stay in the order they appear in the JSON.

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

Object and array properties:

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

## Value Types

JSON values become these .NET types:

| JSON value            | .NET type                  | Notes |
|-----------------------|----------------------------|-------|
| `123`                 | `long` or `double`         | Integers stay `long`; integers too large for `long` become `double`. |
| `19.99`               | `double`                   | Cast inside LINQ projections. |
| `"2025-12-13T00:00Z"` | `DateTime`                 | ISO 8601 strings become `DateTime`. |
| `"text"`              | `string`                   | Any string that is not an ISO 8601 date. |
| `true` / `false`      | `bool`                     | |
| `null`                | `null`                     | |

Because numbers are `long` or `double`, cast to those types (`(long)x.Qty`, `(double)x.Price`) and use `ToScalarList<long>()` rather than `ToScalarList<int>()`.

Date strings follow System.Text.Json's ISO 8601 rules. A `Z` suffix gives a UTC `DateTime`, an offset such as `+02:00` is converted to the machine's local time, and a date with no time or offset gives a `DateTime` of unspecified kind. `ToJson()` writes the `DateTime` back, not the original string, so `"2025-12-21"` comes back as `"2025-12-21T00:00:00"`.

### Reading values

```csharp
dynamic dynItem = """{ "Price": 19.99, "Qty": 2 }""".ToDynamic();
dynamic dynUser = """{ "IsActive": true }""".ToDynamic();
dynamic dynRecord = """{ "Timestamp": "2025-12-13T00:00Z" }""".ToDynamic();

double price = (double)dynItem.Price;
long qty = (long)dynItem.Qty;
bool active = (bool)dynUser.IsActive;
DateTime ts = (DateTime)dynRecord.Timestamp;
```

---

## LINQ

Call `.AsEnumerable()` on a `DynamicJsonList` to query it with LINQ. When the list comes from a dynamic expression, cast it to `DynamicJsonList` first; the C# compiler cannot bind a lambda passed to a dynamic receiver.

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
    Console.WriteLine(name);    // Alice
}
```

`AsEnumerable()` returns `IEnumerable<dynamic>`, so LINQ cannot tell which numeric type a lambda returns. Cast inside the lambda for `Sum`, `Average`, `Max` and the like: `Sum(x => (long)x.Qty)`. Without the cast, C# picks the `int` overload, and the call fails at run time with a `RuntimeBinderException` because the values are `long`.

`AsEnumerable()` returns a JSON `null` element as a plain `object`, not as `null`.

The library adds a `First()` extension method on `IEnumerable<object?>`. It returns the first element that is a `DynamicJsonObject`, or `null` if there is none, and C# picks it over LINQ's `First()` for a `DynamicJsonList` and for the result of `AsEnumerable()`. On a list of numbers or strings it returns `null`; use an index or `ElementAt(0)` to get the first element of any kind. On a dynamic list (`dynObj.profile.roles.First()`), `First()` returns the first object or nested list.

---

## Mapping to POCOs

`AsType<T>()` matches JSON keys to property names after sanitizing both, ignoring case. Either of these:

```json
{
  "Created Date": "1/1/2025"
}
```

```json
{
  "Created-Date": "1/1/2025"
}
```

fills a property named:

```csharp
public DateTime CreatedDate { get; set; }
```

A value whose type already fits the property is assigned as it is. Anything else goes through `Convert.ChangeType` with the current culture, so `"1/1/2025"` is read with the culture's date format. A value that cannot be converted throws, and so does a nested object or array mapped to a class or list property; map those separately, as the next examples do. `TryAsType<T>(out var result)` returns false instead of throwing. `AsType<T>(filter)` takes an optional sanitization delegate for the property names.

### Example POCO mapping

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

## Serializing Back to JSON

```csharp
var profileJson = dynObj.profile.ToJson();
Console.WriteLine(profileJson);
// {"email":"john@doe.com","department":"Engineering","roles":[{"roleName":"Admin","level":5},{"roleName":"Developer","level":3}]}
```

Or with the static helper:

```csharp
var jsonOut = DynamicJson.ToJson(dynObj.preferences.dashboardWidgets);
Console.WriteLine(jsonOut);
// ["inbox","projects","metrics"]
```

The output is compact, and keys are written in their sanitized form.

---

## Working With Lists

```csharp
foreach (var role in dynObj.profile.roles)
{
    Console.WriteLine(role.roleName);
}
```

Indexing a `DynamicJsonList` works like indexing a .NET list. The index must be an `int`.

```csharp
Console.WriteLine(dynObj.profile.roles[0].roleName); // valid

Console.WriteLine(dynObj.profile.roles[5]); 
// throws IndexOutOfRangeException: Index 5 is out of range for this DynamicJsonList. Valid indices are 0 to 1.
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

`ToList<T>()` maps every object in the list with `AsType<T>()` and skips elements that are not objects. A value that cannot be converted throws, as it does in `AsType<T>()`. `Count` gives the number of elements.

`ToScalarList<T>()` keeps only the elements that already are a `T` and converts nothing, so numbers need `long` or `double`:

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
    (List<string>)dyn                   // Cast the result to List<string>
        .Users                          // Access Users array
        .First()                        // Get the first user
        .Locations                      // Access Locations array
        .ToScalarList<string>())        // Convert to List<string>    
    .Skip(1)                            // Get the second location
    .First());                          // Output: Chicago
```

---

## Example End-to-End

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

// 1) Convert JSON to dynamic JSON objects
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

// payload.customer is now a plain Dictionary<string, object?>, so it has no Name member to set.

// 3) Convert entire payload to dynamic JSON
dynamic dyn = payload.ToDynamic();

// 4) Use the result dynamically
Console.WriteLine((string)dyn.customer.Name);      // "John Doe"
Console.WriteLine((double)dyn.total);              // 29.99 (a double)
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
// {"customer":{"CustomerId":42,"Name":"John Doe","Email":"billing@john@example.com"},"items":[{"Sku":"ABC123","Qty":1,"Price":19.99},{"Sku":"XYZ789","Qty":2,"Price":5}],"total":29.99,"timestamp":"2025-12-13T09:47:40.4611875Z"}
```

The timestamp is the time the sample runs.

---

## Diff and Patch

### Diff

A diff lists only what differs between two JSON values: the changes that turn the first into the second.

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
// {"Name":"Alicia","Age":31}
```

The rules:

- Objects are compared key by key, ignoring the case of keys, and nested objects are diffed recursively.
- A key that exists only in the second value is added with its whole value.
- A key missing from the second value appears in the diff with the value `null`, which means "remove".
- Arrays and other values are compared whole. If anything in an array changed, the diff holds the entire new array.
- When nothing differs, `Diff` and `DiffDynamic` return `null`. They also return `null` when the second value is `null`, and a key whose value changes to `null` does not appear in the diff at all, so neither change can be told apart from "no change". `DiffWithPaths` (below) reports such a key as `Removed`.

`DynamicJson.Diff(original, updated)` returns the same diff as plain dictionaries and lists, and `DynamicJson.ApplyPatch(original, patch)` applies one and returns the result in the same plain form.

### Patch

A patch applies a diff to the original value and returns the updated value.

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
// {"Name":"Alicia","Age":31,"City":"Boston"}
```

A `null` in the patch removes that key, a nested object in the patch is applied recursively, and any other value replaces the original value whole. The original is left unchanged.

---

## Merging

A merge lays the fields of the second value over the first and keeps everything else from both. A patch applies only the changes a diff recorded; a merge takes the union.

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
// {"Name":"Alice","Address":{"City":"Boston","Zip":"02110"},"Tags":["admin"],"Age":30}

dynamic mergedConcat = DynamicJson.MergeDynamic(left, right, concatArrays: true);

Console.WriteLine(DynamicJson.ToJson(mergedConcat));

// Output with concatArrays = true:
// {"Name":"Alice","Address":{"City":"Boston","Zip":"02110"},"Tags":["user","admin"],"Age":30}
```

Nested objects are merged recursively, and keys match ignoring case. A `null` in the second value removes nothing: the first value's entry is kept. For any other pair of values the second one wins, and arrays are replaced unless `concatArrays` is true.

`DynamicJson.Merge(left, right)` returns the merged result as plain dictionaries and lists, without the `concatArrays` option; `DynamicJsonMerge.Merge(left, right, concatArrays)` has it.

---

## Cloning

`Clone()` makes a deep copy of a `DynamicJsonObject` or `DynamicJsonList`, nested values included, so changes to the copy leave the original alone.

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

`Clone()` serializes the value to JSON and parses it again with the default sanitizer.

---

## JsonPath

`JsonPath` is a value type naming one location inside a JSON structure, such as `/user/orders[0]/id`. You build it from property and index segments, two paths with the same segments are equal and hash alike (so a path works as a dictionary key), and it holds no reference to any JSON value.

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

`IsRoot`, `Length` and the indexer (`p1[0]`) give the segments too. `Property` throws `ArgumentException` for a null or empty name, and `Index` throws `ArgumentOutOfRangeException` for a negative index.

A path into a root array prints without a leading slash (`JsonPath.Root.Index(0).Property("name")` prints `[0]/name`), and `JsonPath.Parse` rejects that form. Write `/[0]/name` to parse it.

### Path-aware diffs

`DynamicJson.DiffWithPaths` compares two values and returns one entry per change, each with the path where it happened and whether the value was added, removed or modified. It follows the same rules as `Diff`, so a changed array is one entry for the whole array.

```csharp
using WilliamSmithE.DynamicJson;

var original = new
{
    user = new
    {
        orders = new[]
        {
            new { id = 10, price = 19.99m },
            new { id = 11, price = 5.00m }
        },
        address = new { zip = "94105" }
    }
}
.ToDynamic();

var updated = new
{
    user = new
    {
        orders = new[]
        {
            new { id = 10, price = 24.99m },      // price changed (but array is atomic)
            new { id = 11, price = 5.00m }
        }
        // address removed
    },
    metadata = new { lastUpdated = "2025-12-21" } // added
}
.ToDynamic();

var changes = DynamicJson.DiffWithPaths(original, updated);

foreach (var c in changes)
{
    Console.WriteLine($"{c.Kind,-9} {c.Path} | {DynamicJson.ToJson(c.OldValue)} -> {DynamicJson.ToJson(c.NewValue)}");
}

// Expected output:
// Modified  /user/orders | [{"id":10,"price":19.99},{"id":11,"price":5}] -> [{"id":10,"price":24.99},{"id":11,"price":5}]
// Removed   /user/address | {"zip":"94105"} -> null
// Added     /metadata | null -> {"lastUpdated":"2025-12-21T00:00:00"}
```

Each `DiffEntry` has `Path`, `OldValue`, `NewValue` and `Kind` (`Added`, `Removed` or `Modified`). The values are plain dictionaries, lists and primitives, and the paths use the sanitized keys. `lastUpdated` shows the date conversion described under Value Types: `"2025-12-21"` was read as a `DateTime`.

### Path navigation

`JsonPathNavigation` looks a path up in a dynamic JSON value. `TryGetAtPath` returns false when the path does not resolve, and `GetAtPath` throws `KeyNotFoundException`. Both take a `JsonPath` or a path string, so a path from a diff or a log can be read back directly.

The value comes back in plain form: a primitive, a `Dictionary<string, object?>` or a `List<object?>`, not a dynamic wrapper. Property names in the path are matched against the sanitized keys, ignoring case, so `/order-id` does not resolve and `/orderid` does.

```csharp
using WilliamSmithE.DynamicJson;

var json = new
{
    user = new
    {
        orders = new[]
        {
            new { id = 10, price = 19.99m },
            new { id = 11, price = 5.00m }
        }
    }
}
.ToDynamic();

var pathThatExists = JsonPath.Root
    .Property("user")
    .Property("orders")
    .Index(0)
    .Property("price");

if (JsonPathNavigation.TryGetAtPath(json, pathThatExists, out object? value))
    Console.WriteLine(value);                                                       // 19.99

Console.WriteLine(JsonPathNavigation.GetAtPath(json, pathThatExists));              // 19.99

var pathThatDoesNotExist = JsonPath.Root
    .Property("user")
    .Property("orders")
    .Index(2)
    .Property("price");

if (!JsonPathNavigation.TryGetAtPath(json, pathThatDoesNotExist, out object? _))
    Console.WriteLine("Path not found");                                            // Path not found

try
{
    JsonPathNavigation.GetAtPath(json, pathThatDoesNotExist);
}

catch (KeyNotFoundException)
{
    Console.WriteLine("Path not found");                                            // Path not found
}

var pathToOrders = JsonPath.Root
    .Property("user")
    .Property("orders");

var orders = JsonPathNavigation.GetAtPath(json, pathToOrders);

Console.WriteLine(DynamicJson.ToJson(orders));                                      // [{"id":10,"price":19.99},{"id":11,"price":5}]

var pathToUser = JsonPath.Root.Property("user");

var user = JsonPathNavigation.GetAtPath(json, pathToUser);

Console.WriteLine(DynamicJson.ToJson(user));                                        // {"orders":[{"id":10,"price":19.99},{"id":11,"price":5}]}
```

### Parsing paths from strings

`JsonPath.Parse` turns a path string such as `/user/orders[0]/price` into a `JsonPath` equal to the same path built in code. The string must start with `/`, and an index must be a non-negative integer in brackets; anything else throws `FormatException`. `TryParse` returns false instead, and the `JsonPath` it gives back on failure is unusable: reading `IsRoot`, `Length` or `ToString()` on it throws `NullReferenceException`.

```csharp
using WilliamSmithE.DynamicJson;

var json = new
{
    user = new
    {
        orders = new[]
        {
            new { id = 10, price = 19.99m }
        }
    }
}
.ToDynamic();

var path = JsonPath.Parse("/user/orders[0]/price");
Console.WriteLine(path); // /user/orders[0]/price

var value = JsonPathNavigation.GetAtPath(json, path);
Console.WriteLine(value);                                       // 19.99

Console.WriteLine(JsonPath.Parse("/").IsRoot);                  // True

try 
{ 
    JsonPath.Parse("user/orders"); 
} 

catch (FormatException) 
{ 
    Console.WriteLine("Invalid");                               // Invalid
}

try 
{ 
    JsonPath.Parse("/orders[-1]"); 
} 

catch (FormatException) 
{ 
    Console.WriteLine("Invalid");                               // Invalid
}

if (JsonPath.TryParse("/user/orders[0]/price", out var path2))
{
    var value2 = JsonPathNavigation.GetAtPath(json, path2);
    Console.WriteLine(value2);              // 19.99
}

if (!JsonPath.TryParse("/user/order[]", out _))                 // Invalid
{
    Console.WriteLine("Invalid");
}
```

### Validating paths

`JsonPathValidation.IsValidFor` returns true only when a path parses and resolves in the given value, and it never throws. Use it to check a path from user input, configuration or a log before reading with it.

```csharp
using WilliamSmithE.DynamicJson;

var json = new
{
    user = new
    {
        orders = new[]
        {
            new { id = 10, price = 19.99m }
        }
    }
}
.ToDynamic();

if (JsonPathValidation.IsValidFor(json, "/user/orders[0]/price"))
{
    Console.WriteLine("Path exists in this JSON");
    Console.WriteLine(JsonPathNavigation.GetAtPath(json, "/user/orders[0]/price"));
    Console.WriteLine();
}

if (!JsonPathValidation.IsValidFor(json, "/user/order"))
{
    Console.WriteLine("Path is valid syntax, but not valid for this JSON");
}

if (!JsonPathValidation.IsValidFor(json, "/user/orders[2]/price"))
{
    Console.WriteLine("Path is valid syntax, but does not exist in this Json");
}
```

---

## Other Members

The sections above cover the common calls. These public members are also available:

| Member | What it does |
|--------|--------------|
| `DynamicJson.FromJson(json, filter)` | Same as `json.ToDynamic(filter)`. |
| `DynamicJson.ToJson(value)` | Serializes a dynamic value, or any other value, to compact JSON. |
| `DynamicJsonObject.Properties` | The sanitized keys and their values, as a read-only dictionary. |
| `DynamicJsonObject.TryGetValue(name, out value)` | Looks up a sanitized key, ignoring case. |
| `DynamicJsonObject.KeyValuePairsAsString` | One `Key: Value` line per property, not recursive. |
| `DynamicJsonObject.ToRawObject()` | The object as a `Dictionary<string, object?>`, nested values included. |
| `DynamicJsonList.ToRawArray()` | The list as a `List<object?>`, nested values included. |
| `Raw.ToRawObject(value)` | Either of the two above, depending on the value; other values come back unchanged. |
| `DynamicJsonObjectCastingExtensions.AsType<T>(value)` | `AsType<T>()` for a value typed `object`: maps a `DynamicJsonObject`, returns a `T` as it is, and returns `null` for anything else. |
| `JsonElementExtensions.AsDynamic(element)` | Converts a System.Text.Json `JsonElement` (or a `List<JsonElement>`) to dynamic JSON. |
| `DynamicJsonDiff`, `DynamicJsonMerge`, `DynamicJsonPathDiff` | The classes behind `DynamicJson.Diff`, `Merge` and `DiffWithPaths`. |

---

## License

MIT License. See [LICENSE.txt](https://github.com/WilliamSmithEdward/WilliamSmithE.DynamicJson/blob/main/LICENSE.txt) for details.
