using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileManager.Application.Services;

public class GroupService : IGroupService
{
    private readonly IAppDbContext _context;

    public GroupService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GroupAccessDto>> GetGroupPermissionsAsync()
    {
        return await _context.Groups
            .Include(g => g.AccessRules)
            .Select(g => new GroupAccessDto(
                g.Id,
                g.Name,
                g.AccessRules
                    .Where(r => r.FileId == null && r.FolderId == null)
                    .Select(r => r.AccessType)
                    .FirstOrDefault()))
            .ToListAsync();
    }

    public async Task SetGroupPermissionsAsync(Guid groupId, AccessType accessType, Guid grantedById)
    {
        var rule = await _context.AccessRules
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.FileId == null && r.FolderId == null);

        if (rule == null)
        {
            rule = new AccessRule
            {
                GroupId = groupId,
                AccessType = accessType,
                GrantedById = grantedById,
                InheritFromParent = true
            };
            _context.AccessRules.Add(rule);
        }
        else
        {
            rule.AccessType = accessType;
            rule.GrantedById = grantedById;
        }

        await _context.SaveChangesAsync();
    }
}
