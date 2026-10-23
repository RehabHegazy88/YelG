using YelG.Model.Evaluations;

namespace YelG.BLL.IRepository
{
    public interface ICompanyRepository
    {
        /// <summary>Tracked company with its evaluations and their answers (for edits and reviewer changes).</summary>
        Task<Company?> GetForUpdateAsync(long companyId, CancellationToken ct = default);

        /// <summary>Same name (case and spacing already normalised by the caller) and activity.</summary>
        Task<Company?> FindAsync(string name, long activityId, CancellationToken ct = default);

        /// <summary>Read-only list with activity and each evaluation's reviewer.</summary>
        Task<List<Company>> ListAsync(CancellationToken ct = default);

        void Add(Company company);

        void Remove(Company company);
    }
}
