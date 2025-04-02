using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Utilities;

/// <summary>
/// This class provide generic approach for testing overridden methods like Equals and GetHashCode
/// </summary>
public static class GenericEqualityTests
{
    private struct TestResult
    {

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public static TestResult CreateSuccess()
        {
            return new TestResult
            {
                IsSuccess = true
            };
        }

        public static TestResult CreateFailure(string message)
        {
            return new TestResult
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }
    }

    public static void TestEqualObjects<T>(T obj1, T obj2)
    {
        IList<TestResult> testResults = new List<TestResult>
        {
            TestGetHashCodeOnEqualObjects(obj1, obj2),
            TestEquals(obj1, obj2, true),
            TestEqualsWhenNull(obj1, false)
        };

        AssertAllTestsHavePassed(testResults);
    }

    private static TestResult TestGetHashCodeOnEqualObjects<T>(T obj1, T obj2)
    {
        return SafeCall("GetHashCode", () =>
        {
            if (obj1.GetHashCode() != obj2.GetHashCode())
                return TestResult.CreateFailure(
                    "GetHashCode of equal objects returned different values.");

            return TestResult.CreateSuccess();
        });
    }

    private static TestResult TestEquals<T>(T obj1, T obj2, bool expectedEqual)
    {
        return SafeCall("Equals", () =>
        {
            if (obj1.Equals((object)obj2) == expectedEqual) return TestResult.CreateSuccess();
            var message =
                $"Equals returns {!expectedEqual} on {(expectedEqual ? "" : "non-")}equal objects.";
            return TestResult.CreateFailure(message);
        });
    }

    private static TestResult TestEqualsWhenNull<T>(T obj1, bool expectedEqual)
    {
        return SafeCall("Equals", () =>
        {
            if (obj1.Equals(null) == expectedEqual) return TestResult.CreateSuccess();
            var message =
                string.Format($"Equals returns {expectedEqual} when object is null.");
            return TestResult.CreateFailure(message);
        });
    }

    private static TestResult SafeCall(string functionName,Func<TestResult> test)
    {
        try
        {
            return test();
        }
        catch (Exception ex)
        {
            var message = string.Format($"{functionName} threw {ex.GetType().Name}: {ex.Message}");
            return TestResult.CreateFailure(message);
        }
    }

    private static void AssertAllTestsHavePassed(IList<TestResult> testResults)
    {
        var allTestsPass =
            testResults
                .All(r => r.IsSuccess);
        var errors =
            testResults
                .Where(r => !r.IsSuccess)
                .Select(r => r.ErrorMessage)
                .ToArray();
        var compoundMessage = string.Join(Environment.NewLine, errors);

        Assert.True(allTestsPass, "Some tests have failed:\n" +
                                  compoundMessage);
    }
}