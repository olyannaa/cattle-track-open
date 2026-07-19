using Amazon.Runtime.Telemetry;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Logic;
using CAT.Models;
using CAT.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualBasic;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace CAT.Services
{
    public class AnimalService : IAnimalService
    {
        private readonly PostgresContext _db;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor  _hc;
        
        public AnimalService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public List<GroupInfoDTO>? GetGroupsInfo(Guid org_id)
        {
            var res = _db.GetOrgGroups(org_id)
                .Select(x => new GroupInfoDTO() { Id = x.Id, Name = x.Name })
                .ToList();
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetOrgGroups))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: org_id
                ));
            return res;
        }

        public List<IdentificationInfoDTO>? GetIdentificationsFields(Guid org_id)
        {
            var res = _db.GetOrgIdentifications(org_id)
                .ToList();
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetOrgIdentifications))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: org_id
            ));
            return res;
        }

        public Guid RegisterAnimal(AnimalRegistrationDTO dto, Guid organizationId)
        {
            var animalId = Guid.NewGuid();
            var fatherIds = new List<Guid?>();
            fatherIds.Add(dto.FatherTag);
            var fatherJsonString = JsonSerializer.Serialize(fatherIds);
            
            var fatherJsonElement = JsonDocument.Parse(fatherJsonString).RootElement;
            var animal = new Animal
            {
                Id = animalId,
                OrganizationId = organizationId,
                TagNumber = dto.TagNumber,
                BirthDate = dto.BirthDate,
                Type = dto.Type,
                Breed = dto.Breed,
                MotherId = dto.MotherTag,
                FatherJson = fatherJsonElement,
                Status = "Активное",
                GroupId = dto.GroupId,
                Origin = dto.Origin,
                OriginLocation = dto.OriginLocation,
            };

            try
            {
                _db.InsertAnimal(animal);
                _db.SaveChanges();

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimal)),
                    recordId: animalId,
                    oldValues: null,
                    newValues: dto,
                    status: "success",
                    table: "animals"
                ));
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimal)),
                    recordId: animalId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "animals"
                ));
                throw;
            }

            if (dto.Type == "Нетель")
            {
                try
                {
                    var insemination = new InseminationDTO
                    {
                        CowId = animalId,
                        Date = dto.InseminationDate,
                        ExpectedCalvingDate = dto.ExpectedCalvingDate,
                        InseminationType = dto.InseminationType,
                        SpermBatch = dto.SpermBatch,
                        Technician = dto.Technician,
                        Notes = dto.Notes,
                        EmbryoManufacturer = dto.EmbryoManufacturer,
                        BullId = dto.BullId,
                        EmbryoId = dto.EmbryoTag,
                        SpermManufacturer = dto.SpermManufacturer,
                    };

                    var inseminationId = _db.InsertInsemination(insemination);

                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertInsemination)),
                        recordId: inseminationId,
                        oldValues: null,
                        newValues: dto,
                        status: "success",
                        table: "inseminations"
                    ));

                    var pregnancy = new InsertPregnancyDTO
                    {
                        InseminationId = inseminationId,
                        CowId = animalId,
                        Date = dto.InseminationDate,
                        Status = "Стельная",
                        ExpectedCalvingDate = dto.ExpectedCalvingDate
                    };

                    _db.InsertPregnancy(pregnancy);

                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertPregnancy)),
                        recordId: pregnancy.InseminationId,
                        oldValues: pregnancy,
                        status: "success",
                        table: "pregnancies"
                    ));
                }
                catch (PostgresException ex) when (ex.SqlState == "23503")
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertInsemination)),
                        recordId: animalId,
                        oldValues: null,
                        newValues: dto,
                        status: "error",
                        errorMessage: ex.Message,
                        table: "inseminations"
                    ));

                    throw new Exception($"Не удалось зарегистрировать осеменение: животное с ID {animalId} не найдено");
                }
                catch (Exception ex)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertPregnancy)),
                        recordId: animalId,
                        oldValues: null,
                        newValues: dto,
                        status: "error",
                        errorMessage: ex.Message,
                        table: "pregnancies"
                    ));

                    throw;
                }
            }

            foreach (var animalField in dto.AdditionalFields)
            {
                try
                {
                    _db.InsertAnimalIdentification(animalId, animalField.Key, animalField.Value);

                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimalIdentification)),
                        recordId: animalId,
                        oldValues: null,
                        newValues: animalField,
                        status: "success",
                        table: "animal_identifications"
                    ));
                }
                catch (Exception ex)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimalIdentification)),
                        recordId: animalId,
                        oldValues: null,
                        status: "error",
                        errorMessage: ex.Message,
                        table: "animal_identifications"
                    ));
                }
            }

            return animalId;
        }



        public ImportAnimalsInfo ImportAnimalsFromXLSX(List<AnimalInfoDTO> animals, Guid org_id)
        {
            if (animals == null || animals.Count == 0)
                return new ImportAnimalsInfo { Message = "Нет данных для импорта", Errors = 1 };
            
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                 "import",
                 recordId: org_id
              ));

            var importInfo = new ImportAnimalsInfo();
            var addedAnimals = new List<(AnimalInfoDTO animal, Guid animalId)>();

            var (active, ancestors) = CategorizeAnimals(animals);
            var all = ancestors.Concat(active).ToList();


            var prepared = all.Select(a => PrepareRow(a)).ToList();

            const string tempImport = "temp_animals_import";
            const string tempInserted = "temp_animals_inserted";

            using var tx = _db.Database.BeginTransaction();
            try
            {

                var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = (NpgsqlTransaction)tx.GetDbTransaction();

                    cmd.CommandText = $@"
                DROP TABLE IF EXISTS {tempImport};
                CREATE TEMP TABLE {tempImport}
                (
                    tag_number              text NOT NULL,
                    mother_tag              text NULL,
                    father_tag              text NULL,
                    birth_date              date NULL,
                    type                    text NULL,
                    breed                   text NULL,
                    status                  text NOT NULL,
                    origin_location         text NULL,
                    consumption             text NULL,
                    date_of_receipt         date NULL,
                    date_of_disposal        date NULL,
                    last_weight_weight      double precision NULL,
                    live_weight_at_disposal double precision NULL,
                    last_weigh_date         date NULL,
                    reason_of_disposal      text NULL
                ) ON COMMIT DROP;
            ";
                    cmd.ExecuteNonQuery();
                }

                
                using (var writer = ((NpgsqlConnection)_db.Database.GetDbConnection())
                       .BeginBinaryImport($@"
                    COPY {tempImport} (
                        tag_number, mother_tag, father_tag, birth_date, type, breed, status,
                        origin_location, consumption, date_of_receipt, date_of_disposal,
                        last_weight_weight, live_weight_at_disposal, last_weigh_date, reason_of_disposal
                    ) FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var r in prepared)
                    {
                        writer.StartRow();
                        writer.Write(r.TagNumber, NpgsqlDbType.Text);
                        writer.Write(r.MotherTag, NpgsqlDbType.Text);
                        writer.Write(r.FatherTag, NpgsqlDbType.Text);
                        writer.Write(r.BirthDate, NpgsqlDbType.Date);
                        writer.Write(r.Type, NpgsqlDbType.Text);
                        writer.Write(r.Breed, NpgsqlDbType.Text);
                        writer.Write(r.Status, NpgsqlDbType.Text);
                        writer.Write(r.OriginLocation, NpgsqlDbType.Text);
                        writer.Write(r.Consumption, NpgsqlDbType.Text);
                        writer.Write(r.DateOfReceipt, NpgsqlDbType.Date);
                        writer.Write(r.DateOfDisposal, NpgsqlDbType.Date);
                        writer.Write(r.LastWeightWeight, NpgsqlDbType.Double);
                        writer.Write(r.LiveWeightAtDisposal, NpgsqlDbType.Double);
                        writer.Write(r.LastWeighDate, NpgsqlDbType.Date);
                        writer.Write(r.ReasonOfDisposal, NpgsqlDbType.Text);
                    }
                    writer.Complete();
                }

                using (var cmd = ((NpgsqlConnection)_db.Database.GetDbConnection()).CreateCommand())
                {
                    cmd.Transaction = (NpgsqlTransaction)tx.GetDbTransaction();

                    cmd.CommandText = $@"
                DROP TABLE IF EXISTS {tempInserted};
                CREATE TEMP TABLE {tempInserted}
                (
                    id uuid NOT NULL,
                    tag_number text NOT NULL
                ) ON COMMIT DROP;

                WITH ins AS (
                    INSERT INTO animals
                    (
                        id,                
                        organization_id,
                        tag_number,
                        birth_date,
                        type,
                        breed,
                        mother_id,
                        father_id,
                        status,
                        group_id,
                        origin,
                        origin_location,
                        consumption,
                        date_of_receipt,
                        date_of_disposal,
                        last_weight_weight,
                        live_weight_at_disposal,
                        last_weigh_date,
                        reason_of_disposal
                    )
                    SELECT
                        gen_random_uuid(),              
                        @org_id,
                        ti.tag_number,
                        ti.birth_date,
                        ti.type,
                        ti.breed,
                        NULL::uuid as mother_id,
                        NULL::uuid as father_id,
                        ti.status,
                        NULL::uuid as group_id,
                        ''::text   as origin,
                        ti.origin_location,
                        ti.consumption,
                        ti.date_of_receipt,
                        ti.date_of_disposal,
                        ti.last_weight_weight,
                        ti.live_weight_at_disposal,
                        ti.last_weigh_date,
                        ti.reason_of_disposal
                    FROM {tempImport} ti
                    -- если хотите пропускать уже существующих (по org+tag), раскомментируйте:
                    LEFT JOIN animals a
                      ON a.organization_id = @org_id AND a.tag_number = ti.tag_number
                    WHERE a.id IS NULL
                    RETURNING id, tag_number
                )
                INSERT INTO {tempInserted} (id, tag_number)
                SELECT id, tag_number FROM ins;
            ";
                    cmd.Parameters.Add(new NpgsqlParameter("@org_id", org_id));
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = ((NpgsqlConnection)_db.Database.GetDbConnection()).CreateCommand())
                {
                    cmd.Transaction = (NpgsqlTransaction)tx.GetDbTransaction();

                    cmd.CommandText = $@"
                        UPDATE animals child
                        SET
                            mother_id = mom.id,
                            father_id = dad.id
                        FROM {tempInserted} ins
                        JOIN {tempImport} ti
                          ON ti.tag_number = ins.tag_number
                        LEFT JOIN animals mom
                          ON mom.organization_id = @org_id AND mom.tag_number = ti.mother_tag
                        LEFT JOIN animals dad
                          ON dad.organization_id = @org_id AND dad.tag_number = ti.father_tag
                        WHERE child.id = ins.id
                          AND ti.status <> 'Покупное семя';
                    ";
                    cmd.Parameters.Add(new NpgsqlParameter("@org_id", org_id));
                    var affected = cmd.ExecuteNonQuery();
                }

                tx.Commit();

                importInfo.Message = "Импорт завершён (bulk COPY + массовый UPDATE).";
                importInfo.Imported = prepared.Count; 
                importInfo.TotalRows = prepared.Count;
                return importInfo;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return new ImportAnimalsInfo
                {
                    Message = $"Ошибка bulk-импорта: {ex.Message}",
                    Errors = 1,
                    TotalRows = importInfo.TotalRows
                };
            }
        }

        private static string CleanTag(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var trimmed = s.Trim().Trim('\"', '\'', '“', '”', '„', '‟').Trim();
            trimmed = trimmed.Replace('\u00A0', ' ')
                             .Replace("\u200B", "") 
                             .Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private static DateOnly? ParseDateRu(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var ru = CultureInfo.GetCultureInfo("ru-RU");
            return DateOnly.TryParse(s, ru, out var d) ? d : null;
        }

        private static double? ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? v
                : (double?)null;
        }

        private sealed class PreparedRow
        {
            public string TagNumber { get; init; }
            public string MotherTag { get; init; }
            public string FatherTag { get; init; }
            public DateOnly? BirthDate { get; init; }
            public string Type { get; init; }
            public string Breed { get; init; }
            public string Status { get; init; }
            public string OriginLocation { get; init; }
            public string Consumption { get; init; }
            public DateOnly? DateOfReceipt { get; init; }
            public DateOnly? DateOfDisposal { get; init; }
            public double? LastWeightWeight { get; init; }
            public double? LiveWeightAtDisposal { get; init; }
            public DateOnly? LastWeighDate { get; init; }
            public string ReasonOfDisposal { get; init; }
        }

        private PreparedRow PrepareRow(AnimalInfoDTO a)
        {
            return new PreparedRow
            {
                TagNumber = CleanTag(a.TagNumber) ?? throw new InvalidOperationException("tag_number пустой"),
                MotherTag = CleanTag(a.MotherTag),
                FatherTag = CleanTag(a.FatherTag),
                BirthDate = ParseDateRu(a.BirthDate),
                Type = a.Type,
                Breed = a.Breed,
                Status = string.IsNullOrWhiteSpace(a.Status) ? "Активное" : a.Status,
                OriginLocation = BuildOriginLocation(a.OriginFarm, a.OriginRegion, a.OriginCountry),
                Consumption = a.Сonsumption,
                DateOfReceipt = ParseDateRu(a.DateOfReceipt),
                DateOfDisposal = ParseDateRu(a.DateOfDisposal),
                LastWeightWeight = ParseDouble(a.LastWeightWeight),
                LiveWeightAtDisposal = ParseDouble(a.LastWeightWeight),
                LastWeighDate = ParseDateRu(a.LastWeightDate),
                ReasonOfDisposal = a.ReasonOfDisposal
            };
        }



        private Guid? GetParentId(string parentTag, Dictionary<string, Guid> existingParents)
        {
            return !string.IsNullOrWhiteSpace(parentTag) && existingParents.TryGetValue(parentTag, out var id)
                ? id
                : null;
        }

        
        private List<string> AddNewIdentificationFields(
            List<AnimalInfoDTO> animals,
            Guid org_id,
            List<IdentificationField> existingFields,
            ref ImportAnimalsInfo importInfo)
        {
            var createdFields = new List<string>();
            var existingFieldNames = new HashSet<string>(existingFields.Select(f => f.FieldName));

            foreach (var animalField in animals[0].AdditionalFields)
            {
                if (!existingFieldNames.Contains(animalField.Key))
                {
                    try
                    {
                        _db.AddIdentificationField(animalField.Key, org_id);
                        createdFields.Add(animalField.Key);
                        importInfo.CreatedFields++;

                        var newValue = new
                        {
                            FieldName = animalField.Key,
                            OrganizationId = org_id
                        };

                       _actionQueue.Enqueue(UserActionDtoFactory.Create(
                            _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                            actionType: "insert",
                            dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddIdentificationField)),
                            recordId: org_id,
                            oldValues: null,
                            newValues: JsonSerializer.Serialize(newValue),
                            status: "success"
                        ));
                    }
                    catch (Exception ex)
                    {
                       _actionQueue.Enqueue(UserActionDtoFactory.Create(
                            _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                            actionType: "insert",
                            dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddIdentificationField)),
                            recordId: org_id,
                            oldValues: null,
                            newValues: JsonSerializer.Serialize(new { FieldName = animalField.Key }),
                            status: "error",
                            errorMessage: ex.Message
                        ));
                        
                    }
                }
            }

            return createdFields;
        }
        
        

        private (List<AnimalInfoDTO> active, List<AnimalInfoDTO> inactive) CategorizeAnimals(
    List<AnimalInfoDTO> animals)
        {
            var activeAnimals = new List<AnimalInfoDTO>();
            var inactiveAnimals = new List<AnimalInfoDTO>();

            foreach (var animal in animals)
            {

                if (ShouldBeInactive(animal) || animal.Status == "Покупное семя")
                {
                    animal.Status = DetermineInactiveStatus(animal);
                    inactiveAnimals.Add(animal);
                }
                else
                {
                    animal.Status = "Активное";
                    activeAnimals.Add(animal);
                }
            }

            return (activeAnimals, inactiveAnimals);
        }

        private bool ShouldBeInactive(AnimalInfoDTO animal)
        {
            // Критерий 1: Есть признаки выбытия (дата, причина или расход)
            bool hasDisposalInfo = !string.IsNullOrEmpty(animal.DateOfDisposal) ||
                                  !string.IsNullOrEmpty(animal.ReasonOfDisposal) ||
                                  !string.IsNullOrEmpty(animal.Сonsumption);

            // Критерий 2: Статус предка (даже без информации о выбытии)
            bool isAncestor = animal.Status == "Мат.предок" || animal.Status == "Отц.предок";

            return hasDisposalInfo || isAncestor;
        }

        private string DetermineInactiveStatus(AnimalInfoDTO animal)
        {
            // Если есть расход "Продажа" - статус "Проданное"
            if (animal.Сonsumption == "Продажа")
                return "Проданное";

            // Если есть причина выбытия - статус "Выбывшее"
            if (!string.IsNullOrEmpty(animal.ReasonOfDisposal))
                return "Выбывшее";

            // Для предков без конкретной информации
            if (animal.Status == "Мат.предок" || animal.Status == "Отц.предок")
                return "Выбывшее";

            // Дефолтный статус для неактивных животных
            return "Выбывшее";
        }

        private bool TryParseAnimalData(AnimalInfoDTO animal,
                out (DateOnly? birthDate, DateOnly? dateOfReceipt, DateOnly? dateOfDisposal,
                    DateOnly? lastWeightDate, double? lastWeightAtDisposal) parsedData,
                out string error)
        {
            parsedData = default;
            error = null;
            var parseErrors = new List<string>();

            DateOnly? ParseOptionalDate(string dateStr, string fieldName)
            {
                if (string.IsNullOrWhiteSpace(dateStr)) return null;
                var ruRu = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
                if (DateOnly.TryParse(dateStr, ruRu, out var date)) return date;
                parseErrors.Add($"Некорректный формат {fieldName} ('{dateStr}') - будет сохранено как NULL");
                return null;
            }

            var birthDate = ParseOptionalDate(animal.BirthDate, "дата рождения");
            var dateOfReceipt = ParseOptionalDate(animal.DateOfReceipt, "дата поступления");
            var dateOfDisposal = ParseOptionalDate(animal.DateOfDisposal, "дата выбытия");
            var lastWeightDate = ParseOptionalDate(animal.LastWeightDate, "дата взвешивания");

            double? weight = null;
            if (!string.IsNullOrWhiteSpace(animal.LastWeightWeight))
            {
                if (!double.TryParse(animal.LastWeightWeight, out var parsedWeight))
                    parseErrors.Add($"Некорректный вес при выбытии ('{animal.LastWeightWeight}') - будет сохранено как NULL");
                else
                    weight = parsedWeight;
            }

            parsedData = (birthDate, dateOfReceipt, dateOfDisposal, lastWeightDate, weight);

            if (parseErrors.Any())
                error = string.Join("; ", parseErrors);

            return true;
        }
        
        private void AddIdentificationFields(
            List<(AnimalInfoDTO animal, Guid animalId)> addedAnimals,
            Guid org_id,
            ref ImportAnimalsInfo importInfo)
        {
            var identificationFields = GetIdentificationsFields(org_id).ToDictionary(f => f.Name, f => f.Id);

            foreach (var (animal, animalId) in addedAnimals)
            {
                foreach (var field in animal.AdditionalFields)
                {
                    if (identificationFields.TryGetValue(field.Key, out var fieldId))
                    {
                        try
                        {
                            var newValuesJson = fieldId != null ? JsonSerializer.Serialize(fieldId) : null;
                            
                            _db.InsertAnimalIdentification(animalId, fieldId, field.Value);
                            

                           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                                actionType: "insert",
                                dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimalIdentification)),
                                recordId: animalId,
                                oldValues: null,
                                newValues: newValuesJson,
                                status: "success"
                            ));
                        }
                        catch (Exception ex)
                        {
                           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                                actionType: "insert",
                                dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertAnimalIdentification)),
                                recordId: animalId,
                                oldValues: null,
                                newValues: JsonSerializer.Serialize(new { Field = field.Key, Value = field.Value }),
                                status: "error",
                                errorMessage: ex.Message
                            ));

                            Console.WriteLine($"Ошибка при добавлении поля идентификации {field.Key} для животного {animal.TagNumber}: {ex.Message}");
                            importInfo.Errors++;
                        }
                    }
                }
            }
        }


        private string BuildOriginLocation(string farm, string region, string country)
        {
            var parts = new[] { farm, region, country }.Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" ", parts);
        }
        
        private Guid? GetAnimalIdByTag(string tag, Guid organizationId)
        {
            var animal = _db.Animals.FirstOrDefault(x => x.TagNumber == tag && x.OrganizationId == organizationId);
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                actionType: "search",
                table: "animals",
                recordId: organizationId
            ));
            if (animal == null) return null;
            return animal.Id;
        }

        public IEnumerable<dynamic> GetAnimalCensus(Guid organizationId, string? animalType = default,
            string? search = default, CensusSortInfoDTO? sortInfo = default)
        {   
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetBarrenCowsWithIFByOrg))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            if (animalType == "Яловые")
                return _db.GetBarrenCowsWithIFByOrg(organizationId, sortInfo)
                            .Select(e => new { e.Id, e.TagNumber, e.Status});
            return AnimalDTO.Parse(_db.GetAnimalsWithIFByOrg(organizationId, animalType, search, sortInfo));
        }

        public IEnumerable<dynamic> GetAnimalCensusByPage(Guid organizationId, string? animalType = default,
            string? search = default, CensusSortInfoDTO? sortInfo = default, int page = 1, bool isMoblile = default)
        {
            var (skip, take) = ControllersLogic.ComputePagination(isMoblile, page);
            if (animalType == "Яловые")
            {
                var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetBarrenCowsWithIFByOrg))!;
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    "search",
                    dbMethod,
                    recordId: organizationId
                ));
                
                return _db.GetBarrenCowsWithIFByOrg(organizationId, sortInfo)
                    .Select(e => new { e.Id, e.TagNumber, e.Status })
                    .Skip(skip)
                    .Take(take);
            }
            return GetAnimalCensus(organizationId, animalType, search, sortInfo)
                .Skip(skip)
                .Take(take);
        }

        public IEnumerable<AnimalByOrgAllTypesDto> GetAnimalCensusWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null,
            CensusSortInfoDTO? sortInfo = default)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalsWithIFByOrg))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));

            var grouped = _db.GetAnimalsWithIFByOrgWithFilter(organizationId, filters, sortInfo);
            
            return AnimalByOrgAllTypesDto.Parse(grouped);
        }

        public int CountAnimalCensusWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null,
            CensusSortInfoDTO? sortInfo = default)
        {
            return _db.CountAnimalsWithFilters(organizationId, filters, sortInfo);
        }

        public IEnumerable<dynamic> GetAnimalCensusByPageWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null, 
            CensusSortInfoDTO? sortInfo = default, int page = 1, bool isMobile = default)
        {
            var (skip, take) = ControllersLogic.ComputePagination(isMobile, page);

            var animals = AnimalByOrgAllTypesDto.Parse(
                _db.GetAnimalsByOrgWithFilterPage(organizationId, filters, sortInfo, skip, take));
            var animalsWithIF = GetAnimalCensusByPageWithIF(animals);
            return animalsWithIF;
        }
        
        public IEnumerable<AnimalByOrgAllTypesDto> GetAnimalCensusByPageWithIF(
            IEnumerable<AnimalByOrgAllTypesDto> animals)
        {
            var list = animals.ToList();

            var ids = list.Select(x => x.Id).ToList();

            var map = _db.GetIdentificationFields(ids);

            foreach (var a in list)
            {
                a.IdentificationFields = IdentificationFieldNameDTO.FromDictionary(
                    map.ContainsKey(a.Id) ? map[a.Id] : new Dictionary<string, string>());
            }

            return list;
        }

        public string[] GetPlaceOfOrigin(Guid organizationId)
        {
            return _db.GetPlaceOfOrigin(organizationId);
        }

        public string[] GetOrigins(Guid organizationId)
        {
            return _db.GetOrigins(organizationId);
        }

        public bool RemoveCowFromBarren(Guid animalId)
        {
            try
            {
                var result = _db.DeleteBarrenEntry(animalId);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteBarrenEntry)),
                    recordId: animalId,
                    oldValues: null,
                    status: result ? "success" : "error",
                    table: "barren_entries"
                ));

                return result;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteBarrenEntry)),
                    recordId: animalId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "barren_entries"
                ));

                return false;
            }
        }

        public void UpdateAnimal(UpdateAnimalDTO updateInfo)
        {
            var oldAnimal = _db.Animals.FirstOrDefault(e => e.Id == updateInfo.Id);
            var oldValuesJson = oldAnimal != null ? JsonSerializer.Serialize(oldAnimal) : null;

            try
            {
                var animal = oldAnimal ?? throw new Exception($"Животное с ID {updateInfo.Id} не найдено.");
                var father = updateInfo.FatherTagNumber is not null
                    ? _db.Animals.FirstOrDefault(e => e.TagNumber == updateInfo.FatherTagNumber)
                    : null;
                var mother = updateInfo.MotherTagNumber is not null
                    ? _db.Animals.FirstOrDefault(e => e.TagNumber == updateInfo.MotherTagNumber)
                    : null;

                if (updateInfo.FatherTagNumber is not null && father is null)
                    throw new Exception($"Для животного с номером бирки {animal.TagNumber} не удалось изменить номер Отца — животное с заданной биркой не найдено.");

                if (updateInfo.MotherTagNumber is not null && mother is null)
                    throw new Exception($"Для животного с номером бирки {animal.TagNumber} не удалось изменить номер Матери — животное с заданной биркой не найдено.");

                _db.UpdateAnimal(
                    id: updateInfo.Id,
                    tag: updateInfo.TagNumber,
                    breed: updateInfo.Breed,
                    motherId: mother?.Id,
                    fatherId: father?.Id,
                    status: updateInfo.Status,
                    groupId: updateInfo.GroupID,
                    origin: updateInfo.Origin,
                    originLoc: updateInfo.OriginLocation,
                    birthDate: updateInfo.BirthDate
                );

                if (updateInfo.IdentificationFields != null)
                {
                    foreach (var field in updateInfo.IdentificationFields)
                    {
                        if (field.Value != null)
                        {
                            _db.UpdateAnimal(
                                id: updateInfo.Id,
                                identificationFieldName: field.Name,
                                identificationValue: field.Value
                            );
                        }
                    }
                }

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "update",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdateAnimal)),
                    recordId: updateInfo.Id,
                    oldValues: oldValuesJson,
                    newValues: updateInfo,
                    status: "success"
                ));
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "update",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdateAnimal)),
                    recordId: updateInfo.Id,
                    oldValues: oldValuesJson,
                    status: "error",
                    errorMessage: ex.Message
                ));
            }
        }
        
        public IEnumerable<ActiveAnimalDAL> GetAnimalsForDA(Guid organizationId, DailyAnimalsDTO dto,
            int? page = default, bool isMoblile = default)
        {
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalsForActionsWithFilter))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            if (page != null)
            {
                
                var (skip, take) = ControllersLogic.ComputePagination(isMoblile, page ?? 1);
                return _db.GetAnimalsForActionsWithFilter(organizationId, dto)
                            .Skip(skip)
                            .Take(take);
            
            }
            
            return _db.GetAnimalsForActionsWithFilter(organizationId, dto);
        }

        public AnimalDTO? GetAnimalInfo(Guid organizationId, Guid animalId)
        {
            return GetAnimalCensus(organizationId).FirstOrDefault(e => e.Id == animalId);
        }

        public Dictionary<string, int> GetMainPageInfo(Guid organizationId)
        {
            var resultDict = new Dictionary<string, int>();
            var animalTypes = new List<string>() { "Корова", "Нетель", "Телка", "Бычок", "Бык" };

            foreach (var type in animalTypes)
            {
                int count = _db.Animals.Where(x=> x.OrganizationId == organizationId && x.Type == type && x.Status == "Активное").Count();
                resultDict.Add(type, count);
            }

            var totalCount = _db.Animals.Count(x => x.OrganizationId == organizationId && x.Status == "Активное");
        
            resultDict.Add("Общее количество", totalCount);

            return resultDict;
        }

        public IEnumerable<CowDTO> GetCows(Guid organizationId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetCowsByOrganization))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            return _db.GetCowsByOrganization(organizationId).ToList();
        }

        public IEnumerable<BullDTO> GetBulls(Guid organizationId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetBullsByOrganization))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            return _db.GetBullsByOrganization(organizationId).ToList();
        }

        public void InsertInsemination(InseminationDTO dto)
        {
            try
            {
                var inseminationId = _db.InsertInsemination(dto);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertInsemination)),
                    recordId: inseminationId,
                    oldValues: null,
                    newValues: dto,
                    status: "success",
                    table: "inseminations"
                ));

                var pregnancy = new InsertPregnancyDTO
                {
                    InseminationId = inseminationId,
                    CowId = dto.CowId,
                    Date = dto.Date,
                    Status = "Подлежит проверке",
                    ExpectedCalvingDate = dto.ExpectedCalvingDate
                };

                _db.InsertPregnancy(pregnancy);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertPregnancy)),
                    recordId: pregnancy.InseminationId,
                    oldValues: null,
                    newValues: pregnancy,
                    status: "success",
                    table: "pregnancies"
                ));
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertInsemination)),
                    recordId: dto.CowId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "inseminations"
                ));

                throw;
            }
        }


        public IEnumerable<CowInseminationDTO> GetPregnanciesForCalving(Guid organizationId)
        {
            var res = _db.GetAnimalReproductionData(organizationId)
                .ToList()
                .Where(x => x.PregnancyStatus == "Стельная" && x.CalvingId == null);

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalReproductionData))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));

            var bulls = GetBulls(organizationId);
            var result = new List<CowInseminationDTO>();
            var id = 0;

            foreach (var x in res)
            {
                var bullIds = ExtractBullIdsFromJson(x.BullJson);
                var bullTagNumbers = ExtractBullTagNumbers(x.BullTagNumbersJson, bullIds, bulls);
                var bullTagsString = bullTagNumbers.Any()
                    ? string.Join(", ", bullTagNumbers.Select(t => $"№{t}"))
                    : "неизвестен";

                result.Add(new CowInseminationDTO
                {
                    Id = ++id,
                    OrganizationId = x.OrganizationId,
                    CowId = x.AnimalId,
                    Status = x.AnimalStatus,
                    InseminationType = x.InseminationType,
                    InseminationDate = x.InseminationDate,
                    BullIds = bullIds,
                    BullTagNumbers = bullTagNumbers,  // Теперь это List<string> - ошибки нет
                    CowTagNumber = x.TagNumber,
                    PregnancyId = x.PregnancyId,
                    InseminationId = x.InseminationId,
                    Name = $"№{x.TagNumber}, (осеменена {x.InseminationDate?.ToString("dd.MM.yyyy") ?? "дата неизвестна"}), быком(ами) {bullTagsString}"
                });
            }

            return result;
        }



        public IEnumerable<CowInseminationDTO> GetPregnanciesForInsert(Guid organizationId)
        {
            var pregnancies = _db.GetAnimalReproductionData(organizationId)
                .ToList()
                .Where(x => x.AnimalType != "Нетель" && x.PregnancyStatus == "Подлежит проверке");

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalReproductionData))!;
            _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));

            var bulls = GetBulls(organizationId); 
            var result = new List<CowInseminationDTO>();
            var id = 0;

            foreach (var x in pregnancies)
            {
                var bullIds = ExtractBullIdsFromJson(x.BullJson);
                var bullTagNumbers = ExtractBullTagNumbers(x.BullTagNumbersJson, bullIds, bulls);
                var bullTagsString = bullTagNumbers.Any()
                    ? string.Join(", ", bullTagNumbers.Select(t => $"№{t}"))
                    : "неизвестен";

                result.Add(new CowInseminationDTO
                {
                    Id = ++id,
                    OrganizationId = x.OrganizationId,
                    CowId = x.AnimalId,
                    Status = x.AnimalStatus,
                    InseminationType = x.InseminationType,
                    InseminationDate = x.InseminationDate,
                    BullIds = bullIds,
                    BullTagNumbers = bullTagNumbers,  // Теперь это List<string> - ошибки нет
                    CowTagNumber = x.TagNumber,
                    PregnancyId = x.PregnancyId,
                    InseminationId = x.InseminationId,
                    Name = $"№{x.TagNumber}, (осеменена {x.InseminationDate?.ToString("dd.MM.yyyy") ?? "дата неизвестна"}), быком(ами) {bullTagsString}"
                });
            }

            return result;
        }

        private List<Guid> ExtractBullIdsFromJson(JsonElement? bullJson)
        {
            var bullIds = new List<Guid>();

            if (bullJson is null || bullJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return bullIds;

            var root = bullJson.Value;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(item.GetString(), out var bullId))
                    {
                        bullIds.Add(bullId);
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("fathers", out var fathers) && fathers.ValueKind == JsonValueKind.Array)
                {
                    foreach (var father in fathers.EnumerateArray())
                    {
                        if (father.ValueKind == JsonValueKind.Object &&
                            father.TryGetProperty("id", out var idProp) &&
                            idProp.ValueKind == JsonValueKind.String &&
                            Guid.TryParse(idProp.GetString(), out var bullId))
                        {
                            bullIds.Add(bullId);
                        }
                    }
                }
            }

            return bullIds;
        }

        private List<string> ExtractBullTagNumbers(JsonElement? bullTagNumbersJson, List<Guid> bullIds, IEnumerable<BullDTO> bulls)
        {
            var tagNumbers = new List<string>();

            
            if (bullTagNumbersJson.HasValue && bullTagNumbersJson.Value.ValueKind != JsonValueKind.Null)
            {
                var root = bullTagNumbersJson.Value;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(item.GetString()))
                        {
                            tagNumbers.Add(item.GetString());
                        }
                    }
                }
            }

            
            if (!tagNumbers.Any() && bullIds.Any())
            {
                foreach (var bullId in bullIds)
                {
                    var bull = bulls.FirstOrDefault(b => b.Id == bullId);
                    if (bull != null && !string.IsNullOrWhiteSpace(bull.TagNumber))
                    {
                        tagNumbers.Add(bull.TagNumber);
                    }
                }
            }

            return tagNumbers;
        }



        public void InsertPregnancy(InsertPregnancyDTO dto)
        {
            try
            {
                var oldPregnancy = _db.GetPregnancyById(dto.InseminationId);
                var oldValuesJson = oldPregnancy != null ? JsonSerializer.Serialize(oldPregnancy) : null;

                if (dto.Status == "Яловая")
                    _db.MarkAnimalAsBarren(dto.CowId);

                _db.UpdatePregnancy(new UpdatePregnancyDTO
                {
                    Id = dto.InseminationId,
                    Date = dto.Date,
                    ExceptedDate = dto.Status == "Стельная" ? dto.ExpectedCalvingDate : null,
                    Status = dto.Status
                });

                var animal = _db.Animals.FirstOrDefault(x => x.Id == dto.CowId);
                if (animal != null && animal.Type == "Телка")
                {
                    animal.Type = "Нетель";
                    _db.UpdateAnimal(animal);
                }

                if (dto.Status == "Стельная")
                    _db.RemoveAnimalBarren(dto.CowId);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertPregnancy)),
                    recordId: dto.InseminationId,
                    oldValues: oldValuesJson,
                    status: "success",
                    table: "pregnancies"
                ));
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertPregnancy)),
                    recordId: dto.InseminationId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "pregnancies"
                ));

                throw;
            }
        }

        public Guid InsertCalving(InsertCalvingDTO dto, Guid organizationId)
        {
            try
            {
                var calfId = Guid.Empty;
                var cow = _db.Animals.FirstOrDefault(x => x.Id == dto.CowId);
                var cowStatus = _db.GetPregnancyByOrganization(organizationId)
                                   .ToList()
                                   .FirstOrDefault(x => x.CowId == dto.CowId)?.Status;

                if (dto.Type == "Живой" || dto.Type == "Мертворожденный")
                {
                    JsonElement? fathersJson = null;

                    // Создаем JSON для отцов из списка BullIds или одиночного BullId
                    if (dto.BullIds is { Count: > 0 })
                    {
                        // Формируем JSON-массив из списка BullIds
                        var fathersArray = dto.BullIds.Select(bullId => bullId.ToString()).ToArray();
                        var jsonString = JsonSerializer.Serialize(fathersArray);  // Массив UUID в JSON-строке
                        using var doc = JsonDocument.Parse(jsonString);
                        fathersJson = doc.RootElement.Clone();
                    }
                    else if (dto.BullId.HasValue)
                    {
                        // Используем одиночный BullId для обратной совместимости
                        var fathersArray = new[] { dto.BullId.Value.ToString() };  // Одиночный BullId в массив
                        var jsonString = JsonSerializer.Serialize(fathersArray);  // Массив UUID в JSON-строке
                        using var doc = JsonDocument.Parse(jsonString);
                        fathersJson = doc.RootElement.Clone();
                    }

                    var calf = new Animal
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        TagNumber = dto.CalfTagNumber,
                        BirthDate = dto.Date,
                        Type = dto.Method,
                        Breed = null,
                        MotherId = dto.CowId,
                        FatherJson = fathersJson,  // сохраняем как массив UUID
                        Status = dto.Type == "Живой" ? "Активное" : "Выбывшее",
                    };

                    calfId = _db.InsertAnimalWithId(calf);

                    if (calfId == Guid.Empty)
                        throw new Exception("Не удалось создать теленка");

                    if (cowStatus == "Яловая")
                        _db.RemoveAnimalBarren(dto.CowId);

                    if (cow?.Type == "Нетель")
                    {
                        cow.Type = "Корова";
                        _db.UpdateAnimal(cow);
                    }
                }

                var calvingDto = new InsertCalvingDTO
                {
                    CowId = dto.CowId,
                    Date = dto.Date,
                    Complication = dto.Complication,
                    Type = dto.Type,
                    Veterinar = dto.Veterinar,
                    Treatments = dto.Treatments,
                    Pathology = dto.Pathology,
                    InseminationId = dto.InseminationId,
                    BullId = dto.BullId
                };

                var calvingId = _db.InsertCalving(calvingDto, calfId);

                if (dto.Type == "Живой")
                {
                    var weightDto = new InsertAnimalWeightDTO
                    {
                        Id = calvingId,
                        AnimalId = calfId,
                        Date = dto.Date,
                        Weight = dto.Weight,
                        Method = "",
                        Notes = dto.Notes
                    };
                    _db.InsertAnimalWeight(weightDto);
                }

                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertCalving)),
                    recordId: calvingId,
                    oldValues: null,
                    newValues: calvingDto,
                    status: "success",
                    table: "calvings"
                ));

                return calvingId;
            }
            catch (Exception ex)
            {
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertCalving)),
                    recordId: dto.CowId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "calvings"
                ));

                throw;
            }
        }




        public IEnumerable<BreedDTO> GetAllBreeds()
        {
            var res = _db.GetAllBreeds().Select(x => new BreedDTO
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAllBreeds))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "view",
                dbMethod
            ));

            return res;
        }

        public IEnumerable<AnimalReproductionDTO> GetAnimalReproductions(Guid organizationId)
        {
            var allAnimals = _db.GetAnimalReproductionData(organizationId).ToList();
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetAnimalReproductionData))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            var groupedAnimals = allAnimals
                .GroupBy(a => a.AnimalId)
                .Select(g => new
                {
                    Animal = g.First(),
                    Inseminations = g.Where(x => x.InseminationId.HasValue)
                                   .OrderByDescending(x => x.InseminationDate)
                                   .ToList(),
                    HasActivePregnancy = g.Any(x => x.PregnancyStatus == "Подлежит проверке" ||
                                                 (x.PregnancyStatus == "Стельная")),
                    HasCalving = g.Any(x => x.CalvingId.HasValue)
                });

            var result = groupedAnimals
                .Where(a =>
                    (a.Animal.AnimalType == "Корова" ||
                     a.Animal.AnimalType == "Телка"))
                .Where(a => !a.HasActivePregnancy ||
                           (a.HasActivePregnancy && a.HasCalving))
                .Where(a => a.Animal.AnimalStatus == "Активное")
                .Select(a =>
                {
                    var animalDto = a.Animal;
                    if (a.HasCalving)
                    {
                        animalDto.PregnancyId = null;
                        animalDto.PregnancyDate = null;
                        animalDto.PregnancyStatus = null;
                        animalDto.ExpectedCalvingDate = null;
                    }
                    return animalDto;
                });
            foreach(var animal in result)
                animal.Name = $"№{animal.TagNumber}, {animal.AnimalType}{(animal.IsBarren ? ", Яловая" :  "")}";

            return result;
        }

        public IReadOnlyList<Guid> InsertInseminations(IEnumerable<InseminationItemDTO> items)
        {
            var userId = _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var resultIds = _db.InsertInseminationsTransactional(items, (inseminationId, item) =>
                {
                    // Аудит успешной вставки осеменения
                    _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        userId,
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertInseminationsTransactional)),
                        recordId: inseminationId,
                        oldValues: null,
                        newValues: item,
                        status: "success",
                        table: "inseminations"
                    ));

                    // Создаём беременность (в той же транзакции — делается в DAL)
                    // но для аудита зафиксируем после успешной вставки беременности:
                });

                return resultIds;
            }
            catch (Exception ex)
            {
                // Аудит ошибки (на уровне батча)
                _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    userId,
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.InsertInseminationsTransactional)),
                    recordId: Guid.Empty,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "inseminations"
                ));
                throw;
            }
        }


        public void UpdatePregnancy(UpdatePregnancyDTO dto)
        {
            try
            {
                var oldValuesJson = JsonSerializer.Serialize(
                    _db.GetPregnancyById(dto.Id)
                );

                _db.UpdatePregnancy(dto);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "update",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdatePregnancy)),
                    recordId: null,
                    oldValues: oldValuesJson,
                    newValues: dto,
                    status: "success",
                    table: "pregnancies"
                ));
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                    actionType: "update",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdatePregnancy)),
                    recordId: null,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "pregnancies"
                ));
            }
        }


        private static Guid? GetFirstBullId(JsonElement? bullJson)
        {
            if (!bullJson.HasValue) return null;
            var root = bullJson.Value;

            // Формат 1: ["uuid1","uuid2",...]
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(el.GetString(), out var g))
                        return g;

                    if (el.ValueKind == JsonValueKind.Object &&
                        el.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(idProp.GetString(), out var g2))
                        return g2;
                }
            }

            // Формат 2: { "fathers": [ {"id":"uuid", ...}, ... ] }
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("fathers", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Object &&
                        el.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(idProp.GetString(), out var g))
                        return g;
                }
            }

            return null;
        }

        private static string? GetFirstBullTag(JsonElement? bullTagNumbersJson)
        {
            if (!bullTagNumbersJson.HasValue) return null;
            var root = bullTagNumbersJson.Value;
            if (root.ValueKind != JsonValueKind.Array) return null;

            foreach (var el in root.EnumerateArray())
                if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
                    return el.GetString();

            return null;
        }
    }
}
