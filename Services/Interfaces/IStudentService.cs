using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<PaginatedResponse<StudentDto>> GetStudentsPaginatedAsync(StudentFilterDto filter);
        Task<IEnumerable<StudentDto>> GetStudentsByHalaqaAsync(int halaqaId);
        /// <summary>
        /// Get students assigned to any of the specified halaqas (for HalaqaSupervisors)
        /// </summary>
        Task<IEnumerable<StudentDto>> GetStudentsByHalaqasAsync(List<int> halaqaIds);
        Task<IEnumerable<StudentDto>> GetStudentsByTeacherAsync(int teacherId);
        Task<StudentDto?> GetStudentByIdAsync(int id);
        /// <summary>
        /// Look up a student by their national ID number (scoped to the current association).
        /// Used to avoid creating duplicate student records.
        /// </summary>
        Task<StudentDto?> GetStudentByIdNumberAsync(string idNumber);
        Task<StudentDetailDto?> GetStudentDetailsAsync(int id);
        Task<StudentDto> CreateStudentAsync(CreateStudentDto dto);
        Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto dto);
        Task<StudentDto?> UpdateMemorizationAsync(int id, UpdateMemorizationDto dto);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> AssignStudentToHalaqaAsync(AssignStudentDto dto);
        Task<bool> IsStudentAssignedToTeacherAsync(int studentId, int teacherId);
        /// <summary>
        /// True if the teacher is assigned to teach the given halaqa.
        /// Used to authorize teachers adding/assigning students to their own halaqat.
        /// </summary>
        Task<bool> DoesTeacherTeachHalaqaAsync(int teacherId, int halaqaId);
        Task<IEnumerable<StudentDto>> SearchStudentsAsync(string searchTerm);
        Task<IEnumerable<StudentAssignmentDto>> GetStudentAssignmentsAsync(int studentId);
        Task<StudentAssignmentDto?> UpdateAssignmentAsync(int studentId, int halaqaId, int teacherId, UpdateAssignmentDto dto);
        Task<bool> DeleteAssignmentAsync(int studentId, int halaqaId, int teacherId);
    }
}

