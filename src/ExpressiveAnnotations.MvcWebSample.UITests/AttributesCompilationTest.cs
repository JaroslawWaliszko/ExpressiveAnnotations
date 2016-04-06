﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ExpressiveAnnotations.Attributes;
using Xunit;

namespace ExpressiveAnnotations.MvcWebSample.UITests
{
    public static class Helper
    {
        public static IEnumerable<ExpressiveAttribute> CompileExpressiveAttributes(this Assembly assembly)
        {
            return assembly.GetTypes().SelectMany(t => t.CompileExpressiveAttributes());
        }

        public static IEnumerable<ExpressiveAttribute> CompileExpressiveAttributes(this Type type)
        {
            var properties = type.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(ExpressiveAttribute)));

            var attributes = new List<ExpressiveAttribute>();
            foreach (var prop in properties)
            {
                var attribs = prop.GetCustomAttributes<ExpressiveAttribute>().ToList();
                attribs.ForEach(x => x.Compile(prop.DeclaringType));
                attributes.AddRange(attribs);
            }
            return attributes;
        }
    }

    public class AttributesCompilationTest
    {
        private static string GetAssemblyLocation(string assemblyName) // looks inside bin folder of sample project
        {
            return Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\ExpressiveAnnotations.MvcWebSample\bin\", assemblyName));
        }

        private static Assembly LoadAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyPath = GetAssemblyLocation($"{new AssemblyName(args.Name).Name}.dll");
            return !File.Exists(assemblyPath) ? null : Assembly.LoadFrom(assemblyPath);
        }

        [Fact]
        public void expressive_annotations_within_sample_project_compile_with_success() // reveals compile-time errors (no need to wait for application startup)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += LoadAssembly;
                var assemblyPath = GetAssemblyLocation("ExpressiveAnnotations.MvcWebSample.dll");
                var assembly = Assembly.LoadFrom(assemblyPath);
                var attribs = assembly.CompileExpressiveAttributes();

                Assert.Equal(27, attribs.Count());
            }
            catch (ReflectionTypeLoadException e)
            {
                var sb = new StringBuilder();
                sb.AppendLine(e.Message).AppendLine();
                sb.AppendLine("LoaderExceptions:").AppendLine();

                foreach (var loaderEx in e.LoaderExceptions)
                {
                    sb.AppendLine(loaderEx.Message).AppendLine();
                    var fileNotFoundEx = loaderEx as FileNotFoundException;
                    if (string.IsNullOrEmpty(fileNotFoundEx?.FusionLog))
                        continue;

                    sb.AppendLine("FusionLog:");
                    sb.AppendLine(fileNotFoundEx.FusionLog);
                }

                throw new Exception(sb.ToString());
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= LoadAssembly;
            }
        }
    }
}
