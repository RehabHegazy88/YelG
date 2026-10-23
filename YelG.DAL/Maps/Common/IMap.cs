using Microsoft.EntityFrameworkCore;

namespace YelG.DAL.Access.Maps.Common
{
public interface IMap
{
    void Visit(ModelBuilder builder);
}
}