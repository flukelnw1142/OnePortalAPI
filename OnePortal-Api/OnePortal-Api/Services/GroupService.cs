using OnePortal_Api.Data;
using OnePortal_Api.Model;
using Microsoft.EntityFrameworkCore;

namespace OnePortal_Api.Services
{
    public class GroupService : IGroupService
    {
        private readonly AppDbContext _context;

        public GroupService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetGroupNamesByCompany(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(','); // แยกข้อมูล company ก่อน

            var groupIds = await _context.GroupDetails
                .Where(gd => companyList.Any(c => gd.group_detail_name.Contains(c)))
                .Select(gd => gd.group_id)
                .Distinct()
                .ToListAsync(cancellationToken);

            var groupNames = await _context.Groups
                .Where(g => groupIds.Contains(g.id))
                .Select(g => g.group_name)
                .ToListAsync(cancellationToken);

            return groupNames;
        }
    }
}
