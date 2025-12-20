using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<PaginatedResponse<StudentDto>> GetStudentsPaginatedAsync(StudentFilterDto filter);
        Task<IEnumerable<StudentDto>> GetStudentsByHalaqaAsync(int halaqaId);
        Task<IEnumerable<StudentDto>> GetStudentsByTeacherAsync(int teacherId);
        Task<StudentDto?> GetStudentByIdAsync(int id);
        Task<StudentDetailDto?> GetStudentDetailsAsync(int id);
        Task<StudentDto> CreateStudentAsync(CreateStudentDto dto);
        Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto dto);
        Task<StudentDto?> UpdateMemorizationAsync(int id, UpdateMemorizationDto dto);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> AssignStudentToHalaqaAsync(AssignStudentDto dto);
        Task<IEnumerable<StudentDto>> SearchStudentsAsync(string searchTerm);
        Task<IEnumerable<StudentAssignmentDto>> GetStudentAssignmentsAsync(int studentId);
        Task<StudentAssignmentDto?> UpdateAssignmentAsync(int studentId, int halaqaId, int teacherId, UpdateAssignmentDto dto);
        Task<bool> DeleteAssignmentAsync(int studentId, int halaqaId, int teacherId);
    }
}

