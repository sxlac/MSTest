namespace Signify.Spirometry.Core.Models
{
    /// <summary>
    /// The accuracy/gradability of a Spirometry exam. Exams with low confidence cannot accurately diagnose COPD.
    /// </summary>
    public enum SessionGrade
    {
        A = 1,
        B = 2,
        C = 3,
        D = 4,
        E = 5,
        F = 6
    }
}
