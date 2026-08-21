---
title: Overview
nav_order: 2
has_children: true
---

# Overview

*Data Standardizer* provides implementations of various internationally
recognised standards used in data processing, ranging from languages and scripts
to currencies, telephone numbers, and geographical entities.

Where a standard defines a fixed set of codes, that set is represented as a
strongly-typed enumeration (or a struct that behaves like one). By representing
standardised values as types rather than raw strings or integers, whole classes
of error — typos, invalid codes, mismatched formats — are caught by the compiler
instead of surfacing at runtime.

Supported target platforms include modern .NET and .NET Standard, so the packages
can be adopted in new applications as well as older codebases that are being
upgraded gradually or that must remain on older frameworks.

## In this section

- [Standards coverage](standards-coverage.md) — which standard is implemented
  by which package.
- [Platform support](platform-support.md) — the .NET and .NET Standard targets
  for each package.
