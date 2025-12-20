using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TeacherOrSupervisor")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ICurrentUserService _currentUserService;

        public StudentsController(IStudentService studentService, ICurrentUserService currentUserService)
        {
            _studentService = studentService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents([FromQuery] string? search = null)
        {
            if (string.IsNullOrEmpty(search))
            {
                if (_currentUserService.IsTeacher)
                {
                    var teacherId = await _currentUserService.GetTeacherIdAsync();
                    if (!teacherId.HasValue)
                        return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                    var teacherStudents = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
                    return Ok(teacherStudents);
                }

                var allStudents = await _studentService.GetAllStudentsAsync();
                return Ok(allStudents);
            }
            else
            {
                var searchResults = await _studentService.SearchStudentsAsync(search);

                if (_currentUserService.IsTeacher)
                {
                    var teacherId = await _currentUserService.GetTeacherIdAsync();
                    if (!teacherId.HasValue)
                        return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                    var teacherStudents = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
                    var teacherStudentIds = teacherStudents.Select(s => s.Id).ToHashSet();
                    searchResults = searchResults.Where(s => teacherStudentIds.Contains(s.Id));
                }

                return Ok(searchResults);
            }
        }

        [HttpGet("paginated")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetStudentsPaginated([FromQuery] StudentFilterDto filter)
        {
            var result = await _studentService.GetStudentsPaginatedAsync(filter);
            return Ok(result);
        }

        [HttpGet("halaqa/{halaqaId}")]
        public async Task<IActionResult> GetStudentsByHalaqa(int halaqaId)
        {
            var students = await _studentService.GetStudentsByHalaqaAsync(halaqaId);
            return Ok(students);
        }

        [HttpGet("my-students")]
        public async Task<IActionResult> GetMyStudents()
        {
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (!teacherId.HasValue)
                return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            var students = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var teacherStudents = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
                if (!teacherStudents.Any(s => s.Id == id))
                    return Forbid();
            }

            return Ok(student);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetStudentDetails(int id)
        {
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var teacherStudents = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
                if (!teacherStudents.Any(s => s.Id == id))
                    return Forbid();
            }

            var studentDetails = await _studentService.GetStudentDetailsAsync(id);
            if (studentDetails == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(studentDetails);
        }

        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
        {
            var student = await _studentService.CreateStudentAsync(dto);
            return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
        {
            var student = await _studentService.UpdateStudentAsync(id, dto);
            if (student == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(student);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var result = await _studentService.DeleteStudentAsync(id);
            if (!result)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return NoContent();
        }

        [HttpPost("assign")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> AssignStudentToHalaqa([FromBody] AssignStudentDto dto)
        {
            var result = await _studentService.AssignStudentToHalaqaAsync(dto);
            if (!result)
                return BadRequest(new { message = AppConstants.ErrorMessages.CannotAssignStudent });

            return Ok(new { message = AppConstants.SuccessMessages.StudentAssigned });
        }

        [HttpGet("{id}/assignments")]
        public async Task<IActionResult> GetStudentAssignments(int id)
        {
            var assignments = await _studentService.GetStudentAssignmentsAsync(id);
            return Ok(assignments);
        }

        [HttpPut("assign/{studentId}/{halaqaId}/{teacherId}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> UpdateAssignment(int studentId, int halaqaId, int teacherId, [FromBody] UpdateAssignmentDto dto)
        {
            var result = await _studentService.UpdateAssignmentAsync(studentId, halaqaId, teacherId, dto);
            if (result == null)
                return NotFound(new { message = AppConstants.ErrorMessages.AssignmentNotFound });

            return Ok(result);
        }

        [HttpDelete("assign/{studentId}/{halaqaId}/{teacherId}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> DeleteAssignment(int studentId, int halaqaId, int teacherId)
        {
            var result = await _studentService.DeleteAssignmentAsync(studentId, halaqaId, teacherId);
            if (!result)
                return NotFound(new { message = AppConstants.ErrorMessages.AssignmentNotFound });

            return NoContent();
        }

        [HttpPut("{id}/memorization")]
        public async Task<IActionResult> UpdateMemorization(int id, [FromBody] UpdateMemorizationDto dto)
        {
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var teacherStudents = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
                if (!teacherStudents.Any(s => s.Id == id))
                    return Forbid();
            }

            var student = await _studentService.UpdateMemorizationAsync(id, dto);
            if (student == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(student);
        }
    }
}
