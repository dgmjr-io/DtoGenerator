/*
 * Run.cs
 *
 *   Created: 2023-01-06-01:43:40
 *   Modified: 2023-01-06-01:43:40
 *
 *   Author: David G. Moore, Jr. <david@dgmjr.io>
 *
 *   Copyright © 2022-2023 David G. Moore, Jr., All Rights Reserved
 *      License: MIT (https://opensource.org/licenses/MIT)
 */
#if EXE
using Dgmjr.CodeGeneration;
using Microsoft.CodeAnalysis;

var incGenerator = new Dgmjr.CodeGeneration.DtoGenerator();
var generator = incGenerator.AsSourceGenerator();

#endif
