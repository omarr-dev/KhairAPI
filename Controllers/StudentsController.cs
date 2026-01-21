using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppConstants.Policies.AnyRole)]
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
                
                // HalaqaSupervisors only see students in their assigned halaqas
                if (_currentUserService.IsHalaqaSupervisor)
                {
                    var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                    var students = await _studentService.GetStudentsByHalaqasAsync(supervisedHalaqaIds ?? new List<int>());
                    return Ok(students);
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
                else if (_currentUserService.IsHalaqaSupervisor)
                {
                    var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                    var halaqaStudents = await _studentService.GetStudentsByHalaqasAsync(supervisedHalaqaIds ?? new List<int>());
                    var halaqaStudentIds = halaqaStudents.Select(s => s.Id).ToHashSet();
                    searchResults = searchResults.Where(s => halaqaStudentIds.Contains(s.Id));
                }

                return Ok(searchResults);
            }
        }

        [HttpGet("paginated")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetStudentsPaginated([FromQuery] StudentFilterDto filter)
        {
            // HalaqaSupervisors can only filter by their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                filter.HalaqaIds = supervisedHalaqaIds;
            }
            
            var result = await _studentService.GetStudentsPaginatedAsync(filter);
            return Ok(result);
        }

        [HttpGet("halaqa/{halaqaId}")]
        public async Task<IActionResult> GetStudentsByHalaqa(int halaqaId)
        {
            // HalaqaSupervisors can only view students in their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(halaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
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

                var hasAccess = await _studentService.IsStudentAssignedToTeacherAsync(id, teacherId.Value);
                if (!hasAccess)
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

                var hasAccess = await _studentService.IsStudentAssignedToTeacherAsync(id, teacherId.Value);
                if (!hasAccess)
                    return Forbid();
            }

            var studentDetails = await _studentService.GetStudentDetailsAsync(id);
            if (studentDetails == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(studentDetails);
        }

        [HttpPost]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
        {
            // HalaqaSupervisors can only create students in their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor && dto.HalaqaId.HasValue)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(dto.HalaqaId.Value);
                if (!canAccess)
                    return Forbid();
            }
            
            var student = await _studentService.CreateStudentAsync(dto);
            return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
        {
            // TODO: Add halaqa access check for HalaqaSupervisors if needed
            var student = await _studentService.UpdateStudentAsync(id, dto);
            if (student == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(student);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var result = await _studentService.DeleteStudentAsync(id);
            if (!result)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return NoContent();
        }

        [HttpPost("assign")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> AssignStudentToHalaqa([FromBody] AssignStudentDto dto)
        {
            // HalaqaSupervisors can only assign to their halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(dto.HalaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
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
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> UpdateAssignment(int studentId, int halaqaId, int teacherId, [FromBody] UpdateAssignmentDto dto)
        {
            // HalaqaSupervisors can only update assignments in their halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(halaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
            var result = await _studentService.UpdateAssignmentAsync(studentId, halaqaId, teacherId, dto);
            if (result == null)
                return NotFound(new { message = AppConstants.ErrorMessages.AssignmentNotFound });

            return Ok(result);
        }

        [HttpDelete("assign/{studentId}/{halaqaId}/{teacherId}")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> DeleteAssignment(int studentId, int halaqaId, int teacherId)
        {
            // HalaqaSupervisors can only delete assignments in their halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(halaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
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

                var hasAccess = await _studentService.IsStudentAssignedToTeacherAsync(id, teacherId.Value);
                if (!hasAccess)
                    return Forbid();
            }

            var student = await _studentService.UpdateMemorizationAsync(id, dto);
            if (student == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(student);
        }

        #region Daily Targets

        [HttpGet("{id}/target")]
        public async Task<IActionResult> GetStudentTarget(int id, [FromServices] IStudentTargetService targetService)
        {
            // Check access
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var hasAccess = await _studentService.IsStudentAssignedToTeacherAsync(id, teacherId.Value);
                if (!hasAccess)
                    return Forbid();
            }

            var target = await targetService.GetTargetAsync(id);
            return Ok(target ?? new StudentTargetDto { StudentId = id });
        }

        [HttpPut("{id}/target")]
        public async Task<IActionResult> SetStudentTarget(int id, [FromBody] SetStudentTargetDto dto, [FromServices] IStudentTargetService targetService)
        {
            // Check access
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var hasAccess = await _studentService.IsStudentAssignedToTeacherAsync(id, teacherId.Value);
                if (!hasAccess)
                    return Forbid();
            }

            try
            {
                var target = await targetService.SetTargetAsync(id, dto);
                return Ok(target);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("targets/bulk")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> BulkSetTargets([FromBody] BulkSetTargetDto dto, [FromServices] IStudentTargetService targetService)
        {
            // HalaqaSupervisors can only bulk-set targets for their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor && dto.HalaqaId.HasValue)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(dto.HalaqaId.Value);
                if (!canAccess)
                    return Forbid();
            }
            
            try
            {
                var count = await targetService.BulkSetTargetAsync(dto);
                return Ok(new { message = $"تم تحديث الأهداف لـ {count} طالب", count });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("targets/bulk/my-students")]
        public async Task<IActionResult> BulkSetMyStudentsTargets([FromBody] SetStudentTargetDto dto, [FromServices] IStudentTargetService targetService)
        {
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (!teacherId.HasValue)
                return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            try
            {
                var bulkDto = new BulkSetTargetDto
                {
                    TeacherId = teacherId.Value,
                    MemorizationLinesTarget = dto.MemorizationLinesTarget,
                    RevisionPagesTarget = dto.RevisionPagesTarget,
                    ConsolidationPagesTarget = dto.ConsolidationPagesTarget
                };
                var count = await targetService.BulkSetTargetAsync(bulkDto);
                return Ok(new { message = $"تم تحديث الأهداف لـ {count} طالب", count });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gets achievement history for a student within a date range.
        /// For single-day queries, use the same date for startDate and endDate.
        /// Returns daily achievements, streak information, and summary statistics.
        /// </summary>
        [HttpGet("{id}/achievement-history")]
        public async Task<IActionResult> GetAchievementHistory(
            int id, 
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromServices] IStudentTargetService targetService)
        {
            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
            // Check access
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var hasAccess = await _studentService.IsStudentAssignedToTeacherAsync(id, teacherId.Value);
                if (!hasAccess)
                    return Forbid();
            }

            try
            {
                var history = await targetService.GetAchievementHistoryAsync(id, startDate, endDate);
                return Ok(history);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gets achievement history for all of the teacher's students in a single batch call.
        /// Optimized for the "My Students" page to show streaks and achievements efficiently.
        /// </summary>
        [HttpGet("my-students/achievements")]
        public async Task<IActionResult> GetMyStudentsAchievements(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromServices] IStudentTargetService targetService)
        {
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (!teacherId.HasValue)
                return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            try
            {
                // Get all student IDs for this teacher
                var students = await _studentService.GetStudentsByTeacherAsync(teacherId.Value);
                var studentIds = students.Select(s => s.Id).ToList();

                startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

                if (!studentIds.Any())
                {
                    return Ok(new Dictionary<int, AchievementHistoryDto>());
                }

                var achievements = await targetService.GetAchievementHistoryBatchAsync(studentIds, startDate, endDate);
                return Ok(achievements);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }
}

