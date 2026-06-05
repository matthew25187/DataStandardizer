---
title: Use script codes
parent: Language
grand_parent: Packages
nav_order: 3
---

# Use script codes

ISO 15924 script codes are implemented in the `Iso15924Script` enum, where each
member's name is the script code from the standard and its value is the numeric
code from the standard.

To access an individual script code:

```csharp
// Cyrillic script
var script = Iso15924Script.Cyrl;
```

## Next steps

- [Access script metadata](access-script-metadata.md)
