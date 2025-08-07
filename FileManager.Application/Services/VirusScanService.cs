using System.IO;
using System.Threading.Tasks;

namespace FileManager.Application.Services;

public class VirusScanService
{
    public Task<bool> ScanAsync(Stream fileStream)
    {
        // Заглушка для сканирования на вирусы
        return Task.FromResult(true);
    }
}

