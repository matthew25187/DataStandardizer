# Access country and subdivision metadata

There is metadata associated with each of the codes defined by ISO 3166.  This metadata can be retrieved using extension methods on a member of one of the ISO 3166 enums.

## ISO 3166-1 country codes

### English name

You can get the English name for a country by its alpha-2 or alpha-3 code.  You are given the option of retrieving different forms of the name (as defined by ISO 3166) that include Full, Short, and Short Upper.  For example,

    // Thailand full name by alpha-2 code
    var thailandName = Iso3166Part1Alpha2Country.TH.GetEnglishName(Iso3166CountryName.Full);

or,

    // USA short name by alpha-3 code
    var usaName = Iso3166Part1Alpha3Country.USA.GetEnglishName(Iso3166CountryName.Short);

### Native name

You can get the native name for a country by its alpha-2 or alpha-3 code.  You are given the option of retrieving both the name in a language other than English, as well as different forms of the name (as with the English name).

    // Greece short name in Greek by alpha-2 code
    var greeceName = Iso3166Part1Alpha2Country.GR.GetNativeName("el", Iso3166CountryName.Short);

N.B. For countries that are natively English-speaking, their native name may already be in English but may have also been defined in other languages.

### Independence

You can get a flag that indicates if the territory is an independent nation or administered by some other country.  For example,

    // Greenland independence
    var isGreenlandIndependent = Iso3166Part1Alpha2Country.GL.IsIndependent();

## ISO 3166-2 subdivision codes

### Category identifier

You can get the identifier of the category to which a subdivision belongs.

    // Australian Northern Territory category
    var ntCategoryId = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCategoryIdentifier();

### Category name

You can get the name of the category to which a subdivision belongs.

    // Australian Northern Territory category
    var ntCategoryName = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCategoryName("en");

The name of a category may also be available in a plural form, which can be retrieved separately.

    // Australian Northern Territory category
    var ntCategoryNamePlural = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCategoryNamePlural("en");

### Code

You can get the subdivision code as defined by ISO 3166-2 from a `Iso3166Part2Subdivision` code.

    var subdivisionCode = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCode();  // returns AU-NT

### Native name

You can get the native name of a subdivision in a supported language.

    var nativeName = Iso3166Part2Subdivision.ZA._EC.GetSubdivisionNativeName("af");

There may also be a local variant of the native name that can be retrieved separately.

    var nativeNameLocal = Iso3166Part2Subdivision.ZA._EC.GetSubdivisionNativeNameLocalVariant("zu");

### Parent code

Some countries have a hierarchical structure to their subdivisions.  You can retrieve the parent code for a subdivision from a `Iso3166Part2Subdivision` code.

    var subdivisionParentCode = Iso3166Part2Subdivision.PH._SCO.GetSubdivisionParentCode();