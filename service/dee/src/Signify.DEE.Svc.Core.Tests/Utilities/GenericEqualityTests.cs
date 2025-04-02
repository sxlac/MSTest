using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Utilities;

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
            return new TestResult()
            {
                IsSuccess = true
            };
        }

        public static TestResult CreateFailure(string message)
        {
            return new TestResult()
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }
    }

    public static void TestEqualObjects<T>(T obj1, T obj2)
    {
        var objects = new object[] { obj1 , obj2};
        if (objects.Any(o => object.ReferenceEquals(o, null)))
            throw new System.ArgumentNullException();

        IList<TestResult> testResults = new List<TestResult>()
        {
            TestGetHashCodeOnEqualObjects<T>(obj1, obj2),
            TestEquals<T>(obj1, obj2, true),
            TestEqualsWhenNull<T>(obj1, false)
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
            if (obj1.Equals((object)obj2) != expectedEqual)
            {
                string message =
                    string.Format("Equals returns {0} on {1}equal objects.",
                        !expectedEqual, expectedEqual ? "" : "non-");
                return TestResult.CreateFailure(message);
            }
            return TestResult.CreateSuccess();
        });
    }

    private static TestResult TestEqualsWhenNull<T>(T obj1, bool expectedEqual)
    {
        return SafeCall("Equals", () =>
        {
            if (obj1.Equals(null) != expectedEqual)
            {
                string message =
                    string.Format($"Equals returns {expectedEqual} when object is null.");
                return TestResult.CreateFailure(message);
            }
            return TestResult.CreateSuccess();
        });
    }

    private static TestResult SafeCall(string functionName,Func<TestResult> test)
    {
        try
        {
            return test();
        }
        catch (System.Exception ex)
        {
            string message = string.Format($"{functionName} threw {ex.GetType().Name}: {ex.Message}");
            return TestResult.CreateFailure(message);
        }
    }

    private static void AssertAllTestsHavePassed(IList<TestResult> testResults)
    {
        bool allTestsPass =
            testResults
                .All(r => r.IsSuccess);
        string[] errors =
            testResults
                .Where(r => !r.IsSuccess)
                .Select(r => r.ErrorMessage)
                .ToArray();
        string compoundMessage = string.Join(Environment.NewLine, errors);

        Assert.True(allTestsPass, "Some tests have failed:\n" +
                                  compoundMessage);
    }
}