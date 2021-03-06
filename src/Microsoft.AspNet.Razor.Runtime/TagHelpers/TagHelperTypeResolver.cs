﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class that locates valid <see cref="ITagHelper"/>s within an assembly.
    /// </summary>
    public class TagHelperTypeResolver
    {
        private static readonly TypeInfo ITagHelperTypeInfo = typeof(ITagHelper).GetTypeInfo();

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperTypeResolver"/> class.
        /// </summary>
        public TagHelperTypeResolver()
        {
        }

        /// <summary>
        /// Loads an <see cref="Assembly"/> using the given <paramref name="name"/> and resolves
        /// all valid <see cref="ITagHelper"/> <see cref="Type"/>s.
        /// </summary>
        /// <param name="name">The name of an <see cref="Assembly"/> to search.</param>
        /// <param name="documentLocation">The <see cref="SourceLocation"/> of the associated
        /// <see cref="Parser.SyntaxTree.SyntaxTreeNode"/> responsible for the current <see cref="Resolve"/> call.
        /// </param>
        /// <param name="errorSink">The <see cref="ErrorSink"/> used to record errors found when resolving
        /// <see cref="ITagHelper"/> <see cref="Type"/>s.</param>
        /// <returns>An <see cref="IEnumerable{Type}"/> of valid <see cref="ITagHelper"/> <see cref="Type"/>s.
        /// </returns>
        public IEnumerable<Type> Resolve(string name, 
                                         SourceLocation documentLocation, 
                                         [NotNull] ErrorSink errorSink)
        {
            if (string.IsNullOrEmpty(name))
            {
                errorSink.OnError(documentLocation,
                                  Resources.TagHelperTypeResolver_TagHelperAssemblyNameCannotBeEmptyOrNull);

                return Type.EmptyTypes;
            }

            var assemblyName = new AssemblyName(name);

            IEnumerable<TypeInfo> libraryTypes;
            try
            {
                libraryTypes = GetExportedTypes(assemblyName);
            }
            catch (Exception ex)
            {
                errorSink.OnError(
                    documentLocation,
                    Resources.FormatTagHelperTypeResolver_CannotResolveTagHelperAssembly(
                        assemblyName.Name,
                        ex.Message));

                return Type.EmptyTypes;
            }

            var validTagHelpers = libraryTypes.Where(IsTagHelper);

            // Convert from TypeInfo[] to Type[]
            return validTagHelpers.Select(type => type.AsType());
        }

        /// <summary>
        /// Returns all exported types from the given <paramref name="assemblyName"/>
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName"/> to get <see cref="TypeInfo"/>s from.</param>
        /// <returns>
        /// An <see cref="IEnumerable{TypeInfo}"/> of types exported from the given <paramref name="assemblyName"/>.
        /// </returns>
        protected virtual IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);

            return assembly.ExportedTypes.Select(type => type.GetTypeInfo());
        }

        // Internal for testing.
        internal virtual bool IsTagHelper(TypeInfo typeInfo)
        {
            return typeInfo.IsPublic &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.IsGenericType &&
                   !typeInfo.IsNested &&
                   ITagHelperTypeInfo.IsAssignableFrom(typeInfo);
        }
    }
}