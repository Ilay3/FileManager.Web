using FileManager.Domain.Enums;
using System;

namespace FileManager.Application.DTOs;

public record GroupAccessDto(Guid Id, string Name, AccessType AccessType);
