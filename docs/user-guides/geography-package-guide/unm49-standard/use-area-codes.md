# Use area codes

UN M49 area codes are available via two enums that implement the area codes by either alpha-2 or alpha-3 codes from ISO 3166-1.  Though UN M49 defines area codes only in numeric form, technical limitations on the naming of identifiers in .Net means that the enum members must have an alpha (or alpha-numeric) form to satisfy these requirements.  This means that some area codes will only be accessible as metadata as they do not relate directly to an ISO 3166-1 country code.

To access an area code directly, you must first know the country code to which it is related.  For example,

    var switzerlandAreaCode = (ushort)UnM49AreaByAlpha2CountryCode.CH;

Or by its alpha-3 country code,

    var switzerlandAreaCode = (ushort)UnM49AreaByAlpha3CountryCode.CHE;

Other area codes can be retrieved indirectly from a member of one of these enums using the provided extension methods that access this information from the area code's metadata (see [Access area metadata](access-area-metadata.md)).