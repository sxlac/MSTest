using Signify.PAD.Svc.Core.EventHandlers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Utilities;

public class AsyncVoidTests
{
    //This test identifies if there are any async void methods and fails if found any 
    //If you need to have async void (say events), add it to exception list to make this test pass
    //Note: Only event handlers are tested since we expect only them to have async methods in PAD. Add others as required.
    [Fact]
    public void Should_Not_Have_Any_Async_Void_Method_EventHandlers()
    {
        var classesWithAsyncVoid = new Dictionary<string, List<string>>();

        GetClassesWithAsyncVoid(classesWithAsyncVoid);

        Assert.True(classesWithAsyncVoid.Count == 0, $"Classes with void async methods: {string.Join(",", classesWithAsyncVoid.Keys.ToList())}");
    }

    private static void GetClassesWithAsyncVoid(IDictionary<string, List<string>> list)
    {
        var attributeType = typeof(AsyncStateMachineAttribute);
        var assembly = typeof(PdfDeliveredHandler).GetTypeInfo().Assembly;
        foreach (var type in assembly.GetTypes())
        {
            var methodNamesVoid = new List<string>();
            var methods = type.GetMethods();

            foreach (var method in methods)
            {
                var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attributeType);
                if (attrib != null && method.ReturnType == typeof(void))
                {
                    methodNamesVoid.Add(method.Name);
                }
            }

            if (methodNamesVoid.Count > 0)
                list.Add(type.Name, methodNamesVoid);
        }
    }
}