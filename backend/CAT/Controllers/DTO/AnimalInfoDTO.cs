using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CAT.Controllers.DTO
{
    public class AnimalInfoDTO
    {
        [Name("Инвентарный номер")]
        public string TagNumber { get; set; }
        [Name("Дата рождения")]
        public string BirthDate { get; set; }
        [Name("Дата поступления")]
        public string DateOfReceipt { get; set; }
        [Name("Дата выбытия")]
        public string DateOfDisposal { get; set; }
        [Name("Причина выб.")]
        public string ReasonOfDisposal { get; set; }    
        [Name("Последнее взвеш.: дата")]
        public string LastWeightDate { get; set; }
        
        [Name("Последнее взвеш.: жив.масса")]
        public string LastWeightWeight { get; set; }
        [Name("Пол")]
        public string Type { get; set; }
        [Name("Статус")]
        public string Status { get; set; }
        [Name("Инв.№ предка - O")]
        public string? FatherTag { get; set; }
        [Name("Инв.№ предка - M")]
        public string? MotherTag { get; set; }
        [Name("Порода")]
        public string Breed { get; set; }
        [Name("Хозяйство рождения")]
        public string OriginFarm { get; set; }
        [Name("Область рождения")]
        public string OriginRegion { get; set; }

        [Name("Гос-во рождения")]
        public string OriginCountry { get; set; }
        [Name("Живая масса при выбытии")]
        public string WeightOfDisposal { get; set; }
        [Name("Расход")]
        public string Сonsumption { get; set; }
        public Dictionary<string, string>? AdditionalFields { get; set; } = new();

    }
}
