# Access script metadata

There is metadata associated with each of the members of the ISO 15924 enum.  Extension methods are available to retrieve this metadata given a script code.

## Age

The ISO 15924 standard defines an age for each script code.  This age can be retrieved from a script code:

    var age = Iso15924Script.Arab.GetAge();

## Alias

Some script codes have an alias defined for them.  Where a script code has an alias, you can retrieve the alias like so:

    var alias = Iso15924Script.Adlm.GetAlias();

## Date

Each script code has a date associated with it.  You can retrieve the date using an extension method on a script code.

    var scriptDate = Iso15924Script.Egyp.GetDate();

## English name

Script codes have an English name defined for them.  The English name can be retrieved like so:

    var englishName = Iso15924Script.Perm.GetEnglishName();

## French name

The standard defines a French name for each of the script codes.  You can retrieve a script's French name with an extension method on the script code.

    var frenchName = Iso15924Script.Arab.GetFrenchName();