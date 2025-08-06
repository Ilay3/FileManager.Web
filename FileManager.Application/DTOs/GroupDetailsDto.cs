using System;
using System.Collections.Generic;

namespace FileManager.Application.DTOs;

public record GroupDetailsDto(Guid Id, string Name, List<UserDto> Users);

