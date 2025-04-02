using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Signify.DEE.Svc.Core.Tests;

public static class EnumerableComparer
{
    /// <summary>
    /// Asserts that two collections of the same type have the same items, ignoring the location in the collection
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="actual"></param>
    /// <param name="comparer"></param>
    /// <typeparam name="T"></typeparam>
    public static void AssertComparable<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
    {
        if (ReferenceEquals(expected, actual))
            return;

        Assert.Equal(expected != null, actual != null); // Either both or neither must be null

        if (expected == null)
            return; // Both are null; nothing to check

        // Put both into a List<T> so we can work with them easier
        var expectedList = new List<T>(expected);
        var actualList = new List<T>(actual);

        Assert.Equal(expectedList.Count, expectedList.Count);

        if (expectedList.Count < 1)
            return; // Nothing to compare

        foreach (var item in expectedList)
        {
            var index = actualList.FindIndex(each => comparer.Equals(each, item));
            Assert.NotEqual(-1, index); // Ensure a match is found somewhere in the collection
            actualList.RemoveAt(index); // Remove it from the collection for the next iteration
        }
    }

    #region Tests
    /// <summary>
    /// Tests for the <see cref="EnumerableComparer"/>, to ensure it works as expected so tests that use it are
    /// working as expected
    /// </summary>
    public class EnumerableComparerTests
    {
        [Theory]
        [MemberData(nameof(AssertComparable_TestData))]
        public void AssertComparable_Tests(IEnumerable<string> expected, IEnumerable<string> actual, IEqualityComparer<string> comparer, bool shouldSucceed)
        {
            if (shouldSucceed)
                AssertComparable(expected, actual, comparer);
            else
            {
                try // Can't use Assert.Throws to verify that an exception was thrown, because the Xunit exception will still bubble up and fail this test
                {
                    AssertComparable(expected, actual, comparer);
                }
                catch (NotEqualException)
                {
                    // Expected
                }
            }
        }

        public static IEnumerable<object[]> AssertComparable_TestData()
        {
            yield return
            [
                new[] { "a", "b" },
                new[] { "a", "b" },
                StringComparer.InvariantCulture,
                true
            ];

            yield return
            [
                new[] { "a", "b" },
                new[] { "b", "a" },
                StringComparer.InvariantCulture,
                true
            ];

            yield return
            [
                new[] { "a", "a" },
                new[] { "a", "b" },
                StringComparer.InvariantCulture,
                false
            ];

            yield return
            [
                null,
                null,
                StringComparer.InvariantCulture,
                true
            ];

            yield return
            [
                Array.Empty<string>(),
                Array.Empty<string>(),
                StringComparer.InvariantCulture,
                true
            ];

            yield return
            [
                new[] { "a", "b" },
                new[] { "a", "b", "c" },
                StringComparer.InvariantCulture,
                false
            ];

            yield return
            [
                new[] { "a", "b" },
                new[] { "a", "B" },
                StringComparer.InvariantCulture,
                false
            ];

            yield return
            [
                new[] { "a", "a", "a" },
                new[] { "a", "a", "a" },
                StringComparer.InvariantCulture,
                true
            ];

            yield return
            [
                new HashSet<string>(["a", "b"]),
                new List<string>(["b", "a"]),
                StringComparer.InvariantCulture,
                true
            ];
        }

        /// <summary>
        /// Ensures new collections are created with the items in 'actual' and 'expected', and when items are
        /// removed in the loop, it doesn't remove them from the instances passed in as arguments
        /// </summary>
        [Fact]
        public void AssertComparable_WithCollections_DoesNotRemoveItemsArguments()
        {
            var expected = new List<string> { "a", "b" };
            var actual = new List<string> { "a", "b" };

            AssertComparable(expected, actual, StringComparer.InvariantCulture);

            Assert.Equal(2, expected.Count);
            Assert.Equal(2, actual.Count);
        }
    }
    #endregion Tests
}