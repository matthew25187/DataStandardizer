# Use currency codes

ISO 4217 is implemented as two separate enums for current and historical currency codes.  Each member of the enum includes the currency code from the standard as the name of the enum member and the numeric code from the standard as the value of the member.

To access an individual currency code from the current collection you can use it like any other enum.

    var currencyCode = Iso4217CurrencyCurrent.INR;  // Indian Rupee

Similarly, historical currency codes can be accessed from the relevant enum.

    var oldCurrencyCode = Iso4217CurrencyHistoric.ZWD;  // Zimbabwe Dollar