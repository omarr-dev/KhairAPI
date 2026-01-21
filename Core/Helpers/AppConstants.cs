namespace KhairAPI.Core.Helpers
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class AppConstants
    {
        public static class Roles
        {
            public const string Supervisor = "Supervisor";
            public const string Teacher = "Teacher";
            public const string HalaqaSupervisor = "HalaqaSupervisor";
            public const string SupervisorOrTeacher = "Supervisor,Teacher";
            /// <summary>
            /// Both full Supervisors and HalaqaSupervisors (limited scope)
            /// </summary>
            public const string AllSupervisors = "Supervisor,HalaqaSupervisor";
            /// <summary>
            /// All roles that can access the system
            /// </summary>
            public const string AllRoles = "Supervisor,HalaqaSupervisor,Teacher";
        }

        public static class Policies
        {
            /// <summary>
            /// Full supervisor access only (association-wide)
            /// </summary>
            public const string SupervisorOnly = "SupervisorOnly";
            /// <summary>
            /// Teachers and full supervisors
            /// </summary>
            public const string TeacherOrSupervisor = "TeacherOrSupervisor";
            /// <summary>
            /// HalaqaSupervisors and full Supervisors (for halaqa management)
            /// </summary>
            public const string HalaqaSupervisorOrHigher = "HalaqaSupervisorOrHigher";
            /// <summary>
            /// Any authenticated role
            /// </summary>
            public const string AnyRole = "AnyRole";
        }

        public static class ErrorMessages
        {
            // Authentication
            public const string InvalidCredentials = "رقم الجوال غير صحيح";
            public const string PhoneNumberAlreadyExists = "رقم الجوال مستخدم بالفعل";
            public const string InvalidPhoneNumber = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5";
            public const string InvalidToken = "رمز التحديث غير صالح";
            public const string Unauthorized = "غير مصرح لك بالوصول";
            public const string CannotIdentifyTeacher = "لا يمكن تحديد هوية المعلم";

            // Students
            public const string StudentNotFound = "الطالب غير موجود";
            public const string CannotAssignStudent = "لا يمكن تعيين الطالب في هذه الحلقة";
            public const string AssignmentNotFound = "التعيين غير موجود";
            public const string StudentNotInHalaqa = "الطالب غير مسجل في هذه الحلقة مع هذا المعلم";

            // Teachers
            public const string TeacherNotFound = "المعلم غير موجود";
            public const string TeacherAlreadyAssigned = "المعلم معين بالفعل في هذه الحلقة";

            // Halaqat
            public const string HalaqaNotFound = "الحلقة غير موجودة";
            public const string CannotDeleteHalaqaWithStudents = "لا يمكن حذف حلقة بها طلاب نشطين";
            public const string HalaqaNotActiveToday = "الحلقة غير نشطة اليوم";

            // Attendance
            public const string AttendanceNotFound = "سجل الحضور غير موجود";

            // Progress
            public const string ProgressNotFound = "السجل غير موجود أو ليس لديك صلاحية حذفه";
            public const string SurahNotFound = "السورة غير موجودة";
            public const string CannotRecordForOtherTeacher = "لا يمكنك تسجيل تقدم لمعلم آخر";

            // General
            public const string InternalServerError = "حدث خطأ داخلي في الخادم";
            public const string InvalidMonth = "الشهر يجب أن يكون بين 1 و 12";
            public const string InvalidYear = "السنة غير صالحة";
        }

        public static class SuccessMessages
        {
            public const string LogoutSuccess = "تم تسجيل الخروج بنجاح";
            public const string StudentAssigned = "تم تعيين الطالب في الحلقة بنجاح";
            public const string TeacherAssigned = "تم تعيين المعلم في الحلقة بنجاح";
            public const string TeacherRemovedFromHalaqa = "تم إزالة المعلم من الحلقة بنجاح";
            public const string AttendanceSaved = "تم حفظ سجل الحضور بنجاح";
            public const string AttendanceUpdated = "تم تحديث الحضور بنجاح";
            public const string TeacherAttendanceSaved = "تم حفظ حضور المعلمين بنجاح";
        }

        public static class ArabicDayNames
        {
            public static readonly string[] Days = { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };

            public static string GetDayName(DayOfWeek dayOfWeek) => Days[(int)dayOfWeek];
        }

        public static class ArabicMonthNames
        {
            public static readonly string[] Months = 
            {
                "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
                "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
            };

            public static string GetMonthName(int month) => Months[month - 1];
        }
    }
}

