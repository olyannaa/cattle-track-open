using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using CAT.Controllers.DTO;
using CAT.EF.DAL;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using EntityFramework = Microsoft.EntityFrameworkCore.EF;
using NpgsqlTypes;
using System.Text.Json;
using CAT.Controllers.DTO.Feeding;
namespace CAT.EF;

public partial class PostgresContext : DbContext
{
    public PostgresContext()
    {
    }

    public PostgresContext(DbContextOptions<PostgresContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Animal> Animals { get; set; }

    public virtual DbSet<AnimalIdentification> AnimalIdentifications { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<IdentificationField> IdentificationFields { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolesPermission> RolesPermissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Insemination> Inseminations { get; set; }

    public virtual DbSet<DailyAction> DailyActions { get; set; }

    public virtual DbSet<Research> Researches { get; set; }

    public virtual DbSet<GroupType> GroupTypes { get; set; }

    public virtual DbSet<GroupRaw> GroupsRaw { get; set; }
    public virtual DbSet<CowInseminationDTO> CowInseminations { get; set; }
    public virtual DbSet<AnimalReproductionDTO> AnimalReproductions { get; set; }
    public virtual DbSet<Calving> Calvings { get; set; }
    public virtual DbSet<Weight> Weights { get; set; }
    public virtual DbSet<Breed> Breeds { get; set; }
    public virtual DbSet<ActiveAnimalDAL> ActiveAnimals { get; set; }
    public virtual DbSet<AnimalDetailDAL> AnimalDetails { get; set; }
    public virtual DbSet<AnimalDetail2DAL> AnimalDetails2 { get; set; }
    public virtual DbSet<DailyActionAnimalCardDTO> AnimalActions { get; set; }
    public virtual DbSet<AnimalWeightDAL> AnimalWeights { get; set; }
    public virtual DbSet<AnimalReproductionDAL> AnimalReproductionsDAL { get; set; }
    public virtual DbSet<AnimalInseminationDAL> AnimalInseminations { get; set; }
    public virtual DbSet<AnimalPregnancyDAL> AnimalPregnancies { get; set; }
    public virtual DbSet<AnimalResearchDAL> AnimalResearches { get; set; }
    public virtual DbSet<Component> Components { get; set; }
    public virtual DbSet<Ration> Rations { get; set; }
    public virtual DbSet<RationComponent> RationComponents { get; set; }
    public virtual DbSet<GroupRation> GroupRations { get; set; }
    public virtual DbSet<PregnancyStatusDTO> PregnancyStatuses { get; set; }
    public virtual DbSet<Medicine> Medicinies { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("animals_pkey");

            entity.ToTable("animals");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.BirthDate)
                .HasMaxLength(50)
                .HasColumnName("birth_date");
            entity.Property(e => e.Breed).HasColumnName("breed");
            entity.Property(e => e.FatherJson).HasColumnName("father_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.MotherId).HasColumnName("mother_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Origin).HasColumnName("origin");
            entity.Property(e => e.OriginLocation).HasColumnName("origin_location");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TagNumber).HasColumnName("tag_number");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.Group).WithMany(p => p.Animals)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("animals_group_id_fkey");

            entity.HasOne(d => d.Organization).WithMany(p => p.Animals)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("animals_organization_id_fkey");
        });

        modelBuilder.Entity<AnimalIdentification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("animal_identification_pkey");

            entity.ToTable("animal_identification");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AnimalId).HasColumnName("animal_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.FieldId).HasColumnName("field_id");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Animal).WithMany(p => p.AnimalIdentifications)
                .HasForeignKey(d => d.AnimalId)
                .HasConstraintName("animal_identification_animal_id_fkey");

            entity.HasOne(d => d.Field).WithMany(p => p.AnimalIdentifications)
                .HasForeignKey(d => d.FieldId)
                .HasConstraintName("animal_identification_field_id_fkey");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.ToTable("groups");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.Groups)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("groups_organization_id_fkey");
        });

        modelBuilder.Entity<IdentificationField>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("identification_fields_pkey");

            entity.ToTable("identification_fields");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.FieldName).HasColumnName("field_name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.IdentificationFields)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("identification_fields_organization_id_fkey");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("organizations_pkey");

            entity.ToTable("organizations");

            entity.HasIndex(e => e.Name, "organizations_name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.Ogrn).HasColumnName("ogrn");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permissions_pkey");

            entity.ToTable("permissions");

            entity.HasIndex(e => e.Name, "permissions_permission_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("permission");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_role_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("role");
        });

        modelBuilder.Entity<RolesPermission>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("roles_permissions");

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Permission).WithMany()
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("roles_permissions_permission_id_fkey");

            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("roles_permissions_role_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone");
            entity.Property(e => e.TgId).HasColumnName("tg_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("users_organization_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_role_id_fkey");
        });
        modelBuilder.Entity<GroupType>()
            .Property(e => e.Id)
            .HasColumnName("id");

        modelBuilder.Entity<GroupType>()
            .Property(e => e.Name)
            .HasColumnName("name");
        modelBuilder.Entity<Breed>()
            .Property(e => e.Id)
            .HasColumnName("id");

        modelBuilder.Entity<Breed>()
            .Property(e => e.Name)
            .HasColumnName("name");

        modelBuilder.Entity<Insemination>(entity =>
        {
            entity.ToTable("insemination");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CowId).HasColumnName("cow_id");
            entity.Property(e => e.BullId).HasColumnName("bull_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Type).HasColumnName("insemination_type");
            entity.Property(e => e.SpermBatch).HasColumnName("sperm_batch");
            entity.Property(e => e.SpermManufacturer).HasColumnName("sperm_manufacturer");
            entity.Property(e => e.EmbryoId).HasColumnName("embryo_id");
            entity.Property(e => e.EmbryoManufacturer).HasColumnName("embryo_manufacturer");
            entity.Property(e => e.Technician).HasColumnName("technician");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.HasOne(e => e.Cow)
                .WithMany()
                .HasForeignKey(e => e.CowId)
                .HasConstraintName("insemination_cow_id_fkey");
            entity.HasOne(e => e.Bull)
                .WithMany()
                .HasForeignKey(e => e.BullId)
                .HasConstraintName("insemination_bull_id_fkey");
        });

        modelBuilder.Entity<GroupRaw>().HasNoKey().ToView(null);
        modelBuilder.Entity<CowInseminationDTO>().HasNoKey().ToView(null);
        modelBuilder.Entity<AnimalReproductionDTO>().HasNoKey().ToView(null);
        modelBuilder.Entity<CowInseminationDTO>(entity =>
        {
            entity.HasNoKey(); // Если это DTO без первичного ключа
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.CowId).HasColumnName("cow_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.InseminationType).HasColumnName("insemination_type");
            entity.Property(e => e.InseminationDate).HasColumnName("insemination_date");
            entity.Property(e => e.BullIds).HasColumnName("bull_id");
        });
        modelBuilder.Entity<RationComponent>()
        .HasKey(rc => new { rc.RationId, rc.ComponentId });
        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<Pregnancy>(entity =>
        {
            entity.ToTable("pregnancy");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.CowId)
                .HasColumnName("cow_id");

            entity.Property(e => e.Date)
                .HasColumnName("date");

            entity.Property(e => e.Status)
                .HasColumnName("status");

            entity.Property(e => e.ExpectedDate)
                .HasColumnName("expected_calving_date");


            entity.HasOne(p => p.Cow)
                .WithMany()
                .HasForeignKey(p => p.CowId)
                .HasConstraintName("pregnancy_cow_id_fkey");
        });



        modelBuilder.Entity<PregnancyStatusDTO>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);


            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.CowId).HasColumnName("cow_id");
            entity.Property(e => e.CowTagNumber).HasColumnName("cow_tag_number");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.InseminationType).HasColumnName("insemination_type");
            entity.Property(e => e.InseminationDate).HasColumnName("insemination_date");
            entity.Property(e => e.BullId).HasColumnName("bull_id");
            entity.Property(e => e.BullTagNumber).HasColumnName("bull_tag_number");
        });
    }




    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    [TableName("users")]
    public string? GetUserInfo(string? login = default, string? phone = default, string? hashedPass = default, string? tgId = default)
    {
        return Database.SqlQuery<string?>($"SELECT * FROM get_user_info2({login},{phone},{hashedPass},{tgId}) AS \"Value\"").SingleOrDefault();
    }

    [TableName("organizations")]
    public IQueryable<User>? GetOrgEmployees(Guid org_id)
        => Users.FromSqlRaw(@"SELECT * FROM get_users_by_organization({0})", org_id);

    [TableName("animals")]
    public IEnumerable<IGrouping<Guid, AnimalCensus>> GetAnimalsWithIFByOrg(Guid organizationId, string? type = default, string? search = default, CensusSortInfoDTO? sort = default)
    {
        var query = Database.SqlQuery<AnimalCensus>($"SELECT * FROM get_animals_with_if_by_organization2({organizationId})");
        if (type != null) query = query.Where(e => e.Type == type);

        if (sort is not null && sort.Active) query = query.Where(e => e.Status == "Активное");

        query = Sort(query, sort);

        var eQuery = query.AsEnumerable().GroupBy(e => e.Id);

        if (search != null)
        {
            eQuery = eQuery.Where(g => g.First().TagNumber.Contains(search)
                || g.Any(e => e.IdentificationValue != null && e.IdentificationValue.Contains(search)));
        }

        return eQuery;
    }

    public Dictionary<Guid, Dictionary<string, string>> GetIdentificationFields(
        IEnumerable<Guid> animalIds)
    {
        var ids = animalIds.ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, Dictionary<string, string>>();

        var rows = Database.SqlQuery<AnimalIdentificationFieldDTO>(
            $"""
            SELECT
                ai.animal_id,
                f.field_name,
                ai.value
            FROM animal_identification ai
                JOIN identification_fields f ON f.id = ai.field_id
            WHERE ai.animal_id = ANY({ids});
            """
        ).ToList();

        return rows
            .GroupBy(r => r.AnimalId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .GroupBy(x => x.Name ?? string.Empty)
                    .ToDictionary(
                        gg => gg.Key,
                        gg => gg.First().Value ?? string.Empty
                    )
            );
    }

    public class AnimalIdentificationFieldDTO
    {
        [Column("animal_id")]
        public Guid AnimalId { get; set; }
        [Column("field_name")]
        public string Name { get; set; }
        [Column("value")]
        public string Value { get; set; }
    }

    private IQueryable<AnimalCensus> GetAnimalsByOrganizationQuery(Guid organizationId)
    {
        return Database.SqlQuery<AnimalCensus>($@"
        SELECT
          a.id,
          a.tag_number,
          a.birth_date,
          a.type,
          a.breed,
          g.name AS group_name,
          a.status,
          a.origin,
          a.origin_location,
          m.tag_number AS mother_tag_number,
          COALESCE((
              SELECT jsonb_agg(f.tag_number)
              FROM jsonb_array_elements_text(
                  CASE
                      WHEN jsonb_typeof(a.father_id) = 'array' THEN a.father_id
                      WHEN jsonb_typeof(a.father_id) = 'string' THEN jsonb_build_array(a.father_id)
                      ELSE '[]'::jsonb
                  END
              ) father_ids(father_id)
              JOIN animals f ON f.id = father_ids.father_id::uuid
          ), '[]'::jsonb)::text AS father_tag_numbers,
          (
              SELECT max(da.date)
              FROM daily_actions da
              WHERE da.animal_id = a.id
                AND da.action_type = 'Вакцинация'
          ) AS last_vaccination_date,

          NULL::date               AS date_of_receipt,
          NULL::date               AS date_of_disposal,
          NULL::text               AS reason_of_disposal,
          NULL::text               AS consumption,
          NULL::double precision   AS live_weight_at_disposal,
          NULL::date               AS last_weigh_date,
          NULL::text               AS last_weight_weight,
          NULL::text               AS identification_field_name,
          NULL::text               AS identification_value
        FROM animals a
          LEFT JOIN groups g ON g.id = a.group_id
          LEFT JOIN animals m ON m.id = a.mother_id
        WHERE a.organization_id = {organizationId}
        ");
    }

    [TableName("animals")]
    public IEnumerable<IGrouping<Guid, AnimalCensus>> GetAnimalsWithIFByOrgWithFilter(Guid organizationId, AnimalFiltersDTO? filters = default, CensusSortInfoDTO? sort = default)
    {
        return GetFilteredAnimalsByOrganizationQuery(organizationId, filters, sort)
            .AsEnumerable()
            .GroupBy(x => x.Id);
    }

    public int CountAnimalsWithFilters(Guid organizationId, AnimalFiltersDTO? filters = default, CensusSortInfoDTO? sort = default)
    {
        return GetFilteredAnimalsByOrganizationQuery(organizationId, filters, sort).Count();
    }

    public IEnumerable<IGrouping<Guid, AnimalCensus>> GetAnimalsByOrgWithFilterPage(
        Guid organizationId,
        AnimalFiltersDTO? filters = default,
        CensusSortInfoDTO? sort = default,
        int skip = default,
        int take = default)
    {
        var query = GetFilteredAnimalsByOrganizationQuery(organizationId, filters, sort);

        if (take > 0)
            query = query.Skip(skip).Take(take);

        return query.AsEnumerable().GroupBy(x => x.Id);
    }

    private IQueryable<AnimalCensus> GetFilteredAnimalsByOrganizationQuery(
        Guid organizationId,
        AnimalFiltersDTO? filters = default,
        CensusSortInfoDTO? sort = default)
    {
        var query = GetAnimalsByOrganizationQuery(organizationId);

        query = ApplyFiltersSqlFriendly(query, filters, sort);

        return Sort(query, sort);
    }

    private static IQueryable<AnimalCensus> ApplyFiltersSqlFriendly(
        IQueryable<AnimalCensus> query, AnimalFiltersDTO? f,
        CensusSortInfoDTO? sort)
    {
        if (f is null) return ApplyLegacyActive(query, sort);

        if (f.Types is { Count: > 0 })
            query = query.Where(x => x.Type != null && f.Types.Contains(x.Type));

        if (f.Statuses is { Count: > 0 })
            query = query.Where(x => x.Status != null && f.Statuses.Contains(x.Status));

        query = ApplyLegacyActive(query, sort, f);

        if (!string.IsNullOrWhiteSpace(f.TagNumber))
            query = query.Where(x => x.TagNumber.Contains(f.TagNumber));

        if (!string.IsNullOrWhiteSpace(f.MotherTagNumber))
            query = query.Where(x => x.MotherTagNumber != null && x.MotherTagNumber.Contains(f.MotherTagNumber));

        if (f.BirthDateFrom.HasValue)
            query = query.Where(x => x.BirthDate.HasValue && x.BirthDate.Value >= f.BirthDateFrom.Value);

        if (f.BirthDateTo.HasValue)
            query = query.Where(x => x.BirthDate.HasValue && x.BirthDate.Value <= f.BirthDateTo.Value);

        if (f.Breeds is { Count: > 0 })
            query = query.Where(x => x.Breed != null && f.Breeds.Contains(x.Breed));

        if (f.GroupNames is { Count: > 0 })
            query = query.Where(x => x.GroupName != null && f.GroupNames.Contains(x.GroupName));

        if (f.Origins is { Count: > 0 })
            query = query.Where(x => x.Origin != null && f.Origins.Contains(x.Origin));

        if (f.OriginLocations is { Count: > 0 })
            query = query.Where(x => x.OriginLocation != null && f.OriginLocations.Contains(x.OriginLocation));

        if (!string.IsNullOrWhiteSpace(f.FatherTagNumber))
            query = query.Where(x => x.FatherTagNumbers != null && x.FatherTagNumbers.Contains(f.FatherTagNumber));

        return query;
    }

    private static IQueryable<AnimalCensus> ApplyLegacyActive(
        IQueryable<AnimalCensus> query,
        CensusSortInfoDTO? sort,
        AnimalFiltersDTO? f = null)
    {
        if (sort is not null && sort.Active && (f?.Statuses is null || f.Statuses.Count == 0))
            query = query.Where(x => x.Status == "Активное");

        return query;
    }

    private static IEnumerable<IGrouping<Guid, AnimalCensus>> ApplyFiltersInMemory(
        IEnumerable<IGrouping<Guid, AnimalCensus>> grouped,
        AnimalFiltersDTO? f)
    {
        if (f is null) return grouped;

        if (string.IsNullOrWhiteSpace(f.FatherTagNumber)) return grouped;
        var s = f.FatherTagNumber;
        grouped = grouped.Where(g =>
        {
            var first = g.First();
            var list = first.FatherTagNumbersList;
            return list != null && list.Any(x => x.Contains(s));
        });

        return grouped;
    }


    [TableName("animals")]
    public IQueryable<AnimalWeightsDAL> GetAnimalWeightInfo(Guid animalId, WeightsSortInfoDTO? sort = default)
    {
        var query = Database.SqlQuery<AnimalWeightsDAL>($"SELECT * FROM get_animal_weights2({animalId})");

        query = Sort(query, sort);

        return query;
    }

    [TableName("animals")]
    public IQueryable<BarrenCowDAL> GetBarrenCowsWithIFByOrg(Guid organizationId, CensusSortInfoDTO? sort = default)
    {
        var query = GetBarrenCows(organizationId);

        if (sort is not null && sort.Active) query = query.Where(e => e.Status == "Активное");
        query = Sort(query, sort);

        return query;
    }

    [TableName("animals")]
    public IQueryable<ActiveAnimalDAL> GetAnimalsForActionsWithFilter(Guid organizationId, DailyAnimalsDTO dto)
    {
        var orgAnimals = Animals.Where(e => e.OrganizationId == organizationId);

        if (dto.Filter.IsActive ?? false)
            orgAnimals = orgAnimals.Where(e => e.Status == "Активное");

        if (dto.Filter.GroupId != null)
            orgAnimals = orgAnimals.Where(e => e.GroupId == dto.Filter.GroupId);

        if (dto.Filter.Type != null)
            orgAnimals = orgAnimals.Where(e => e.Type == dto.Filter.Type);

        if (dto.Filter.TagNumber != null)
            orgAnimals = orgAnimals.Where(e => e.TagNumber == dto.Filter.TagNumber);

        var field = dto.Filter.IdentificationField;
        if (field != null)
        {
            var animalIds = AnimalIdentifications.Where(e => e.FieldId == field.Id && e.Value == field.Value)
                                                .Select(e => e.AnimalId)
                                                .ToList();
            orgAnimals = orgAnimals.Where(e => animalIds.Contains(e.Id));
        }

        var result = orgAnimals.Include(e => e.Group)
                        .Select(e => new ActiveAnimalDAL
                        {
                            Id = e.Id,
                            TagNumber = e.TagNumber,
                            Type = e.Type,
                            Status = e.Status,
                            GroupId = e.GroupId,
                            GroupName = e.Group.Name
                        });

        return Sort(result, dto.SortInfo);
    }

    [TableName("daily_actions")]
    public IQueryable<dynamic>? GetDailyActionsWithPagination(Guid organizationId, string type,
        DailyActionsSortInfoDTO sort, int skip = default, int take = default)
    {
        return GetDailyActions(organizationId, type, sort)?.Skip(skip)?.Take(take);
    }

    [TableName("daily_actions")]
    public IQueryable<dynamic> GetDailyActions(Guid organizationId, string type, DailyActionsSortInfoDTO? sort = default)
    {
        IQueryable<dynamic> query;

        if (type == "Исследования")
            query = Database.SqlQuery<GetResearchDAL>($"SELECT * FROM get_research_by_organization({organizationId})");
        else
            query = Database.SqlQuery<GetActionsDAL>($"SELECT * FROM get_actions_by_organization_and_type({organizationId},{type})");

        query = Sort(query, sort);

        return query;
    }

    [TableName("daily_actions")]
    public int InsertDailyAction(Guid id, Guid animalId = default, string? actionType = default, string? actionSubtype = default,
        DateOnly? date = default, string? performedBy = default, string? result = default, string? medicine = default,
        string? dose = default, string? notes = default, DateOnly? nextActionDate = default, Guid? oldGroupId = default, Guid? newGroupId = default)
    {
        return Database.ExecuteSqlInterpolated($@"SELECT insert_daily_action({id},{animalId},{actionType},{actionSubtype},{date},
            {performedBy},{result},{medicine},{dose},{notes},{nextActionDate},{oldGroupId},{newGroupId})");
    }

    [TableName("daily_actions")]
    public int InsertDailyActionType(Guid id, Guid animalId = default, string? actionType = default, string? actionSubtype = default,
        DateOnly? date = default, string? performedBy = default, string? result = default, string? medicine = default,
        string? dose = default, string? notes = default, DateOnly? nextActionDate = default, string? oldType = default, string? newType = default)
    {
        return Database.ExecuteSqlInterpolated($@"SELECT insert_daily_action_new_type({id},{animalId},{actionType},{actionSubtype},{date},
            {performedBy},{result},{medicine},{dose},{notes},{nextActionDate},{oldType},{newType})");
    }

    [TableName("research")]
    public int InsertResearch(Guid id, Guid orgId, Guid animalId = default, string? name = default, string? materialType = default,
        DateOnly? collectionDate = default, string? collectedBy = default, string? result = default, string? notes = default)
    {
        return Database.ExecuteSqlInterpolated($@"SELECT insert_research({id},{orgId},{animalId},{name},
            {materialType},{collectionDate},{collectedBy},{result},{notes})");
    }

    [TableName("animals")]
    public int UpdateAnimal(Guid id, string? tag = default, string? type = default, string? breed = default, Guid? motherId = default,
        Guid? fatherId = default, string? status = default, Guid? groupId = default, string? origin = default, string? originLoc = default,
        DateOnly? birthDate = default, DateOnly? dateOfReceipt = default, DateOnly? dateOfDisposal = default, string? reasonOfDisposal = default,
        string? consumption = default, double? liveWeightAtDisposal = default, DateOnly? lastWeightDate = default,
        string? lastWeightWeight = default, string? identificationFieldName = default, string? identificationValue = default)
    {
        return Database.ExecuteSqlInterpolated($@"SELECT update_animal_data_with_if({id},{tag},{type},{breed},
            {motherId},{fatherId},{status},{groupId},{origin},{originLoc},{birthDate},{dateOfReceipt},{dateOfDisposal},
            {reasonOfDisposal},{consumption},{liveWeightAtDisposal},{lastWeightDate},{lastWeightWeight},{identificationFieldName},
            {identificationValue})");

    }

    [TableName("organizations")]
    public IQueryable<IdentificationInfoDTO>? GetOrgIdentifications(Guid org_id)
        => IdentificationFields.FromSqlRaw(@"SELECT * FROM get_identification_fields({0})", org_id)
                                .Select(x => new IdentificationInfoDTO { Id = x.Id, Name = x.FieldName });

    [TableName("groups")]
    public IQueryable<Group>? GetOrgGroups(Guid org_id)
        => Groups.FromSqlRaw(@"SELECT * FROM get_groups({0})", org_id);

    [TableName("animals")]
    public void InsertAnimal(Animal animal)
        => Database.ExecuteSqlInterpolated($@"SELECT insert_animal2(
                                       {animal.Id}, {animal.OrganizationId}, {animal.TagNumber},
                                       {animal.BirthDate}, {animal.Type},
                                       {animal.Breed}, {animal.MotherId}, {animal.FatherJson}, {animal.Status},
                                       {animal.GroupId}, {animal.Origin}, {animal.OriginLocation}
                                       )");


    [TableName("animal")]
    public Guid InsertAnimalWithId(Animal animal)
    {
        var connection = (NpgsqlConnection)Database.GetDbConnection();
        connection.Open();
        try
        {
            // функция возвращает void → просто вызов SELECT ...; ExecuteNonQuery()
            const string sql = @"SELECT public.insert_animal2(
            @p_id,
            @p_organization_id,
            @p_tag_number,
            @p_birth_date,
            @p_type,
            @p_breed,
            @p_mother_id,
            @p_father_id,          -- jsonb
            @p_status,
            @p_group_id,
            @p_origin,
            @p_origin_location
        );";

            using var cmd = new NpgsqlCommand(sql, connection);

            cmd.Parameters.Add("p_id", NpgsqlDbType.Uuid).Value = animal.Id;
            cmd.Parameters.Add("p_organization_id", NpgsqlDbType.Uuid).Value = animal.OrganizationId;

            var pTag = cmd.Parameters.Add("p_tag_number", NpgsqlDbType.Text);
            pTag.Value = (object?)animal.TagNumber ?? DBNull.Value;

            var pBirth = cmd.Parameters.Add("p_birth_date", NpgsqlDbType.Date);
            pBirth.Value = animal.BirthDate.HasValue ? animal.BirthDate.Value : (object)DBNull.Value;

            var pType = cmd.Parameters.Add("p_type", NpgsqlDbType.Text);
            pType.Value = (object?)animal.Type ?? DBNull.Value;

            var pBreed = cmd.Parameters.Add("p_breed", NpgsqlDbType.Text);
            pBreed.Value = (object?)animal.Breed ?? DBNull.Value;

            var pMother = cmd.Parameters.Add("p_mother_id", NpgsqlDbType.Uuid);
            pMother.Value = animal.MotherId.HasValue ? animal.MotherId.Value : (object)DBNull.Value;

            var pFather = cmd.Parameters.Add("p_father_id", NpgsqlDbType.Jsonb);
            if (animal.FatherJson is null || animal.FatherJson?.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                pFather.Value = DBNull.Value;
            else
                pFather.Value = animal.FatherJson.Value;

            var pStatus = cmd.Parameters.Add("p_status", NpgsqlDbType.Text);
            pStatus.Value = (object?)animal.Status ?? DBNull.Value;

            var pGroup = cmd.Parameters.Add("p_group_id", NpgsqlDbType.Uuid);
            pGroup.Value = animal.GroupId.HasValue ? animal.GroupId.Value : (object)DBNull.Value;

            var pOrigin = cmd.Parameters.Add("p_origin", NpgsqlDbType.Text);
            pOrigin.Value = (object?)animal.Origin ?? DBNull.Value;

            var pOriginLoc = cmd.Parameters.Add("p_origin_location", NpgsqlDbType.Text);
            pOriginLoc.Value = (object?)animal.OriginLocation ?? DBNull.Value;

            cmd.ExecuteNonQuery();   // функция RETURNS void
            return animal.Id;
        }
        finally
        {
            connection.Close();
        }
    }

    [TableName("animal_identification")]
    public void InsertAnimalIdentification(Guid id, Guid fieldName, string fieldValue)
        => Database.ExecuteSqlInterpolated($@"SELECT insert_animal_identification({id}, {fieldName}, {fieldValue})");

    [TableName("insemination")]
    public void IfNetelInsertReproduction(Guid animalId, DateOnly? inseminationDate,
                                      DateOnly? expectedCalvingDate, string inseminationType,
                                      string spermBatch, string technician, string notes)
    => Database.ExecuteSqlInterpolated($@"SELECT if_netel_insert_insemination_and_pregnancy({animalId},
                                {inseminationDate}, {expectedCalvingDate}, {inseminationType},
                                {spermBatch}, {technician}, {notes}, {"Подлежит проверке"})");
    [TableName("identification_fields")]
    public void AddIdentificationField(string fieldName, Guid organizationId)
        => Database.ExecuteSqlInterpolated($@"SELECT add_identification_field({fieldName}, {organizationId})");

    [TableName("identification_fields")]
    public void DeleteIdentification(Guid identificationId)
        => Database.ExecuteSqlInterpolated($@"SELECT delete_identification_field({identificationId})");

    [TableName("groups")]
    public void AddGroupType(Guid organizationId, string name)
        => Database.ExecuteSqlInterpolated($@"SELECT add_group_type({name}, {organizationId})");

    [TableName("groups")]
    public IQueryable<GroupType> GetGroupTypes(Guid organizationId)
        => GroupTypes.FromSqlRaw(@"SELECT * FROM get_group_types_by_organization({0})", organizationId);

    [TableName("groups")]
    public void DeleteGroupType(Guid typeId)
        => Database.ExecuteSqlInterpolated($@"SELECT delete_group_type({typeId})");

    [TableName("groups")]
    public void AddGroup(Guid organizationId, string name, Guid? typeId, string? description = "", string? location = "")
        => Database.ExecuteSqlInterpolated($@"SELECT add_group({organizationId}, {name}, {typeId}, {description}, {location})");

    [TableName("groups")]
    public IQueryable<GroupRaw> GetGroupsByOrganization(Guid organizationId)
       => GroupsRaw.FromSqlRaw(@"SELECT * FROM get_groups_by_organization({0})", organizationId);

    [TableName("groups")]
    public void DeleteGroup(Guid groupId)
        => Database.ExecuteSqlInterpolated($@"SELECT delete_group({groupId})");

    public bool DeleteBarrenEntry(Guid animalId)
        => Database.SqlQuery<bool>($"SELECT remove_animal_from_barren({animalId}) AS \"Value\"").SingleOrDefault();

    [TableName("groups")]
    public void EditGroup(Guid groupId, Guid organizationId, string groupName, Guid? typeId, string? description = "", string? location = "")
        => Database.ExecuteSqlInterpolated($@"SELECT update_group({groupId},{organizationId}, {groupName}, {typeId}, {description}, {location})");

    [TableName("animal")]
    public Guid InsertAnimalToDatabase(Guid org_id, AnimalInfoDTO animal,
                    (DateOnly? birthDate, DateOnly? dateOfReceipt, DateOnly? dateOfDisposal,
                     DateOnly? lastWeightDate, double? lastWeightAtDisposal) parsedData,
                    Guid? motherId, Guid? fatherId, string originLocation)
    {


        var parameters = new[]
         {
                new NpgsqlParameter("@p_organization_id", org_id),
                new NpgsqlParameter("@p_tag_number", animal.TagNumber),
                new NpgsqlParameter("@p_birth_date", parsedData.birthDate ?? (object)DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Date },
                new NpgsqlParameter("@p_type", animal.Type ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_breed", animal.Breed ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_mother_id", motherId ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_father_id", fatherId ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_status", animal.Status),
                new NpgsqlParameter("@p_group_id", DBNull.Value),
                new NpgsqlParameter("@p_origin", string.Empty),
                new NpgsqlParameter("@p_origin_location", originLocation ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_consumption", animal.Сonsumption ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_date_of_receipt", parsedData.dateOfReceipt ?? (object)DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Date },
                new NpgsqlParameter("@p_date_of_disposal", parsedData.dateOfDisposal ?? (object)DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Date },
                new NpgsqlParameter("@p_last_weight_weight", animal.LastWeightWeight ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_live_weight_at_disposal", parsedData.lastWeightAtDisposal ?? (object)DBNull.Value),
                new NpgsqlParameter("@p_last_weigh_date", parsedData.lastWeightDate ?? (object)DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Date },
                new NpgsqlParameter("@p_reason_of_disposal", animal.ReasonOfDisposal ?? (object)DBNull.Value)
            };

        Database.ExecuteSqlRaw(@"SELECT FROM insert_animal_from_csv(
                @p_organization_id, @p_tag_number, @p_birth_date, @p_type,
                @p_breed, @p_mother_id, @p_father_id, @p_status,
                @p_group_id, @p_origin, @p_origin_location, @p_consumption,
                @p_date_of_receipt, @p_date_of_disposal, @p_last_weight_weight,
                @p_live_weight_at_disposal, @p_last_weigh_date, @p_reason_of_disposal)", parameters);
        var createdAnimal = Animals
            .FirstOrDefault(a => a.OrganizationId == org_id && a.TagNumber == animal.TagNumber);

        return createdAnimal.Id;
    }

    [TableName("animals")]
    public void UpdateAnimalParents(Guid animalId, Guid? motherId, Guid? fatherId)
    {
        var sql = @"
        UPDATE animals
        SET mother_id = @p_mother_id,
            father_id = @p_father_id
        WHERE id = @p_id";

        var parameters = new[]
        {
            new NpgsqlParameter("@p_id", animalId),
            new NpgsqlParameter("@p_mother_id", (object?)motherId ?? DBNull.Value),
            new NpgsqlParameter("@p_father_id", (object?)fatherId ?? DBNull.Value)
        };

        Database.ExecuteSqlRaw(sql, parameters);
    }




    [TableName("animals")]
    public IQueryable<CowDTO> GetCowsByOrganization(Guid organizationId)
        => Animals.FromSqlRaw(@"SELECT * FROM get_cows_by_organization({0})", organizationId)
        .Select(a => new CowDTO
        {
            Id = a.Id,
            OrganizationId = a.OrganizationId,
            TagNumber = a.TagNumber ?? string.Empty,
            Type = a.Type,
            BirthDate = a.BirthDate,
            Status = a.Status
        });

    [TableName("animals")]
    public IQueryable<BullDTO> GetBullsByOrganization(Guid organizationId)
        => Animals.FromSqlRaw(@"SELECT * FROM get_bulls_by_organization({0})", organizationId)
        .Select(a => new BullDTO
        {
            Id = a.Id,
            OrganizationId = a.OrganizationId,
            TagNumber = a.TagNumber ?? string.Empty,
            Type = a.Type,
            BirthDate = a.BirthDate,
            Status = a.Status
        });

    [TableName("insemination")]
    public Guid InsertInsemination(InseminationDTO insemination)
    {
        var connection = (NpgsqlConnection)Database.GetDbConnection();
        try
        {
            connection.Open();
            string functionName = insemination.InseminationType == "Искусственное"
                ? "insert_insemination2"
                : "insert_insemination";

            string sql = $"SELECT {functionName}(@cowId, @date, @inseminationType, @spermBatch, @spermManufacturer, @bullId, @embryoId, @embryoManufacturer, @technician, @notes{(functionName == "insert_insemination2" ? ", @bull_name" : "")})";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("cowId", insemination.CowId);
            command.Parameters.AddWithValue("date", insemination.Date);
            command.Parameters.AddWithValue("inseminationType", insemination.InseminationType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("spermBatch", insemination.SpermBatch ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("spermManufacturer", insemination.SpermManufacturer ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("bullId", insemination.BullId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("embryoId", insemination.EmbryoId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("embryoManufacturer", insemination.EmbryoManufacturer ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("technician", insemination.Technician ?? (object)DBNull.Value);
            if (functionName == "insert_insemination2")
            {
                command.Parameters.AddWithValue("bull_name", insemination.BullName ?? (object)DBNull.Value);
            }
            command.Parameters.AddWithValue("notes", insemination.Notes ?? (object)DBNull.Value);


            return (Guid)command.ExecuteScalar();
        }
        finally
        {
            connection.Close();
        }
    }
    [TableName("insemination")]
    public IReadOnlyList<Guid> InsertInseminationsTransactional(
        IEnumerable<InseminationItemDTO> items,
        Action<Guid, InseminationItemDTO>? onInserted = null)
    {
        var ids = new List<Guid>();
        var connection = (NpgsqlConnection)Database.GetDbConnection();

        connection.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            foreach (var item in items)
            {

                var cowIds = (item.CowIds is { Count: > 0 })
                    ? item.CowIds
                    : (item.CowId.HasValue ? new List<Guid> { item.CowId.Value } : new List<Guid>());

                if (cowIds.Count == 0)
                    throw new InvalidOperationException("Не переданы идентификаторы коров (CowIds/CowId).");

                JsonElement? bullsJsonEl = null;
                if (item.BullIds is { Count: > 0 })
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(item.BullIds);
                    using var doc = JsonDocument.Parse(json);
                    bullsJsonEl = doc.RootElement.Clone();
                }
                else if (item.BullJson.HasValue &&
                         !(item.BullJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined))
                {
                    bullsJsonEl = item.BullJson.Value;
                }

                foreach (var cowId in cowIds)
                {
                    const string sql = @"SELECT public.insert_insemination4(
                @p_cow_id,
                @p_date,
                @p_insemination_type,
                @p_sperm_batch,
                @p_sperm_manufacturer,
                @p_bull_id,  -- теперь передаем JSONB
                @p_embryo_id,
                @p_embryo_manufacturer,
                @p_technician,
                @p_notes,
                @p_bull_name
            );";

                    using var cmd = new NpgsqlCommand(sql, connection, tx);

                    cmd.Parameters.Add("p_cow_id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = cowId;
                    cmd.Parameters.Add("p_date", NpgsqlTypes.NpgsqlDbType.Date).Value = item.Date;
                    cmd.Parameters.Add("p_insemination_type", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.InseminationType ?? DBNull.Value;
                    cmd.Parameters.Add("p_sperm_batch", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.SpermBatch ?? DBNull.Value;
                    cmd.Parameters.Add("p_sperm_manufacturer", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.SpermManufacturer ?? DBNull.Value;

                    // ИСПРАВЛЕНИЕ: передаем JSONB вместо UUID
                    var pBull = cmd.Parameters.Add("p_bull_id", NpgsqlTypes.NpgsqlDbType.Jsonb);
                    if (bullsJsonEl.HasValue && !(bullsJsonEl.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined))
                        pBull.Value = bullsJsonEl.Value;
                    else
                        pBull.Value = DBNull.Value;

                    cmd.Parameters.Add("p_embryo_id", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.EmbryoId ?? DBNull.Value;
                    cmd.Parameters.Add("p_embryo_manufacturer", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.EmbryoManufacturer ?? DBNull.Value;
                    cmd.Parameters.Add("p_technician", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.Technician ?? DBNull.Value;
                    cmd.Parameters.Add("p_notes", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.Notes ?? DBNull.Value;
                    cmd.Parameters.Add("p_bull_name", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)item.BullName ?? DBNull.Value;

                    var inseminationId = (Guid)cmd.ExecuteScalar()!;
                    ids.Add(inseminationId);

                    var preg = new InsertPregnancyDTO
                    {
                        InseminationId = inseminationId,
                        CowId = cowId,
                        Date = item.Date,
                        Status = "Подлежит проверке",
                        ExpectedCalvingDate = item.ExpectedCalvingDate
                    };
                    InsertPregnancyTransactional(connection, tx, preg);

                    onInserted?.Invoke(inseminationId, item);
                }
            }

            tx.Commit();
            return ids;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
        finally
        {
            connection.Close();
        }
    }


    // Локальная версия вставки беременности, работающая в текущей транзакции
    private Guid InsertPregnancyTransactional(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        InsertPregnancyDTO pregnancy)
    {
        const string sql = @"SELECT public.insert_pregnancy2(
        @p_cow_id,
        @p_date,
        @p_status,
        @p_expected_calving_date,
        @p_insemination_id
    );";

        using var cmd = new NpgsqlCommand(sql, connection, tx);

        cmd.Parameters.Add("p_cow_id", NpgsqlDbType.Uuid).Value = pregnancy.CowId;
        cmd.Parameters.Add("p_date", NpgsqlDbType.Date).Value = pregnancy.Date;
        cmd.Parameters.Add("p_status", NpgsqlDbType.Text).Value = pregnancy.Status;

        var pCalving = cmd.Parameters.Add("p_expected_calving_date", NpgsqlDbType.Date);
        pCalving.Value = pregnancy.ExpectedCalvingDate.HasValue
            ? pregnancy.ExpectedCalvingDate.Value
            : (object)DBNull.Value;

        cmd.Parameters.Add("p_insemination_id", NpgsqlDbType.Uuid).Value = pregnancy.InseminationId;

        return (Guid)cmd.ExecuteScalar()!;
    }


    [TableName("insemination")]
    public void InsertPregnancy(InsertPregnancyDTO pregnancy)
        => Database.ExecuteSqlInterpolated($@"
        SELECT insert_pregnancy(
            {pregnancy.CowId}, {pregnancy.Date}, {pregnancy.Status}, {pregnancy.ExpectedCalvingDate}, {pregnancy.InseminationId})");

    [TableName("pregnancy")]
    public void UpdatePregnancy(UpdatePregnancyDTO pregnancy)
        => Database.ExecuteSqlInterpolated($@"
        SELECT update_pregnancy2(
            {pregnancy.Id}, {pregnancy.Date}, {pregnancy.Status}, {pregnancy.ExceptedDate})");

    [TableName("calvings")]
    public int DeleteCalvingsByCow(Guid cowId)
        => Database.ExecuteSqlInterpolated($"SELECT delete_calvings_by_cow({cowId})");

    [TableName("insemination")]
    public int DeleteInseminationByCow(Guid cowId)
        => Database.ExecuteSqlInterpolated($"SELECT delete_insemination_by_cow({cowId})");

    [TableName("pregnancy")]
    public void DeletePregnancyByCow(Guid cowId)
        => Database.ExecuteSqlInterpolated($"SELECT delete_pregnancy_by_cow({cowId})");

    [TableName("pregnancy")]
    public IQueryable<PregnancyStatusDTO> GetPregnancyByOrganization(Guid organizationId)
        => PregnancyStatuses
            .FromSqlRaw(@"SELECT * FROM get_pregnancy_by_organization2({0})", organizationId);

    [TableName("pregnancy")]
    public Pregnancy? GetPregnancyById(Guid pregnancyId)
    {
        return Set<Pregnancy>()
            .FromSqlRaw("SELECT * FROM pregnancy WHERE id = {0}", pregnancyId)
            .Include(p => p.Cow)
            .FirstOrDefault();
    }

    [TableName("calvings")]
    public Guid InsertCalving(InsertCalvingDTO dto, Guid calfId)
    {
        var connection = Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT insert_calving2(@cowId, @date, @complication, @type, @veterinar, @treatments, @pathology, @calfId, @inseminationId)";
        command.Parameters.Add(new NpgsqlParameter("cowId", dto.CowId));
        command.Parameters.Add(new NpgsqlParameter("date", dto.Date));
        command.Parameters.Add(new NpgsqlParameter("complication", dto.Complication));
        command.Parameters.Add(new NpgsqlParameter("type", dto.Type));
        command.Parameters.Add(new NpgsqlParameter("veterinar", dto.Veterinar ?? (object)DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("treatments", dto.Treatments ?? (object)DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("pathology", dto.Pathology ?? (object)DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("calfId", calfId));
        command.Parameters.Add(new NpgsqlParameter("inseminationId", dto.InseminationId));
        var newCalvingId = (Guid)command.ExecuteScalar();
        return newCalvingId;
    }

    [TableName("animals")]
    public void InsertAnimalWeight(InsertAnimalWeightDTO dto)
        => Database.ExecuteSqlInterpolated($@"
            INSERT INTO weights (id, animal_id, date, weight, method, notes)
            VALUES ({dto.Id}, {dto.AnimalId}, {dto.Date}, {dto.Weight}, {dto.Method}, {dto.Notes})");

    [TableName("daily_actions")]
    public int DeleteDailyAction(Guid actionId)
        => Database.ExecuteSqlInterpolated($@"SELECT delete_daily_action({actionId})");

    [TableName("research")]
    public int DeleteResearch(Guid researchId)
        => Database.ExecuteSqlInterpolated($@"SELECT delete_research({researchId})");

    public IQueryable<string?> GetIdentificationValues(Guid identificationId, Guid orgId, IdentificationValuesFilterDTO? filter = default)
    {
        var query = AnimalIdentifications.Include(e => e.Animal)
                                        .Where(e => e.Animal.OrganizationId == orgId)
                                        .Where(e => e.FieldId == identificationId);

        if (filter is not null)
        {
            if (filter.GroupId != null) query = query.Where(e => e.Animal.GroupId == filter.GroupId);
            if (filter.Type != null) query = query.Where(e => e.Animal.Type == filter.Type);
            if (filter.IsActive ?? false) query = query.Where(e => e.Animal.Status == "Активное");
        }
        return query.Select(e => e.Value).Where(e => e != String.Empty);
    }

    private IQueryable<T> Sort<T>(IQueryable<T> query, BaseSortInfoDTO? sort = default)
    {
        if (sort is not null && sort.Column is not null)
        {
            if (sort.Column == "Status")
            {
                query = sort.Descending
                    ? query
                        .OrderByDescending(p => EntityFramework.Property<string>(p, "Status") == "Активное" ? 0 : 1)
                        .ThenByDescending(p => EntityFramework.Property<string>(p, "Status"))
                    : query
                        .OrderBy(p => EntityFramework.Property<string>(p, "Status") == "Активное" ? 0 : 1)
                        .ThenBy(p => EntityFramework.Property<string>(p, "Status"));

                return query;
            }

            query = sort.Descending ? query.OrderByDescending(p => EntityFramework.Property<T>(p, sort.Column))
                                    : query.OrderBy(p => EntityFramework.Property<T>(p, sort.Column));
        }

        return query;
    }

    [TableName("breeds")]
    public IQueryable<Breed> GetAllBreeds()
        => Breeds;


    [TableName("barren")]
    public IQueryable<BarrenCowDAL> GetBarrenCows(Guid organizationId)
    {
        return Database.SqlQuery<BarrenCowDAL>($"SELECT * FROM get_barren_cows({organizationId})");
    }

    public IQueryable<AnimalReproductionDTO> GetAnimalReproductionData(Guid organizationId)
    => AnimalReproductions.FromSqlRaw(
        "SELECT * FROM public.get_animals_with_reproduction_data4({0})", organizationId);


    public void MarkAnimalAsBarren(Guid animalId)
        => Database.ExecuteSqlInterpolated($@"SELECT mark_animal_as_barren({animalId})");

    [TableName("animals")]
    public void UpdateAnimal(Animal animal)
        => Database.ExecuteSqlInterpolated($@" SELECT update_animal(
            {animal.Id}, {animal.TagNumber}, {animal.Type}, {animal.GroupId}, {animal.BirthDate}, {animal.Status})");

    [TableName("barren")]
    public void RemoveAnimalBarren(Guid animalId)
        => Database.ExecuteSqlInterpolated($@"SELECT remove_animal_from_barren({animalId})");

    [TableName("animals")]
    public IQueryable<ActiveAnimalDAL> GetActiveAnimals(Guid organizationId)
        => ActiveAnimals.FromSqlRaw(@"SELECT * FROM get_active_animals({0})", organizationId);

    [TableName("animals")]
    public IQueryable<AnimalDetailDAL> GetAnimalDetails(Guid animalId)
        => AnimalDetails.FromSqlRaw("SELECT * FROM get_animal_details({0})", animalId);

    [TableName("animals")]
    public IQueryable<AnimalDetail2DAL> GetAnimalDetails2(Guid animalId)
    => AnimalDetails2.FromSqlRaw("SELECT * FROM public.get_animal_detail2({0})", animalId);
    [TableName("animals")]
    public IQueryable<AnimalDetail2DAL> GetAnimalDetails4(Guid animalId)
    => AnimalDetails2.FromSqlRaw("SELECT * FROM public.get_animal_details4({0})", animalId);

    [TableName("daily_actions")]
    public IQueryable<DailyActionAnimalCardDTO> GetAnimalAction(Guid animalId)
        => AnimalActions.FromSqlRaw("SELECT * FROM get_animal_daily_actions({0})", animalId);

    [TableName("weights")]
    public IQueryable<AnimalWeightDAL> GetAnimalWeights(Guid animalId)
        => AnimalWeights.FromSqlRaw("SELECT * FROM get_animal_weights({0})", animalId);

    [TableName("calvings")]
    public IQueryable<AnimalReproductionDAL> GetAnimalCalvings(Guid animalId)
        => AnimalReproductionsDAL.FromSqlRaw("SELECT * FROM get_animal_calvings({0})", animalId);

    [TableName("calvings")]
public async Task<List<AnimalReproductionDAL>> GetAnimalChildrenFromAnimalsAsync(Guid animalId)
    {
        const string sql = "SELECT * FROM get_animal_children_from_animals(@mother_id)";
        var result = new List<AnimalReproductionDAL>();

        await using var conn = new NpgsqlConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add("@mother_id", NpgsqlDbType.Uuid).Value = animalId;

        await using var reader = await cmd.ExecuteReaderAsync();

        var idOrd             = reader.GetOrdinal("id");
        var cowIdOrd          = reader.GetOrdinal("cow_id");
        var calvingDateOrd    = reader.GetOrdinal("calving_date");
        var complicationOrd   = reader.GetOrdinal("complication");
        var calvingTypeOrd    = reader.GetOrdinal("calving_type");
        var veterinarOrd      = reader.GetOrdinal("veterinar");
        var treatmentsOrd     = reader.GetOrdinal("treatments");
        var pathologyOrd      = reader.GetOrdinal("pathology");
        var calfIdOrd         = reader.GetOrdinal("calf_id");
        var calfTagNumberOrd  = reader.GetOrdinal("calf_tag_number");
        var inseminationIdOrd = reader.GetOrdinal("insemination_id");

        while (await reader.ReadAsync())
        {
            var item = new AnimalReproductionDAL
            {
                Id = reader.IsDBNull(idOrd)
                    ? (Guid?)null
                    : reader.GetGuid(idOrd),

                CowId = reader.GetGuid(cowIdOrd),

                // calving_date (date) → DateOnly
                CalvingDate = reader.IsDBNull(calvingDateOrd)
                    ? default
                    : DateOnly.FromDateTime(reader.GetDateTime(calvingDateOrd)),

                Complication = reader.IsDBNull(complicationOrd)
                    ? null
                    : reader.GetString(complicationOrd),

                CalvingType = reader.IsDBNull(calvingTypeOrd)
                    ? null
                    : reader.GetString(calvingTypeOrd),

                Veterinarian = reader.IsDBNull(veterinarOrd)
                    ? null
                    : reader.GetString(veterinarOrd),

                Treatments = reader.IsDBNull(treatmentsOrd)
                    ? null
                    : reader.GetString(treatmentsOrd),

                Pathology = reader.IsDBNull(pathologyOrd)
                    ? null
                    : reader.GetString(pathologyOrd),

                CalfId = reader.IsDBNull(calfIdOrd)
                    ? (Guid?)null
                    : reader.GetGuid(calfIdOrd),

                CalfTagNumber = reader.IsDBNull(calfTagNumberOrd)
                    ? null
                    : reader.GetString(calfTagNumberOrd),

                InseminationId = reader.IsDBNull(inseminationIdOrd)
                    ? (Guid?)null
                    : reader.GetGuid(inseminationIdOrd)
            };

            result.Add(item);
        }

        return result;
    }

    [TableName("insemination")]
    public IQueryable<AnimalInseminationDAL> GetAnimalInseminations(Guid animalId)
        => AnimalInseminations.FromSqlRaw("SELECT * FROM get_cow_inseminations({0})", animalId);

    [TableName("pregnancy")]
    public IQueryable<AnimalPregnancyDAL> GetAnimalPregnancy(Guid animalId)
        => AnimalPregnancies.FromSqlRaw("SELECT * FROM get_cow_pregnancies({0})", animalId);

    [TableName("research")]
    public IQueryable<AnimalResearchDAL> GetAnimalResearch(Guid animalId)
        => AnimalResearches.FromSqlRaw("SELECT * FROM get_animal_researches({0})", animalId);

    [TableName("components")]
    public List<ComponentDTO> GetComponentsByOrganization(Guid organizationId)
        => Database.SqlQuery<ComponentDTO>($"SELECT * FROM get_organization_components({organizationId})").ToList();

    [TableName("components")]
    public ComponentDTO GetComponentsById(Guid Id)
        => Components.Where(c => c.Id == Id)
            .Select(c => new ComponentDTO
            {
                Id = c.Id,
                Name = c.Name,
                Cost = c.Cost,
                SV = c.SV,
                SP = c.SP,
                CEP = c.CEP,
                NDK = c.NDK
            }).FirstOrDefault();


    [TableName("rations")]
    public Guid CreateRationWithComponents(CreateRationRequestDTO dto)
    {
        var componentsJson = JsonSerializer.Serialize(
             dto.Components.Select(c => new RationCreateComponentJson
             {
                 ComponentId = c.ComponentId,
                 Kg = c.Kg,
                 Cost = c.Cost
             }));

        using var command = CreateCommand(
            @"SELECT create_ration_with_components(
            @organizationId,
            @rationName,
            @description,
            @components::jsonb
        )",
            ("rationName", dto.RationName),
            ("description", dto.Description),
            ("organizationId", dto.OrganizationId),
            ("components", componentsJson)
        );

        return (Guid)command.ExecuteScalar();
    }

    [TableName("feeding_record")]
    public List<FeedingRecordDailyDTO> GetFeedingDailyRecords(Guid organizationId)
    {
        var result = new List<FeedingRecordDailyDTO>();

        using var command = CreateCommand(
            "SELECT * FROM get_group_feeding_last_30_days(@org_id)",
            ("org_id", organizationId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var dto = new FeedingRecordDailyDTO
            {
                EventDate = reader.GetDateTime(0),
                OrganizationId = reader.GetGuid(1),
                GroupId = reader.GetGuid(2),
                GroupName = reader.IsDBNull(3) ? null : reader.GetString(3),
                DailyFactKg = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                FeedingDetails = reader.IsDBNull(5)
                    ? new List<FallbackFeedingDetailDTO>()
                    : JsonSerializer.Deserialize<List<FallbackFeedingDetailDTO>>(reader.GetString(5),
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })
            };

            result.Add(dto);
        }

        return result;
    }

    [TableName("feeding_record")]
    public List<GroupFeedingRecordDTO> GetGroupFeedingStats(Guid organizationId, Guid groupId)
    {
        var result = new List<GroupFeedingRecordDTO>();

        using var command = CreateCommand(
            "SELECT * FROM get_group_feeding_stats_last_30_days(@p_organization_id, @p_group_id)",
            ("p_organization_id", organizationId),
            ("p_group_id", groupId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var dto = new GroupFeedingRecordDTO
            {
                EventDate = reader.GetDateTime(0),
                GroupId = reader.GetGuid(2),
                GroupName = reader.IsDBNull(3) ? null : reader.GetString(3),
                RationId = reader.GetGuid(4),
                RationName = reader.IsDBNull(5) ? null : reader.GetString(5),
                DailyFactKg = reader.IsDBNull(6) ? 0 : reader.GetDouble(6)
            };

            result.Add(dto);
        }

        return result;
    }

    [TableName("")]
    public static double ConvertToKg(double count, string measure)
    {
        switch (measure.ToLower())
        {
            case "кг":
            case "kg":
                return count;

            case "г":
            case "гр":
            case "g":
                return count / 1000.0;

            case "т":
            case "тонна":
            case "тонн":
            case "тн":
                return count * 1000.0;

            case "мг":
                return count / 1_000_000.0;

            default:
                throw new ArgumentException($"Неизвестная единица измерения: {measure}");
        }
    }

    [TableName("components")]
    public void DeleteComponent(Guid componentId)
    {
        using var command = CreateCommand(
            "SELECT delete_component(@componentId)",
            ("componentId", componentId)
        );

        command.ExecuteNonQuery();
    }


    [TableName("components")]
    public Guid CreateComponent(CreateComponentDTO dto)
    {
        using var command = CreateCommand(
            @"SELECT create_component(
            @organizationId,
            @name,
            @cost,
            @sv,
            @sp,
            @cep,
            @ndk
        )",
            ("organizationId", dto.OrganizationId),
            ("name", dto.Name),
            ("cost", dto.Cost),
            ("sv", dto.SV),
            ("sp", dto.SP),
            ("cep", dto.CEP),
            ("ndk", dto.NDK)
        );

        return (Guid)command.ExecuteScalar();
    }


    [TableName("components")]
    public void UpdateComponent(UpdateComponentDTO dto)
    {
        using var command = CreateCommand(
            @"SELECT update_component(
            @componentId,
            @name,
            @cost,
            @sv,
            @sp,
            @cep,
            @ndk
        )",
            ("componentId", dto.Id),
            ("name", dto.Name),
            ("cost", dto.Cost),
            ("sv", dto.SV),
            ("sp", dto.SP),
            ("cep", dto.CEP),
            ("ndk", dto.NDK)
        );

        command.ExecuteNonQuery();
    }


    [TableName("rations")]
    public void CreateRationToGroup(Guid groupId, Guid rationId, string accounting)
    {
        using var command = CreateCommand(
            @"SELECT public.create_group_ration(
            @groupId,
            @rationId,
            @accounting
        )",
            ("groupId", groupId),
            ("rationId", rationId),
            ("accounting", accounting)
        );

        command.ExecuteNonQuery();
    }

    [TableName("rations")]
    public Guid AssignRationToGroup(
        Guid organizationId,
        Guid groupId,
        Guid rationId,
        double morningFeeding,
        double dayFeeding,
        double nightFeeding)
    {
        using var command = CreateCommand(
            "SELECT assign_ration_to_group(:p_group_id, :p_ration_id, :p_organization_id, :p_morning_feeding, :p_day_feeding, :p_night_feeding)",
            ("p_organization_id", organizationId),
            ("p_group_id", groupId),
            ("p_ration_id", rationId),
            ("p_morning_feeding", morningFeeding),
            ("p_day_feeding", dayFeeding),
            ("p_night_feeding", nightFeeding)
        );

        var result = command.ExecuteScalar();
        return (Guid)result!;
    }


    [TableName("groups")]
    public List<GroupWithStatsDTO> GetGroupsWithStats(Guid organizationId)
    {
        var result = new List<GroupWithStatsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_groups_with_stats2(@orgId)",
            ("orgId", organizationId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupWithStatsDTO
            {
                GroupId = reader.GetGuid(0),
                GroupName = reader.IsDBNull(1) ? null : reader.GetString(1),
                ActiveAnimalsCount = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                MorningFeeding = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                DayFeeding = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                NightFeeding = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                RationId = reader.IsDBNull(6) ? null : reader.GetGuid(6),
                RationName = reader.IsDBNull(7) ? null : reader.GetString(7),
                RationCostPerHead = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                TotalRationCost = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                SvPerHead = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                SpPerHead = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                CepPerHead = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                NdkPerHead = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                TotalSv = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                TotalSp = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                TotalCep = reader.IsDBNull(16) ? null : reader.GetDouble(16),
                TotalNdk = reader.IsDBNull(17) ? null : reader.GetInt32(17)
            });
        }

        return result;
    }



    [TableName("rations")]
    public RationSummaryDTO? GetRationSummaryEnhanced(Guid rationId)
    {
        using var command = CreateCommand(
            "SELECT * FROM public.get_ration_summary_enhanced(@rationId)",
            ("rationId", rationId)
        );

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        return new RationSummaryDTO
        {
            RationId = reader.GetGuid(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            OrganizationId = reader.GetGuid(3),
            CreatedAt = reader.GetDateTime(4),
            TotalDryMatter = reader.IsDBNull(5) ? null : reader.GetDouble(5),
            TotalNEMaintenance = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            TotalNEGain = reader.IsDBNull(7) ? null : reader.GetDouble(7),
            TotalCrudeProtein = reader.IsDBNull(8) ? null : reader.GetDouble(8),
            TotalDegradableProtein = reader.IsDBNull(9) ? null : reader.GetDouble(9),
            TotalCrudeFat = reader.IsDBNull(10) ? null : reader.GetDouble(10),
            TotalByproduct = reader.IsDBNull(11) ? null : reader.GetDouble(11),
            TotalRoughage = reader.IsDBNull(12) ? null : reader.GetDouble(12),
            TotalNDF = reader.IsDBNull(13) ? null : reader.GetDouble(13),
            TotalForageNDF = reader.IsDBNull(14) ? null : reader.GetDouble(14),
            TotalStarch = reader.IsDBNull(15) ? null : reader.GetDouble(15),
            TotalCalcium = reader.IsDBNull(16) ? null : reader.GetDouble(16),
            TotalPhosphorus = reader.IsDBNull(17) ? null : reader.GetDouble(17),
            TotalSalt = reader.IsDBNull(18) ? null : reader.GetDouble(18),
            TotalPotassium = reader.IsDBNull(19) ? null : reader.GetDouble(19),
            TotalSulfur = reader.IsDBNull(20) ? null : reader.GetDouble(20),
            TotalCost = reader.IsDBNull(21) ? null : reader.GetDouble(21),
            ComponentsCount = reader.GetDouble(22)
        };
    }

    [TableName("rations")]
    public List<RationGroupedDTO> GetRationsGroupedWithComponentsByOrganization(Guid organizationId)
    {
        var flatList = new List<RationWithComponentsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_rations_with_components(@organizationId)",
            ("organizationId", organizationId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var dto = new RationWithComponentsDTO
            {
                RationId = reader.GetGuid(0),
                RationName = reader.GetString(1),
                RationDescription = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = reader.GetDateTime(3),

                ComponentId = reader.GetGuid(4),
                ComponentName = reader.GetString(5),
                Kg = reader.GetDouble(6),
                Cost = reader.IsDBNull(7) ? null : reader.GetDouble(7),

                SV = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                SP = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                CEP = reader.IsDBNull(10) ? null : reader.GetFloat(10),
                NDK = reader.IsDBNull(11) ? null : reader.GetInt32(11)
            };

            flatList.Add(dto);
        }

        // Группировка по рациону
        var grouped = flatList
            .GroupBy(x => new { x.RationId, x.RationName, x.RationDescription, x.CreatedAt })
            .Select(group => new RationGroupedDTO
            {
                RationId = group.Key.RationId,
                RationName = group.Key.RationName,
                RationDescription = group.Key.RationDescription,
                CreatedAt = group.Key.CreatedAt,
                Components = group.Select(comp => new RationComponentDTO
                {
                    ComponentId = comp.ComponentId,
                    ComponentName = comp.ComponentName,
                    Kg = comp.Kg,
                    Cost = comp.Cost,
                    SV = comp.SV,
                    SP = comp.SP,
                    CEP = comp.CEP,
                    NDK = comp.NDK
                }).ToList(),
                TotalCost = group.Sum(c => (c.Cost ?? 0) * c.Kg)
            })
            .ToList();

        return grouped;
    }

    [TableName("feedings")]
    public List<FlatFeedingCostRecord> GetGroupFeedingStatsCost(Guid organizationId, Guid groupId)
    {
        var result = new List<FlatFeedingCostRecord>();

        using var command = CreateCommand(
            "SELECT * FROM get_group_ration_costs_last_30_days(@p_organization_id, @p_group_id)",
            ("p_organization_id", organizationId),
            ("p_group_id", groupId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var record = new FlatFeedingCostRecord
            {
                EventDate = reader.GetDateTime(0),
                OrganizationId = reader.GetGuid(1),
                GroupId = reader.GetGuid(2),
                GroupName = reader.GetString(3),
                GroupRationId = reader.GetGuid(4),
                GroupRationName = reader.GetString(5),
                RationCost = reader.GetDouble(6),
                TotalRationCost = reader.GetDouble(7)
            };

            result.Add(record);
        }

        return result;
    }

    [TableName("feedings")]
    public List<GroupFeedingRecordYearlyCostRawDTO> GetGroupFeedingStatsCostYearly(Guid organizationId, Guid groupId)
    {
        var result = new List<GroupFeedingRecordYearlyCostRawDTO>();

        using var command = CreateCommand(
            "SELECT * FROM get_group_ration_costs_last_year(@p_organization_id, @p_group_id)",
            ("p_organization_id", organizationId),
            ("p_group_id", groupId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupFeedingRecordYearlyCostRawDTO
            {
                EventDate = reader.GetDateTime(0),
                OrganizationId = reader.GetGuid(1),
                GroupId = reader.GetGuid(2),
                GroupName = reader.GetString(3),
                GroupRationId = reader.GetGuid(4),
                GroupRationName = reader.GetString(5),
                RationCost = reader.GetDouble(6),
                TotalRationCost = reader.GetDouble(7),
                MonthYear = reader.GetString(8)
            });
        }

        return result;
    }

    [TableName("feedings")]
    public List<GroupFeedingNutritionRawDTO> GetGroupFeedingStatsNutrition(Guid organizationId, Guid groupId)
    {
        var result = new List<GroupFeedingNutritionRawDTO>();

        using var command = CreateCommand(
            "SELECT * FROM get_group_nutrition_last_30_days(@p_organization_id, @p_group_id)",
            ("p_organization_id", organizationId),
            ("p_group_id", groupId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupFeedingNutritionRawDTO
            {
                EventDate = reader.GetDateTime(0),
                OrganizationId = reader.GetGuid(1),
                GroupId = reader.GetGuid(2),
                GroupName = reader.GetString(3),
                GroupRationId = reader.GetGuid(4),
                GroupRationName = reader.GetString(5),
                TotalSv = reader.GetDouble(6),
                TotalSp = reader.GetDouble(7),
                TotalCep = reader.GetDouble(8),
                TotalNdk = reader.GetDouble(9)
            });
        }

        return result;
    }

    [TableName("groups")]
    public List<GroupFeedingStatsDTO> GetOrganizationGroupStats(Guid organizationId)
    {
        var result = new List<GroupFeedingStatsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_organization_groups_with_stats2(@p_org_id)",
            ("p_org_id", organizationId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupFeedingStatsDTO
            {
                GroupId = reader.GetGuid(0),
                GroupName = reader.GetString(1),
                AnimalCount = reader.GetInt32(2),
                GroupRationId = reader.IsDBNull(3) ? (Guid?)null : reader.GetGuid(3),
                GroupRationName = reader.IsDBNull(4) ? null : reader.GetString(4),
                MorningFeeding = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                DayFeeding = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                NightFeeding = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                TotalKg = reader.IsDBNull(8) ? 0 : reader.GetDouble(8),
                TotalKgForGroup = reader.IsDBNull(9) ? 0 : reader.GetDouble(9),
                TotalCost = reader.IsDBNull(10) ? 0 : reader.GetDouble(10)
            });
        }

        return result;
    }


    [TableName("group_rations")]
    public async Task FillDailyFeedingRecords(Guid organizationId, DateOnly date)
    {
        using var command = CreateCommand("SELECT fill_daily_feeding_records(@p_org_id, @p_date)",
            ("p_org_id", organizationId),
            ("p_date", date));

        command.ExecuteNonQuery();
    }


    [TableName("rations_components")]
    public List<GroupFeedingDailyDTO> GetGroupFeedingDailyStats(Guid organizationId, DateOnly date)
    {
        var result = new List<GroupFeedingDailyDTO>();

        using var command = CreateCommand(
            "SELECT * FROM get_feeding_info_by_date(@org_id, @target_date)",
            ("org_id", organizationId),
            ("target_date", date)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupFeedingDailyDTO
            {
                GroupId = reader.GetGuid(0),
                GroupName = reader.GetString(1),
                AnimalCount = reader.GetInt32(2),
                TotalFactKg = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                FeedingDetails = reader.IsDBNull(4)
                    ? null
                    : JsonSerializer.Deserialize<List<FalbackFeedingDetailDTO>>(reader.GetString(4)),
                DataSource = reader.GetString(5)
            });
        }

        return result;
    }

    [TableName("feeding_record")]
    public Guid RecordFeeding(RecordFeedingDTO dto)
    {
        using var command = CreateCommand(
            "SELECT record_feeding(@p_event_date, @p_organization_id, @p_group_id, @p_animal_count, @p_group_ration_id, " +
            "@p_total_kg, @p_total_kg_for_group, @p_fact_kg, @p_feeding_time, @p_feeding_coefficient, @p_mark, @p_feeding_mark)",
            ("p_event_date", dto.EventDate),
            ("p_organization_id", dto.OrganizationId),
            ("p_group_id", dto.GroupId),
            ("p_animal_count", dto.AnimalCount),
            ("p_group_ration_id", dto.GroupRationId),
            ("p_total_kg", dto.TotalKg),
            ("p_total_kg_for_group", dto.TotalKgForGroup),
            ("p_fact_kg", dto.FactKg),
            ("p_feeding_time", dto.FeedingTime),
            ("p_feeding_coefficient", dto.FeedingCoefficient),
            ("p_mark", dto.Mark),
            ("p_feeding_mark", (int) (dto.FeedingMark * 100))
        );

        var result = command.ExecuteScalar();
        return (Guid)result!;
    }

    [TableName("rations")]
    public List<GroupWithRationDTO> GetGroupsWithRations(Guid organizationId)
    {
        var result = new List<GroupWithRationDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_groups_with_stats(@orgId)",
            ("orgId", organizationId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupWithRationDTO
            {
                GroupId = reader.GetGuid(0),
                GroupName = reader.GetString(1),
                GroupDescription = reader.IsDBNull(2) ? null : reader.GetString(2),
                GroupLocation = reader.IsDBNull(3) ? null : reader.GetString(3),
                ActiveAnimalsCount = reader.GetInt64(4),
                RationId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                RationName = reader.IsDBNull(6) ? null : reader.GetString(6),
                RationDescription = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            });
        }

        return result;
    }



    [TableName("rations")]
    public void UpdateRationFull(Guid rationId, string name, string? description, Guid? organizationId, List<RationUpdateComponentJson>? components)
    {
        var componentsJson = components is not null
            ? JsonSerializer.Serialize(components)
            : null;

        using var command = CreateCommand(
            "SELECT public.update_ration_full(@rationId, @name, @description, @organizationId, @components::jsonb)",
            ("rationId", rationId),
            ("name", name),
            ("description", (object?)description ?? DBNull.Value),
            ("organizationId", (object?)organizationId ?? DBNull.Value),
            ("components", (object?)componentsJson ?? DBNull.Value)
        );

        command.ExecuteNonQuery();
    }

    [TableName("")]
    private NpgsqlCommand CreateCommand(string sql, params (string name, object value)[] parameters)
    {
        var connection = Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new Npgsql.NpgsqlParameter(name, value ?? DBNull.Value));
        }

        return (NpgsqlCommand)command;
    }

    [TableName("")]
    private NpgsqlCommand CreateCommand(
    string sql,
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    params (string name, object value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new Npgsql.NpgsqlParameter(name, value ?? DBNull.Value));
        }

        return (NpgsqlCommand)command;
    }

    [TableName("users")]
    public void CreateUser(Guid? organizationId = default, string? login = default, string? password = default,
        string? name = default, string? phoneNumber = default, Guid? role = default, string? tgId = default)
        => Database.ExecuteSqlInterpolated($@"SELECT create_user({organizationId}, {login}, {password}, {name}, {phoneNumber}, {role}, {tgId})");

    [TableName("users")]
    public void UpdateUser(Guid userId, Guid? organizationId = default, string? login = default, string? password = default,
        Guid? role = default, string? name = default, string? phoneNumber = default)
        => Database.ExecuteSqlInterpolated($@"SELECT update_user({userId}, {organizationId}, {login}, {password}, {role}, {name}, {phoneNumber})");

    [TableName("organizations")]
    public Guid CreateOrg(string name, string inn, string ogrn)
        => Database.SqlQuery<Guid>($"SELECT create_organization({name}, {null}, {inn}, {ogrn}) AS \"Value\"").SingleOrDefault();


    [TableName("animals")]
    public string? UpdateAnimalCard(UpdateAnimalCardDTO dto)
    {
        var sql = """
            select update_animal_with_idfields(
                @p_animal_id::uuid,
                @p_organization_id::uuid,
                @p_tag_number::text,
                @p_type::text,
                @p_breed::text,
                @p_mother_id::uuid,
                @p_father_ids::jsonb,
                @p_status::text,
                @p_group_id::uuid,
                @p_origin::text,
                @p_origin_location::text,
                @p_birth_date::date,
                @p_date_of_receipt::date,
                @p_date_of_disposal::date,
                @p_reason_of_disposal::text,
                @p_identification_data::jsonb
            );
        """;

        var fatherIdsJson = dto.FatherIds == null
            ? null
            : JsonSerializer.Serialize(dto.FatherIds);

        var identificationJson = dto.IdentificationData == null
            ? null
            : JsonSerializer.Serialize(dto.IdentificationData);

        object[] parameters =
        [
            new NpgsqlParameter("p_animal_id", dto.Id),

            new NpgsqlParameter("p_organization_id", (object?)dto.OrgId ?? DBNull.Value),

            new NpgsqlParameter("p_tag_number", (object?)dto.TagNumber ?? DBNull.Value),
            new NpgsqlParameter("p_type", (object?)dto.Type ?? DBNull.Value),
            new NpgsqlParameter("p_breed", (object?)dto.Breed ?? DBNull.Value),

            new NpgsqlParameter("p_mother_id", (object?)dto.MotherId ?? DBNull.Value),

            new NpgsqlParameter("p_father_ids", (object?)fatherIdsJson ?? DBNull.Value),

            new NpgsqlParameter("p_status", (object?)dto.Status ?? DBNull.Value),

            new NpgsqlParameter("p_group_id", (object?)dto.GroupId ?? DBNull.Value),

            new NpgsqlParameter("p_origin", (object?)dto.Origin ?? DBNull.Value),
            new NpgsqlParameter("p_origin_location", (object?)dto.OriginLocation ?? DBNull.Value),

            new NpgsqlParameter("p_birth_date", dto.BirthDate.HasValue
                ? dto.BirthDate.Value.Date
                : (object)DBNull.Value),

            new NpgsqlParameter("p_date_of_receipt", dto.DateOfReceipt.HasValue
                ? dto.DateOfReceipt.Value.Date
                : (object)DBNull.Value),

            new NpgsqlParameter("p_date_of_disposal", dto.DateOfDisposal.HasValue
                ? dto.DateOfDisposal.Value.Date
                : (object)DBNull.Value),

            new NpgsqlParameter("p_reason_of_disposal", (object?)dto.ReasonOfDisposal ?? DBNull.Value),

            new NpgsqlParameter("p_identification_data", (object?)identificationJson ?? DBNull.Value)
        ];

        try
        {
            Database.ExecuteSqlRaw(sql, parameters);
            return null;
        }
        catch (Exception ex)
        {
            // Ловим спец. код, который ты выставляешь в функции (22023)
            if (ex is not NpgsqlException { SqlState: "22023" })
                throw new Exception($"Error updating animal card: {ex.Message}", ex);

            const string error = "Недопустимое значение в поле идентификации животного";
            return error;
        }
    }

    public List<AnimalReproductionDAL> GetAnimalChildrenFromAnimals(Guid motherId)
    {
        const string sql = "SELECT * FROM get_animal_children_from_animals(@mother_id)";
        var result = new List<AnimalReproductionDAL>();

        var conn = Database.GetDbConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var p = cmd.CreateParameter();
        p.ParameterName = "mother_id";
        p.Value = motherId;
        cmd.Parameters.Add(p);

        var shouldClose = false;
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
            shouldClose = true;
        }

        try
        {
            using var reader = cmd.ExecuteReader();

            var idOrd             = reader.GetOrdinal("id");
            var cowIdOrd          = reader.GetOrdinal("cow_id");
            var calvingDateOrd    = reader.GetOrdinal("calving_date");
            var complicationOrd   = reader.GetOrdinal("complication");
            var calvingTypeOrd    = reader.GetOrdinal("calving_type");
            var veterinarOrd      = reader.GetOrdinal("veterinar");
            var treatmentsOrd     = reader.GetOrdinal("treatments");
            var pathologyOrd      = reader.GetOrdinal("pathology");
            var calfIdOrd         = reader.GetOrdinal("calf_id");
            var calfTagNumberOrd  = reader.GetOrdinal("calf_tag_number");
            var inseminationIdOrd = reader.GetOrdinal("insemination_id");

            while (reader.Read())
            {
                var item = new AnimalReproductionDAL
                {
                    Id = Guid.Empty,
                    CowId = reader.GetGuid(cowIdOrd),

                    // calving_date (date) → DateOnly
                    CalvingDate = reader.IsDBNull(calvingDateOrd)
                        ? default
                        : DateOnly.FromDateTime(reader.GetDateTime(calvingDateOrd)),

                    Complication = reader.IsDBNull(complicationOrd)
                        ? null
                        : reader.GetString(complicationOrd),

                    CalvingType = reader.IsDBNull(calvingTypeOrd)
                        ? null
                        : reader.GetString(calvingTypeOrd),

                    Veterinarian = reader.IsDBNull(veterinarOrd)
                        ? null
                        : reader.GetString(veterinarOrd),

                    Treatments = reader.IsDBNull(treatmentsOrd)
                        ? null
                        : reader.GetString(treatmentsOrd),

                    Pathology = reader.IsDBNull(pathologyOrd)
                        ? null
                        : reader.GetString(pathologyOrd),

                    CalfId = reader.IsDBNull(calfIdOrd)
                        ? (Guid?)null
                        : reader.GetGuid(calfIdOrd),

                    CalfTagNumber = reader.IsDBNull(calfTagNumberOrd)
                        ? null
                        : reader.GetString(calfTagNumberOrd),

                    InseminationId = reader.IsDBNull(inseminationIdOrd)
                        ? (Guid?)null
                        : reader.GetGuid(inseminationIdOrd)
                };

                result.Add(item);
            }
        }
        finally
        {
            if (shouldClose)
                conn.Close();
        }

        return result;
    }

    [TableName("medicine")]
    public Guid CreateMedicine(
        Guid organizationId,
        string name,
        string? substance = null,
        string? drugEliminationPeriod = null,
        string? shelfLife = null,
        string? factory = null)
    {
        using var command = CreateCommand(
            @"SELECT public.create_medicine(
            @p_organization_id,
            @p_name,
            @p_substance,
            @p_drug_elimination_period,
            @p_shelf_life,
            @p_factory
        )",
            ("p_organization_id", organizationId),
            ("p_name", name),
            ("p_substance", substance),
            ("p_drug_elimination_period", drugEliminationPeriod),
            ("p_shelf_life", shelfLife),
            ("p_factory", factory)
        );

        return (Guid)command.ExecuteScalar()!;
    }

    [TableName("medicine")]
    public bool DeleteMedicine(Guid id)
    {
        using var command = CreateCommand(
            "SELECT public.delete_medicine(@p_id)",
            ("p_id", id)
        );

        return (bool)command.ExecuteScalar()!;
    }

    [TableName("medicine")]
    public bool UpdateMedicine(
        Guid id,
        string? name = null,
        string? substance = null,
        string? drugEliminationPeriod = null,
        string? shelfLife = null,
        string? factory = null)
    {
        using var command = CreateCommand(
            @"SELECT public.update_medicine(
            @p_id,
            @p_name,
            @p_substance,
            @p_drug_elimination_period,
            @p_shelf_life,
            @p_factory
        )",
            ("p_id", id),
            ("p_name", name),
            ("p_substance", substance),
            ("p_drug_elimination_period", drugEliminationPeriod),
            ("p_shelf_life", shelfLife),
            ("p_factory", factory)
        );

        return (bool)command.ExecuteScalar()!;
    }

    [TableName("medicine")]
    public List<Medicine> GetMedicinesByOrganization(Guid organizationId)
    {
        var result = new List<Medicine>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_medicines_by_organization(@p_organization_id)",
            ("p_organization_id", organizationId)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Medicine
            {
                Id = reader.GetGuid(0),
                OrganizationId = reader.GetGuid(1),
                Name = reader.GetString(2),
                Substance = reader.IsDBNull(3) ? null : reader.GetString(3),
                DrugEliminatior = reader.IsDBNull(4) ? null : reader.GetString(4),
                ShelfLife = reader.IsDBNull(5) ? null : reader.GetString(5),
                Factory = reader.IsDBNull(6) ? null : reader.GetString(6),
            });
        }

        return result;
    }

    [TableName("daily_actions")]
    public void InsertDailyActionWithMedicine(
           Guid id,
           Guid animalId,
           string actionType,
           string actionSubtype,
           DateOnly? date = default,
           string? performedBy = default,
           string? result = default,
           string? medicine = default,
           string? dose = default,
           string? notes = default,
           DateOnly? nextActionDate = default,
           Guid? oldGroupId = default,
           Guid? newGroupId = default,
           string? oldType = default,
           string? newType = default,
           string? drugEliminationPeriod = default)
    {
        using var command = CreateCommand(
            @"SELECT public.insert_daily_action_with_medicine(
                    @p_id,
                    @p_animal_id,
                    @p_action_type,
                    @p_action_subtype,
                    @p_date,
                    @p_performed_by,
                    @p_result,
                    @p_medicine,
                    @p_dose,
                    @p_notes,
                    @p_next_action_date,
                    @p_old_group_id,
                    @p_new_group_id,
                    @p_old_type,
                    @p_new_type,
                    @p_drug_elimination_period
                )",
            ("p_id", id),
            ("p_animal_id", animalId),
            ("p_action_type", actionType),
            ("p_action_subtype", actionSubtype),
            ("p_date", (object?)date ?? DBNull.Value),
            ("p_performed_by", (object?)performedBy ?? DBNull.Value),
            ("p_result", (object?)result ?? DBNull.Value),
            ("p_medicine", (object?)medicine ?? DBNull.Value),
            ("p_dose", (object?)dose ?? DBNull.Value),
            ("p_notes", (object?)notes ?? DBNull.Value),
            ("p_next_action_date", (object?)nextActionDate ?? DBNull.Value),
            ("p_old_group_id", (object?)oldGroupId ?? DBNull.Value),
            ("p_new_group_id", (object?)newGroupId ?? DBNull.Value),
            ("p_old_type", (object?)oldType ?? DBNull.Value),
            ("p_new_type", (object?)newType ?? DBNull.Value),
            ("p_drug_elimination_period", (object?)drugEliminationPeriod ?? DBNull.Value)
        );

        command.ExecuteNonQuery();
    }

    [TableName("calvings")]
    public List<CalvingWithStatisticsDTO> GetCalvingsWithStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<CalvingWithStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_calvings_with_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new CalvingWithStatisticsDTO
            {
                CalvingId = reader.GetGuid(0),
                CowId = reader.GetGuid(1),
                CalfId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
                CalvingDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
                CalvingType = reader.GetString(4),

                TotalCalvings = reader.GetInt64(5),
                LiveCount = reader.GetInt64(6),
                AbortCount = reader.GetInt64(7),
                StillbornCount = reader.GetInt64(8),

                LiveRatio = reader.IsDBNull(9) ? null : reader.GetFieldValue<decimal>(9),
                AbortRatio = reader.IsDBNull(10) ? null : reader.GetFieldValue<decimal>(10),
                StillbornRatio = reader.IsDBNull(11) ? null : reader.GetFieldValue<decimal>(11),
            });
        }

        return result;
    }

    [TableName("weights")]
    public List<BirthWeightStatisticsDTO> GetBirthWeightStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<BirthWeightStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_birth_weight_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new BirthWeightStatisticsDTO
            {
                Sex = reader.GetString(0),
                AnimalCount = reader.GetInt64(1),
                AvgWeight = reader.IsDBNull(2) ? null : reader.GetFieldValue<decimal>(2),
                MinWeight = reader.IsDBNull(3) ? null : reader.GetFieldValue<decimal>(3),
                MaxWeight = reader.IsDBNull(4) ? null : reader.GetFieldValue<decimal>(4),
                TotalAnimals = reader.GetInt64(5),
                Ratio = reader.IsDBNull(6) ? null : reader.GetFieldValue<decimal>(6)
            });
        }

        return result;
    }

    [TableName("weights")]
    public List<DailyWeightGainStatisticsDTO> GetDailyWeightGainStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<DailyWeightGainStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_daily_weight_gain_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new DailyWeightGainStatisticsDTO
            {
                AnimalId = reader.GetGuid(0),
                WeighDate = DateOnly.FromDateTime(reader.GetDateTime(1)),
                Weight = reader.IsDBNull(2) ? null : reader.GetDouble(2),
                PrevWeight = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                DaysDiff = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                DailyGain = reader.IsDBNull(5) ? null : reader.GetFieldValue<decimal>(5),

                AvgDailyGain = reader.IsDBNull(6) ? null : reader.GetFieldValue<decimal>(6),
                MinDailyGain = reader.IsDBNull(7) ? null : reader.GetFieldValue<decimal>(7),
                MaxDailyGain = reader.IsDBNull(8) ? null : reader.GetFieldValue<decimal>(8),
            });
        }

        return result;
    }

    [TableName("weights")]
    public List<WeightAt12MonthsStatisticsDTO> GetWeightAt12MonthsStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<WeightAt12MonthsStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_weight_at_12_months_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new WeightAt12MonthsStatisticsDTO
            {
                AnimalId = reader.GetGuid(0),
                BirthDate = DateOnly.FromDateTime(reader.GetDateTime(1)),
                TargetDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
                WeighDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
                Weight = reader.IsDBNull(4) ? null : reader.GetDouble(4),

                AnimalCount = reader.GetInt64(5),
                AvgWeight = reader.IsDBNull(6) ? null : reader.GetFieldValue<decimal>(6),
                MinWeight = reader.IsDBNull(7) ? null : reader.GetFieldValue<decimal>(7),
                MaxWeight = reader.IsDBNull(8) ? null : reader.GetFieldValue<decimal>(8),
            });
        }

        return result;
    }

    [TableName("pregnancy")]
    public List<PregnancyStatisticsDTO> GetPregnancyStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<PregnancyStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_pregnancy_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PregnancyStatisticsDTO
            {
                PregnancyId = reader.GetGuid(0),
                CowId = reader.GetGuid(1),
                PregnancyDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
                Status = reader.GetString(3),
                ExpectedCalvingDate = reader.IsDBNull(4) ? null : DateOnly.FromDateTime(reader.GetDateTime(4)),

                TotalRecords = reader.GetInt64(5),
                StatusCount = reader.GetInt64(6),
                StatusRatio = reader.IsDBNull(7) ? null : reader.GetFieldValue<decimal>(7)
            });
        }

        return result;
    }

    [TableName("daily_actions")]
    public List<VaccinationStatisticsDTO> GetVaccinationStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<VaccinationStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_vaccination_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new VaccinationStatisticsDTO
            {
                ActionId = reader.GetGuid(0),
                AnimalId = reader.GetGuid(1),
                ActionDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
                Medicine = reader.IsDBNull(3) ? null : reader.GetString(3),
                PerformedBy = reader.IsDBNull(4) ? null : reader.GetString(4),
                Notes = reader.IsDBNull(5) ? null : reader.GetString(5),

                TotalVaccinations = reader.GetInt64(6),
                MedicineCount = reader.GetInt64(7),
                MedicineRatio = reader.IsDBNull(8) ? null : reader.GetFieldValue<decimal>(8)
            });
        }

        return result;
    }

    [TableName("research")]
    public List<BloodTestStatisticsDTO> GetBloodTestStatistics(
        Guid organizationId,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var result = new List<BloodTestStatisticsDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_blood_test_statistics(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new BloodTestStatisticsDTO
            {
                ResearchId = reader.GetGuid(0),
                AnimalId = reader.GetGuid(1),
                ResearchName = reader.GetString(2),
                CollectionDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
                Result = reader.IsDBNull(4) ? null : reader.GetString(4),

                TotalTests = reader.GetInt64(5),
                PositiveCount = reader.GetInt64(6),
                NegativeCount = reader.GetInt64(7),
                PositiveRatio = reader.IsDBNull(8) ? null : reader.GetFieldValue<decimal>(8),
                NegativeRatio = reader.IsDBNull(9) ? null : reader.GetFieldValue<decimal>(9),
            });
        }

        return result;
    }

    [TableName("daily_actions")]
    public List<VaccinationMedicineDTO> GetVaccinationMedicines2(Guid organizationId, DateOnly dateFrom, DateOnly dateTo)
    {
        var result = new List<VaccinationMedicineDTO>();

        using var command = CreateCommand(
            "SELECT * FROM public.get_vaccination_medicines_2(@p_organization_id, @p_date_from, @p_date_to)",
            ("p_organization_id", organizationId),
            ("p_date_from", dateFrom),
            ("p_date_to", dateTo)
        );

        using var reader = command.ExecuteReader();

        var idOrd = reader.GetOrdinal("medicine_id");
        var nameOrd = reader.GetOrdinal("medicine_name");

        while (reader.Read())
        {
            result.Add(new VaccinationMedicineDTO
            {
                MedicineId = reader.GetGuid(idOrd),
                MedicineName = reader.IsDBNull(nameOrd) ? string.Empty : reader.GetString(nameOrd)
            });
        }

        return result;
    }

    public string[] GetPlaceOfOrigin(Guid organizationId)
    {
        var originLocations = Database
            .SqlQuery<string>($"SELECT origin_location FROM get_animal_origin_locations({organizationId})")
            .ToArray();

        return originLocations;
    }

    public string[] GetOrigins(Guid organizationId)
    {
        var originLocations = Database
            .SqlQuery<string>($"SELECT origin FROM get_animal_origins({organizationId})")
            .ToArray();

        return originLocations;
    }
}
