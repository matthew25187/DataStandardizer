# Use country codes

Country codes from ISO 3166 are available in both alpha-2 and alpha-3 variants.  They take the form of the `Iso3166Part1Alpha2Country` and `Iso3166Part1Alpha3Country` enums, respectively.

## Alpha-2 country codes

The `Iso3166Part1Alpha2Country` enum contains a member for each of the ISO 3166-1 alpha-2 country codes.  The name of the member is the country code with the value being the numeric code associated with the alpha-2 code.

    // United Kingdom of Great Britain and Northern Ireland
    var ukCountryCode = Iso3166Part1Alpha2Country.GB;

## Alpha-3 country codes

The `Iso3166Part1Alpha3Country` enum contains a member for each of the ISO 3166-1 alpha-3 country codes.  The name of the member is the country code with the value being the numeric code associated with the alpha-3 code.

    // Greece
    var greeceCountryCode = Iso3166Part1Alpha3Country.GRC;