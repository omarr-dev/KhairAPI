using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Services.Implementations
{
    public class TeacherService : ITeacherService
    {
        private readonly AppDbContext _context;
        private readonly ITenantService _tenantService;

        public TeacherService(AppDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.HalaqaTeachers)
                .Include(t => t.StudentHalaqat)
                .AsSplitQuery()
                .ToListAsync();

            return teachers.Select(MapToDto);
        }

        public async Task<PaginatedResponse<TeacherDto>> GetTeachersPaginatedAsync(TeacherFilterDto filter)
        {
            var query = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.HalaqaTeachers)
                .Include(t => t.StudentHalaqat)
                .AsSplitQuery()
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                query = query.Where(t =>
                    t.FullName.ToLower().Contains(searchLower) ||
                    (t.User != null && t.User.PhoneNumber.ToLower().Contains(searchLower)) ||
                    (t.PhoneNumber != null && t.PhoneNumber.Contains(searchLower))
                );
            }

            // Apply halaqa filter
            if (filter.HalaqaId.HasValue && filter.HalaqaId.Value > 0)
            {
                query = query.Where(t => t.HalaqaTeachers.Any(ht => ht.HalaqaId == filter.HalaqaId.Value));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "studentscount" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.StudentHalaqat.Count(sh => sh.IsActive))
                    : query.OrderBy(t => t.StudentHalaqat.Count(sh => sh.IsActive)),
                "halaqatcount" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.HalaqaTeachers.Count)
                    : query.OrderBy(t => t.HalaqaTeachers.Count),
                "joindate" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.JoinDate)
                    : query.OrderBy(t => t.JoinDate),
                _ => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.FullName)
                    : query.OrderBy(t => t.FullName)
            };

            // Apply pagination
            var teachers = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResponse<TeacherDto>
            {
                Items = teachers.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<TeacherDto?> GetTeacherByIdAsync(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.HalaqaTeachers)
                .Include(t => t.StudentHalaqat)
                .AsSplitQuery()
                .OrderBy(t => t.Id)
                .FirstOrDefaultAsync(t => t.Id == id);

            return teacher == null ? null : MapToDto(teacher);
        }

        public async Task<IEnumerable<TeacherDto>> GetTeachersByHalaqaAsync(int halaqaId)
        {
            var teachers = await _context.HalaqaTeachers
                .Where(ht => ht.HalaqaId == halaqaId)
                .Include(ht => ht.Teacher)
                    .ThenInclude(t => t.User)
                .Include(ht => ht.Teacher.HalaqaTeachers)
                .Include(ht => ht.Teacher.StudentHalaqat)
                .AsSplitQuery()
                .Select(ht => ht.Teacher)
                .ToListAsync();

            return teachers.Select(MapToDto);
        }

        public async Task<TeacherDto> CreateTeacherAsync(CreateTeacherDto dto)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            // Format and validate phone number
            var formattedPhone = PhoneNumberValidator.Format(dto.PhoneNumber);
            if (formattedPhone == null)
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.InvalidPhoneNumber);
            }

            // Check if phone number already exists
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == formattedPhone))
            {
                throw new InvalidOperationException(AppConstants.ErrorMessages.PhoneNumberAlreadyExists);
            }

            // Create user
            var user = new User
            {
                PhoneNumber = formattedPhone,
                FullName = dto.FullName,
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create teacher record
            var teacher = new Teacher
            {
                UserId = user.Id,
                FullName = dto.FullName,
                PhoneNumber = formattedPhone,
                Qualification = dto.Qualification,
                JoinDate = DateTime.UtcNow,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            return new TeacherDto
            {
                Id = teacher.Id,
                UserId = teacher.UserId,
                FullName = teacher.FullName,
                PhoneNumber = teacher.PhoneNumber,
                Qualification = teacher.Qualification,
                JoinDate = teacher.JoinDate,
                HalaqatCount = 0,
                StudentsCount = 0
            };
        }

        public async Task<bool> UpdateTeacherAsync(int id, UpdateTeacherDto dto)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .OrderBy(t => t.Id)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return false;

            teacher.FullName = dto.FullName;
            teacher.PhoneNumber = dto.PhoneNumber;
            teacher.Qualification = dto.Qualification;

            if (teacher.User != null)
            {
                teacher.User.FullName = dto.FullName;
                teacher.User.PhoneNumber = dto.PhoneNumber;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTeacherAsync(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .OrderBy(t => t.Id)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return false;

            if (teacher.User != null)
            {
                teacher.User.IsActive = false;
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AssignTeacherToHalaqaAsync(int teacherId, int halaqaId, bool isPrimary = false)
        {
            if (_tenantService.CurrentAssociationId == null)
            {
                throw new InvalidOperationException("لم يتم تحديد الجمعية. يرجى تسجيل الدخول مرة أخرى.");
            }

            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
                throw new KeyNotFoundException(AppConstants.ErrorMessages.TeacherNotFound);

            var halaqa = await _context.Halaqat.FindAsync(halaqaId);
            if (halaqa == null)
                throw new KeyNotFoundException(AppConstants.ErrorMessages.HalaqaNotFound);

            var existingAssignment = await _context.HalaqaTeachers
                .OrderBy(ht => ht.HalaqaId).ThenBy(ht => ht.TeacherId)
                .FirstOrDefaultAsync(ht => ht.TeacherId == teacherId && ht.HalaqaId == halaqaId);

            if (existingAssignment != null)
                throw new InvalidOperationException(AppConstants.ErrorMessages.TeacherAlreadyAssigned);

            var halaqaTeacher = new HalaqaTeacher
            {
                TeacherId = teacherId,
                HalaqaId = halaqaId,
                AssignedDate = DateTime.UtcNow,
                IsPrimary = isPrimary,
                AssociationId = _tenantService.CurrentAssociationId.Value
            };

            _context.HalaqaTeachers.Add(halaqaTeacher);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<TeacherHalaqaDto>> GetTeacherHalaqatAsync(int teacherId)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
                throw new KeyNotFoundException(AppConstants.ErrorMessages.TeacherNotFound);

            return await _context.HalaqaTeachers
                .Where(ht => ht.TeacherId == teacherId)
                .Include(ht => ht.Halaqa)
                .Select(ht => new TeacherHalaqaDto
                {
                    HalaqaId = ht.HalaqaId,
                    HalaqaName = ht.Halaqa.Name,
                    AssignedDate = ht.AssignedDate,
                    IsPrimary = ht.IsPrimary
                })
                .ToListAsync();
        }

        public async Task<bool> RemoveTeacherFromHalaqaAsync(int teacherId, int halaqaId)
        {
            var assignment = await _context.HalaqaTeachers
                .OrderBy(ht => ht.HalaqaId).ThenBy(ht => ht.TeacherId)
                .FirstOrDefaultAsync(ht => ht.TeacherId == teacherId && ht.HalaqaId == halaqaId);

            if (assignment == null)
                return false;

            _context.HalaqaTeachers.Remove(assignment);
            await _context.SaveChangesAsync();

            return true;
        }

        private static TeacherDto MapToDto(Teacher teacher)
        {
            return new TeacherDto
            {
                Id = teacher.Id,
                UserId = teacher.UserId,
                FullName = teacher.FullName,
                PhoneNumber = teacher.PhoneNumber,
                Qualification = teacher.Qualification,
                JoinDate = teacher.JoinDate,
                HalaqatCount = teacher.HalaqaTeachers.Count,
                StudentsCount = teacher.StudentHalaqat.Count(sh => sh.IsActive)
            };
        }
    }
}

