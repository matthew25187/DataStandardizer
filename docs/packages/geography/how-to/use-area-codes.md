---
title: Use area codes
parent: Geography
grand_parent: Packages
nav_order: 4
---

# Use area codes

UN M49 area codes are exposed through two enums that key the area codes by
either the alpha-2 or alpha-3 country codes from ISO 3166-1.

Although UN M49 defines area codes only in numeric form, technical limitations
on identifier naming in .NET mean the enum members must take an alpha (or
alpha-numeric) form. As a result, some area codes are only accessible as
metadata, because they do not relate directly to an ISO 3166-1 country code.

To access an area code directly, you must first know the country code it relates
to. For example:

```csharp
var switzerlandAreaCode = (ushort)UnM49AreaByAlpha2CountryCode.CH;
```

Or by its alpha-3 country code:

```csharp
var switzerlandAreaCode = (ushort)UnM49AreaByAlpha3CountryCode.CHE;
```

Other area codes can be retrieved indirectly from a member of one of these enums
using the provided extension methods, which read this information from the area
code's metadata (see [Access area metadata](access-area-metadata.md)).
