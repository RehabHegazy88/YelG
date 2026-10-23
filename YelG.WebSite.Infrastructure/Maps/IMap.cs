using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace YelG.WebSite.Infrastructure.Maps
{
    /// <summary>Maps one area of the model (same convention as YELG Lens: IMap + MappingsHelper).</summary>
    public interface IMap
    {
        void Visit(ModelBuilder builder);
    }

    public static class MappingsHelper
    {
        /// <summary>Every non-abstract IMap in this assembly.</summary>
        public static IEnumerable<IMap> GetMappings() =>
            typeof(IMap).Assembly.DefinedTypes
                .Where(t => !t.IsAbstract && typeof(IMap).GetTypeInfo().IsAssignableFrom(t))
                .Select(t => (IMap)Activator.CreateInstance(t.AsType())!)
                .ToArray();
    }
}
