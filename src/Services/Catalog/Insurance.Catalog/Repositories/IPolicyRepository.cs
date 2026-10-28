using Insurance.Catalog.Models;

namespace Insurance.Catalog.Repositories;

public interface IPolicyRepository
{
    Task<List<Policy>> GetAllAsync(bool includeInactive);
    Task<Policy?> GetByIdAsync(int policyId);
    Task<List<Policy>> GetManyAsync(IEnumerable<int> policyIds);   // active or not
    Task<int> CreateAsync(Policy policy);
    Task<bool> UpdateAsync(Policy policy);      // false => no row matched (404)
    Task<bool> DeactivateAsync(int policyId);   // false => no row matched (404)
}
