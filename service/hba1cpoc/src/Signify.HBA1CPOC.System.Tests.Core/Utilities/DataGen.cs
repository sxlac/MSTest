using System.Text;
using System.Security.Cryptography;

namespace Signify.HBA1CPOC.System.Tests.Core.Utilities;

public static class DataGen
{
    public static long NewChPatId() => RandomInt(1, int.MaxValue);

    private static char RandomChar() => (char) ('A' + RandomInt(26));

    /// <summary>
    /// Generates a random integer between 0 (inclusive) and a specified exclusive upper bound using a cryptographically strong random number generator
    /// </summary>
    /// <param name="toMax"></param>
    /// <returns></returns>
    private static int RandomInt(int toMax) => RandomNumberGenerator.GetInt32(toMax);

    /// <summary>
    /// Generates a random integer between a specified lower value and a specified exclusive upper bound using a cryptographically strong random number generator
    /// </summary>
    /// <param name="fromMin"></param>
    /// <param name="toMax"></param>
    /// <returns></returns>
    private static int RandomInt(int fromMin, int toMax) => RandomNumberGenerator.GetInt32(fromMin, toMax);
    
    public static string NewCenseoId() {
        var censeoId = new StringBuilder(RandomChar().ToString());
        return censeoId.Append(RandomInt(1000000,9999999)).ToString();
    }
    
    public static Guid NewUuid() => Guid.NewGuid();
}