---
title: Iso3166Part2Subdivision Struct
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166Part2Subdivision Struct

## Definition

Namespace: `DataStandardizer.Geography`

An ISO 3166-2 country subdivision code (for example `AD-07`). You don't construct
these; instead use the predefined static members, grouped by country in nested
static classes and accessed as `Iso3166Part2Subdivision.<country>.<member>` (for
example `Iso3166Part2Subdivision.AD._07`).

```csharp
public readonly partial struct Iso3166Part2Subdivision : IComparable, IEquatable<Iso3166Part2Subdivision>
```

## Remarks

Per-subdivision metadata (code, name, category) is read through the extension
methods on [Iso3166Extensions](Iso3166Extensions.md). See
[Use subdivision codes](../how-to/use-subdivision-codes.md).

## Fields

The subdivision members are grouped by ISO 3166-1 alpha-2 country code:

| Country | Subdivisions |
| --- | --- |
| [AD](Iso3166Part2Subdivision.AD.md) | 7 |
| [AE](Iso3166Part2Subdivision.AE.md) | 7 |
| [AF](Iso3166Part2Subdivision.AF.md) | 34 |
| [AG](Iso3166Part2Subdivision.AG.md) | 8 |
| [AL](Iso3166Part2Subdivision.AL.md) | 12 |
| [AM](Iso3166Part2Subdivision.AM.md) | 11 |
| [AO](Iso3166Part2Subdivision.AO.md) | 18 |
| [AR](Iso3166Part2Subdivision.AR.md) | 24 |
| [AT](Iso3166Part2Subdivision.AT.md) | 9 |
| [AU](Iso3166Part2Subdivision.AU.md) | 8 |
| [AZ](Iso3166Part2Subdivision.AZ.md) | 78 |
| [BA](Iso3166Part2Subdivision.BA.md) | 3 |
| [BB](Iso3166Part2Subdivision.BB.md) | 11 |
| [BD](Iso3166Part2Subdivision.BD.md) | 72 |
| [BE](Iso3166Part2Subdivision.BE.md) | 13 |
| [BF](Iso3166Part2Subdivision.BF.md) | 58 |
| [BG](Iso3166Part2Subdivision.BG.md) | 28 |
| [BH](Iso3166Part2Subdivision.BH.md) | 4 |
| [BI](Iso3166Part2Subdivision.BI.md) | 18 |
| [BJ](Iso3166Part2Subdivision.BJ.md) | 12 |
| [BN](Iso3166Part2Subdivision.BN.md) | 4 |
| [BO](Iso3166Part2Subdivision.BO.md) | 9 |
| [BQ](Iso3166Part2Subdivision.BQ.md) | 3 |
| [BR](Iso3166Part2Subdivision.BR.md) | 27 |
| [BS](Iso3166Part2Subdivision.BS.md) | 32 |
| [BT](Iso3166Part2Subdivision.BT.md) | 20 |
| [BW](Iso3166Part2Subdivision.BW.md) | 16 |
| [BY](Iso3166Part2Subdivision.BY.md) | 7 |
| [BZ](Iso3166Part2Subdivision.BZ.md) | 6 |
| [CA](Iso3166Part2Subdivision.CA.md) | 13 |
| [CD](Iso3166Part2Subdivision.CD.md) | 26 |
| [CF](Iso3166Part2Subdivision.CF.md) | 17 |
| [CG](Iso3166Part2Subdivision.CG.md) | 12 |
| [CH](Iso3166Part2Subdivision.CH.md) | 26 |
| [CI](Iso3166Part2Subdivision.CI.md) | 14 |
| [CL](Iso3166Part2Subdivision.CL.md) | 16 |
| [CM](Iso3166Part2Subdivision.CM.md) | 10 |
| [CN](Iso3166Part2Subdivision.CN.md) | 34 |
| [CO](Iso3166Part2Subdivision.CO.md) | 33 |
| [CR](Iso3166Part2Subdivision.CR.md) | 7 |
| [CU](Iso3166Part2Subdivision.CU.md) | 16 |
| [CV](Iso3166Part2Subdivision.CV.md) | 24 |
| [CY](Iso3166Part2Subdivision.CY.md) | 6 |
| [CZ](Iso3166Part2Subdivision.CZ.md) | 90 |
| [DE](Iso3166Part2Subdivision.DE.md) | 16 |
| [DJ](Iso3166Part2Subdivision.DJ.md) | 6 |
| [DK](Iso3166Part2Subdivision.DK.md) | 5 |
| [DM](Iso3166Part2Subdivision.DM.md) | 10 |
| [DO](Iso3166Part2Subdivision.DO.md) | 42 |
| [DZ](Iso3166Part2Subdivision.DZ.md) | 58 |
| [EC](Iso3166Part2Subdivision.EC.md) | 24 |
| [EE](Iso3166Part2Subdivision.EE.md) | 94 |
| [EG](Iso3166Part2Subdivision.EG.md) | 27 |
| [ER](Iso3166Part2Subdivision.ER.md) | 6 |
| [ES](Iso3166Part2Subdivision.ES.md) | 69 |
| [ET](Iso3166Part2Subdivision.ET.md) | 13 |
| [FI](Iso3166Part2Subdivision.FI.md) | 19 |
| [FJ](Iso3166Part2Subdivision.FJ.md) | 19 |
| [FM](Iso3166Part2Subdivision.FM.md) | 4 |
| [FR](Iso3166Part2Subdivision.FR.md) | 124 |
| [GA](Iso3166Part2Subdivision.GA.md) | 9 |
| [GB](Iso3166Part2Subdivision.GB.md) | 221 |
| [GD](Iso3166Part2Subdivision.GD.md) | 7 |
| [GE](Iso3166Part2Subdivision.GE.md) | 12 |
| [GH](Iso3166Part2Subdivision.GH.md) | 16 |
| [GL](Iso3166Part2Subdivision.GL.md) | 5 |
| [GM](Iso3166Part2Subdivision.GM.md) | 6 |
| [GN](Iso3166Part2Subdivision.GN.md) | 41 |
| [GQ](Iso3166Part2Subdivision.GQ.md) | 10 |
| [GR](Iso3166Part2Subdivision.GR.md) | 14 |
| [GT](Iso3166Part2Subdivision.GT.md) | 22 |
| [GW](Iso3166Part2Subdivision.GW.md) | 12 |
| [GY](Iso3166Part2Subdivision.GY.md) | 10 |
| [HN](Iso3166Part2Subdivision.HN.md) | 18 |
| [HR](Iso3166Part2Subdivision.HR.md) | 21 |
| [HT](Iso3166Part2Subdivision.HT.md) | 10 |
| [HU](Iso3166Part2Subdivision.HU.md) | 43 |
| [ID](Iso3166Part2Subdivision.ID.md) | 45 |
| [IE](Iso3166Part2Subdivision.IE.md) | 30 |
| [IL](Iso3166Part2Subdivision.IL.md) | 6 |
| [IN](Iso3166Part2Subdivision.IN.md) | 36 |
| [IQ](Iso3166Part2Subdivision.IQ.md) | 19 |
| [IR](Iso3166Part2Subdivision.IR.md) | 31 |
| [IS](Iso3166Part2Subdivision.IS.md) | 72 |
| [IT](Iso3166Part2Subdivision.IT.md) | 126 |
| [JM](Iso3166Part2Subdivision.JM.md) | 14 |
| [JO](Iso3166Part2Subdivision.JO.md) | 12 |
| [JP](Iso3166Part2Subdivision.JP.md) | 47 |
| [KE](Iso3166Part2Subdivision.KE.md) | 47 |
| [KG](Iso3166Part2Subdivision.KG.md) | 9 |
| [KH](Iso3166Part2Subdivision.KH.md) | 25 |
| [KI](Iso3166Part2Subdivision.KI.md) | 3 |
| [KM](Iso3166Part2Subdivision.KM.md) | 3 |
| [KN](Iso3166Part2Subdivision.KN.md) | 16 |
| [KP](Iso3166Part2Subdivision.KP.md) | 13 |
| [KR](Iso3166Part2Subdivision.KR.md) | 17 |
| [KW](Iso3166Part2Subdivision.KW.md) | 6 |
| [KZ](Iso3166Part2Subdivision.KZ.md) | 20 |
| [LA](Iso3166Part2Subdivision.LA.md) | 18 |
| [LB](Iso3166Part2Subdivision.LB.md) | 8 |
| [LC](Iso3166Part2Subdivision.LC.md) | 10 |
| [LI](Iso3166Part2Subdivision.LI.md) | 11 |
| [LK](Iso3166Part2Subdivision.LK.md) | 34 |
| [LR](Iso3166Part2Subdivision.LR.md) | 15 |
| [LS](Iso3166Part2Subdivision.LS.md) | 10 |
| [LT](Iso3166Part2Subdivision.LT.md) | 70 |
| [LU](Iso3166Part2Subdivision.LU.md) | 12 |
| [LV](Iso3166Part2Subdivision.LV.md) | 43 |
| [LY](Iso3166Part2Subdivision.LY.md) | 22 |
| [MA](Iso3166Part2Subdivision.MA.md) | 87 |
| [MC](Iso3166Part2Subdivision.MC.md) | 17 |
| [MD](Iso3166Part2Subdivision.MD.md) | 37 |
| [ME](Iso3166Part2Subdivision.ME.md) | 25 |
| [MG](Iso3166Part2Subdivision.MG.md) | 6 |
| [MH](Iso3166Part2Subdivision.MH.md) | 26 |
| [MK](Iso3166Part2Subdivision.MK.md) | 80 |
| [ML](Iso3166Part2Subdivision.ML.md) | 11 |
| [MM](Iso3166Part2Subdivision.MM.md) | 15 |
| [MN](Iso3166Part2Subdivision.MN.md) | 22 |
| [MR](Iso3166Part2Subdivision.MR.md) | 15 |
| [MT](Iso3166Part2Subdivision.MT.md) | 68 |
| [MU](Iso3166Part2Subdivision.MU.md) | 12 |
| [MV](Iso3166Part2Subdivision.MV.md) | 21 |
| [MW](Iso3166Part2Subdivision.MW.md) | 31 |
| [MX](Iso3166Part2Subdivision.MX.md) | 32 |
| [MY](Iso3166Part2Subdivision.MY.md) | 16 |
| [MZ](Iso3166Part2Subdivision.MZ.md) | 11 |
| [NA](Iso3166Part2Subdivision.NA.md) | 14 |
| [NE](Iso3166Part2Subdivision.NE.md) | 8 |
| [NG](Iso3166Part2Subdivision.NG.md) | 37 |
| [NI](Iso3166Part2Subdivision.NI.md) | 17 |
| [NL](Iso3166Part2Subdivision.NL.md) | 18 |
| [NO](Iso3166Part2Subdivision.NO.md) | 13 |
| [NP](Iso3166Part2Subdivision.NP.md) | 7 |
| [NR](Iso3166Part2Subdivision.NR.md) | 14 |
| [NZ](Iso3166Part2Subdivision.NZ.md) | 17 |
| [OM](Iso3166Part2Subdivision.OM.md) | 11 |
| [PA](Iso3166Part2Subdivision.PA.md) | 14 |
| [PE](Iso3166Part2Subdivision.PE.md) | 26 |
| [PG](Iso3166Part2Subdivision.PG.md) | 22 |
| [PH](Iso3166Part2Subdivision.PH.md) | 99 |
| [PK](Iso3166Part2Subdivision.PK.md) | 7 |
| [PL](Iso3166Part2Subdivision.PL.md) | 16 |
| [PS](Iso3166Part2Subdivision.PS.md) | 16 |
| [PT](Iso3166Part2Subdivision.PT.md) | 20 |
| [PW](Iso3166Part2Subdivision.PW.md) | 16 |
| [PY](Iso3166Part2Subdivision.PY.md) | 18 |
| [QA](Iso3166Part2Subdivision.QA.md) | 8 |
| [RO](Iso3166Part2Subdivision.RO.md) | 42 |
| [RS](Iso3166Part2Subdivision.RS.md) | 32 |
| [RU](Iso3166Part2Subdivision.RU.md) | 83 |
| [RW](Iso3166Part2Subdivision.RW.md) | 5 |
| [SA](Iso3166Part2Subdivision.SA.md) | 13 |
| [SB](Iso3166Part2Subdivision.SB.md) | 10 |
| [SC](Iso3166Part2Subdivision.SC.md) | 27 |
| [SD](Iso3166Part2Subdivision.SD.md) | 18 |
| [SE](Iso3166Part2Subdivision.SE.md) | 21 |
| [SG](Iso3166Part2Subdivision.SG.md) | 5 |
| [SH](Iso3166Part2Subdivision.SH.md) | 3 |
| [SI](Iso3166Part2Subdivision.SI.md) | 212 |
| [SK](Iso3166Part2Subdivision.SK.md) | 8 |
| [SL](Iso3166Part2Subdivision.SL.md) | 5 |
| [SM](Iso3166Part2Subdivision.SM.md) | 9 |
| [SN](Iso3166Part2Subdivision.SN.md) | 14 |
| [SO](Iso3166Part2Subdivision.SO.md) | 18 |
| [SR](Iso3166Part2Subdivision.SR.md) | 10 |
| [SS](Iso3166Part2Subdivision.SS.md) | 10 |
| [ST](Iso3166Part2Subdivision.ST.md) | 7 |
| [SV](Iso3166Part2Subdivision.SV.md) | 14 |
| [SY](Iso3166Part2Subdivision.SY.md) | 14 |
| [SZ](Iso3166Part2Subdivision.SZ.md) | 4 |
| [TD](Iso3166Part2Subdivision.TD.md) | 23 |
| [TG](Iso3166Part2Subdivision.TG.md) | 5 |
| [TH](Iso3166Part2Subdivision.TH.md) | 78 |
| [TJ](Iso3166Part2Subdivision.TJ.md) | 5 |
| [TL](Iso3166Part2Subdivision.TL.md) | 13 |
| [TM](Iso3166Part2Subdivision.TM.md) | 6 |
| [TN](Iso3166Part2Subdivision.TN.md) | 24 |
| [TO](Iso3166Part2Subdivision.TO.md) | 5 |
| [TR](Iso3166Part2Subdivision.TR.md) | 81 |
| [TT](Iso3166Part2Subdivision.TT.md) | 15 |
| [TV](Iso3166Part2Subdivision.TV.md) | 8 |
| [TW](Iso3166Part2Subdivision.TW.md) | 22 |
| [TZ](Iso3166Part2Subdivision.TZ.md) | 31 |
| [UA](Iso3166Part2Subdivision.UA.md) | 27 |
| [UG](Iso3166Part2Subdivision.UG.md) | 139 |
| [UM](Iso3166Part2Subdivision.UM.md) | 9 |
| [US](Iso3166Part2Subdivision.US.md) | 57 |
| [UY](Iso3166Part2Subdivision.UY.md) | 19 |
| [UZ](Iso3166Part2Subdivision.UZ.md) | 14 |
| [VC](Iso3166Part2Subdivision.VC.md) | 6 |
| [VE](Iso3166Part2Subdivision.VE.md) | 25 |
| [VN](Iso3166Part2Subdivision.VN.md) | 63 |
| [VU](Iso3166Part2Subdivision.VU.md) | 6 |
| [WF](Iso3166Part2Subdivision.WF.md) | 3 |
| [WS](Iso3166Part2Subdivision.WS.md) | 11 |
| [YE](Iso3166Part2Subdivision.YE.md) | 22 |
| [ZA](Iso3166Part2Subdivision.ZA.md) | 9 |
| [ZM](Iso3166Part2Subdivision.ZM.md) | 10 |
| [ZW](Iso3166Part2Subdivision.ZW.md) | 10 |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | |
| `Equals(Iso3166Part2Subdivision other)` | `bool` | |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `ToString()` | `string` | Override. Returns the subdivision code. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator Iso3166Part2Subdivision(string)` | Wraps a subdivision code string. |
| Implicit | `implicit operator string(Iso3166Part2Subdivision)` | Unwraps to the code string. |
| Equality | `operator ==`, `!=` `(Iso3166Part2Subdivision, Iso3166Part2Subdivision)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166Extensions](Iso3166Extensions.md)
- [Use subdivision codes](../how-to/use-subdivision-codes.md)
- [Geography API reference](index.md)
