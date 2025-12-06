# Access area metadata

There is metadata associated with each of the members of the UN M49 enums.  Extension methods are available to retrieve this metadata by members of the enums.

UN M49 defines names for each of the elements in a geographical hierarchy in six languages: English, Chinese, Russian, French, Spanish, and Arabic.

## Country or area

The name of the country or area for an area code can be retrieved.

    // Germany name in Arabic
    var countryName = UnM49AreaByAlpha2CountryCode.DE.GetCountryOrAreaName("ar");

To do the same thing using alpha-3 codes,

    // Germany name in Arabic
    var countryName = UnM49AreaByAlpha3CountryCode.DEU.GetCountryOrAreaName("ara");

## Global area

The identifier of the global area can be retrieved for any area code.

    var globalCode = UnM49AreaByAlpha2CountryCode.PT.GetGlobalCode();

And likewise the global name.

    var globalName = UnM49AreaByAlpha2CountryCode.PT.GetGlobalName("en");

In practice, the global code is the same no matter which area code it is retrieved for because all areas defined by the standard are on planet Earth.

## Intermediate region

Intermediate regions are identified by code retrieved like so:

    // Italy region code
    var intermediateRegionCode = UnM49AreaByAlpha2CountryCode.IT.GetIntermediateRegionCode();

The name of an intermediate region can also be retrieved from an area code.

    // Italy region name
    var intermediateRegionName = UnM49AreaByAlpha2CountryCode.IT.GetIntermediateRegionName("en");

## Region

The identifier of an area's region can be retrieved:

    var regionCode = UnM49AreaByAlpha2CountryCode.CH.GetRegionCode();

And the region name:

    var regionName = UnM49AreaByAlpha2CountryCode.CH.GetRegionName("en");

## Sub region

A sub region's code can be retrieved from an area code.

    var subRegionCode = UnM49AreaByAlpha2CountryCode.RO.GetSubRegionCode();

And the sub region name.

    var subRegionName = UnM49AreaByAlpha2CountryCode.RO.GetSubRegionName("en");