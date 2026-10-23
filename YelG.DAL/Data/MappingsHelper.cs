using YelG.DAL.Access.Maps.Common;
using YelG.DAL.Access.Maps.Main;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace YelG.DAL.Access.Data
{
    public static class MappingsHelper
    {
        public static IEnumerable<IMap> GetMainMappings()
        {
            var assemblyTypes = typeof(UserMap).GetTypeInfo().Assembly.DefinedTypes;
            var mapsNamespace = typeof(UserMap).Namespace!;
            var mappings = assemblyTypes
                .Where(t => t.Namespace != null && t.Namespace.Contains(mapsNamespace))
                .Where(t => typeof(IMap).GetTypeInfo().IsAssignableFrom(t));
            mappings = mappings.Where(x => !x.IsAbstract);
            // Every map has a public parameterless constructor, so CreateInstance never returns null here.
            return mappings.Select(m => (IMap)Activator.CreateInstance(m.AsType())!).ToArray();
        }
    }

}
