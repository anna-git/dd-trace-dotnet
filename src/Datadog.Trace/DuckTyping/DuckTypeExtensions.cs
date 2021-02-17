using System;
using System.Runtime.CompilerServices;
using Datadog.Trace.Logging;

namespace Datadog.Trace.DuckTyping
{
    /// <summary>
    /// Duck type extensions
    /// </summary>
    public static class DuckTypeExtensions
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(DuckType));

        /// <summary>
        /// Gets the duck type instance for the object implementing a base class or interface T
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <typeparam name="T">Target type</typeparam>
        /// <returns>DuckType instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DuckCast<T>(this object instance)
            => DuckType.Create<T>(instance);

        /// <summary>
        /// Gets the duck type instance for the object implementing a base class or interface T
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="targetType">Target type</param>
        /// <returns>DuckType instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object DuckCast(this object instance, Type targetType)
            => DuckType.Create(targetType, instance);

        /// <summary>
        /// Tries to ducktype the object implementing a base class or interface T
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="instance">Object instance</param>
        /// <param name="value">Ducktype instance</param>
        /// <returns>true if the object instance was ducktyped; otherwise, false.</returns>
        public static bool DuckIs<T>(this object instance, out T value)
        {
            try
            {
                value = DuckType.Create<T>(instance);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to ducktype the object implementing a base class or interface T
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="targetType">Target type</param>
        /// <param name="value">Ducktype instance</param>
        /// <returns>true if the object instance was ducktyped; otherwise, false.</returns>
        public static bool DuckIs(this object instance, Type targetType, out object value)
        {
            try
            {
                value = DuckType.Create(targetType, instance);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                value = default;
                return false;
            }
        }
    }
}
