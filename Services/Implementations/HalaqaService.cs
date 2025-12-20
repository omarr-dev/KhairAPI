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

        public HalaqaService(AppDbContext context, IQuranService quranService)
        {
            _context = context;
            _quranService = quranService;
        }

        public async Task<IEnumerable<HalaqaDto>> GetAllHalaqatAsync(int? teacherId = null)
        {
            IQueryable<Halaqa> query = _context.Halaqat
                .Include(h => h.StudentHalaqat)
                .Include(h => h.HalaqaTeachers);

            if (teacherId.HasValue)
            {
                query = query.Where(h => h.HalaqaTeachers.Any(ht => ht.TeacherId == teacherId.Value));
            }

            var halaqat = await query.ToListAsync();

            return halaqat.Select(MapToDto);
        }

        public async Task<IEnumerable<HalaqaHierarchyDto>> GetHalaqatHierarchyAsync()
        {
            var halaqat = await _context.Halaqat
                .Include(h => h.HalaqaTeachers)
                    .ThenInclude(ht => ht.Teacher)
                .Include(h => h.StudentHalaqat)
                    .ThenInclude(sh => sh.Student)
                .Where(h => h.IsActive)
                .OrderBy(h => h.Name)
                .ToListAsync();

            return halaqat.Select(h => new HalaqaHierarchyDto
            {
                Id = h.Id,
                Name = h.Name,
                Location = h.Location,
                TimeSlot = h.TimeSlot,
                ActiveDays = h.ActiveDays,
                IsActive = h.IsActive,
                StudentCount = h.StudentHalaqat.Count(sh => sh.IsActive),
                TeacherCount = h.HalaqaTeachers.Count,
                Teachers = h.HalaqaTeachers.Select(ht => new TeacherInHalaqaDto
                {
                    Id = ht.Teacher.Id,
                    FullName = ht.Teacher.FullName,
                    PhoneNumber = ht.Teacher.PhoneNumber,
                    StudentCount = h.StudentHalaqat.Count(sh => sh.IsActive && sh.TeacherId == ht.TeacherId),
                    Students = h.StudentHalaqat
                        .Where(sh => sh.IsActive && sh.TeacherId == ht.TeacherId)
                        .Select(sh => new StudentInHalaqaDto
                        {
                            Id = sh.Student.Id,
                            FullName = $"{sh.Student.FirstName} {sh.Student.LastName}",
                            MemorizationDirection = sh.Student.MemorizationDirection.ToString(),
                            CurrentSurahNumber = sh.Student.CurrentSurahNumber,
                            CurrentSurahName = _quranService.GetSurahByNumber(sh.Student.CurrentSurahNumber)?.Name,
                            CurrentVerse = sh.Student.CurrentVerse,
                            JuzMemorized = _quranService.CalculateJuzMemorized(
                                sh.Student.MemorizationDirection,
                                sh.Student.CurrentSurahNumber,
                                sh.Student.CurrentVerse)
                        })
                        .OrderBy(s => s.FullName)
                        .ToList()
                })
                .OrderBy(t => t.FullName)
                .ToList()
            }).ToList();
        }

        public async Task<HalaqaDto?> GetHalaqaByIdAsync(int id)
        {
            var halaqa = await _context.Halaqat
                .Include(h => h.StudentHalaqat)
                .Include(h => h.HalaqaTeachers)
                .FirstOrDefaultAsync(h => h.Id == id);

            return halaqa == null ? null : MapToDto(halaqa);
        }

        public async Task<HalaqaDto> CreateHalaqaAsync(CreateHalaqaDto dto)
        {
            var halaqa = new Halaqa
            {
                Name = dto.Name,
                Location = dto.Location,
                TimeSlot = dto.TimeSlot,
                ActiveDays = dto.ActiveDays,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
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

