using FileManager.Application.DTOs;
using FileManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileManager.Application.Interfaces;

public interface IGroupService
{
    Task<List<GroupAccessDto>> GetGroupPermissionsAsync();
    Task SetGroupPermissionsAsync(Guid groupId, AccessType accessType, Guid grantedById);
}
