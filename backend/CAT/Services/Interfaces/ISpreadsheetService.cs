using CAT.Controllers.DTO;

namespace CAT.Services.Interfaces
{
    public interface ISpreadsheetService
    {
        public IEnumerable<T> Read<T>(Stream file);
        public byte[] Write<T>(IEnumerable<T> items);
        public IEnumerable<AnimalInfoDTO> ReadAnimals(Stream file);
        public string GetFileName(string input);
    }
}
