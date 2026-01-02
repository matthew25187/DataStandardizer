# Use E.164 international numbers

The *DataStandardizer.Communication* package provides an implementation of E.164 international numbers that are used in telephony applications to identify individual subscribers.  Using this package, you will be able to store international numbers in strongly-typed variables, format the presentation of international numbers according to your requirements, and parse international numbers from strings.

## Types of international numbers {#international-number-types}

As per the E.164 standard, the *DataStandardizer.Communication* package supports the creation of the following types of international numbers:

- International numbers for Geographic Areas
- International numbers for Global Services
- International numbers for Networks
- International numbers for Groups of Countries
- International numbers for Trials

It is likely that you will only ever need to use numbers for Geographical Areas as the other types are for specialised use cases and require the use of specific "country codes" that have been allocated for those purposes.

## Creating an international number

International numbers are represented by the `ItuE164InternationalNumber` type.  To create an instance of this type, you use the appropriate static factory method according to the type of international number you want to create (see [above](#international-number-types)).  These factory methods follow the pattern of having two overloads: one that takes an international number in the form of an integer, and a second overload that has parameters for each of the components of the international number.

With a number as an integer, you can create an international number like so:

    var myNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(12055550199L);

Alternately, you can specify the components of a number when creating it:

    var myNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(ItuE164AssignedCountryCodesForGeographicAreas.US, 2055550199L);

The second argument in this example could be specified either as a string or an integer, which may be useful if you need to specify a number with leading zeros that would otherwise be lost if the argument was expressed as an integer.

You may notice that there is no way to specify the "area code" and subscriber number separately when creating an international number.  This is because national-level concerns are outside the scope of the E.164 standard and are therefore not supported in this implementation.

## Parsing a string for an international number

If you have a number that is not already in the form of an integer, you can parse a string to extract the number so it can be stored in a `ItuE164InternationalNumber` type variable.

    var myNumberString = "+1 205 555-0199";
    var myNumber = ItuE164InternationalNumber.Parse(myNumberString);

This method will throw an exception if the input string does not contain an international number in a format it recognises.

Valid formats for numbers stored in strings permit the use of an optional international prefix symbol (the "+" sign) and spacing or separator characters between groups of digits.  N.B. the international number to be extracted must comprise the entirety of the string; the parsing methods provided by the `ItuE164InternationalNumber` type are not intended to be used to find international numbers embedded within a larger string.

If you are not sure if your string contains an international number in a valid format, you can attempt to parse the string without an exception being thrown if it fails.

    var isValid = ItuE164InternationalNumber.TryParse(myNumberString, out var myNumber);

It should be noted that when parsing a string to extract an international number, you don't need to know in advance or to tell the parse method what type of international number it should expect (for Geographic Areas, Global Services, etc.)  Because each type of international number has its own range of pre-allocated "country codes" assigned by ITU, the parse methods are able to automatically identify what type of number the string contains if it is valid.

## Formatting international numnber output

Similar to how other types in .Net are able to produce formatted strings to represent their values, the `ItuE164InternationalNumber` type provides `ToString()` overloads with various options to control how the string representation is generated.

    var myNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(14115550198L);
    var myFormattedNumber = myNumber.ToString(TelephonyInfo.CurrentTelephony);    // produces a string formatted using the standard "G" (general) pattern

You can also make use of .Net's built-in formatting capabilities to produce formatted strings for your `ItuE164InternationalNumber` variables.

    var myFormattedNumber = string.Format(TelephonyInfo.CurrentTelephony, "International: {0:G}", myNumber);

## Accessing parts of an international number

The `ItuE164InternationalNumber` type implements several interfaces that can be used to access the fields of an international number, depending on what type of number it is.  Once you know what type of number you have, use the correct interface to retrieve the field values from the relevant properties.

    var myNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(15125550190L);
    if (myNumber.IsNumberForGeographicArea())
    {
        IItuE164InternationalNumberForGeographicAreas myGeographicNumber = myNumber;
        var countryCode = myGeographicNumber.CountryCode;
        var nationalNumber = myGeographicNumber.NationalSignificantNumber;
    }

If you attempt to access properties through an interface on a number whose type does not match the interface, an exception will be thrown.