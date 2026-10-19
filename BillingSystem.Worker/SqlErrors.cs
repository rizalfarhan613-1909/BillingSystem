using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Worker;

internal static class SqlErrors
{
    internal static bool IsUniqueViolation(DbUpdateException ex)
    {
        var current = ex.InnerException;
        while (current is not null)
        {
            if (current is SqlException sqlEx)
            {
                foreach (SqlError error in sqlEx.Errors)
                {
                    if (error.Number is 2601 or 2627)
                    {
                        return true;
                    }
                }
            }

            current = current.InnerException;
        }

        return false;
    }
}
