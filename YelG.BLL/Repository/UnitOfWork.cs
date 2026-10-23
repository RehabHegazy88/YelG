using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.DAL.Data;

namespace YelG.BLL.Repository
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        // SQL Server unique index / unique constraint violations.
        private static readonly int[] DuplicateKeyErrors = { 2601, 2627 };

        private readonly YelgDbContext _context;

        public UnitOfWork(YelgDbContext context)
        {
            _context = context;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && DuplicateKeyErrors.Contains(sql.Number))
            {
                throw new BusinessRuleException("البيانات موجودة بالفعل ولا يمكن تكرارها.");
            }
        }
    }
}
