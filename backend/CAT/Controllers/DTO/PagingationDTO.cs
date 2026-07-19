using System.ComponentModel.DataAnnotations;

namespace CAT.Controllers.DTO
{
    public class PaginationDTO
    {
        /// <example>d9776ffe-58e9-4ec2-bb03-d1a3f57942b9</example>
        public int Count { get; init; }

        /// <example>Активное</example>
        public int EntriesPerPage { get; init; }
    }
}
