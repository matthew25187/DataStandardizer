# Use subdivision codes

Individual subdivisions are represented by instances of the `Iso3166Part2Subdivision` type.  You don't create instances of `Iso3166Part2Subdivision` yourself, but rather use predefined instances for each of the defined subdivision codes.

As the ISO 3166-2 standard defines an individual set of subdivision codes for most countries, the predefined subdivision codes are implemented in a hierarchical structure in which the subdivision codes are delineated by the country code of the country the subdivisions belong to.  This takes the form of nested classes in the `Iso3166Part2Subdivision` type that are named after the ISO 3166-1 alpha-2 country code.  In these nested classes are members for each of the subdivision codes defined by ISO 3166-2 for that country.  For example,

    // Australian Capital Territory
    var act = Iso3166Part2Subdivision.AU._ACT;

You may notice that all subdivision codes start with an underscore.  This was motivated by a technical limitation on the naming of identifiers in .Net: as it is not possible to start an identifier with a digit, and some subdivision codes defined by ISO 3166-2 do start with digits, it was decided to make all subdivision codes start with the underscore for consistency.