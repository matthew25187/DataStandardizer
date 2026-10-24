using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class AssemblySmokeTests
    {
        [Fact]
        public void Load_LibraryAssembly_HasExpectedTitle()
        {
            // arrange
            var assemblyName = new AssemblyName("DataStandardizer.File.iCal");

            // act
            var assembly = Assembly.Load(assemblyName);
            var testResult = assembly.GetCustomAttribute<AssemblyTitleAttribute>();

            // assert
            testResult.Should().NotBeNull();
            testResult.Title.Should().Be("Data Standardizer: File (iCal)");
        }
    }
}
