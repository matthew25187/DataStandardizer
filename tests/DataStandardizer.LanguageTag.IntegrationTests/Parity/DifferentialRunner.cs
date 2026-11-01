using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// An input of a differential test, with what the reference implementation gives for it.
    /// </summary>
    internal readonly struct DifferentialCase<T>
    {
        internal DifferentialCase(string input, T expected)
        {
            Input = input;
            Expected = expected;
        }

        internal string Input { get; }

        internal T Expected { get; }
    }

    /// <summary>
    /// An input for which the implementation under test gave something other than the reference implementation.
    /// </summary>
    internal sealed class DifferentialMismatch<T>
    {
        internal DifferentialMismatch(string input, T expected, T actual)
        {
            Input = InputDescription.Of(input);
            Expected = expected;
            Actual = actual;
        }

        /// <summary>
        /// The input, quoted, with every character outside printable ASCII written as a <c>\uXXXX</c> escape.
        /// </summary>
        public string Input { get; }

        public T Expected { get; }

        public T Actual { get; }
    }

    /// <summary>
    /// Runs a differential test: observes the implementation under test for every input of a corpus, and finds the inputs for which it gives
    /// something other than the reference implementation did.
    /// </summary>
    /// <remarks>
    /// Its own tests are in <see cref="DifferentialRunnerTests"/>. A parity test makes one call to <see cref="FindMismatches{T}"/> and asserts
    /// that there are none, so the test itself has no loop or condition. The inputs are observed on every core.
    /// </remarks>
    internal static class DifferentialRunner
    {
        internal static IReadOnlyList<DifferentialMismatch<T>> FindMismatches<T>(IReadOnlyList<DifferentialCase<T>> cases, Func<string, T> observe)
        {
            var comparer = EqualityComparer<T>.Default;
            return cases
                .AsParallel()
                .AsOrdered()
                .Select(differentialCase => new { differentialCase, actual = observe(differentialCase.Input) })
                .Where(result => !comparer.Equals(result.actual, result.differentialCase.Expected))
                .Select(result => new DifferentialMismatch<T>(result.differentialCase.Input, result.differentialCase.Expected, result.actual))
                .ToList();
        }
    }

    /// <summary>
    /// Writes an input so that a failure message shows every character of it.
    /// </summary>
    internal static class InputDescription
    {
        internal static string Of(string input)
        {
            var description = new StringBuilder("\"");
            foreach (var character in input)
            {
                if (character < ' ' || character > '~')
                {
                    description.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                }
                else
                {
                    description.Append(character);
                }
            }

            return description.Append('"').ToString();
        }
    }
}
