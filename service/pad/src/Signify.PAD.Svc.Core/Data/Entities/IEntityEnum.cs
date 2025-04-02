using System.Collections.Generic;

namespace Signify.PAD.Svc.Core.Data.Entities;

/// <summary>
/// Interface for entities that correspond to enumeration types. Allows
/// you to loop through all enumerations of the entity type.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IEntityEnum<out TValue>
{
    /// <summary>
    /// Gets all enumeration entities
    /// </summary>
    /// <returns></returns>
    IEnumerable<TValue> GetAllEnumerations();
}