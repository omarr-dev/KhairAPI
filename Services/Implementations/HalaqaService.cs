using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Services.Implementations
{
    public class HalaqaService : IHalaqaService
    {
        private readonly AppDbContext _context;
        private readonly IQuranService _quranService;
        private readonly ITenantService _tenantService;

        public HalaqaService(AppDbContext context, IQuranService quranService, ITenantService tenantService)
        {
            _context = context;
            _quranService = quranService;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<HalaqaDto>> GetAllHalaqatAsync(int? teacherId = null, List<int>? supervisedHalaqaIds = null)
        {
            IQueryable<Halaqa> query = _context.Halaqat
                .AsNoTracking()
                .Include(h => h.StudentHalaqat)
                .Include(h => h.HalaqaTeachers)
                .AsSplitQuery();

            // Filter by teacher if specified
            if (teacherId.HasValue)
            {
                query = query.Where(h => h.HalaqaTeachers.Any(ht => ht.TeacherId == teacherId.Value));
            }
            
            // Filter by supervised halaqas if specified (for HalaqaSupervisors)
            if (supervisedHalaqaIds != null)
            {
                query = query.Where(h => supervisedHalaqaIds.Contains(h.Id));
            }

            var halaqat = await query.ToListAsync();

            return halaqat.Select(MapToDto);
        }

        public async Task<List<LookupDto>> GetHalaqatLookupAsync(int? teacherId = null, List<int>? supervisedHalaqaIds = null)
        {
            var query = _context.Halaqat
                .AsNoTracking()
                .Where(h => h.IsActive);

            if (teacherId.HasValue)
            {
                query = query.Where(h => h.HalaqaTeachers.Any(ht => ht.TeacherId == teacherId.Value));
            }

            if (supervisedHalaqaIds != null)
            {
                query = query.Where(h => supervisedHalaqaIds.Contains(h.Id));
            }

            return await query
                .OrderBy(h => h.Name)
                .Select(h => new LookupDto { Id = h.Id, Name = h.Name })
                .ToListAsync();
        }

        public async Task<PaginatedResponse<HalaqaHierarchyDto>> GetHalaqatHierarchyAsync(HalaqaHierarchyFilterDto filter, List<int>? supervisedHalaqaIds = null)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

            // Students are intentionally NOT loaded here; clients fetch them
            // on demand via GetHalaqaStudentsAsync to keep this payload small.
            var query = _context.Halaqat
                .AsNoTracking()
                .Include(h => h.HalaqaTeachers)
                    .ThenInclude(ht => ht.Teacher)
                .Where(h => h.IsActive);

            // Filter by supervised halaqas if specified (for HalaqaSupervisors)
            if (supervisedHalaqaIds != null)
            {
                query = query.Where(h => supervisedHalaqaIds.Contains(h.Id));
            }

            // Search by halaqa name OR any of its teachers' names
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(h =>
                    h.Name.Contains(search) ||
                    h.HalaqaTeachers.Any(ht => ht.Teacher.FullName.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var halaqat = await query
                .OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Single grouped query for active student counts per (halaqa, teacher),
            // scoped to just this page's halaqat.
            var pageHalaqaIds = halaqat.Select(h => h.Id).ToList();
            var counts = await _context.StudentHalaqat
                .AsNoTracking()
                .Where(sh => sh.IsActive && pageHalaqaIds.Contains(sh.HalaqaId))
                .GroupBy(sh => new { sh.HalaqaId, sh.TeacherId })
                .Select(g => new { g.Key.HalaqaId, g.Key.TeacherId, Count = g.Count() })
                .ToListAsync();

            var countByHalaqaTeacher = counts.ToDictionary(c => (c.HalaqaId, c.TeacherId), c => c.Count);
            var countByHalaqa = counts
                .GroupBy(c => c.HalaqaId)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Count));

            var items = halaqat.Select(h => new HalaqaHierarchyDto
            {
                Id = h.Id,
                Name = h.Name,
                Location = h.Location,
                TimeSlot = h.TimeSlot,
                ActiveDays = h.ActiveDays,
                IsActive = h.IsActive,
                StudentCount = countByHalaqa.GetValueOrDefault(h.Id),
                TeacherCount = h.HalaqaTeachers.Count,
                Teachers = h.HalaqaTeachers.Select(ht => new TeacherInHalaqaDto
                {
                    Id = ht.Teacher.Id,
                    FullName = ht.Teacher.FullName,
                    PhoneNumber = ht.Teacher.PhoneNumber,
                    StudentCount = countByHalaqaTeacher.GetValueOrDefault((h.Id, ht.TeacherId)),
                    Students = new List<StudentInHalaqaDto>()
                })
                .OrderBy(t => t.FullName)
                .ToList()
            }).ToList();

            return new PaginatedResponse<HalaqaHierarchyDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<StudentInHalaqaWithTeacherDto>> GetHalaqaStudentsAsync(int halaqaId)
        {
            var rows = await _context.StudentHalaqat
                .AsNoTracking()
                .Where(sh => sh.HalaqaId == halaqaId && sh.IsActive)
                .Select(sh => new
                {
                    sh.TeacherId,
                    sh.Student.Id,
                    sh.Student.FirstName,
                    sh.Student.LastName,
                    sh.Student.MemorizationDirection,
                    sh.Student.CurrentSurahNumber,
                    sh.Student.CurrentVerse,
                    sh.Student.JuzMemorized
                })
                .ToListAsync();

            return rows.Select(r => new StudentInHalaqaWithTeacherDto
                {
                    Id = r.Id,
                    TeacherId = r.TeacherId,
                    FullName = $"{r.FirstName} {r.LastName}",
                    MemorizationDirection = r.MemorizationDirection.ToString(),
                    CurrentSurahNumber = r.CurrentSurahNumber,
                    CurrentSurahName = _quranService.GetSurahByNumber(r.CurrentSurahNumber)?.Name,
                    CurrentVerse = r.CurrentVerse,
                    JuzMemorized = r.JuzMemorized
                })
                .OrderBy(s => s.FullName)
                .ToList();
        }

        public async Task<HalaqaDto?> GetHalaqaByIdAsync(int id)
        {
            var halaqa = await _context.Halaqat
                .AsNoTracking()
                .Include(h => h.StudentHalaqat)
                .Include(h => h.HalaqaTeachers)
                .AsSplitQuery()
                .OrderBy(h => h.Id)
                .FirstOrDefaultAsync(h => h.Id == id);

            return halaqa == null ? null : MapToDto(halaqa);
        }

        public async Task<HalaqaDto> CreateHalaqaAsync(CreateHalaqaDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var halaqa = new Halaqa
            {
                Name = dto.Name,
                Location = dto.Location,
                TimeSlot = dto.TimeSlot,
                ActiveDays = dto.ActiveDays,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.Halaqat.Add(halaqa);
            await _context.SaveChangesAsync();

            return new HalaqaDto
            {
                Id = halaqa.Id,
                Name = halaqa.Name,
                Location = halaqa.Location,
                TimeSlot = halaqa.TimeSlot,
                ActiveDays = halaqa.ActiveDays,
                IsActive = halaqa.IsActive,
                StudentCount = 0,
                TeacherCount = 0,
                CreatedAt = halaqa.CreatedAt
            };
        }

        public async Task<bool> UpdateHalaqaAsync(int id, UpdateHalaqaDto dto)
        {
            var halaqa = await _context.Halaqat.FindAsync(id);
            if (halaqa == null)
                return false;

            halaqa.Name = dto.Name;
            halaqa.Location = dto.Location;
            halaqa.TimeSlot = dto.TimeSlot;
            halaqa.ActiveDays = dto.ActiveDays;
            halaqa.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHalaqaAsync(int id)
        {
            var halaqa = await _context.Halaqat
                .Include(h => h.StudentHalaqat)
                .OrderBy(h => h.Id)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (halaqa == null)
                return false;

            if (halaqa.StudentHalaqat.Any(sh => sh.IsActive))
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.CannotDeleteHalaqaWithStudents);
            }

            _context.Halaqat.Remove(halaqa);
            await _context.SaveChangesAsync();
            return true;
        }

        private static HalaqaDto MapToDto(Halaqa halaqa)
        {
            return new HalaqaDto
            {
                Id = halaqa.Id,
                Name = halaqa.Name,
                Location = halaqa.Location,
                TimeSlot = halaqa.TimeSlot,
                ActiveDays = halaqa.ActiveDays,
                IsActive = halaqa.IsActive,
                StudentCount = halaqa.StudentHalaqat.Count(sh => sh.IsActive),
                TeacherCount = halaqa.HalaqaTeachers.Count(),
                CreatedAt = halaqa.CreatedAt
            };
        }
    }
}

