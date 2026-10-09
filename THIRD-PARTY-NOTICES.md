# Third-party notices

FpmlToolKit.Net's own code is licensed under the MIT license (see README.md). Each schema project and NuGet package
also contains XML schemas owned by third parties, under the terms below. Those terms, not the MIT license, govern the
schemas themselves.

## FpML schemas

Files: `fpml-*/xsd/fpml-*.xsd` (embedded as `FpmlSchemas/*` resources in each assembly of the `FpmlToolKit.Fpml*`
packages), and the official FpML
example documents in `tests/FpmlToolKit.Tests/Examples/` (copied unmodified from FpML's "Schema and Examples"
downloads).

The FpML Specifications of this document are subject to the FpML Public License (the "License"); you may not use the
FpML Specifications except in compliance with the License. You may obtain a copy of the License at
https://www.FpML.org.

The FpML Specifications distributed under the License are distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY
KIND, either express or implied. See the License for the specific language governing rights and limitations under the
License.

The Licensor of the FpML Specifications is the International Swaps and Derivatives Association, Inc. All Rights
Reserved.

FpML® is a registered trademark of the International Swaps and Derivatives Association, Inc. This project is not
affiliated with or endorsed by ISDA. The FpML Public License is published at
https://www.fpml.org/the_standard/fpml-public-license/.

The schemas for FpML 5.4-5.13 are FpML's published files, unmodified. For FpML 4.0-4.9 and 5.0-5.3, each version and
view is a single combined `fpml-main-*.xsd` file, made in 2012 from FpML's multi-file schemas with Eclipse EMF
tooling, which added `ecore:` annotations; these are modified versions of the FpML schemas. The README's Building
section lists how they differ from FpML's current downloads.

## XML Signature schema

Files: `fpml-*/xsd/xmldsig-core-schema.xsd` (also embedded in each assembly).

Copyright 2001 The Internet Society and W3C (Massachusetts Institute of Technology, Institut National de Recherche en
Informatique et en Automatique, Keio University). All Rights Reserved. http://www.w3.org/Consortium/Legal/

This document is governed by the W3C Software License, as described at
http://www.w3.org/Consortium/Legal/copyright-software-19980720.
