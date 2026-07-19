using System.Text.Json;

namespace CAT.Controllers.DTO
{
    public class InseminationDTO
    {
        public Guid CowId { get; set; }               
        public DateOnly Date { get; set; }
        public string InseminationType { get; set; }  
        public string? SpermBatch { get; set; }       
        public string? SpermManufacturer { get; set; }
        public Guid? BullId { get; set; }            
        public string? EmbryoId { get; set; }          
        public string? EmbryoManufacturer { get; set; } 
        public string? Technician { get; set; }      
        public string? Notes { get; set; }
        public string? BullName { get; set; }    
        public DateOnly? ExpectedCalvingDate { get; set; }
    }

    public class InseminationBatchDTO
    {
        public List<InseminationItemDTO> Items { get; set; } = new();
    }

    public class InseminationItemDTO
    {
        public List<Guid>? CowIds { get; set; }
        public Guid? CowId { get; set; }
        public DateOnly Date { get; set; }
        public string InseminationType { get; set; } = default!;
        public string? SpermBatch { get; set; }
        public string? SpermManufacturer { get; set; }

        public List<Guid>? BullIds { get; set; }


        public JsonElement? BullJson { get; set; }

        public string? EmbryoId { get; set; }
        public string? EmbryoManufacturer { get; set; }
        public string? Technician { get; set; }
        public string? Notes { get; set; }
        public string? BullName { get; set; }
        public DateOnly? ExpectedCalvingDate { get; set; }
    }

}
