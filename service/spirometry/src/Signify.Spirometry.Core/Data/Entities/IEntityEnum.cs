using System.Collections.Generic;

namespace Signify.Spirometry.Core.Data.Entities
{
    /// <summary>
    /// Interface for entities that correspond to enumeration types. Allows
    /// you to loop through all enumerations of the entity type.
    /// </summary>
    /// <typeparam name="TValue">Type of the entity</typeparam>
    public interface IEntityEnum<out TValue>
    {
        /// <summary>
        /// Gets all enumeration entities
        /// </summary>
        IEnumerable<TValue> GetAllEnumerations();
    }
}
