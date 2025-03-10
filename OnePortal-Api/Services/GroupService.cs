using OnePortal_Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace OnePortal_Api.Services
{
    public class GroupService(AppDbContext context) : IGroupService
    {
        private readonly AppDbContext _context = context;

        public async Task<List<string>> GetGroupNamesByCompany(string company, CancellationToken cancellationToken = default)
        {
            if (company.Contains("ALL"))
            {
                var allGroupNames = _context.Group
                    .FromSqlRaw("EXEC GetAllGroupNames")
                    .AsEnumerable()
                    .Select(g => g.GroupName)
                    .ToList();

                return await Task.FromResult(allGroupNames);
            }
            else
            {
                var parameter = new SqlParameter("@Company", company);
                var groupNames = _context.Group
                    .FromSqlRaw("EXEC GetGroupNamesByCompany @Company", parameter)
                    .AsEnumerable()
                    .Select(g => g.GroupName)
                    .ToList();

                return await Task.FromResult(groupNames);
            }
        }
    }
}