# PinionCore Serialization

## Introduction
This is a simple serialization tool that supports types ```short, ushort, int, uint, bool, long, ulong, float, decimal, double, char, byte, enum, string, System.Guid```, their array types, and custom classes/structs composed of them.


## Use

**Define**
```csharp
public struct ClassA
{
    public int Field1;
    public string Field2;
}
```

**Static**
```csharp
var classA = new ClassA();
classA.Field1 = 1;
classA.Field2 = "2";

// need to add the required type when creating.
var serializer = new PinionCore.Serialization.Serializer(new PinionCore.Serialization.DescriberBuilder(typeof(ClassA)).Describers);

// Serialize
var buffer = serializer.ObjectToBuffer(classA);

// Deserialize
var cloneClassA = (ClassA)serializer.BufferToObject(buffer);

```



**Dynamic**
```csharp
var classA = new ClassA();
classA.Field1 = 1;
classA.Field2 = "2";

var serializer = new PinionCore.Serialization.Dynamic.Serializer();

// Serialization
var buffer = serializer.ObjectToBuffer(classA);

// Deserialization
var cloneClassA = (ClassA)serializer.BufferToObject(buffer);

```

## Wire Format & Compression

Top-level layout is `[varint type-id][payload]`; `null` serializes to a single `0` byte. The encoder applies these size optimizations:

- **Varint (LEB128)** — every integer, length, enum value, and type-id is written as a 7-bit variable-length integer.
- **ZigZag** — `int`, `long`, and `short` are ZigZag-encoded before varint, so small negative numbers stay small (`-1` → 1 byte instead of 10).
- **UTF-8 strings** — strings are UTF-8 encoded with a varint length prefix.
- **Default-value skipping** — fields and array elements equal to their type's default (`0`, `null`, ...) are not written at all.
- **Field bitmask** — a class/struct payload starts with `ceil(N/8)` bytes whose bits mark which of the N public fields follow. Field identity is the ordinal in the name-sorted public-field list, so **renaming or adding/removing a public field is a wire-breaking change**.
- **Type-id elision** — a field or array element whose *declared* type is a value type or a `sealed` class (`string` and all array types included) carries no runtime type-id; the decoder derives the describer from the declared type. Fields declared as a non-sealed class still carry a runtime type-id so polymorphic values round-trip.
- **Dense arrays** — when every element differs from the default, per-element indices are omitted and elements are written in order; otherwise each written element is prefixed with its varint index (sparse encoding).

### Tips

- Mark protocol classes `sealed` whenever inheritance is not needed — sealed classes get the same encoding density as structs (no per-occurrence type-id).
- The wire format has no version negotiation. Both endpoints must run the same PinionCore.Serialization version.

### Limitations

- `DateTime`, `Nullable<T>`, and `sbyte` are not supported.
- Object graphs must be acyclic (no cycle detection).
